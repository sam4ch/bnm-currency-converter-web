using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Contracts;

/// <summary>
/// Сервис валидации входных параметров конвертации.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Валидирует и парсит текстовое представление суммы.
    /// Выбрасывает ValidationException при ошибке.
    /// </summary>
    Amount ValidateAmount(string? amountText);

    /// <summary>
    /// Пытается валидировать текстовое представление суммы без выброса исключения.
    /// </summary>
    bool TryValidateAmount(string? amountText, out Amount? amount, out ValidationError? error);

    /// <summary>
    /// Валидирует дату запроса относительно текущего времени Кишинёва.
    /// </summary>
    void ValidateDate(DateOnly date);

    /// <summary>
    /// Валидирует код валюты.
    /// </summary>
    CurrencyCode ValidateCurrency(string? currencyText, string fieldName);

    /// <summary>
    /// Комплексно валидирует запрос пользователя.
    /// </summary>
    ConversionRequest ValidateRequest(
        string? amountText,
        string? sourceCurrencyText,
        string? targetCurrencyText,
        DateOnly date,
        bool cacheFallbackApproved = false);
}
