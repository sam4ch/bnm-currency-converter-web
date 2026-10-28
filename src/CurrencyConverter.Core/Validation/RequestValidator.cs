using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Validation;

/// <summary>
/// Реализация сервиса валидации запросов пользователя.
/// </summary>
public sealed class RequestValidator : IValidationService
{
    private readonly IClock _clock;

    public RequestValidator(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Amount ValidateAmount(string? amountText)
    {
        return AmountValidator.Validate(amountText);
    }

    public bool TryValidateAmount(string? amountText, out Amount? amount, out ValidationError? error)
    {
        return AmountValidator.TryValidate(amountText, out amount, out error);
    }

    public void ValidateDate(DateOnly date)
    {
        if (date == default)
        {
            throw new ValidationException(new ValidationError(
                "date-empty",
                "validation.date.empty",
                "Дата курса должна быть указана.",
                "selectedDate"));
        }

        DateOnly todayInChisinau = _clock.TodayInChisinau;
        if (date > todayInChisinau)
        {
            throw new ValidationException(new ValidationError(
                "date-future",
                "validation.date.future",
                $"Дата курса ({date:dd.MM.yyyy}) не может быть в будущем относительно текущего времени Кишинёва ({todayInChisinau:dd.MM.yyyy}).",
                "selectedDate"));
        }
    }

    public CurrencyCode ValidateCurrency(string? currencyText, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(currencyText))
        {
            throw new ValidationException(new ValidationError(
                $"{fieldName}-empty",
                $"validation.{fieldName}.empty",
                $"Валюта в поле '{fieldName}' должна быть указана.",
                fieldName));
        }

        if (!CurrencyCode.TryParse(currencyText, out var code))
        {
            throw new ValidationException(new ValidationError(
                $"{fieldName}-unsupported",
                $"validation.{fieldName}.unsupported",
                $"Неподдерживаемая валюта: '{currencyText}'. Допустимые валюты: MDL, EUR, USD, RON, GBP.",
                fieldName));
        }

        return code;
    }

    public ConversionRequest ValidateRequest(
        string? amountText,
        string? sourceCurrencyText,
        string? targetCurrencyText,
        DateOnly date,
        bool cacheFallbackApproved = false)
    {
        var errors = new List<ValidationError>();

        if (!AmountValidator.TryValidate(amountText, out var amount, out var amountError))
        {
            errors.Add(amountError!);
        }

        CurrencyCode sourceCode = default;
        try
        {
            sourceCode = ValidateCurrency(sourceCurrencyText, "sourceCurrency");
        }
        catch (ValidationException ex)
        {
            errors.AddRange(ex.Errors);
        }

        CurrencyCode targetCode = default;
        try
        {
            targetCode = ValidateCurrency(targetCurrencyText, "targetCurrency");
        }
        catch (ValidationException ex)
        {
            errors.AddRange(ex.Errors);
        }

        try
        {
            ValidateDate(date);
        }
        catch (ValidationException ex)
        {
            errors.AddRange(ex.Errors);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return new ConversionRequest(
            amount!,
            sourceCode,
            targetCode,
            date,
            cacheFallbackApproved);
    }
}
