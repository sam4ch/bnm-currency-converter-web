namespace CurrencyConverter.Core.Models;

/// <summary>
/// Статус результата операции конвертации.
/// </summary>
public enum ConversionStatus
{
    /// <summary>
    /// Конвертация успешно выполнена.
    /// </summary>
    Success,

    /// <summary>
    /// Источник недоступен, но доступен подходящий локальный кэш, требующий явного подтверждения пользователя.
    /// </summary>
    CacheConfirmationRequired,

    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    ValidationError,

    /// <summary>
    /// Источник курсов недоступен и подходящий кэш отсутствует.
    /// </summary>
    SourceUnavailable
}
