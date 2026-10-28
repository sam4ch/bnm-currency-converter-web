using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Core.Validation;
using CurrencyConverter.Tests.Helpers;
using Xunit;

namespace CurrencyConverter.Tests.Unit;

public sealed class RequestValidatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 28);
    private readonly FixedClock _clock = new(Today);
    private readonly RequestValidator _validator;

    public RequestValidatorTests()
    {
        _validator = new RequestValidator(_clock);
    }

    [Theory]
    [InlineData("MDL")]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("RON")]
    [InlineData("GBP")]
    [InlineData("eur")]
    [InlineData("usd")]
    public void ValidateCurrency_SupportedCurrencies_ReturnsParsedCode(string codeStr)
    {
        var code = _validator.ValidateCurrency(codeStr, "sourceCurrency");
        Assert.True(CurrencyCode.IsSupported(code.Value));
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("RUB")]
    [InlineData("XYZ")]
    public void ValidateCurrency_UnsupportedCurrency_ThrowsValidationException(string codeStr)
    {
        var ex = Assert.Throws<ValidationException>(() => _validator.ValidateCurrency(codeStr, "sourceCurrency"));
        Assert.Contains(ex.Errors, e => e.Code == "sourceCurrency-unsupported");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCurrency_EmptyCurrency_ThrowsValidationException(string? codeStr)
    {
        var ex = Assert.Throws<ValidationException>(() => _validator.ValidateCurrency(codeStr, "sourceCurrency"));
        Assert.Contains(ex.Errors, e => e.Code == "sourceCurrency-empty");
    }

    [Fact]
    public void ValidateDate_TodayInChisinau_Succeeds()
    {
        // Дата сегодня по времени Кишинёва должна быть валидной
        _validator.ValidateDate(Today);
    }

    [Fact]
    public void ValidateDate_PastDate_Succeeds()
    {
        _validator.ValidateDate(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void ValidateDate_FutureDate_ThrowsValidationException()
    {
        var futureDate = Today.AddDays(1); // 29.10.2026
        var ex = Assert.Throws<ValidationException>(() => _validator.ValidateDate(futureDate));
        Assert.Contains(ex.Errors, e => e.Code == "date-future");
    }

    [Fact]
    public void ValidateDate_DefaultEmptyDate_ThrowsValidationException()
    {
        var ex = Assert.Throws<ValidationException>(() => _validator.ValidateDate(default));
        Assert.Contains(ex.Errors, e => e.Code == "date-empty");
    }

    [Fact]
    public void ValidateRequest_ValidParameters_ReturnsConversionRequest()
    {
        var req = _validator.ValidateRequest("100.50", "EUR", "MDL", Today, cacheFallbackApproved: false);

        Assert.NotNull(req);
        Assert.Equal(100.50m, req.Amount.Value);
        Assert.Equal(CurrencyCode.Eur, req.SourceCurrency);
        Assert.Equal(CurrencyCode.Mdl, req.TargetCurrency);
        Assert.Equal(Today, req.RequestedDate);
        Assert.False(req.CacheFallbackApproved);
    }

    [Fact]
    public void ValidateRequest_MultipleInvalidFields_AggregatesAllErrors()
    {
        // Невалидная сумма, неподдерживаемая исходная валюта, будущая дата
        var ex = Assert.Throws<ValidationException>(() =>
            _validator.ValidateRequest("abc", "RUB", "MDL", Today.AddDays(5)));

        Assert.True(ex.Errors.Count >= 3);
        Assert.Contains(ex.Errors, e => e.Code == "amount-invalid-characters");
        Assert.Contains(ex.Errors, e => e.Code == "sourceCurrency-unsupported");
        Assert.Contains(ex.Errors, e => e.Code == "date-future");
    }
}
