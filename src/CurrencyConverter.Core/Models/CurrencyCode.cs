namespace CurrencyConverter.Core.Models;

/// <summary>
/// Представляет нормализованный код валюты, поддерживаемый приложением.
/// </summary>
public readonly struct CurrencyCode : IEquatable<CurrencyCode>, IComparable<CurrencyCode>
{
    private static readonly HashSet<string> SupportedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MDL", "EUR", "USD", "RON", "GBP"
    };

    public static readonly CurrencyCode Mdl = new("MDL");
    public static readonly CurrencyCode Eur = new("EUR");
    public static readonly CurrencyCode Usd = new("USD");
    public static readonly CurrencyCode Ron = new("RON");
    public static readonly CurrencyCode Gbp = new("GBP");

    public static readonly IReadOnlyCollection<CurrencyCode> SupportedCurrencies =
    [
        Mdl, Eur, Usd, Ron, Gbp
    ];

    public string Value { get; }

    public CurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Код валюты не может быть пустым.", nameof(value));
        }

        string normalized = value.Trim().ToUpperInvariant();
        if (!SupportedCodes.Contains(normalized))
        {
            throw new ArgumentException($"Неподдерживаемый код валюты: '{value}'.", nameof(value));
        }

        Value = normalized;
    }

    public static bool IsSupported(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return SupportedCodes.Contains(value.Trim());
    }

    public static bool TryParse(string? value, out CurrencyCode currencyCode)
    {
        if (IsSupported(value))
        {
            currencyCode = new CurrencyCode(value!);
            return true;
        }

        currencyCode = default;
        return false;
    }

    public static CurrencyCode Parse(string value) => new(value);

    public bool Equals(CurrencyCode other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is CurrencyCode other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);

    public int CompareTo(CurrencyCode other) => string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value ?? string.Empty;

    public static bool operator ==(CurrencyCode left, CurrencyCode right) => left.Equals(right);

    public static bool operator !=(CurrencyCode left, CurrencyCode right) => !left.Equals(right);

    public static implicit operator string(CurrencyCode code) => code.ToString();
}
