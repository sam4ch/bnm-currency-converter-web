namespace CurrencyConverter.Core.Models;

/// <summary>
/// Представляет контекст даты для поиска и отображения курса валюты.
/// </summary>
public sealed class RateDate
{
    public DateOnly RequestedDate { get; }
    public DateOnly? ActualRateDate { get; }
    public DateTimeOffset? RetrievalTimestampUtc { get; }

    public RateDate(DateOnly requestedDate, DateOnly? actualRateDate = null, DateTimeOffset? retrievalTimestampUtc = null)
    {
        RequestedDate = requestedDate;
        ActualRateDate = actualRateDate;
        RetrievalTimestampUtc = retrievalTimestampUtc;
    }
}
