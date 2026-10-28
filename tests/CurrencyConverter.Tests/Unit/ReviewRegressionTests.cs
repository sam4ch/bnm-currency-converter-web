using System.Globalization;
using System.Text.Json.Nodes;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Infrastructure.Bnm;
using CurrencyConverter.Infrastructure.Caching;
using CurrencyConverter.Infrastructure.Time;
using CurrencyConverter.Tests.Helpers;
using Xunit;

namespace CurrencyConverter.Tests.Unit;

public sealed class ChisinauClockTests
{
    [Theory]
    [InlineData("2026-01-15T22:30:00Z", "2026-01-16", 2)]
    [InlineData("2026-07-15T21:30:00Z", "2026-07-16", 3)]
    [InlineData("2026-01-15T21:59:59Z", "2026-01-15", 2)]
    [InlineData("2026-07-15T20:59:59Z", "2026-07-15", 3)]
    public void Today_UsesActualChisinauZoneAcrossMidnight(string utcText, string expectedDate, int offset)
    {
        var utc = DateTimeOffset.Parse(utcText, CultureInfo.InvariantCulture);
        var clock = new ChisinauClock(new FixedTimeProvider(utc));
        Assert.Equal(utc, clock.UtcNow);
        Assert.Equal(DateOnly.ParseExact(expectedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture), clock.TodayInChisinau);
        Assert.Equal(TimeSpan.FromHours(offset), clock.NowInChisinau.Offset);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utc;
    }
}

public sealed class CurrencyCodeRegressionTests
{
    [Fact]
    public void StaticInitializationAndNormalizedCodes_AreUsable()
    {
        Assert.Equal(new[] { "MDL", "EUR", "USD", "RON", "GBP" },
            CurrencyCode.SupportedCurrencies.Select(code => code.Value));
        Assert.Equal(CurrencyCode.Eur, CurrencyCode.Parse(" eur "));
        Assert.Equal("MDL", CurrencyCode.Mdl.Value);
    }
}

public sealed class BnmXmlParserRegressionTests
{
    [Theory]
    [InlineData(null, "20")]
    [InlineData("1", null)]
    [InlineData("0", "20")]
    [InlineData("1", "-20")]
    [InlineData("1", "1,000.25")]
    [InlineData("1 000", "20")]
    [InlineData("1", "1e3")]
    public void Parse_InvalidOrMissingRequiredNumbers_RejectsPublication(string? nominal, string? rate)
    {
        var nominalElement = nominal is null ? "" : $"<Nominal>{nominal}</Nominal>";
        var rateElement = rate is null ? "" : $"<Value>{rate}</Value>";
        var xml = $"<ValCurs Date=\"28.10.2026\"><Valute><CharCode>EUR</CharCode>{nominalElement}{rateElement}</Valute></ValCurs>";
        Assert.Throws<RateSourceFormatException>(() => BnmXmlParser.Parse(xml, "https://www.bnm.md/test", DateTimeOffset.Parse("2026-10-28T12:00:00Z", CultureInfo.InvariantCulture)));
    }
}

public sealed class JsonCacheRegressionTests
{
    private static readonly DateOnly Date = new(2026, 10, 28);
    private static readonly DateTimeOffset Timestamp = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("version")]
    [InlineData("snapshot-missing")]
    [InlineData("snapshot-invalid")]
    [InlineData("retrieved-default")]
    [InlineData("mdl-value")]
    [InlineData("mdl-nominal")]
    [InlineData("entry-date-missing")]
    [InlineData("source-empty")]
    [InlineData("rate-set-type")]
    [InlineData("entries-type")]
    public async Task Read_CorruptMetadataOrStructure_IsCacheMiss(string mutation)
    {
        using var directory = new TestDirectoryScope();
        var file = directory.GetFilePath("cache.json");
        var store = new JsonCacheStore(file);
        await store.SaveCacheAsync(ValidRates());
        var root = JsonNode.Parse(await File.ReadAllTextAsync(file))!;
        switch (mutation)
        {
            case "version": root["Version"] = "2.0"; break;
            case "snapshot-missing": root.AsObject().Remove("SnapshotDateUtc"); break;
            case "snapshot-invalid": root["SnapshotDateUtc"] = "not-a-timestamp"; break;
            case "retrieved-default": root["RateSet"]!["RetrievedAtUtc"] = "0001-01-01T00:00:00+00:00"; break;
            case "mdl-value": root["RateSet"]!["Entries"]![0]!["RateValue"] = 2m; break;
            case "mdl-nominal": root["RateSet"]!["Entries"]![0]!["Nominal"] = 2m; break;
            case "entry-date-missing": root["RateSet"]!["Entries"]![1]!.AsObject().Remove("PublicationDate"); break;
            case "source-empty": root["RateSet"]!["SourceUrl"] = ""; break;
            case "rate-set-type": root["RateSet"] = new JsonArray(); break;
            case "entries-type": root["RateSet"]!["Entries"] = new JsonObject(); break;
            default: throw new InvalidOperationException("Unknown test mutation");
        }
        await File.WriteAllTextAsync(file, root.ToJsonString());
        Assert.Null(await new JsonCacheStore(file).GetLatestCacheAsync());
    }

    [Fact]
    public async Task Save_InvalidMetadata_DoesNotChangePreviousFileBytes()
    {
        using var directory = new TestDirectoryScope();
        var file = directory.GetFilePath("cache.json");
        var store = new JsonCacheStore(file);
        var valid = ValidRates();
        await store.SaveCacheAsync(valid);
        var previous = await File.ReadAllBytesAsync(file);
        var invalid = new RateSet(Date, valid.Entries, "", Timestamp);
        await Assert.ThrowsAsync<CacheException>(() => store.SaveCacheAsync(invalid));
        Assert.Equal(previous, await File.ReadAllBytesAsync(file));
    }

    private static RateSet ValidRates() => new(Date,
        new[] { new CurrencyRateEntry(CurrencyCode.Mdl, 1m, 1m, Date), new CurrencyRateEntry(CurrencyCode.Eur, 1m, 20m, Date) },
        "https://www.bnm.md/test", Timestamp);
}
