using System.Net;

namespace CurrencyConverter.Tests.Helpers;

/// <summary>
/// Тестовый HTTP-обработчик для изоляции запросов к BNM без реального сетевого доступа.
/// </summary>
public sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _responder;
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _asyncResponder;

    public int CallCount { get; private set; }
    public List<HttpRequestMessage> RecordedRequests { get; } = [];

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder ?? throw new ArgumentNullException(nameof(responder));
    }

    public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> asyncResponder)
    {
        _asyncResponder = asyncResponder ?? throw new ArgumentNullException(nameof(asyncResponder));
    }

    public static TestHttpMessageHandler Returning(HttpStatusCode statusCode, string content)
    {
        return new TestHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        });
    }

    public static TestHttpMessageHandler Throwing(Exception exception)
    {
        return new TestHttpMessageHandler(_ => throw exception);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        RecordedRequests.Add(request);

        if (_asyncResponder != null)
        {
            return await _asyncResponder(request, cancellationToken).ConfigureAwait(false);
        }

        if (_responder != null)
        {
            return _responder(request);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
