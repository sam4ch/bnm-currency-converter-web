using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Infrastructure.Bnm;

/// <summary>
/// Безопасный парсер официального XML-документа курсов валют BNM (ValCurs).
/// </summary>
public static class BnmXmlParser
{
    public const int MaxAllowedXmlLength = 2_000_000;

    public static RateSet? Parse(string xmlContent, string sourceUrl, DateTimeOffset retrievedAtUtc)
    {
        // Пустой HTTP body — отсутствие публикации
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return null;
        }

        if (retrievedAtUtc == default)
        {
            throw new RateSourceFormatException("Некорректная метка времени получения курсов.");
        }

        if (xmlContent.Length > MaxAllowedXmlLength)
        {
            throw new RateSourceFormatException("Размер XML-документа превышает допустимый предел безопасности (2 МБ).");
        }

        XDocument doc;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1_000_000
            };

            using var stringReader = new StringReader(xmlContent);
            using var xmlReader = XmlReader.Create(stringReader, settings);
            doc = XDocument.Load(xmlReader);
        }
        catch (XmlException ex)
        {
            throw new RateSourceFormatException("Некорректный синтаксис XML в ответе BNM.", ex);
        }
        catch (Exception ex)
        {
            throw new RateSourceFormatException("Ошибка при чтении XML-ответа BNM.", ex);
        }

        XElement? root = doc.Root;
        if (root == null || !string.Equals(root.Name.LocalName, "ValCurs", StringComparison.OrdinalIgnoreCase))
        {
            throw new RateSourceFormatException("Корневой элемент XML должен быть 'ValCurs'.");
        }

        XAttribute? dateAttr = root.Attribute("Date");
        if (dateAttr == null || string.IsNullOrWhiteSpace(dateAttr.Value))
        {
            throw new RateSourceFormatException("В элементе ValCurs отсутствует обязательный атрибут 'Date'.");
        }

        if (!DateOnly.TryParseExact(dateAttr.Value.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly publicationDate) || publicationDate == default)
        {
            throw new RateSourceFormatException($"Некорректный формат даты в атрибуте Date: '{dateAttr.Value}'. Ожидается формат DD.MM.YYYY.");
        }

        var valuteElements = root.Elements().Where(e => string.Equals(e.Name.LocalName, "Valute", StringComparison.OrdinalIgnoreCase)).ToList();

        // Если записей Valute нет вообще — курсы на указанную дату не публиковались
        if (valuteElements.Count == 0)
        {
            return null;
        }

        var seenCharCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var supportedEntries = new List<CurrencyRateEntry>();

        foreach (var valute in valuteElements)
        {
            XElement? charCodeElem = valute.Element("CharCode");
            XElement? nominalElem = valute.Element("Nominal");
            XElement? valueElem = valute.Element("Value");

            if (charCodeElem == null || string.IsNullOrWhiteSpace(charCodeElem.Value))
            {
                throw new RateSourceFormatException("В записи Valute отсутствует обязательный элемент CharCode.");
            }

            string rawCode = charCodeElem.Value.Trim().ToUpperInvariant();

            // Проверка на дубликаты кодов в документе
            if (!seenCharCodes.Add(rawCode))
            {
                throw new RateSourceFormatException($"Обнаружен дубликат валюты с кодом '{rawCode}' в XML-ответе BNM.");
            }

            if (nominalElem == null || string.IsNullOrWhiteSpace(nominalElem.Value))
            {
                throw new RateSourceFormatException($"В записи валюты '{rawCode}' отсутствует элемент Nominal.");
            }

            if (!TryParseXmlDecimal(nominalElem.Value, out decimal nominal))
            {
                throw new RateSourceFormatException($"Некорректный числовой формат Nominal для валюты '{rawCode}': '{nominalElem.Value}'. Ожидается десятичное число с точкой без группировки.");
            }

            if (nominal <= 0)
            {
                throw new RateSourceFormatException($"Номинал для валюты '{rawCode}' должен быть строго больше нуля, получено: {nominal}.");
            }

            if (valueElem == null || string.IsNullOrWhiteSpace(valueElem.Value))
            {
                throw new RateSourceFormatException($"В записи валюты '{rawCode}' отсутствует элемент Value.");
            }

            if (!TryParseXmlDecimal(valueElem.Value, out decimal rateValue))
            {
                throw new RateSourceFormatException($"Некорректный числовой формат Value для валюты '{rawCode}': '{valueElem.Value}'. Ожидается десятичное число с точкой без группировки.");
            }

            if (rateValue <= 0)
            {
                throw new RateSourceFormatException($"Курс для валюты '{rawCode}' должен быть строго больше нуля, получено: {rateValue}.");
            }

            // Дополнительные корректные валюты в XML не инвалидируют весь документ, но мы отбираем поддерживаемые
            if (CurrencyCode.IsSupported(rawCode))
            {
                var code = CurrencyCode.Parse(rawCode);

                if (code == CurrencyCode.Mdl && (nominal != 1m || rateValue != 1m))
                {
                    throw new RateSourceFormatException($"Некорректные курс или номинал базовой валюты MDL в XML-ответе (курс: {rateValue}, номинал: {nominal}).");
                }

                supportedEntries.Add(new CurrencyRateEntry(code, nominal, rateValue, publicationDate));
            }
        }

        // Если в ответе не найдено ни одной поддерживаемой иностранной валюты — не создаём фиктивный набор из одного MDL
        if (!supportedEntries.Any(e => e.Code != CurrencyCode.Mdl))
        {
            throw new RateSourceFormatException("В ответе BNM не найдено ни одной поддерживаемой иностранной валюты (EUR, USD, RON, GBP).");
        }

        // Базовая валюта MDL добавляется ТОЛЬКО после подтверждения наличия опубликованных валют
        if (!supportedEntries.Any(e => e.Code == CurrencyCode.Mdl))
        {
            supportedEntries.Add(new CurrencyRateEntry(
                CurrencyCode.Mdl,
                nominal: 1m,
                rateValue: 1m,
                publicationDate: publicationDate));
        }

        return new RateSet(publicationDate, supportedEntries, sourceUrl, retrievedAtUtc);
    }

    private static bool TryParseXmlDecimal(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string trimmed = raw.Trim();

        // Проверка: только цифры и не более одной десятичной точки, без знаков +/-, пробелов, экспоненты и запятых
        if (!trimmed.All(c => char.IsAsciiDigit(c) || c == '.'))
        {
            return false;
        }

        if (trimmed.StartsWith('.') || trimmed.EndsWith('.'))
        {
            return false;
        }

        int dotCount = trimmed.Count(c => c == '.');
        if (dotCount > 1)
        {
            return false;
        }

        return decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }
}
