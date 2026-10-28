# Результаты xUnit — 28.10.2026

Фактически выполнено: **162 тестов прошли, 0 ошибок, 0 пропусков**.

```powershell
dotnet test
```

| Группа | Проверок |
| --- | ---: |
| AmountValidatorTests | 48 |
| BnmRateClientTests | 12 |
| BnmXmlParserRegressionTests | 7 |
| BnmXmlParserTests | 17 |
| ChisinauClockTests | 4 |
| ConversionServiceTests | 12 |
| CurrencyCalculatorTests | 10 |
| CurrencyCodeRegressionTests | 1 |
| JsonCacheRegressionTests | 11 |
| JsonCacheStoreTests | 12 |
| RateLookupTests | 9 |
| RequestValidatorTests | 19 |

Тесты используют фиксированные часы, XML-фикстуры, поддельный HTTP-обработчик и собственные временные файлы; обращений к BNM нет.

Локальный машинный протокол: `.tools/test-results/lab3.trx` (не публикуется, как и runtime-данные). Браузерные проверки описаны отдельно в [BROWSER_VALIDATION.md](BROWSER_VALIDATION.md).
