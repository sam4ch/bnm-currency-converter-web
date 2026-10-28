using System.Net;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Infrastructure.Bnm;
using CurrencyConverter.Tests.Fixtures;
using CurrencyConverter.Tests.Helpers;
using Xunit;

namespace CurrencyConverter.Tests.Unit;

public sealed class BnmRateClientTests
{
    private static readonly DateOnly TestDate = new(2026, 10, 28);
    private static readonly DateTimeOffset TestTimestamp = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        Assert.Throws<ArgumentNullException>(() => new BnmRateClient(null!, clock));
    }

    [Fact]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        using var httpClient = new HttpClient(TestHttpMessageHandler.Returning(HttpStatusCode.OK, ""));
        Assert.Throws<ArgumentNullException>(() => new BnmRateClient(httpClient, null!));
    }

    [Fact]
    public async Task GetRatesForDateAsync_ValidXmlResponse_ReturnsParsedRateSet()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = TestHttpMessageHandler.Returning(HttpStatusCode.OK, BnmXmlFixtures.ValidBnmXml20261028);
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock);

        var result = await client.GetRatesForDateAsync(TestDate);

        Assert.NotNull(result);
        Assert.Equal(TestDate, result.PublicationDate);
        Assert.Equal(TestTimestamp, result.RetrievedAtUtc);
        Assert.True(result.ContainsCurrency(CurrencyCode.Eur));
        Assert.True(result.ContainsCurrency(CurrencyCode.Mdl));
        Assert.Equal(1, handler.CallCount);

        string expectedUrl = $"{BnmRateClient.DefaultBaseUrl}28.10.2026";
        Assert.Equal(expectedUrl, handler.RecordedRequests[0].RequestUri?.ToString());
    }

    [Fact]
    public async Task GetRatesForDateAsync_CustomBaseUrl_ConstructsProperRequestUrl()
    {
        const string customBaseUrl = "https://example.com/api/rates?date=";
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = TestHttpMessageHandler.Returning(HttpStatusCode.OK, BnmXmlFixtures.ValidBnmXml20261028);
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock, customBaseUrl);

        await client.GetRatesForDateAsync(TestDate);

        Assert.Equal($"{customBaseUrl}28.10.2026", handler.RecordedRequests[0].RequestUri?.ToString());
    }

    [Fact]
    public async Task GetRatesForDateAsync_EmptyValCurs_ReturnsNull()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = TestHttpMessageHandler.Returning(HttpStatusCode.OK, BnmXmlFixtures.EmptyValCursNoValutes);
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock);

        var result = await client.GetRatesForDateAsync(TestDate);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRatesForDateAsync_EmptyResponseBody_ReturnsNull()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = TestHttpMessageHandler.Returning(HttpStatusCode.OK, "   ");
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock);

        var result = await client.GetRatesForDateAsync(TestDate);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRatesForDateAsync_Http404NotFound_ThrowsRateSourceUnavailableException()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = TestHttpMessageHandler.Returning(HttpStatusCode.NotFound, "Not Found");
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock);

        var ex = await Assert.ThrowsAsync<RateSourceUnavailableException>(() =>
            client.GetRatesForDateAsync(TestDate));

        Assert.Contains("404", ex.Message);
    }

    [Fact]
    public async Task GetRatesForDateAsync_Http500InternalServerError_ThrowsRateSourceUnavailableException()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = TestHttpMessageHandler.Returning(HttpStatusCode.InternalServerError, "Server Error");
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock);

        var ex = await Assert.ThrowsAsync<RateSourceUnavailableException>(() =>
            client.GetRatesForDateAsync(TestDate));

        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task GetRatesForDateAsync_NetworkFailure_ThrowsRateSourceUnavailableExceptionWithInner()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = TestHttpMessageHandler.Throwing(new HttpRequestException("Connection refused by peer"));
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock);

        var ex = await Assert.ThrowsAsync<RateSourceUnavailableException>(() =>
            client.GetRatesForDateAsync(TestDate));

        Assert.Contains("Сетевая ошибка", ex.Message);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task GetRatesForDateAsync_TimeoutDuringRequest_ThrowsRateSourceUnavailableException()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = new TestHttpMessageHandler((_, _) =>
            throw new OperationCanceledException("Synthetic request timeout"));
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock);

        var ex = await Assert.ThrowsAsync<RateSourceUnavailableException>(() =>
            client.GetRatesForDateAsync(TestDate, CancellationToken.None));

        Assert.Contains("Превышено время ожидания", ex.Message);
    }

    [Fact]
    public async Task GetRatesForDateAsync_ExternalCancellation_ThrowsOperationCanceledExceptionDirectly()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = TestHttpMessageHandler.Returning(HttpStatusCode.OK, BnmXmlFixtures.ValidBnmXml20261028);
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetRatesForDateAsync(TestDate, cts.Token));
    }

    [Fact]
    public async Task GetRatesForDateAsync_MalformedXml_ThrowsRateSourceFormatException()
    {
        var clock = new FixedClock(TestDate, TestTimestamp);
        var handler = TestHttpMessageHandler.Returning(HttpStatusCode.OK, BnmXmlFixtures.MalformedXml);
        using var httpClient = new HttpClient(handler);
        var client = new BnmRateClient(httpClient, clock);

        await Assert.ThrowsAsync<RateSourceFormatException>(() =>
            client.GetRatesForDateAsync(TestDate));
    }
}
