using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Contracts;

/// <summary>
/// Главный оркестратор бизнес-процесса конвертации валют.
/// </summary>
public interface IConversionService
{
    /// <summary>
    /// Выполняет конвертацию валюты по запросу с учётом одинаковых валют, прямого источника и кэша.
    /// </summary>
    Task<ConversionResult> ConvertAsync(ConversionRequest request, CancellationToken cancellationToken = default);
}
