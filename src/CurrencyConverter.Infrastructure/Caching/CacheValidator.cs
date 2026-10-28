using CurrencyConverter.Core.Models;
using CoreCacheValidator = CurrencyConverter.Core.Validation.CacheValidator;

namespace CurrencyConverter.Infrastructure.Caching;

/// <summary>
/// Адаптер валидатора кэша в слое инфраструктуры.
/// </summary>
public static class CacheValidator
{
    public static bool IsCacheSuitable(
        VerifiedCacheSnapshot? snapshot,
        DateOnly requestedDate,
        CurrencyCode sourceCurrency,
        CurrencyCode targetCurrency,
        DateOnly todayInChisinau,
        out string? reason)
    {
        return CoreCacheValidator.IsCacheSuitable(snapshot, requestedDate, sourceCurrency, targetCurrency, todayInChisinau, out reason);
    }
}
