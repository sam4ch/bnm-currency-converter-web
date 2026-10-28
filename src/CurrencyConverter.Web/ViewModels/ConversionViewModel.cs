using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Web.ViewModels;

/// <summary>
/// Модель представления для страницы конвертера валют.
/// </summary>
public sealed class ConversionViewModel
{
    public string? Amount { get; set; }
    public string SourceCurrency { get; set; } = "EUR";
    public string TargetCurrency { get; set; } = "MDL";
    public string RateDate { get; set; } = string.Empty;

    public bool HasResult { get; set; }
    public ConversionResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WarningMessage { get; set; }

    public bool ShowCacheConfirmation { get; set; }
    public string? CacheConfirmationMessage { get; set; }
    public DateOnly? CacheDate { get; set; }
    public DateTimeOffset? CacheTimestamp { get; set; }

    public bool IsSameCurrency => string.Equals(SourceCurrency, TargetCurrency, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> SupportedCurrencies { get; } = ["MDL", "EUR", "USD", "RON", "GBP"];
}
