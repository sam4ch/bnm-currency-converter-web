namespace CurrencyConverter.Core.Models;

/// <summary>
/// Представляет снимок проверенного кэша курсов валют, сохранённого на диске.
/// </summary>
public sealed class VerifiedCacheSnapshot
{
    public const string CurrentVersion = "1.0";

    public DateTimeOffset SnapshotDateUtc { get; }
    public RateSet RateSet { get; }
    public string Version { get; }
    public string CacheSourceLabel { get; }

    public VerifiedCacheSnapshot(
        DateTimeOffset snapshotDateUtc,
        RateSet rateSet,
        string version = CurrentVersion,
        string cacheSourceLabel = "BNM Official XML Cache")
    {
        SnapshotDateUtc = snapshotDateUtc;
        RateSet = rateSet ?? throw new ArgumentNullException(nameof(rateSet));
        Version = version ?? CurrentVersion;
        CacheSourceLabel = cacheSourceLabel ?? string.Empty;
    }
}
