using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Contracts;

/// <summary>
/// Чистый калькулятор пересчёта валют с учётом номинала через базовую валюту MDL.
/// </summary>
public interface ICurrencyCalculator
{
    /// <summary>
    /// Вычисляет сконвертированную сумму из исходной валюты в целевую.
    /// Результат округляется до 2 знаков по правилу AwayFromZero только в самом конце.
    /// </summary>
    decimal Convert(decimal amount, CurrencyRateEntry sourceEntry, CurrencyRateEntry targetEntry);
}
