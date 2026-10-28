using System.Globalization;
using System.Text.Json;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Infrastructure.Caching;

/// <summary>
/// Реализация файлового JSON-кэша курсов валют с атомарной записью и строгой валидацией снимков.
/// </summary>
public sealed class JsonCacheStore : ICacheStore
{
    public const string ExpectedSchemaVersion = "1.0";

    private readonly string _cacheFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonCacheStore(string cacheFilePath)
    {
        if (string.IsNullOrWhiteSpace(cacheFilePath))
        {
            throw new ArgumentException("Путь к файлу кэша не может быть пустым.", nameof(cacheFilePath));
        }

        _cacheFilePath = Path.GetFullPath(cacheFilePath);
    }

    public async Task<VerifiedCacheSnapshot?> GetLatestCacheAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(_cacheFilePath))
            {
                return null;
            }

            string json;
            try
            {
                json = await File.ReadAllTextAsync(_cacheFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // Ошибка чтения повреждённого файла возвращает cache miss
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            CacheDocumentDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<CacheDocumentDto>(json, JsonOptions);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // Невалидный JSON или повреждённая структура возвращает cache miss
                return null;
            }

            // 1. Строгая проверка версии схемы (отсутствующая версия не считается 1.0)
            if (dto == null || dto.Version != ExpectedSchemaVersion)
            {
                return null;
            }

            // 2. Проверка метки времени создания снимка
            if (dto.SnapshotDateUtc == default)
            {
                return null;
            }

            if (dto.RateSet == null || string.IsNullOrWhiteSpace(dto.RateSet.SourceUrl) || dto.RateSet.RetrievedAtUtc == default)
            {
                return null;
            }

            if (!DateOnly.TryParseExact(dto.RateSet.PublicationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly pubDate) || pubDate == default)
            {
                return null;
            }

            if (dto.RateSet.Entries == null || dto.RateSet.Entries.Count == 0)
            {
                return null;
            }

            var entries = new List<CurrencyRateEntry>();
            var seenCodes = new HashSet<CurrencyCode>();
            bool hasValidMdl = false;

            foreach (var eDto in dto.RateSet.Entries)
            {
                // Если элемент списка null (например Entries:[null]) — документ повреждён
                if (eDto == null)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(eDto.Code) || !CurrencyCode.TryParse(eDto.Code, out var code))
                {
                    return null; // Неизвестный код валюты в кэше
                }

                if (eDto.Nominal <= 0 || eDto.RateValue <= 0)
                {
                    return null; // Повреждённые числовые данные в кэше
                }

                if (!seenCodes.Add(code))
                {
                    return null; // Дубликат в кэше
                }

                // Единообразие дат записей: каждая запись должна иметь явную дату, совпадающую с датой корня
                if (string.IsNullOrWhiteSpace(eDto.PublicationDate) ||
                    !DateOnly.TryParseExact(eDto.PublicationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly entryPubDate) ||
                    entryPubDate != pubDate)
                {
                    return null; // Запрещено молча исправлять неверную дату записи
                }

                if (code == CurrencyCode.Mdl)
                {
                    if (eDto.Nominal == 1m && eDto.RateValue == 1m)
                    {
                        hasValidMdl = true;
                    }
                    else
                    {
                        return null; // MDL должен иметь строго курс 1 и номинал 1
                    }
                }

                entries.Add(new CurrencyRateEntry(code, eDto.Nominal, eDto.RateValue, entryPubDate));
            }

            // Базовый MDL=1 обязан присутствовать изначально в сохранённом документе
            if (!hasValidMdl)
            {
                return null;
            }

            var rateSet = new RateSet(pubDate, entries, dto.RateSet.SourceUrl, dto.RateSet.RetrievedAtUtc);
            return new VerifiedCacheSnapshot(
                dto.SnapshotDateUtc,
                rateSet,
                dto.Version,
                dto.CacheSourceLabel ?? "BNM Official XML Cache");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveCacheAsync(RateSet rateSet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rateSet);
        cancellationToken.ThrowIfCancellationRequested();

        // Валидация RateSet перед записью, чтобы повреждённые данные не вытеснили хороший кэш
        ValidateRateSetForCache(rateSet);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? tempFilePath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? dir = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var dto = new CacheDocumentDto
            {
                Version = ExpectedSchemaVersion,
                CacheSourceLabel = "BNM Official XML Cache",
                SnapshotDateUtc = rateSet.RetrievedAtUtc,
                RateSet = new RateSetDto
                {
                    PublicationDate = rateSet.PublicationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    SourceUrl = rateSet.SourceUrl,
                    RetrievedAtUtc = rateSet.RetrievedAtUtc,
                    Entries = rateSet.Entries.Select(e => (CurrencyRateEntryDto?)new CurrencyRateEntryDto
                    {
                        Code = e.Code.Value,
                        Nominal = e.Nominal,
                        RateValue = e.RateValue,
                        PublicationDate = e.PublicationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    }).ToList()
                }
            };

            string json = JsonSerializer.Serialize(dto, JsonOptions);

            // Атомарная запись через временный файл в том же каталоге
            string dirToUse = string.IsNullOrEmpty(dir) ? "." : dir;
            tempFilePath = Path.Combine(dirToUse, $"{Guid.NewGuid():N}.tmp");

            await File.WriteAllTextAsync(tempFilePath, json, cancellationToken).ConfigureAwait(false);

            // Атомарная замена целевого файла
            File.Move(tempFilePath, _cacheFilePath, overwrite: true);
            tempFilePath = null; // Успешно перемещён
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CacheWriteException($"Не удалось сохранить файл кэша по пути '{_cacheFilePath}': {ex.Message}", ex);
        }
        finally
        {
            if (tempFilePath != null && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    // Игнорируем ошибку очистки временного файла
                }
            }

            _lock.Release();
        }
    }

    private static void ValidateRateSetForCache(RateSet rateSet)
    {
        if (rateSet.PublicationDate == default)
        {
            throw new CacheException("Нельзя сохранить в кэш набор с пустой датой публикации.");
        }

        if (string.IsNullOrWhiteSpace(rateSet.SourceUrl))
        {
            throw new CacheException("Нельзя сохранить в кэш набор с пустым SourceUrl.");
        }

        if (rateSet.RetrievedAtUtc == default)
        {
            throw new CacheException("Нельзя сохранить в кэш набор без метки времени получения.");
        }

        if (rateSet.Entries == null || rateSet.Entries.Count == 0)
        {
            throw new CacheException("Нельзя сохранить в кэш пустой набор записей курсов.");
        }

        var mdlEntry = rateSet.GetEntry(CurrencyCode.Mdl);
        if (mdlEntry == null || mdlEntry.Nominal != 1m || mdlEntry.RateValue != 1m)
        {
            throw new CacheException("Набор курсов для сохранения в кэш должен содержать валидную запись MDL с курсом и номиналом 1.");
        }

        foreach (var entry in rateSet.Entries)
        {
            if (entry.PublicationDate != rateSet.PublicationDate)
            {
                throw new CacheException($"Дата публикации записи '{entry.Code}' ({entry.PublicationDate:dd.MM.yyyy}) не совпадает с датой набора ({rateSet.PublicationDate:dd.MM.yyyy}).");
            }

            if (entry.Nominal <= 0 || entry.RateValue <= 0)
            {
                throw new CacheException($"Запись '{entry.Code}' содержит неположительные числовые значения.");
            }
        }
    }

    private sealed class CacheDocumentDto
    {
        // По умолчанию null, чтобы отсутствие поля version в JSON не подменялось на "1.0"
        public string? Version { get; set; }
        public string? CacheSourceLabel { get; set; }
        public DateTimeOffset SnapshotDateUtc { get; set; }
        public RateSetDto? RateSet { get; set; }
    }

    private sealed class RateSetDto
    {
        public string? PublicationDate { get; set; }
        public string? SourceUrl { get; set; }
        public DateTimeOffset RetrievedAtUtc { get; set; }
        public List<CurrencyRateEntryDto?>? Entries { get; set; }
    }

    private sealed class CurrencyRateEntryDto
    {
        public string? Code { get; set; }
        public decimal Nominal { get; set; }
        public decimal RateValue { get; set; }
        public string? PublicationDate { get; set; }
    }
}
