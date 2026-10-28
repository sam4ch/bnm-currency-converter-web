using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Contracts;

/// <summary>
/// Хранилище постоянного проверенного кэша курсов валют на диске.
/// </summary>
public interface ICacheStore
{
    /// <summary>
    /// Считывает последний проверенный снимок кэша.
    /// Возвращает null, если кэш отсутствует или повреждён.
    /// </summary>
    Task<VerifiedCacheSnapshot?> GetLatestCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Атомарно сохраняет проверенный набор курсов в кэш.
    /// </summary>
    Task SaveCacheAsync(RateSet rateSet, CancellationToken cancellationToken = default);
}
