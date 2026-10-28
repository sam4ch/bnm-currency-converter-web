namespace CurrencyConverter.Core.Contracts;

/// <summary>
/// Абстракция часов для изоляции времени в юнит-тестах и поддержки часового пояса Кишинёва.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Текущее время в формате UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Текущая календарная дата по официальному времени Кишинёва.
    /// </summary>
    DateOnly TodayInChisinau { get; }

    /// <summary>
    /// Текущее время со смещением по часовому поясу Кишинёва.
    /// </summary>
    DateTimeOffset NowInChisinau { get; }
}
