namespace CurrencyConverter.Tests.Fixtures;

/// <summary>
/// Фикстуры XML-ответов Национального банка Молдовы для детерминированного тестирования.
/// </summary>
public static class BnmXmlFixtures
{
    public const string ValidBnmXml20261028 = """
        <?xml version="1.0" encoding="utf-8"?>
        <ValCurs Date="28.10.2026" name="Official exchange rates">
            <Valute ID="47">
                <NumCode>978</NumCode>
                <CharCode>EUR</CharCode>
                <Nominal>1</Nominal>
                <Name>Euro</Name>
                <Value>20.1760</Value>
            </Valute>
            <Valute ID="44">
                <NumCode>840</NumCode>
                <CharCode>USD</CharCode>
                <Nominal>1</Nominal>
                <Name>US Dollar</Name>
                <Value>18.5430</Value>
            </Valute>
            <Valute ID="49">
                <NumCode>946</NumCode>
                <CharCode>RON</CharCode>
                <Nominal>1</Nominal>
                <Name>Romanian Leu</Name>
                <Value>4.0560</Value>
            </Valute>
            <Valute ID="46">
                <NumCode>826</NumCode>
                <CharCode>GBP</CharCode>
                <Nominal>1</Nominal>
                <Name>Pound Sterling</Name>
                <Value>24.1250</Value>
            </Valute>
            <Valute ID="50">
                <NumCode>756</NumCode>
                <CharCode>CHF</CharCode>
                <Nominal>1</Nominal>
                <Name>Swiss Franc</Name>
                <Value>21.3400</Value>
            </Valute>
        </ValCurs>
        """;

    public const string ValidWithNominalGreaterThanOne = """
        <?xml version="1.0" encoding="utf-8"?>
        <ValCurs Date="28.10.2026" name="Official exchange rates">
            <Valute ID="47">
                <NumCode>978</NumCode>
                <CharCode>EUR</CharCode>
                <Nominal>100</Nominal>
                <Name>Euro</Name>
                <Value>2000.0000</Value>
            </Valute>
            <Valute ID="55">
                <NumCode>348</NumCode>
                <CharCode>HUF</CharCode>
                <Nominal>100</Nominal>
                <Name>Hungarian Forint</Name>
                <Value>5.2500</Value>
            </Valute>
            <Valute ID="44">
                <NumCode>840</NumCode>
                <CharCode>USD</CharCode>
                <Nominal>1</Nominal>
                <Name>US Dollar</Name>
                <Value>18.0000</Value>
            </Valute>
        </ValCurs>
        """;

    public const string EmptyValCursNoValutes = """
        <?xml version="1.0" encoding="utf-8"?>
        <ValCurs Date="28.10.2026" name="Official exchange rates">
        </ValCurs>
        """;

    public const string MalformedXml = """
        <ValCurs Date="28.10.2026">
            <Valute><CharCode>EUR</CharCode>
        """;

    public const string MissingDateAttribute = """
        <ValCurs name="Official exchange rates">
            <Valute>
                <CharCode>EUR</CharCode>
                <Nominal>1</Nominal>
                <Value>20.0000</Value>
            </Valute>
        </ValCurs>
        """;

    public const string DtdForbiddenPayload = """
        <?xml version="1.0"?>
        <!DOCTYPE ValCurs [
            <!ENTITY xxe SYSTEM "file:///etc/passwd">
        ]>
        <ValCurs Date="28.10.2026">
            <Valute>
                <CharCode>&xxe;</CharCode>
                <Nominal>1</Nominal>
                <Value>20.0000</Value>
            </Valute>
        </ValCurs>
        """;

    public const string DuplicateCurrencyCode = """
        <ValCurs Date="28.10.2026">
            <Valute>
                <CharCode>EUR</CharCode>
                <Nominal>1</Nominal>
                <Value>20.0000</Value>
            </Valute>
            <Valute>
                <CharCode>EUR</CharCode>
                <Nominal>1</Nominal>
                <Value>20.5000</Value>
            </Valute>
        </ValCurs>
        """;

    public const string NegativeNominal = """
        <ValCurs Date="28.10.2026">
            <Valute>
                <CharCode>EUR</CharCode>
                <Nominal>-1</Nominal>
                <Value>20.0000</Value>
            </Valute>
        </ValCurs>
        """;

    public const string ZeroRateValue = """
        <ValCurs Date="28.10.2026">
            <Valute>
                <CharCode>EUR</CharCode>
                <Nominal>1</Nominal>
                <Value>0.0000</Value>
            </Valute>
        </ValCurs>
        """;

    public const string CommaInRateValue = """
        <ValCurs Date="28.10.2026">
            <Valute>
                <CharCode>EUR</CharCode>
                <Nominal>1</Nominal>
                <Value>20,1760</Value>
            </Valute>
        </ValCurs>
        """;

    public const string ExponentInRateValue = """
        <ValCurs Date="28.10.2026">
            <Valute>
                <CharCode>EUR</CharCode>
                <Nominal>1</Nominal>
                <Value>2.0176e1</Value>
            </Valute>
        </ValCurs>
        """;

    public const string OnlyUnsupportedCurrencies = """
        <ValCurs Date="28.10.2026">
            <Valute>
                <CharCode>JPY</CharCode>
                <Nominal>100</Nominal>
                <Value>12.5000</Value>
            </Valute>
            <Valute>
                <CharCode>CAD</CharCode>
                <Nominal>1</Nominal>
                <Value>13.4000</Value>
            </Valute>
        </ValCurs>
        """;

    public const string OnlyMdlWithInvalidValue = """
        <ValCurs Date="28.10.2026">
            <Valute>
                <CharCode>MDL</CharCode>
                <Nominal>1</Nominal>
                <Value>19.5000</Value>
            </Valute>
        </ValCurs>
        """;
}
