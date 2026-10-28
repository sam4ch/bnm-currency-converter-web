namespace CurrencyConverter.Core.Models;

/// <summary>
/// Состояние контекста поиска курса при переборе предыдущих дат.
/// </summary>
public sealed class RateLookupContext
{
    public const int DefaultMaxSearchDays = 31;

    public DateOnly RequestedDate { get; }
    public DateOnly CurrentCandidateDate { get; set; }
    public int MaxSearchDays { get; }
    public int CurrentDayOffset { get; set; }

    public RateLookupContext(DateOnly requestedDate, int maxSearchDays = DefaultMaxSearchDays)
    {
        RequestedDate = requestedDate;
        CurrentCandidateDate = requestedDate;
        MaxSearchDays = maxSearchDays;
        CurrentDayOffset = 0;
    }
}
