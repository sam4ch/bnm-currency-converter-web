using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Infrastructure.Caching;
using CurrencyConverter.Tests.Helpers;
using Xunit;

namespace CurrencyConverter.Tests.Unit;

public sealed class JsonCacheStoreTests
{
    private static readonly DateOnly TestDate = new(2026, 10, 28);
    private static readonly DateTimeOffset TestTimestamp = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveAndGet_NewStoreInstance_RestoresVerifiedCacheSnapshot()
    {
        using var scope = new TestDirectoryScope();
        string cacheFile = scope.GetFilePath("rates_cache.json");

        var store1 = new JsonCacheStore(cacheFile);
        var rateSet = CreateValidRateSet(TestDate);

        // 1. Запись первым экземпляром
        await store1.SaveCacheAsync(rateSet);

        // 2. Симуляция перезапуска приложения: чтение абсолютно новым экземпляром сервиса
        var store2 = new JsonCacheStore(cacheFile);
        var snapshot = await store2.GetLatestCacheAsync();

        Assert.NotNull(snapshot);
        Assert.Equal("1.0", snapshot.Version);
        Assert.Equal(TestDate, snapshot.RateSet.PublicationDate);
        Assert.Equal(rateSet.SourceUrl, snapshot.RateSet.SourceUrl);
        Assert.True(snapshot.RateSet.ContainsCurrency(CurrencyCode.Eur));
        Assert.True(snapshot.RateSet.ContainsCurrency(CurrencyCode.Mdl));
    }

    [Fact]
    public async Task GetLatestCacheAsync_FileNotFound_ReturnsNullWithoutThrowing()
    {
        using var scope = new TestDirectoryScope();
        string nonExistent = scope.GetFilePath("missing.json");
        var store = new JsonCacheStore(nonExistent);

        var snapshot = await store.GetLatestCacheAsync();

        Assert.Null(snapshot);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a json")]
    [InlineData("{ broken json")]
    public async Task GetLatestCacheAsync_CorruptedOrEmptyFile_ReturnsNullWithoutThrowing(string brokenContent)
    {
        using var scope = new TestDirectoryScope();
        string file = scope.GetFilePath("corrupted.json");
        await File.WriteAllTextAsync(file, brokenContent);

        var store = new JsonCacheStore(file);
        var snapshot = await store.GetLatestCacheAsync();

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task GetLatestCacheAsync_MissingVersionField_ReturnsNull()
    {
        using var scope = new TestDirectoryScope();
        string file = scope.GetFilePath("no_version.json");

        // JSON без поля version
        string json = """
            {
                "snapshotDateUtc": "2026-10-28T12:00:00Z",
                "rateSet": {
                    "publicationDate": "2026-10-28",
                    "sourceUrl": "https://bnm.md",
                    "retrievedAtUtc": "2026-10-28T12:00:00Z",
                    "entries": [
                        { "code": "MDL", "nominal": 1, "rateValue": 1, "publicationDate": "2026-10-28" },
                        { "code": "EUR", "nominal": 1, "rateValue": 20, "publicationDate": "2026-10-28" }
                    ]
                }
            }
            """;
        await File.WriteAllTextAsync(file, json);

        var store = new JsonCacheStore(file);
        var snapshot = await store.GetLatestCacheAsync();

        // Отсутствующая версия не должна подменяться на "1.0"
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task GetLatestCacheAsync_EntriesWithNullElement_ReturnsNull()
    {
        using var scope = new TestDirectoryScope();
        string file = scope.GetFilePath("null_entry.json");

        // entries содержит null
        string json = """
            {
                "version": "1.0",
                "snapshotDateUtc": "2026-10-28T12:00:00Z",
                "rateSet": {
                    "publicationDate": "2026-10-28",
                    "sourceUrl": "https://bnm.md",
                    "retrievedAtUtc": "2026-10-28T12:00:00Z",
                    "entries": [
                        null,
                        { "code": "MDL", "nominal": 1, "rateValue": 1, "publicationDate": "2026-10-28" }
                    ]
                }
            }
            """;
        await File.WriteAllTextAsync(file, json);

        var store = new JsonCacheStore(file);
        var snapshot = await store.GetLatestCacheAsync();

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task GetLatestCacheAsync_InconsistentEntryDate_ReturnsNullWithoutSilentRepair()
    {
        using var scope = new TestDirectoryScope();
        string file = scope.GetFilePath("inconsistent_date.json");

        // Дата записи (2026-10-20) отличается от даты корня (2026-10-28)
        string json = """
            {
                "version": "1.0",
                "snapshotDateUtc": "2026-10-28T12:00:00Z",
                "rateSet": {
                    "publicationDate": "2026-10-28",
                    "sourceUrl": "https://bnm.md",
                    "retrievedAtUtc": "2026-10-28T12:00:00Z",
                    "entries": [
                        { "code": "MDL", "nominal": 1, "rateValue": 1, "publicationDate": "2026-10-20" },
                        { "code": "EUR", "nominal": 1, "rateValue": 20, "publicationDate": "2026-10-28" }
                    ]
                }
            }
            """;
        await File.WriteAllTextAsync(file, json);

        var store = new JsonCacheStore(file);
        var snapshot = await store.GetLatestCacheAsync();

        // Запрещено молча заменять плохую дату на дату корня
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task GetLatestCacheAsync_MissingBaseMdl_ReturnsNullWithoutFabricatingMdl()
    {
        using var scope = new TestDirectoryScope();
        string file = scope.GetFilePath("no_mdl.json");

        // В кэше нет MDL
        string json = """
            {
                "version": "1.0",
                "snapshotDateUtc": "2026-10-28T12:00:00Z",
                "rateSet": {
                    "publicationDate": "2026-10-28",
                    "sourceUrl": "https://bnm.md",
                    "retrievedAtUtc": "2026-10-28T12:00:00Z",
                    "entries": [
                        { "code": "EUR", "nominal": 1, "rateValue": 20, "publicationDate": "2026-10-28" }
                    ]
                }
            }
            """;
        await File.WriteAllTextAsync(file, json);

        var store = new JsonCacheStore(file);
        var snapshot = await store.GetLatestCacheAsync();

        // Запрещено дописывать MDL в повреждённый кэш
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task SaveCacheAsync_InvalidRateSet_ThrowsAndDoesNotOverwriteGoodCache()
    {
        using var scope = new TestDirectoryScope();
        string file = scope.GetFilePath("protected_cache.json");
        var store = new JsonCacheStore(file);

        // 1. Сохраняем хороший кэш
        var goodRateSet = CreateValidRateSet(TestDate);
        await store.SaveCacheAsync(goodRateSet);

        // 2. Пытаемся сохранить повреждённый набор без MDL (создаём через обход)
        var badEntries = new List<CurrencyRateEntry>
        {
            new(CurrencyCode.Eur, 1m, 20m, TestDate)
        };
        var badRateSet = new RateSet(TestDate, badEntries, "https://bnm.md", TestTimestamp);

        // Должно выбросить исключение до записи файла
        await Assert.ThrowsAsync<CacheException>(() => store.SaveCacheAsync(badRateSet));

        // 3. Проверяем, что исходный хороший кэш не пострадал
        var snapshot = await store.GetLatestCacheAsync();
        Assert.NotNull(snapshot);
        Assert.True(snapshot.RateSet.ContainsCurrency(CurrencyCode.Mdl));
    }

    [Fact]
    public async Task SaveCacheAsync_ConcurrentWrites_PreserveDocumentIntegrity()
    {
        using var scope = new TestDirectoryScope();
        string file = scope.GetFilePath("concurrent.json");
        var store = new JsonCacheStore(file);

        // 10 конкурентных задач сохранения
        var tasks = Enumerable.Range(0, 10).Select(i =>
        {
            var date = TestDate.AddDays(-i);
            var rateSet = CreateValidRateSet(date);
            return Task.Run(() => store.SaveCacheAsync(rateSet));
        }).ToArray();

        await Task.WhenAll(tasks);

        // Проверяем, что итоговый файл не повреждён и корректно парсится новым экземпляром
        var verifyStore = new JsonCacheStore(file);
        var snapshot = await verifyStore.GetLatestCacheAsync();

        Assert.NotNull(snapshot);
        Assert.True(snapshot.RateSet.ContainsCurrency(CurrencyCode.Mdl));
    }

    private static RateSet CreateValidRateSet(DateOnly date)
    {
        var entries = new List<CurrencyRateEntry>
        {
            new(CurrencyCode.Mdl, 1m, 1m, date),
            new(CurrencyCode.Eur, 1m, 20m, date),
            new(CurrencyCode.Usd, 1m, 18m, date)
        };
        return new RateSet(date, entries, "https://www.bnm.md/test", TestTimestamp);
    }
}
