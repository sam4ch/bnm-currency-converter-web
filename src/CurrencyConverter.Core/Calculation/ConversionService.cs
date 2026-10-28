using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Core.Validation;

namespace CurrencyConverter.Core.Calculation;

/// <summary>
/// Главный сервис оркестрации бизнес-логики конвертации валют.
/// </summary>
public sealed class ConversionService : IConversionService
{
    private readonly IRateLookupService _rateLookupService;
    private readonly ICacheStore _cacheStore;
    private readonly ICurrencyCalculator _calculator;
    private readonly IClock _clock;

    public ConversionService(
        IRateLookupService rateLookupService,
        ICacheStore cacheStore,
        ICurrencyCalculator calculator,
        IClock clock)
    {
        _rateLookupService = rateLookupService ?? throw new ArgumentNullException(nameof(rateLookupService));
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ConversionResult> ConvertAsync(ConversionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Одинаковые валюты: возврат суммы без обращения к источнику и без кэша
        if (request.IsSameCurrency)
        {
            return ConversionResult.ForSameCurrency(
                request.Amount.Value,
                request.SourceCurrency,
                request.RequestedDate);
        }

        DateOnly today = _clock.TodayInChisinau;

        // 2. Если пользователь уже явно подтвердил использование сохранённого кэша
        if (request.CacheFallbackApproved)
        {
            // При подтверждении НЕ делаем повторный сетевой запрос
            VerifiedCacheSnapshot? cachedSnapshot = await _cacheStore.GetLatestCacheAsync(cancellationToken).ConfigureAwait(false);

            if (!CacheValidator.IsCacheSuitable(cachedSnapshot, request.RequestedDate, request.SourceCurrency, request.TargetCurrency, today, out string? unsuitabilityReason))
            {
                return ConversionResult.ForSourceUnavailable(
                    request.Amount.Value,
                    request.SourceCurrency,
                    request.TargetCurrency,
                    request.RequestedDate,
                    $"Сохранённый кэш не может быть использован: {unsuitabilityReason}");
            }

            var sourceRate = cachedSnapshot!.RateSet.GetEntry(request.SourceCurrency)!;
            var targetRate = cachedSnapshot.RateSet.GetEntry(request.TargetCurrency)!;

            decimal converted = _calculator.Convert(request.Amount.Value, sourceRate, targetRate);

            string warning = $"Использован сохранённый кэш от {cachedSnapshot.RateSet.PublicationDate:dd.MM.yyyy} из-за недоступности источника.";
            if (cachedSnapshot.RateSet.PublicationDate < request.RequestedDate)
            {
                warning += $" Обратите внимание: курс датирован {cachedSnapshot.RateSet.PublicationDate:dd.MM.yyyy}, что ранее запрошенной даты {request.RequestedDate:dd.MM.yyyy}.";
            }

            return ConversionResult.ForSuccess(
                sourceAmount: request.Amount.Value,
                sourceCurrency: request.SourceCurrency,
                targetCurrency: request.TargetCurrency,
                convertedAmount: converted,
                requestedDate: request.RequestedDate,
                rateDateUsed: cachedSnapshot.RateSet.PublicationDate,
                sourceReference: cachedSnapshot.RateSet.SourceUrl,
                retrievalTimestampUtc: cachedSnapshot.SnapshotDateUtc,
                warningText: warning,
                usedCacheFallback: true);
        }

        // 3. Штатный путь: запрос курса через RateLookupService
        RateSet rates;
        try
        {
            rates = await _rateLookupService.GetRatesAsync(request.RequestedDate, cancellationToken).ConfigureAwait(false);
        }
        catch (RateSourceException ex)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Ошибка источника (сеть, HTTP, таймаут, неверный XML или исчерпан поиск)
            // Проверяем наличие подходящего кэша для запроса подтверждения
            VerifiedCacheSnapshot? cachedSnapshot = null;
            try
            {
                cachedSnapshot = await _cacheStore.GetLatestCacheAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Ошибка чтения кэша не должна скрывать исходную проблему источника
            }

            if (CacheValidator.IsCacheSuitable(cachedSnapshot, request.RequestedDate, request.SourceCurrency, request.TargetCurrency, today, out _))
            {
                // Возвращаем состояние ожидания подтверждения кэша — без явного согласия НЕ рассчитываем молча
                string confirmationPrompt = $"Официальный источник курсов недоступен ({ex.Message}). Доступен сохранённый кэш от {cachedSnapshot!.RateSet.PublicationDate:dd.MM.yyyy}. Желаете использовать сохранённый курс?";
                return ConversionResult.ForCacheConfirmationRequired(
                    request.Amount.Value,
                    request.SourceCurrency,
                    request.TargetCurrency,
                    request.RequestedDate,
                    cachedSnapshot,
                    confirmationPrompt);
            }

            // Подходящий кэш отсутствует
            return ConversionResult.ForSourceUnavailable(
                request.Amount.Value,
                request.SourceCurrency,
                request.TargetCurrency,
                request.RequestedDate,
                $"Официальный источник курсов недоступен ({ex.Message}), и подходящий сохранённый кэш на запрошенную дату отсутствует.");
        }

        // 4. Проверка наличия запрошенных валют в наборе ДО сохранения кэша,
        // чтобы непригодный для расчёта ответ не вытеснил рабочий кэш
        var sourceEntry = rates.GetEntry(request.SourceCurrency);
        var targetEntry = rates.GetEntry(request.TargetCurrency);

        if (sourceEntry == null || targetEntry == null)
        {
            return ConversionResult.ForSourceUnavailable(
                request.Amount.Value,
                request.SourceCurrency,
                request.TargetCurrency,
                request.RequestedDate,
                "В полученном наборе курсов отсутствуют данные для запрошенных валют.");
        }

        // 5. Попытка сохранить полученный успешный и пригодный ответ в постоянный кэш
        string? cacheWarning = null;
        try
        {
            await _cacheStore.SaveCacheAsync(rates, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Ошибка записи кэша сохраняет успешный результат и выводит отдельное предупреждение
            cacheWarning = $"Курс успешно получен, но не удалось сохранить локальный кэш: {ex.Message}";
        }

        // 6. Расчёт сконвертированной суммы
        decimal convertedAmount = _calculator.Convert(request.Amount.Value, sourceEntry, targetEntry);

        // 7. Формирование предупреждения при несовпадении даты курса с запрошенной
        string? dateWarning = null;
        if (rates.PublicationDate < request.RequestedDate)
        {
            dateWarning = $"Курс на запрошенную дату ({request.RequestedDate:dd.MM.yyyy}) не публиковался. Использован последний доступный курс от {rates.PublicationDate:dd.MM.yyyy}.";
        }

        string? finalWarning = (dateWarning, cacheWarning) switch
        {
            (not null, not null) => $"{dateWarning} {cacheWarning}",
            (not null, null) => dateWarning,
            (null, not null) => cacheWarning,
            _ => null
        };

        return ConversionResult.ForSuccess(
            sourceAmount: request.Amount.Value,
            sourceCurrency: request.SourceCurrency,
            targetCurrency: request.TargetCurrency,
            convertedAmount: convertedAmount,
            requestedDate: request.RequestedDate,
            rateDateUsed: rates.PublicationDate,
            sourceReference: rates.SourceUrl,
            retrievalTimestampUtc: rates.RetrievedAtUtc,
            warningText: finalWarning,
            usedCacheFallback: false);
    }
}
