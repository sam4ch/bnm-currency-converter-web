namespace CurrencyConverter.Core.Models;

/// <summary>
/// Представляет запись официального курса BNM для одной валюты на определённую дату.
/// </summary>
public sealed class CurrencyRateEntry
{
    public CurrencyCode Code { get; }
    public decimal Nominal { get; }
    public decimal RateValue { get; }
    public decimal EffectiveMdlPerUnit { get; }
    public DateOnly PublicationDate { get; }

    public CurrencyRateEntry(CurrencyCode code, decimal nominal, decimal rateValue, DateOnly publicationDate)
    {
        if (nominal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nominal), "Номинал должен быть строго больше нуля.");
        }

        if (rateValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rateValue), "Курс должен быть строго больше нуля.");
        }

        Code = code;
        Nominal = nominal;
        RateValue = rateValue;
        EffectiveMdlPerUnit = rateValue / nominal;
        PublicationDate = publicationDate;
    }
}
