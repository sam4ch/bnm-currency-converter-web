# xUnit results — October 28, 2026

Actual execution: **162 tests passed, 0 failures, 0 skips**.

```powershell
dotnet test
```

| Group | Checks |
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

Tests use fixed clocks, XML fixtures, a fake HTTP handler, and isolated temporary
files; they do not contact BNM.

Local machine-readable log: `.tools/test-results/lab3.trx` (not published, like
other runtime data). Browser checks are documented separately in
[BROWSER_VALIDATION.md](BROWSER_VALIDATION.md).
