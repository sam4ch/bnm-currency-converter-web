using System.Globalization;
using System.Text.Json;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Infrastructure.Caching;

/// <summary>
/// Реализация файлового JSON-кэша курсов валют с атомарной записью.
/// </summary>
public sealed class JsonCacheStore : ICacheStore
{
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
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return null;
            }

            string json;
            try
            {
                json = await File.ReadAllTextAsync(_cacheFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ошибка чтения повреждённого файла не должна ронять приложение
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
            catch (Exception)
            {
                // Невалидный JSON не роняет приложение
                return null;
            }

            if (dto?.RateSet == null || dto.RateSet.Entries == null || dto.RateSet.Entries.Count == 0)
            {
                return null;
            }

            if (!DateOnly.TryParseExact(dto.RateSet.PublicationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly pubDate))
            {
                return null;
            }

            var entries = new List<CurrencyRateEntry>();
            var seenCodes = new HashSet<CurrencyCode>();

            foreach (var eDto in dto.RateSet.Entries)
            {
                if (!CurrencyCode.TryParse(eDto.Code, out var code))
                {
                    continue;
                }

                if (eDto.Nominal <= 0 || eDto.RateValue <= 0)
                {
                    return null; // Повреждённые данные записи в кэше
                }

                if (!seenCodes.Add(code))
                {
                    return null; // Дубликат в кэше
                }

                DateOnly entryPubDate = pubDate;
                if (!string.IsNullOrWhiteSpace(eDto.PublicationDate) &&
                    DateOnly.TryParseExact(eDto.PublicationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly epd))
                {
                    entryPubDate = epd;
                }

                entries.Add(new CurrencyRateEntry(code, eDto.Nominal, eDto.RateValue, entryPubDate));
            }

            if (entries.Count == 0)
            {
                return null;
            }

            // Убеждаемся, что MDL присутствует
            if (!entries.Any(e => e.Code == CurrencyCode.Mdl))
            {
                entries.Add(new CurrencyRateEntry(CurrencyCode.Mdl, 1m, 1m, pubDate));
            }

            var rateSet = new RateSet(pubDate, entries, dto.RateSet.SourceUrl, dto.RateSet.RetrievedAtUtc);
            return new VerifiedCacheSnapshot(
                dto.SnapshotDateUtc,
                rateSet,
                dto.Version ?? VerifiedCacheSnapshot.CurrentVersion,
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

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? tempFilePath = null;
        try
        {
            string? dir = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var dto = new CacheDocumentDto
            {
                Version = VerifiedCacheSnapshot.CurrentVersion,
                CacheSourceLabel = "BNM Official XML Cache",
                SnapshotDateUtc = rateSet.RetrievedAtUtc,
                RateSet = new RateSetDto
                {
                    PublicationDate = rateSet.PublicationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    SourceUrl = rateSet.SourceUrl,
                    RetrievedAtUtc = rateSet.RetrievedAtUtc,
                    Entries = rateSet.Entries.Select(e => new CurrencyRateEntryDto
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

    private sealed class CacheDocumentDto
    {
        public string Version { get; set; } = "1.0";
        public string CacheSourceLabel { get; set; } = "BNM Official XML Cache";
        public DateTimeOffset SnapshotDateUtc { get; set; }
        public RateSetDto? RateSet { get; set; }
    }

    private sealed class RateSetDto
    {
        public string PublicationDate { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public DateTimeOffset RetrievedAtUtc { get; set; }
        public List<CurrencyRateEntryDto> Entries { get; set; } = [];
    }

    private sealed class CurrencyRateEntryDto
    {
        public string Code { get; set; } = string.Empty;
        public decimal Nominal { get; set; }
        public decimal RateValue { get; set; }
        public string PublicationDate { get; set; } = string.Empty;
    }
}
