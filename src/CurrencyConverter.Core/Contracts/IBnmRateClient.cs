using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Contracts;

/// <summary>
/// Клиент получения официальных курсов валют Национального банка Молдовы (BNM).
/// </summary>
public interface IBnmRateClient
{
    /// <summary>
    /// Запрашивает курсы валют на указанную дату.
    /// Возвращает null, если на эту дату курсы не публиковались (пустой набор).
    /// Выбрасывает исключения доменного уровня при сетевых, HTTP или форматных ошибках.
    /// </summary>
    Task<RateSet?> GetRatesForDateAsync(DateOnly date, CancellationToken cancellationToken = default);
}
