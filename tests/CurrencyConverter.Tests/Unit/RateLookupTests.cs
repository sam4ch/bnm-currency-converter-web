using CurrencyConverter.Core.Calculation;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using Xunit;

namespace CurrencyConverter.Tests.Unit;

public sealed class RateLookupTests
{
    private static readonly DateOnly RequestedDate = new(2026, 10, 28);
    private static readonly DateTimeOffset TestTimestamp = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetRatesAsync_RequestedDatePublished_ReturnsOnFirstAttempt()
    {
        var fakeClient = new FakeBnmClient(date =>
        {
            if (date == RequestedDate)
            {
                return CreateRateSet(date);
            }
            return null;
        });

        var lookupService = new RateLookupService(fakeClient);

        var result = await lookupService.GetRatesAsync(RequestedDate);

        Assert.NotNull(result);
        Assert.Equal(RequestedDate, result.PublicationDate);
        Assert.Equal(1, fakeClient.CallCount);
    }

    [Fact]
    public async Task GetRatesAsync_RequestedDateMissing_FindsPreviousDayPublication()
    {
        // 28.10.2026 нет публикации (null), 27.10.2026 есть
        var previousDate = RequestedDate.AddDays(-1);
        var fakeClient = new FakeBnmClient(date =>
        {
            if (date == previousDate)
            {
                return CreateRateSet(date);
            }
            return null;
        });

        var lookupService = new RateLookupService(fakeClient);

        var result = await lookupService.GetRatesAsync(RequestedDate);

        Assert.NotNull(result);
        Assert.Equal(previousDate, result.PublicationDate);
        Assert.Equal(2, fakeClient.CallCount); // 28.10 (null) -> 27.10 (успех)
    }

    [Fact]
    public async Task GetRatesAsync_PublicationOn31stDayEarlier_SucceedsWithinWindow()
    {
        var day31 = RequestedDate.AddDays(-31);
        var fakeClient = new FakeBnmClient(date =>
        {
            if (date == day31)
            {
                return CreateRateSet(date);
            }
            return null;
        });

        var lookupService = new RateLookupService(fakeClient);

        var result = await lookupService.GetRatesAsync(RequestedDate);

        Assert.NotNull(result);
        Assert.Equal(day31, result.PublicationDate);
        Assert.Equal(32, fakeClient.CallCount); // запрошенная + 31 день назад
    }

    [Fact]
    public async Task GetRatesAsync_NoPublicationWithin31Days_ThrowsRateSourceExhaustedException()
    {
        // Все дни возвращают null (нет публикаций)
        var fakeClient = new FakeBnmClient(_ => null);
        var lookupService = new RateLookupService(fakeClient);

        var ex = await Assert.ThrowsAsync<RateSourceExhaustedException>(() =>
            lookupService.GetRatesAsync(RequestedDate));

        Assert.Equal(32, fakeClient.CallCount); // проверил запрошенную + 31 день назад
        Assert.Equal(RequestedDate, ex.RequestedDate);
    }

    [Fact]
    public async Task GetRatesAsync_NetworkError_StopsImmediatelyWithoutLooping()
    {
        var fakeClient = new FakeBnmClient(_ =>
            throw new RateSourceUnavailableException("Сеть недоступна"));

        var lookupService = new RateLookupService(fakeClient);

        await Assert.ThrowsAsync<RateSourceUnavailableException>(() =>
            lookupService.GetRatesAsync(RequestedDate));

        // Критическое требование: сетевая ошибка НЕ повторяется 32 раза!
        Assert.Equal(1, fakeClient.CallCount);
    }

    [Fact]
    public async Task GetRatesAsync_XmlFormatError_StopsImmediatelyWithoutLooping()
    {
        var fakeClient = new FakeBnmClient(_ =>
            throw new RateSourceFormatException("Невалидный XML"));

        var lookupService = new RateLookupService(fakeClient);

        await Assert.ThrowsAsync<RateSourceFormatException>(() =>
            lookupService.GetRatesAsync(RequestedDate));

        Assert.Equal(1, fakeClient.CallCount);
    }

    [Fact]
    public async Task GetRatesAsync_BnmReturnsFutureDate_ThrowsRateSourceFormatException()
    {
        // Клиент запросил 28.10, а BNM вернул курс от 29.10
        var fakeClient = new FakeBnmClient(_ => CreateRateSet(RequestedDate.AddDays(1)));
        var lookupService = new RateLookupService(fakeClient);

        await Assert.ThrowsAsync<RateSourceFormatException>(() =>
            lookupService.GetRatesAsync(RequestedDate));
    }

    [Fact]
    public async Task GetRatesAsync_BnmReturnsDateBeyond31DaysWindow_ThrowsRateSourceFormatException()
    {
        // Клиент запросил 28.10, а ответ вернул дату на 50 дней раньше (выходит за 31 день)
        var fakeClient = new FakeBnmClient(_ => CreateRateSet(RequestedDate.AddDays(-50)));
        var lookupService = new RateLookupService(fakeClient);

        await Assert.ThrowsAsync<RateSourceFormatException>(() =>
            lookupService.GetRatesAsync(RequestedDate));
    }

    [Fact]
    public async Task GetRatesAsync_DateOnlyMinValue_DoesNotUnderflow()
    {
        var fakeClient = new FakeBnmClient(_ => null);
        var lookupService = new RateLookupService(fakeClient);

        // DateOnly.MinValue (01.01.0001) не должно вызывать ArgumentOutOfRangeException
        await Assert.ThrowsAsync<RateSourceExhaustedException>(() =>
            lookupService.GetRatesAsync(DateOnly.MinValue));
    }

    private static RateSet CreateRateSet(DateOnly pubDate)
    {
        var entries = new List<CurrencyRateEntry>
        {
            new(CurrencyCode.Mdl, 1m, 1m, pubDate),
            new(CurrencyCode.Eur, 1m, 20.0m, pubDate),
            new(CurrencyCode.Usd, 1m, 18.0m, pubDate)
        };
        return new RateSet(pubDate, entries, "https://www.bnm.md/test", TestTimestamp);
    }

    private sealed class FakeBnmClient : IBnmRateClient
    {
        private readonly Func<DateOnly, RateSet?> _responder;
        public int CallCount { get; private set; }

        public FakeBnmClient(Func<DateOnly, RateSet?> responder)
        {
            _responder = responder;
        }

        public Task<RateSet?> GetRatesForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_responder(date));
        }
    }
}
