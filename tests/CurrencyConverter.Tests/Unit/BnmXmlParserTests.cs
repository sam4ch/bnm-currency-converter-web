using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Infrastructure.Bnm;
using CurrencyConverter.Tests.Fixtures;
using Xunit;

namespace CurrencyConverter.Tests.Unit;

public sealed class BnmXmlParserTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2026, 10, 28, 10, 0, 0, TimeSpan.Zero);
    private const string TestSourceUrl = "https://www.bnm.md/test";

    [Fact]
    public void Parse_ValidXml_ReturnsRateSetWithSupportedCurrenciesAndMdl()
    {
        // Act
        var rateSet = BnmXmlParser.Parse(BnmXmlFixtures.ValidBnmXml20261028, TestSourceUrl, TestTimestamp);

        // Assert
        Assert.NotNull(rateSet);
        Assert.Equal(new DateOnly(2026, 10, 28), rateSet.PublicationDate);
        Assert.Equal(TestSourceUrl, rateSet.SourceUrl);
        Assert.Equal(TestTimestamp, rateSet.RetrievedAtUtc);

        // Проверяем наличие всех ключевых поддерживаемых валют
        Assert.True(rateSet.ContainsCurrency(CurrencyCode.Eur));
        Assert.True(rateSet.ContainsCurrency(CurrencyCode.Usd));
        Assert.True(rateSet.ContainsCurrency(CurrencyCode.Ron));
        Assert.True(rateSet.ContainsCurrency(CurrencyCode.Gbp));

        // Базовая валюта MDL автоматически добавлена с курсом и номиналом 1
        var mdl = rateSet.GetEntry(CurrencyCode.Mdl);
        Assert.NotNull(mdl);
        Assert.Equal(1m, mdl.Nominal);
        Assert.Equal(1m, mdl.RateValue);
        Assert.Equal(1m, mdl.EffectiveMdlPerUnit);

        var eur = rateSet.GetEntry(CurrencyCode.Eur);
        Assert.NotNull(eur);
        Assert.Equal(1m, eur.Nominal);
        Assert.Equal(20.1760m, eur.RateValue);
        Assert.Equal(20.1760m, eur.EffectiveMdlPerUnit);
    }

    [Fact]
    public void Parse_XmlWithNominalGreaterThanOne_ComputesEffectiveMdlPerUnitCorrectly()
    {
        // Act
        var rateSet = BnmXmlParser.Parse(BnmXmlFixtures.ValidWithNominalGreaterThanOne, TestSourceUrl, TestTimestamp);

        // Assert
        Assert.NotNull(rateSet);
        var eur = rateSet.GetEntry(CurrencyCode.Eur);
        Assert.NotNull(eur);
        Assert.Equal(2000.0000m, eur.RateValue);
        Assert.Equal(100m, eur.Nominal);
        Assert.Equal(20m, eur.EffectiveMdlPerUnit);

        // MDL присутствует
        var mdl = rateSet.GetEntry(CurrencyCode.Mdl);
        Assert.NotNull(mdl);
        Assert.Equal(1m, mdl.EffectiveMdlPerUnit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void Parse_EmptyOrWhitespaceBody_ReturnsNull(string emptyBody)
    {
        // Act
        var result = BnmXmlParser.Parse(emptyBody, TestSourceUrl, TestTimestamp);

        // Assert: пустой ответ означает отсутствие публикации на дату
        Assert.Null(result);
    }

    [Fact]
    public void Parse_ValCursWithoutValuteChildren_ReturnsNull()
    {
        // Act
        var result = BnmXmlParser.Parse(BnmXmlFixtures.EmptyValCursNoValutes, TestSourceUrl, TestTimestamp);

        // Assert: 0 записей курсов означает отсутствие публикации
        Assert.Null(result);
    }

    [Fact]
    public void Parse_MalformedXml_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.MalformedXml, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_MissingDateAttribute_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.MissingDateAttribute, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_DtdPayload_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.DtdForbiddenPayload, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_DuplicateCurrencyCode_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.DuplicateCurrencyCode, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_NegativeNominal_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.NegativeNominal, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_ZeroRateValue_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.ZeroRateValue, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_CommaInRateValue_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.CommaInRateValue, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_ExponentInRateValue_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.ExponentInRateValue, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_OnlyUnsupportedCurrencies_ThrowsRateSourceFormatException()
    {
        // Не должен превращать документ без поддерживаемых валют в фиктивный набор из одного MDL
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.OnlyUnsupportedCurrencies, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_MdlWithInvalidValueInXml_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.OnlyMdlWithInvalidValue, TestSourceUrl, TestTimestamp));
    }

    [Fact]
    public void Parse_DefaultTimestamp_ThrowsRateSourceFormatException()
    {
        Assert.Throws<RateSourceFormatException>(() =>
            BnmXmlParser.Parse(BnmXmlFixtures.ValidBnmXml20261028, TestSourceUrl, default));
    }
}
