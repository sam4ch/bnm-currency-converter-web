namespace CurrencyConverter.Core.Models;

/// <summary>
/// Представляет валидный набор курсов валют на определённую дату публикации.
/// </summary>
public sealed class RateSet
{
    private readonly Dictionary<CurrencyCode, CurrencyRateEntry> _entriesByCode;

    public DateOnly PublicationDate { get; }
    public IReadOnlyList<CurrencyRateEntry> Entries { get; }
    public string SourceUrl { get; }
    public DateTimeOffset RetrievedAtUtc { get; }

    public RateSet(
        DateOnly publicationDate,
        IEnumerable<CurrencyRateEntry> entries,
        string sourceUrl,
        DateTimeOffset retrievedAtUtc)
    {
        PublicationDate = publicationDate;
        SourceUrl = sourceUrl ?? string.Empty;
        RetrievedAtUtc = retrievedAtUtc;

        var entriesList = entries?.ToList() ?? throw new ArgumentNullException(nameof(entries));
        if (entriesList.Count == 0)
        {
            throw new ArgumentException("Набор курсов не может быть пустым.", nameof(entries));
        }

        Entries = entriesList.AsReadOnly();
        _entriesByCode = new Dictionary<CurrencyCode, CurrencyRateEntry>();

        foreach (var entry in entriesList)
        {
            if (_entriesByCode.ContainsKey(entry.Code))
            {
                throw new ArgumentException($"Дубликат валюты '{entry.Code}' в наборе курсов.", nameof(entries));
            }

            _entriesByCode[entry.Code] = entry;
        }
    }

    public CurrencyRateEntry? GetEntry(CurrencyCode code)
    {
        return _entriesByCode.GetValueOrDefault(code);
    }

    public bool TryGetEntry(CurrencyCode code, out CurrencyRateEntry? entry)
    {
        return _entriesByCode.TryGetValue(code, out entry);
    }

    public bool ContainsCurrency(CurrencyCode code)
    {
        return _entriesByCode.ContainsKey(code);
    }
}
