using CurrencyConverter.Core.Calculation;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using Xunit;

namespace CurrencyConverter.Tests.Unit;

public sealed class CurrencyCalculatorTests
{
    private readonly CurrencyCalculator _calculator = new();
    private static readonly DateOnly TestDate = new(2026, 10, 28);

    [Fact]
    public void Convert_DirectEurToMdl_CalculatesAccurately()
    {
        // 100 EUR по курсу 20.1760 = 2017.60 MDL
        var eur = new CurrencyRateEntry(CurrencyCode.Eur, nominal: 1m, rateValue: 20.1760m, TestDate);
        var mdl = new CurrencyRateEntry(CurrencyCode.Mdl, nominal: 1m, rateValue: 1m, TestDate);

        decimal result = _calculator.Convert(100m, eur, mdl);

        Assert.Equal(2017.60m, result);
    }

    [Fact]
    public void Convert_ReverseMdlToEur_CalculatesAccurately()
    {
        // 2017.60 MDL в EUR по курсу 20.1760 = 100.00 EUR
        var mdl = new CurrencyRateEntry(CurrencyCode.Mdl, nominal: 1m, rateValue: 1m, TestDate);
        var eur = new CurrencyRateEntry(CurrencyCode.Eur, nominal: 1m, rateValue: 20.1760m, TestDate);

        decimal result = _calculator.Convert(2017.60m, mdl, eur);

        Assert.Equal(100.00m, result);
    }

    [Fact]
    public void Convert_CrossCurrencyEurToUsd_CalculatesAccurately()
    {
        // 100 EUR (курс 20.00) в USD (курс 18.00): 100 * 20 / 18 = 111.1111... -> 111.11
        var eur = new CurrencyRateEntry(CurrencyCode.Eur, nominal: 1m, rateValue: 20.0000m, TestDate);
        var usd = new CurrencyRateEntry(CurrencyCode.Usd, nominal: 1m, rateValue: 18.0000m, TestDate);

        decimal result = _calculator.Convert(100m, eur, usd);

        Assert.Equal(111.11m, result);
    }

    [Fact]
    public void Convert_WithNominalGreaterThanOne_AccountsForNominalCorrectly()
    {
        // Валюта с Nominal = 100, Value = 5.25 MDL (эффективный курс = 0.0525 MDL за 1 единицу)
        // 1000 MDL -> 1000 / 0.0525 = 19047.619047... -> 19047.62
        var mdl = new CurrencyRateEntry(CurrencyCode.Mdl, nominal: 1m, rateValue: 1m, TestDate);
        var custom = new CurrencyRateEntry(CurrencyCode.Ron, nominal: 100m, rateValue: 5.2500m, TestDate);

        decimal result = _calculator.Convert(1000m, mdl, custom);

        Assert.Equal(19047.62m, result);
    }

    [Theory]
    // 1.005 при банковском ToEven округляется к чётному 1.00, но AwayFromZero обязан округлить к 1.01!
    [InlineData(1.005, 1.01)]
    // 2.025 при банковском ToEven округляется к 2.02, но AwayFromZero обязан округлить к 2.03!
    [InlineData(2.025, 2.03)]
    // 3.045 при банковском ToEven округляется к 3.04, но AwayFromZero обязан округлить к 3.05!
    [InlineData(3.045, 3.05)]
    public void Convert_ExactMidpoint_UsesAwayFromZeroNotBankersRounding(decimal exactMidpoint, decimal expected)
    {
        var unitMdl = new CurrencyRateEntry(CurrencyCode.Mdl, nominal: 1m, rateValue: 1m, TestDate);

        decimal result = _calculator.Convert(exactMidpoint, unitMdl, unitMdl);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var mdl = new CurrencyRateEntry(CurrencyCode.Mdl, nominal: 1m, rateValue: 1m, TestDate);
        var eur = new CurrencyRateEntry(CurrencyCode.Eur, nominal: 1m, rateValue: 20m, TestDate);

        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Convert(-10m, mdl, eur));
    }

    [Fact]
    public void Convert_DoesNotRoundRateBeforeFinalResult()
    {
        var source = new CurrencyRateEntry(CurrencyCode.Eur, 1m, 1.2345m, TestDate);
        var target = new CurrencyRateEntry(CurrencyCode.Mdl, 1m, 1m, TestDate);
        Assert.Equal(123.45m, _calculator.Convert(100m, source, target));
    }

    [Fact]
    public void Convert_ArithmeticOverflow_ThrowsCalculationOverflowException()
    {
        var mdl = new CurrencyRateEntry(CurrencyCode.Mdl, nominal: 1m, rateValue: decimal.MaxValue, TestDate);
        var tiny = new CurrencyRateEntry(CurrencyCode.Eur, nominal: 1000000m, rateValue: 0.0000001m, TestDate);

        Assert.Throws<CalculationOverflowException>(() => _calculator.Convert(decimal.MaxValue, mdl, tiny));
    }
}
