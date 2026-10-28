using System.Globalization;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Infrastructure.Bnm;

/// <summary>
/// Реализация HTTP-клиента для получения XML-курсов валют BNM.
/// </summary>
public sealed class BnmRateClient : IBnmRateClient
{
    public const string DefaultBaseUrl = "https://www.bnm.md/en/official_exchange_rates?get_xml=1&date=";
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient _httpClient;
    private readonly IClock _clock;
    private readonly string _baseUrl;

    public BnmRateClient(HttpClient httpClient, IClock clock, string? baseUrl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _baseUrl = baseUrl ?? DefaultBaseUrl;
    }

    public async Task<RateSet?> GetRatesForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string dateString = date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        string requestUrl = $"{_baseUrl}{dateString}";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUrl, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Внешняя отмена не должна маскироваться под сетевой таймаут
            throw;
        }
        catch (OperationCanceledException ex)
        {
            throw new RateSourceUnavailableException(
                $"Превышено время ожидания ответа от источника BNM ({DefaultTimeout.TotalSeconds:0} сек.) при запросе на дату {dateString}.",
                ex);
        }
        catch (HttpRequestException ex)
        {
            throw new RateSourceUnavailableException(
                $"Сетевая ошибка при обращении к источнику BNM ({requestUrl}): {ex.Message}",
                ex);
        }
        catch (Exception ex) when (ex is not RateSourceException && ex is not OperationCanceledException)
        {
            throw new RateSourceUnavailableException(
                $"Не удалось установить соединение с сервером BNM: {ex.Message}",
                ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new RateSourceUnavailableException(
                    $"Сервер BNM вернул ошибку HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) при запросе на дату {dateString}.");
            }

            string xmlContent;
            try
            {
                xmlContent = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                throw new RateSourceUnavailableException(
                    $"Превышено время ожидания при чтении тела ответа от источника BNM ({dateString}).",
                    ex);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new RateSourceUnavailableException("Ошибка чтения ответа от сервера BNM.", ex);
            }

            DateTimeOffset retrievedAtUtc = _clock.UtcNow;
            return BnmXmlParser.Parse(xmlContent, requestUrl, retrievedAtUtc);
        }
    }
}
