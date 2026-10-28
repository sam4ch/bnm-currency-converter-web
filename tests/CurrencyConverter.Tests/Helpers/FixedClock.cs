using CurrencyConverter.Core.Contracts;

namespace CurrencyConverter.Tests.Helpers;

/// <summary>
/// Фиксированные часы для детерминированного тестирования времени и часового пояса Кишинёва.
/// </summary>
public sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; set; }
    public DateOnly TodayInChisinau { get; set; }
    public DateTimeOffset NowInChisinau { get; set; }

    public FixedClock(DateOnly? today = null, DateTimeOffset? utcNow = null)
    {
        TodayInChisinau = today ?? new DateOnly(2026, 10, 28);
        UtcNow = utcNow ?? new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
        NowInChisinau = UtcNow.ToOffset(TimeSpan.FromHours(2));
    }
}
