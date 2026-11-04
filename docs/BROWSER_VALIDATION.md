# Browser interface validation — October 28, 2026

The assistant tested the app through Playwright in installed Opera GX 138.0.6044.45,
using a separate temporary profile. These are browser integration checks and
visual screenshot reviews, separate from xUnit. User acceptance on a physical
phone was not performed. Edge automation was blocked by administrator policy;
that policy was not changed.

Build command: `dotnet build CurrencyConverter.sln --no-restore` succeeded,
with 0 warnings and 0 errors. The application actually ran over local HTTP.
UI labels in the table are described by their English meanings.

| Scenario | Result |
| --- | --- |
| Empty amount, letters, zero, minus, exponent, excess decimal places/limit, `+1`, `.5`, `1.` | Button disabled |
| Dot/comma, 0.0001, and the maximum amount | Valid input accepted |
| Future date | Button disabled |
| Currency swap and full Tab order: amount, source currency, swap, target currency, date, button | Working |
| EUR → EUR, 123.4567 | Exact value, rate-not-required status, no rate metadata |
| Form submission | Button disabled; previous result hidden |
| Live BNM, 100 EUR → MDL, 28.10.2026 | 2017.60 MDL; actual/selected dates, link, and timestamp shown |
| JavaScript bypass, direct submission of `abc` | Server rejects input; no result |
| Process restart with the same JSON and unavailable source | Cache offered without a result first; 2017.60 MDL only after explicit confirmation |
| Date 27.10.2026 with cache dated 28.10.2026 | Newer cache not offered |
| Unavailable source without a cache file | Clear error; no fabricated result |
| USD → USD, 10,1234 offline without a cache | Exact amount preserved |
| Widths of 1280 and 390 px | Screenshots reviewed; no horizontal overflow |
| JavaScript pageerror | No errors |

The offline check configured only its own test process
(`Bnm__BaseUrl=http://127.0.0.1:9/?date=`); system networking was not disabled.
Cache checks used isolated test files in ignored `.tools/`.
Full local logs: `.tools/browser-qa/1793197898520/results.json`.

An actual defect was found: static `CurrencyCode.Mdl` was initialized before
`SupportedCodes`, causing `TypeInitializationException` on the first conversion.
Antigravity corrected the order, and the repeated run passed.
Same-currency rounding, JavaScript future-date validation, and hiding the previous
result during a request were also fixed.

![Desktop interface](screenshots/desktop.png)

![Mobile interface](screenshots/mobile.png)

Additional final run: **10/10 scenarios**. Verified the full keyboard-focus order
and that retrying without consent does not apply the cache. Source recovery was
checked using a local HTTP source with fixed XML: first 503, then 200 in the same
form; the error disappeared and the result was correct. This controlled recovery
check is separate from the live BNM request.
