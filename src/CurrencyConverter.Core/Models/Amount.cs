namespace CurrencyConverter.Core.Models;

/// <summary>
/// Представляет валидное положительное числовое значение суммы конвертации.
/// </summary>
public sealed class Amount : IEquatable<Amount>
{
    public const decimal MaxAllowedValue = 1_000_000_000_000m;
    public const int MaxDecimalPlaces = 4;

    public string RawText { get; }
    public decimal Value { get; }
    public int DecimalPlaces { get; }

    public Amount(string rawText, decimal value, int decimalPlaces)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Сумма должна быть строго положительной.");
        }

        if (value > MaxAllowedValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Сумма не может превышать {MaxAllowedValue}.");
        }

        if (decimalPlaces < 0 || decimalPlaces > MaxDecimalPlaces)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), $"Количество знаков после запятой должно быть от 0 до {MaxDecimalPlaces}.");
        }

        RawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
        Value = value;
        DecimalPlaces = decimalPlaces;
    }

    public bool Equals(Amount? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as Amount);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static implicit operator decimal(Amount amount) => amount.Value;
}
