using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Validation;

/// <summary>
/// Проверяет пригодность сохранённого снимка кэша для конкретного запроса конвертации.
/// </summary>
public static class CacheValidator
{
    public static bool IsCacheSuitable(
        VerifiedCacheSnapshot? snapshot,
        DateOnly requestedDate,
        CurrencyCode sourceCurrency,
        CurrencyCode targetCurrency,
        DateOnly todayInChisinau,
        out string? reason)
    {
        if (snapshot == null || snapshot.RateSet == null)
        {
            reason = "Сохранённый локальный кэш отсутствует или повреждён.";
            return false;
        }

        RateSet rateSet = snapshot.RateSet;

        // Кэш с датой позже запрошенной строго запрещён
        if (rateSet.PublicationDate > requestedDate)
        {
            reason = $"Дата кэша ({rateSet.PublicationDate:dd.MM.yyyy}) позже запрошенной даты ({requestedDate:dd.MM.yyyy}) и не может быть использована.";
            return false;
        }

        // Кэш не может быть в будущем относительно текущего времени
        if (rateSet.PublicationDate > todayInChisinau)
        {
            reason = $"Дата кэша ({rateSet.PublicationDate:dd.MM.yyyy}) находится в будущем относительно текущего времени Кишинёва.";
            return false;
        }

        // Проверка наличия необходимых валют в наборе курсов кэша
        if (!rateSet.ContainsCurrency(sourceCurrency))
        {
            reason = $"В сохранённом кэше отсутствует курс для исходной валюты '{sourceCurrency}'.";
            return false;
        }

        if (!rateSet.ContainsCurrency(targetCurrency))
        {
            reason = $"В сохранённом кэше отсутствует курс для целевой валюты '{targetCurrency}'.";
            return false;
        }

        reason = null;
        return true;
    }
}
