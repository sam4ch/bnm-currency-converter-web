namespace CurrencyConverter.Core.Models;

/// <summary>
/// Представляет валидный запрос на конвертацию валюты.
/// </summary>
public sealed class ConversionRequest
{
    public Amount Amount { get; }
    public CurrencyCode SourceCurrency { get; }
    public CurrencyCode TargetCurrency { get; }
    public DateOnly RequestedDate { get; }
    public bool CacheFallbackApproved { get; }

    public ConversionRequest(
        Amount amount,
        CurrencyCode sourceCurrency,
        CurrencyCode targetCurrency,
        DateOnly requestedDate,
        bool cacheFallbackApproved = false)
    {
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        SourceCurrency = sourceCurrency;
        TargetCurrency = targetCurrency;
        RequestedDate = requestedDate;
        CacheFallbackApproved = cacheFallbackApproved;
    }

    public bool IsSameCurrency => SourceCurrency == TargetCurrency;
}
