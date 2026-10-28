using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Calculation;

/// <summary>
/// Реализация калькулятора валют с точным пересчётом через базовую валюту MDL и номиналы.
/// </summary>
public sealed class CurrencyCalculator : ICurrencyCalculator
{
    public decimal Convert(decimal amount, CurrencyRateEntry sourceEntry, CurrencyRateEntry targetEntry)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Сумма для конвертации не может быть отрицательной.");
        }

        ArgumentNullException.ThrowIfNull(sourceEntry);
        ArgumentNullException.ThrowIfNull(targetEntry);

        if (sourceEntry.Nominal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceEntry), "Номинал исходной валюты должен быть строго больше нуля.");
        }

        if (sourceEntry.RateValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceEntry), "Курс исходной валюты должен быть строго больше нуля.");
        }

        if (targetEntry.Nominal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetEntry), "Номинал целевой валюты должен быть строго больше нуля.");
        }

        if (targetEntry.RateValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetEntry), "Курс целевой валюты должен быть строго больше нуля.");
        }

        try
        {
            // Формула: amount * (ValueSource / NominalSource) / (ValueTarget / NominalTarget)
            decimal sourceMdlPerUnit = sourceEntry.RateValue / sourceEntry.Nominal;
            decimal targetMdlPerUnit = targetEntry.RateValue / targetEntry.Nominal;

            decimal mdlAmount = amount * sourceMdlPerUnit;
            decimal unroundedResult = mdlAmount / targetMdlPerUnit;

            // Округление AwayFromZero до 2 знаков выполняется ТОЛЬКО в самом конце
            return Math.Round(unroundedResult, 2, MidpointRounding.AwayFromZero);
        }
        catch (OverflowException ex)
        {
            throw new CalculationOverflowException("Переполнение при выполнении математического расчёта конвертации суммы.", ex);
        }
    }
}
