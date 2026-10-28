using System.Globalization;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Validation;

/// <summary>
/// Валидатор числового ввода суммы конвертации.
/// </summary>
public static class AmountValidator
{
    public const decimal MaxAmount = Amount.MaxAllowedValue; // 1_000_000_000_000m
    public const int MaxDecimalPlaces = Amount.MaxDecimalPlaces; // 4

    public static Amount Validate(string? input, string fieldName = "amount")
    {
        if (TryValidate(input, out var amount, out var error, fieldName))
        {
            return amount!;
        }

        throw new ValidationException(error!);
    }

    public static bool TryValidate(
        string? input,
        out Amount? amount,
        out ValidationError? error,
        string fieldName = "amount")
    {
        amount = null;
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = new ValidationError("amount-empty", "validation.amount.empty", "Введите сумму для конвертации.", fieldName);
            return false;
        }

        string trimmed = input.Trim();

        // Проверка на экспоненциальную запись
        if (trimmed.Contains('e', StringComparison.OrdinalIgnoreCase))
        {
            error = new ValidationError("amount-exponent-forbidden", "validation.amount.exponent", "Экспоненциальная запись не допускается.", fieldName);
            return false;
        }

        // Проверка на пробелы или группировку разрядов
        if (trimmed.Any(char.IsWhiteSpace) || trimmed.Contains('_') || trimmed.Contains('\''))
        {
            error = new ValidationError("amount-grouping-forbidden", "validation.amount.grouping", "Использование пробелов и разделителей групп разрядов не допускается.", fieldName);
            return false;
        }

        // Проверка на отрицательный знак
        if (trimmed.StartsWith('-'))
        {
            error = new ValidationError("amount-negative", "validation.amount.negative", "Сумма должна быть положительным числом больше нуля.", fieldName);
            return false;
        }

        // Подсчёт десятичных разделителей (точка или запятая)
        int dotCount = trimmed.Count(c => c == '.');
        int commaCount = trimmed.Count(c => c == ',');
        int totalSeparators = dotCount + commaCount;

        if (totalSeparators > 1)
        {
            error = new ValidationError("amount-multiple-separators", "validation.amount.separators", "Некорректный формат суммы: обнаружено несколько десятичных разделителей.", fieldName);
            return false;
        }

        // Проверка допустимых символов: только цифры, плюс и один разделитель
        string charsToCheck = trimmed.StartsWith('+') ? trimmed[1..] : trimmed;
        if (charsToCheck.Length == 0)
        {
            error = new ValidationError("amount-invalid", "validation.amount.invalid", "Сумма должна содержать числовое значение.", fieldName);
            return false;
        }

        if (charsToCheck.Any(c => !char.IsAsciiDigit(c) && c != '.' && c != ','))
        {
            error = new ValidationError("amount-invalid-characters", "validation.amount.characters", "Сумма может содержать только цифры и десятичный разделитель (точку или запятую).", fieldName);
            return false;
        }

        // Определение количества знаков после запятой
        int decimalPlaces = 0;
        int sepIndex = charsToCheck.IndexOfAny(['.', ',']);
        if (sepIndex >= 0)
        {
            string fraction = charsToCheck[(sepIndex + 1)..];
            decimalPlaces = fraction.Length;

            if (decimalPlaces == 0)
            {
                error = new ValidationError("amount-incomplete-fraction", "validation.amount.incomplete_fraction", "Укажите дробную часть после десятичного разделителя.", fieldName);
                return false;
            }

            if (decimalPlaces > MaxDecimalPlaces)
            {
                error = new ValidationError("amount-too-many-decimals", "validation.amount.max_decimals", $"Количество знаков после десятичного разделителя не должно превышать {MaxDecimalPlaces}.", fieldName);
                return false;
            }
        }

        // Нормализация к точке для инвариантного парсинга
        string normalizedString = charsToCheck.Replace(',', '.');

        if (!decimal.TryParse(normalizedString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal parsedValue))
        {
            error = new ValidationError("amount-overflow", "validation.amount.overflow", "Значение суммы превышает допустимый предел точности.", fieldName);
            return false;
        }

        if (parsedValue <= 0)
        {
            error = new ValidationError("amount-zero-or-negative", "validation.amount.non_positive", "Сумма должна быть положительным числом больше нуля.", fieldName);
            return false;
        }

        if (parsedValue > MaxAmount)
        {
            error = new ValidationError("amount-exceeds-maximum", "validation.amount.max_exceeded", $"Сумма не должна превышать {MaxAmount:N0}.", fieldName);
            return false;
        }

        amount = new Amount(trimmed, parsedValue, decimalPlaces);
        return true;
    }
}
