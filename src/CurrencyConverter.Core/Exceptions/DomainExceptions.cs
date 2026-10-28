using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Exceptions;

/// <summary>
/// Базовое доменное исключение приложения.
/// </summary>
public abstract class CurrencyConverterException : Exception
{
    protected CurrencyConverterException(string message) : base(message) { }
    protected CurrencyConverterException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Исключение валидации входных данных.
/// </summary>
public class ValidationException : CurrencyConverterException
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = [new ValidationError("validation-error", "validation.general", message)];
    }

    public ValidationException(ValidationError error) : base(error.Message)
    {
        Errors = [error];
    }

    public ValidationException(IEnumerable<ValidationError> errors)
        : base(string.Join("; ", errors.Select(e => e.Message)))
    {
        Errors = errors.ToList().AsReadOnly();
    }
}

/// <summary>
/// Базовое исключение источника курсов BNM.
/// </summary>
public class RateSourceException : CurrencyConverterException
{
    public RateSourceException(string message) : base(message) { }
    public RateSourceException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Источник курсов BNM недоступен (сетевая ошибка, HTTP-ошибка, обрыв соединения или таймаут).
/// </summary>
public class RateSourceUnavailableException : RateSourceException
{
    public RateSourceUnavailableException(string message) : base(message) { }
    public RateSourceUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Ошибка формата ответа источника (невалидный XML, отсутствующая дата, неположительный номинал/курс, дубликаты).
/// </summary>
public class RateSourceFormatException : RateSourceException
{
    public RateSourceFormatException(string message) : base(message) { }
    public RateSourceFormatException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Источник исчерпал окно поиска (до 31 дня назад) и не вернул опубликованных курсов.
/// </summary>
public class RateSourceExhaustedException : RateSourceException
{
    public DateOnly RequestedDate { get; }
    public int DaysSearched { get; }

    public RateSourceExhaustedException(DateOnly requestedDate, int daysSearched)
        : base($"Не удалось найти опубликованный курс валют за период {daysSearched} календарных дней до {requestedDate:dd.MM.yyyy}.")
    {
        RequestedDate = requestedDate;
        DaysSearched = daysSearched;
    }
}

/// <summary>
/// Ошибка чтения или валидации кэша.
/// </summary>
public class CacheException : CurrencyConverterException
{
    public CacheException(string message) : base(message) { }
    public CacheException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Ошибка записи локального кэша на диск.
/// </summary>
public class CacheWriteException : CacheException
{
    public CacheWriteException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Ошибка арифметического переполнения при расчёте конвертации.
/// </summary>
public class CalculationOverflowException : CurrencyConverterException
{
    public CalculationOverflowException(string message) : base(message) { }
    public CalculationOverflowException(string message, Exception innerException) : base(message, innerException) { }
}
