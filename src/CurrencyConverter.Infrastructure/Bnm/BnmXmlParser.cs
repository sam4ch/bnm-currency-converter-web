using System.Globalization;
using System.Xml.Linq;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Infrastructure.Bnm;

/// <summary>
/// Парсер официального XML-документа курсов валют BNM (ValCurs).
/// </summary>
public static class BnmXmlParser
{
    public static RateSet? Parse(string xmlContent, string sourceUrl, DateTimeOffset retrievedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            throw new RateSourceFormatException("Получен пустой XML-ответ от источника BNM.");
        }

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xmlContent);
        }
        catch (Exception ex)
        {
            throw new RateSourceFormatException("Некорректный синтаксис XML в ответе BNM.", ex);
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

        if (!DateOnly.TryParseExact(dateAttr.Value.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly publicationDate))
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

            if (!decimal.TryParse(nominalElem.Value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nominal))
            {
                throw new RateSourceFormatException($"Некорректный числовой формат Nominal для валюты '{rawCode}': '{nominalElem.Value}'.");
            }

            if (nominal <= 0)
            {
                throw new RateSourceFormatException($"Номинал для валюты '{rawCode}' должен быть строго больше нуля, получено: {nominal}.");
            }

            if (valueElem == null || string.IsNullOrWhiteSpace(valueElem.Value))
            {
                throw new RateSourceFormatException($"В записи валюты '{rawCode}' отсутствует элемент Value.");
            }

            if (!decimal.TryParse(valueElem.Value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rateValue))
            {
                throw new RateSourceFormatException($"Некорректный числовой формат Value для валюты '{rawCode}': '{valueElem.Value}'.");
            }

            if (rateValue <= 0)
            {
                throw new RateSourceFormatException($"Курс для валюты '{rawCode}' должен быть строго больше нуля, получено: {rateValue}.");
            }

            // Дополнительные корректные валюты в XML не инвалидируют весь документ, но мы сохраняем только поддерживаемые
            if (CurrencyCode.IsSupported(rawCode))
            {
                var code = CurrencyCode.Parse(rawCode);
                supportedEntries.Add(new CurrencyRateEntry(code, nominal, rateValue, publicationDate));
            }
        }

        // Базовая валюта MDL добавляется ТОЛЬКО после проверки, что ответ не пустой и содержит опубликованные курсы
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
}
