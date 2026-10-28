namespace CurrencyConverter.Core.Models;

/// <summary>
/// Представляет итоговый результат операции конвертации.
/// </summary>
public sealed class ConversionResult
{
    public ConversionStatus Status { get; }
    public decimal SourceAmount { get; }
    public CurrencyCode SourceCurrency { get; }
    public CurrencyCode TargetCurrency { get; }
    public decimal? ConvertedAmount { get; }
    public DateOnly RequestedDate { get; }
    public DateOnly? RateDateUsed { get; }
    public string? SourceReference { get; }
    public DateTimeOffset? RetrievalTimestampUtc { get; }
    public string? WarningText { get; }
    public string? UserMessage { get; }
    public bool UsedCacheFallback { get; }
    public bool SameCurrencyPath { get; }
    public VerifiedCacheSnapshot? AvailableCacheSnapshot { get; }

    private ConversionResult(
        ConversionStatus status,
        decimal sourceAmount,
        CurrencyCode sourceCurrency,
        CurrencyCode targetCurrency,
        decimal? convertedAmount,
        DateOnly requestedDate,
        DateOnly? rateDateUsed,
        string? sourceReference,
        DateTimeOffset? retrievalTimestampUtc,
        string? warningText,
        string? userMessage,
        bool usedCacheFallback,
        bool sameCurrencyPath,
        VerifiedCacheSnapshot? availableCacheSnapshot)
    {
        Status = status;
        SourceAmount = sourceAmount;
        SourceCurrency = sourceCurrency;
        TargetCurrency = targetCurrency;
        ConvertedAmount = convertedAmount;
        RequestedDate = requestedDate;
        RateDateUsed = rateDateUsed;
        SourceReference = sourceReference;
        RetrievalTimestampUtc = retrievalTimestampUtc;
        WarningText = warningText;
        UserMessage = userMessage;
        UsedCacheFallback = usedCacheFallback;
        SameCurrencyPath = sameCurrencyPath;
        AvailableCacheSnapshot = availableCacheSnapshot;
    }

    public static ConversionResult ForSameCurrency(
        decimal amount,
        CurrencyCode currency,
        DateOnly requestedDate)
    {
        return new ConversionResult(
            status: ConversionStatus.Success,
            sourceAmount: amount,
            sourceCurrency: currency,
            targetCurrency: currency,
            convertedAmount: amount,
            requestedDate: requestedDate,
            rateDateUsed: null,
            sourceReference: null,
            retrievalTimestampUtc: null,
            warningText: null,
            userMessage: null,
            usedCacheFallback: false,
            sameCurrencyPath: true,
            availableCacheSnapshot: null);
    }

    public static ConversionResult ForSuccess(
        decimal sourceAmount,
        CurrencyCode sourceCurrency,
        CurrencyCode targetCurrency,
        decimal convertedAmount,
        DateOnly requestedDate,
        DateOnly rateDateUsed,
        string sourceReference,
        DateTimeOffset retrievalTimestampUtc,
        string? warningText = null,
        bool usedCacheFallback = false)
    {
        return new ConversionResult(
            status: ConversionStatus.Success,
            sourceAmount: sourceAmount,
            sourceCurrency: sourceCurrency,
            targetCurrency: targetCurrency,
            convertedAmount: convertedAmount,
            requestedDate: requestedDate,
            rateDateUsed: rateDateUsed,
            sourceReference: sourceReference,
            retrievalTimestampUtc: retrievalTimestampUtc,
            warningText: warningText,
            userMessage: null,
            usedCacheFallback: usedCacheFallback,
            sameCurrencyPath: false,
            availableCacheSnapshot: null);
    }

    public static ConversionResult ForCacheConfirmationRequired(
        decimal sourceAmount,
        CurrencyCode sourceCurrency,
        CurrencyCode targetCurrency,
        DateOnly requestedDate,
        VerifiedCacheSnapshot cacheSnapshot,
        string userMessage)
    {
        return new ConversionResult(
            status: ConversionStatus.CacheConfirmationRequired,
            sourceAmount: sourceAmount,
            sourceCurrency: sourceCurrency,
            targetCurrency: targetCurrency,
            convertedAmount: null,
            requestedDate: requestedDate,
            rateDateUsed: cacheSnapshot.RateSet.PublicationDate,
            sourceReference: cacheSnapshot.RateSet.SourceUrl,
            retrievalTimestampUtc: cacheSnapshot.SnapshotDateUtc,
            warningText: null,
            userMessage: userMessage,
            usedCacheFallback: false,
            sameCurrencyPath: false,
            availableCacheSnapshot: cacheSnapshot);
    }

    public static ConversionResult ForSourceUnavailable(
        decimal sourceAmount,
        CurrencyCode sourceCurrency,
        CurrencyCode targetCurrency,
        DateOnly requestedDate,
        string userMessage)
    {
        return new ConversionResult(
            status: ConversionStatus.SourceUnavailable,
            sourceAmount: sourceAmount,
            sourceCurrency: sourceCurrency,
            targetCurrency: targetCurrency,
            convertedAmount: null,
            requestedDate: requestedDate,
            rateDateUsed: null,
            sourceReference: null,
            retrievalTimestampUtc: null,
            warningText: null,
            userMessage: userMessage,
            usedCacheFallback: false,
            sameCurrencyPath: false,
            availableCacheSnapshot: null);
    }

    public static ConversionResult ForValidationError(
        decimal sourceAmount,
        CurrencyCode sourceCurrency,
        CurrencyCode targetCurrency,
        DateOnly requestedDate,
        string userMessage)
    {
        return new ConversionResult(
            status: ConversionStatus.ValidationError,
            sourceAmount: sourceAmount,
            sourceCurrency: sourceCurrency,
            targetCurrency: targetCurrency,
            convertedAmount: null,
            requestedDate: requestedDate,
            rateDateUsed: null,
            sourceReference: null,
            retrievalTimestampUtc: null,
            warningText: null,
            userMessage: userMessage,
            usedCacheFallback: false,
            sameCurrencyPath: false,
            availableCacheSnapshot: null);
    }
}
