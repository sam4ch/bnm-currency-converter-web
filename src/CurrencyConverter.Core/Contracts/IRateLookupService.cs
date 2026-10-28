using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Contracts;

/// <summary>
/// Сервис поиска курса валют с поддержкой перехода к предыдущим датам (до 31 дня назад).
/// </summary>
public interface IRateLookupService
{
    /// <summary>
    /// Находит актуальный опубликованный набор курсов на запрошенную дату либо на ближайшую предыдущую дату в пределах 31 дня.
    /// Прекращает поиск немедленно при сетевой, HTTP или форматной ошибке источника.
    /// </summary>
    Task<RateSet> GetRatesAsync(DateOnly requestedDate, CancellationToken cancellationToken = default);
}
