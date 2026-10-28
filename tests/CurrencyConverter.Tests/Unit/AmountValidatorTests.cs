using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Validation;
using Xunit;

namespace CurrencyConverter.Tests.Unit;

public sealed class AmountValidatorTests
{
    [Fact]
    public void Validate_NumberLargerThanDecimalRange_ReturnsValidationError()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            AmountValidator.Validate("79228162514264337593543950336"));
        Assert.Contains(exception.Errors, error => error.Code == "amount-overflow");
    }

    [Theory]
    [InlineData("10.50", 10.50, 2)]
    [InlineData("10,50", 10.50, 2)]
    [InlineData("100", 100.0, 0)]
    [InlineData("0.0001", 0.0001, 4)]
    [InlineData("0,0001", 0.0001, 4)]
    [InlineData("123.4567", 123.4567, 4)]
    [InlineData("1000000000000", 1000000000000.0, 0)]
    [InlineData("  10.50  ", 10.50, 2)] // внешние пробелы обрезаются
    public void Validate_ValidInputs_ReturnsAmountObject(string input, decimal expectedValue, int expectedDecimals)
    {
        var amount = AmountValidator.Validate(input);

        Assert.NotNull(amount);
        Assert.Equal(expectedValue, amount.Value);
        Assert.Equal(expectedDecimals, amount.DecimalPlaces);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrEmpty_ThrowsValidationException(string? input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-empty");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.0")]
    [InlineData("0,00")]
    [InlineData("0.0000")]
    public void Validate_Zero_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-zero-or-negative");
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("-0.01")]
    [InlineData("-100,50")]
    public void Validate_Negative_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-negative");
    }

    [Theory]
    [InlineData("+10")]
    [InlineData("+0.50")]
    public void Validate_LeadingPlus_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-leading-plus");
    }

    [Theory]
    [InlineData(".5")]
    [InlineData(",5")]
    [InlineData(".0001")]
    public void Validate_MissingIntegerPart_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-missing-integer-part");
    }

    [Theory]
    [InlineData("1.")]
    [InlineData("1,")]
    [InlineData("100.")]
    public void Validate_MissingFractionalPart_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-incomplete-fraction");
    }

    [Theory]
    [InlineData("1 000")]
    [InlineData("10 000.50")]
    [InlineData("1_000")]
    [InlineData("1'000")]
    public void Validate_InnerWhitespaceOrGrouping_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-grouping-forbidden");
    }

    [Theory]
    [InlineData("1e5")]
    [InlineData("2E-3")]
    [InlineData("1.5e2")]
    public void Validate_ExponentialNotation_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-exponent-forbidden");
    }

    [Theory]
    [InlineData("1.2.3")]
    [InlineData("1,2,3")]
    [InlineData("1.2,3")]
    [InlineData("1,2.3")]
    public void Validate_MultipleSeparators_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-multiple-separators");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("10a")]
    [InlineData("$50")]
    [InlineData("10#5")]
    public void Validate_LettersOrSpecialCharacters_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-invalid-characters");
    }

    [Theory]
    [InlineData("10.12345")] // 5 знаков
    [InlineData("0.00001")] // 5 знаков
    [InlineData("1,123456")] // 6 знаков
    public void Validate_MoreThanFourDecimalPlaces_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-too-many-decimals");
    }

    [Theory]
    [InlineData("1000000000001")]
    [InlineData("1000000000000.01")]
    [InlineData("999999999999999")]
    public void Validate_ExceedingMaximumAmount_ThrowsValidationException(string input)
    {
        var ex = Assert.Throws<ValidationException>(() => AmountValidator.Validate(input));
        Assert.Contains(ex.Errors, e => e.Code == "amount-exceeds-maximum");
    }
}
