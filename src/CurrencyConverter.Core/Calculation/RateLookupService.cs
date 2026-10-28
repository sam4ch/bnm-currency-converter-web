using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Calculation;

/// <summary>
/// Реализация сервиса поиска актуального курса с перебором до 31 календарного дня назад при отсутствии публикации.
/// </summary>
public sealed class RateLookupService : IRateLookupService
{
    private readonly IBnmRateClient _bnmClient;

    public RateLookupService(IBnmRateClient bnmClient)
    {
        _bnmClient = bnmClient ?? throw new ArgumentNullException(nameof(bnmClient));
    }

    public async Task<RateSet> GetRatesAsync(DateOnly requestedDate, CancellationToken cancellationToken = default)
    {
        var context = new RateLookupContext(requestedDate, RateLookupContext.DefaultMaxSearchDays);

        for (int offset = 0; offset <= context.MaxSearchDays; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateOnly candidateDate = requestedDate.AddDays(-offset);
            context.CurrentCandidateDate = candidateDate;
            context.CurrentDayOffset = offset;

            RateSet? rateSet;
            try
            {
                rateSet = await _bnmClient.GetRatesForDateAsync(candidateDate, cancellationToken).ConfigureAwait(false);
            }
            catch (RateSourceException)
            {
                // Не повторяем сетевую ошибку или ошибку формата 32 раза — немедленно останавливаем поиск
                throw;
            }

            if (rateSet != null)
            {
                // Если BNM вернул курс с датой позже запрошенной — это некорректно для исторического поиска
                if (rateSet.PublicationDate > requestedDate)
                {
                    throw new RateSourceFormatException(
                        $"Дата публикации курса BNM ({rateSet.PublicationDate:dd.MM.yyyy}) позже запрошенной клиентом даты ({requestedDate:dd.MM.yyyy}).");
                }

                return rateSet;
            }

            // На эту дату курсы не публиковались (пустой набор Valute) — пробуем предыдущий календарный день
        }

        throw new RateSourceExhaustedException(requestedDate, context.MaxSearchDays);
    }
}
