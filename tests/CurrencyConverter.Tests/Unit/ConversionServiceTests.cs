using CurrencyConverter.Core.Calculation;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Tests.Helpers;
using Xunit;

namespace CurrencyConverter.Tests.Unit;

public sealed class ConversionServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 28);
    private static readonly DateTimeOffset TestTimestamp = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
    private readonly FixedClock _clock = new(Today);
    private readonly CurrencyCalculator _calculator = new();

    [Fact]
    public async Task ConvertAsync_SameCurrency_ReturnsExactAmountWithoutCallingSourceOrCache()
    {
        var mockLookup = new TrackingLookupService();
        var mockCache = new TrackingCacheStore();
        var service = new ConversionService(mockLookup, mockCache, _calculator, _clock);

        // 123.4567 MDL в MDL
        var request = new ConversionRequest(
            new Amount("123.4567", 123.4567m, 4),
            CurrencyCode.Mdl,
            CurrencyCode.Mdl,
            Today);

        var result = await service.ConvertAsync(request);

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.True(result.SameCurrencyPath);
        Assert.Equal(123.4567m, result.ConvertedAmount);
        Assert.Equal(123.4567m, result.SourceAmount);

        // Критическое требование: не выдумывать дату/источник/время
        Assert.Null(result.RateDateUsed);
        Assert.Null(result.SourceReference);
        Assert.Null(result.RetrievalTimestampUtc);

        // Сеть и кэш вообще не вызываются
        Assert.Equal(0, mockLookup.CallCount);
        Assert.Equal(0, mockCache.GetCallCount);
        Assert.Equal(0, mockCache.SaveCallCount);
    }

    [Fact]
    public async Task ConvertAsync_LiveSourceSuccess_CalculatesAndSavesToCache()
    {
        var rateSet = CreateRateSet(Today, 20.0m);
        var mockLookup = new TrackingLookupService(rateSet);
        var mockCache = new TrackingCacheStore();
        var service = new ConversionService(mockLookup, mockCache, _calculator, _clock);

        var request = new ConversionRequest(
            new Amount("100", 100m, 0),
            CurrencyCode.Eur,
            CurrencyCode.Mdl,
            Today);

        var result = await service.ConvertAsync(request);

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.False(result.SameCurrencyPath);
        Assert.False(result.UsedCacheFallback);
        Assert.Equal(2000.00m, result.ConvertedAmount);
        Assert.Equal(Today, result.RateDateUsed);
        Assert.Equal(1, mockCache.SaveCallCount);
    }

    [Fact]
    public async Task ConvertAsync_LiveSourceEarlierDate_IncludesWarning()
    {
        var earlierDate = Today.AddDays(-3); // 25.10.2026
        var rateSet = CreateRateSet(earlierDate, 20.0m);
        var mockLookup = new TrackingLookupService(rateSet);
        var mockCache = new TrackingCacheStore();
        var service = new ConversionService(mockLookup, mockCache, _calculator, _clock);

        var request = new ConversionRequest(
            new Amount("100", 100m, 0),
            CurrencyCode.Eur,
            CurrencyCode.Mdl,
            Today);

        var result = await service.ConvertAsync(request);

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.Equal(earlierDate, result.RateDateUsed);
        Assert.NotNull(result.WarningText);
        Assert.Contains("25.10.2026", result.WarningText);
    }

    [Fact]
    public async Task ConvertAsync_CacheWriteFails_PreservesSuccessfulResultWithWarning()
    {
        var rateSet = CreateRateSet(Today, 20.0m);
        var mockLookup = new TrackingLookupService(rateSet);
        var mockCache = new TrackingCacheStore
        {
            SaveThrowsException = new CacheWriteException("Диск переполнен", new IOException())
        };
        var service = new ConversionService(mockLookup, mockCache, _calculator, _clock);

        var request = new ConversionRequest(
            new Amount("100", 100m, 0),
            CurrencyCode.Eur,
            CurrencyCode.Mdl,
            Today);

        var result = await service.ConvertAsync(request);

        // Расчёт сохраняется успешным
        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.Equal(2000.00m, result.ConvertedAmount);

        // Отдельное предупреждение об ошибке сохранения кэша
        Assert.NotNull(result.WarningText);
        Assert.Contains("не удалось сохранить локальный кэш", result.WarningText);
    }

    [Fact]
    public async Task ConvertAsync_OfflineWithNoCache_ReturnsSourceUnavailable()
    {
        var mockLookup = new TrackingLookupService
        {
            ThrowsException = new RateSourceUnavailableException("Сеть отключена")
        };
        var mockCache = new TrackingCacheStore(); // кэш пуст
        var service = new ConversionService(mockLookup, mockCache, _calculator, _clock);

        var request = new ConversionRequest(
            new Amount("100", 100m, 0),
            CurrencyCode.Eur,
            CurrencyCode.Mdl,
            Today);

        var result = await service.ConvertAsync(request);

        Assert.Equal(ConversionStatus.SourceUnavailable, result.Status);
        Assert.Null(result.ConvertedAmount);
    }

    [Fact]
    public async Task ConvertAsync_OfflineWithSuitableCacheUnapproved_RequiresConfirmationWithoutCalculating()
    {
        var mockLookup = new TrackingLookupService
        {
            ThrowsException = new RateSourceUnavailableException("Таймаут источника")
        };
        var cachedRateSet = CreateRateSet(Today.AddDays(-2), 20.0m);
        var mockCache = new TrackingCacheStore(new VerifiedCacheSnapshot(TestTimestamp, cachedRateSet));
        var service = new ConversionService(mockLookup, mockCache, _calculator, _clock);

        var request = new ConversionRequest(
            new Amount("100", 100m, 0),
            CurrencyCode.Eur,
            CurrencyCode.Mdl,
            Today,
            cacheFallbackApproved: false);

        var result = await service.ConvertAsync(request);

        // Без явного согласия НЕ рассчитываем молча
        Assert.Equal(ConversionStatus.CacheConfirmationRequired, result.Status);
        Assert.Null(result.ConvertedAmount);
        Assert.NotNull(result.AvailableCacheSnapshot);
        Assert.NotNull(result.UserMessage);
    }

    [Fact]
    public async Task ConvertAsync_OfflineWithSuitableCacheApproved_CalculatesFromCacheWithoutNetworkCall()
    {
        var mockLookup = new TrackingLookupService
        {
            ThrowsException = new InvalidOperationException("Сетевой вызов не должен производиться!")
        };
        var cachedRateSet = CreateRateSet(Today.AddDays(-2), 20.0m);
        var mockCache = new TrackingCacheStore(new VerifiedCacheSnapshot(TestTimestamp, cachedRateSet));
        var service = new ConversionService(mockLookup, mockCache, _calculator, _clock);

        var request = new ConversionRequest(
            new Amount("100", 100m, 0),
            CurrencyCode.Eur,
            CurrencyCode.Mdl,
            Today,
            cacheFallbackApproved: true);

        var result = await service.ConvertAsync(request);

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.True(result.UsedCacheFallback);
        Assert.Equal(2000.00m, result.ConvertedAmount);
        Assert.NotNull(result.WarningText);

        // Повторный сетевой запрос НЕ выполнялся
        Assert.Equal(0, mockLookup.CallCount);
        Assert.Equal(Today.AddDays(-2), result.RateDateUsed);
        Assert.Equal("https://www.bnm.md/test", result.SourceReference);
        Assert.Equal(TestTimestamp, result.RetrievalTimestampUtc);
        Assert.Equal(1, mockCache.GetCallCount);
    }

    [Fact]
    public async Task ConvertAsync_CacheDateLaterThanRequestedDate_ProhibitedAndRejected()
    {
        var mockLookup = new TrackingLookupService
        {
            ThrowsException = new RateSourceUnavailableException("Сеть недоступна")
        };
        // Кэш датирован 28.10.2026, а пользователь запросил историческую дату 25.10.2026
        var cachedRateSet = CreateRateSet(new DateOnly(2026, 10, 28), 20.0m);
        var mockCache = new TrackingCacheStore(new VerifiedCacheSnapshot(TestTimestamp, cachedRateSet));
        var service = new ConversionService(mockLookup, mockCache, _calculator, _clock);

        var request = new ConversionRequest(
            new Amount("100", 100m, 0),
            CurrencyCode.Eur,
            CurrencyCode.Mdl,
            new DateOnly(2026, 10, 25),
            cacheFallbackApproved: true);

        var result = await service.ConvertAsync(request);

        // Кэш позже запрошенной даты строго запрещён
        Assert.Equal(ConversionStatus.SourceUnavailable, result.Status);
        Assert.Null(result.ConvertedAmount);
    }

    [Fact]
    public async Task ConvertAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockLookup = new TrackingLookupService(CreateRateSet(Today, 20m));
        var mockCache = new TrackingCacheStore();
        var service = new ConversionService(mockLookup, mockCache, _calculator, _clock);

        var request = new ConversionRequest(
            new Amount("100", 100m, 0),
            CurrencyCode.Eur,
            CurrencyCode.Mdl,
            Today);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ConvertAsync(request, cts.Token));
    }

    private static RateSet CreateRateSet(DateOnly pubDate, decimal eurRate)
    {
        var entries = new List<CurrencyRateEntry>
        {
            new(CurrencyCode.Mdl, 1m, 1m, pubDate),
            new(CurrencyCode.Eur, 1m, eurRate, pubDate),
            new(CurrencyCode.Usd, 1m, 18.0m, pubDate)
        };
        return new RateSet(pubDate, entries, "https://www.bnm.md/test", TestTimestamp);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConvertAsync_MissingSelectedCurrency_ReturnsNoResultAndDoesNotSave(bool useCache)
    {
        var rates = CreateRateSet(Today, 20m); // RON отсутствует.
        var lookup = new TrackingLookupService(rates);
        var cache = new TrackingCacheStore(new VerifiedCacheSnapshot(TestTimestamp, rates));
        var service = new ConversionService(lookup, cache, _calculator, _clock);
        var request = new ConversionRequest(new Amount("100", 100m, 0),
            CurrencyCode.Eur, CurrencyCode.Ron, Today, useCache);

        var result = await service.ConvertAsync(request);

        Assert.Equal(ConversionStatus.SourceUnavailable, result.Status);
        Assert.Null(result.ConvertedAmount);
        Assert.Equal(0, cache.SaveCallCount);
        Assert.Equal(useCache ? 0 : 1, lookup.CallCount);
    }

    [Fact]
    public async Task ConvertAsync_CancelledSameCurrency_DoesNotReturnSuccess()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var lookup = new TrackingLookupService();
        var cache = new TrackingCacheStore();
        var service = new ConversionService(lookup, cache, _calculator, _clock);
        var request = new ConversionRequest(new Amount("1", 1m, 0), CurrencyCode.Mdl, CurrencyCode.Mdl, Today);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ConvertAsync(request, cancellation.Token));
        Assert.Equal(0, lookup.CallCount);
        Assert.Equal(0, cache.GetCallCount);
    }

    private sealed class TrackingLookupService : IRateLookupService
    {
        private readonly RateSet? _rateSet;
        public Exception? ThrowsException { get; set; }
        public int CallCount { get; private set; }

        public TrackingLookupService(RateSet? rateSet = null)
        {
            _rateSet = rateSet;
        }

        public Task<RateSet> GetRatesAsync(DateOnly requestedDate, CancellationToken cancellationToken = default)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();

            if (ThrowsException != null)
            {
                throw ThrowsException;
            }

            return Task.FromResult(_rateSet ?? CreateRateSet(requestedDate, 20m));
        }
    }

    private sealed class TrackingCacheStore : ICacheStore
    {
        private VerifiedCacheSnapshot? _snapshot;
        public Exception? SaveThrowsException { get; set; }
        public int GetCallCount { get; private set; }
        public int SaveCallCount { get; private set; }

        public TrackingCacheStore(VerifiedCacheSnapshot? snapshot = null)
        {
            _snapshot = snapshot;
        }

        public Task<VerifiedCacheSnapshot?> GetLatestCacheAsync(CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_snapshot);
        }

        public Task SaveCacheAsync(RateSet rateSet, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            cancellationToken.ThrowIfCancellationRequested();

            if (SaveThrowsException != null)
            {
                throw SaveThrowsException;
            }

            _snapshot = new VerifiedCacheSnapshot(DateTimeOffset.UtcNow, rateSet);
            return Task.CompletedTask;
        }
    }
}
