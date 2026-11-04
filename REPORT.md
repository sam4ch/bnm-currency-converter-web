# Lab 3 Report

> Lab 3: Currency Converter with ASP.NET Core Razor Pages (.NET 8).
> Status: T001–T027 implementation and application validation are complete.
> Test stage: `tests/currency-converter`, separate PR #3.
> Verified: 162/162 xUnit tests, 0 skips; 10/10 browser scenarios.

## 1. Data source and rationale

The National Bank of Moldova was selected for its official public XML feed,
absence of an API key, MDL-based rates, and relevance to the local context.
`Nominal` must be respected rather than treating every entry as a per-unit rate.
The bank publishes reference rates, not guaranteed transaction prices.
Source: https://www.bnm.md/en/official_exchange_rates.

## 2. Tools and models actually used

| Stage | Tool | Confirmed work |
| --- | --- | --- |
| PDF analysis and setup | Assistant in the current chat | Read the PDF, prepared the environment and initial prompts |
| Spec Kit | specify-cli 1.2.4 | Copilot initialization, PowerShell scripts, integration status: ok |
| Constitution | Copilot in VS Code 1.108.2 | Created and saved version 1.0.0 |
| Specification and design | Copilot + Spec Kit | Created spec, plan, research, data-model, contracts, and tasks; performed clarification and analysis |
| Final document review | Assistant in the current chat | Verified coverage of 29 FR / 9 SC / 27 tasks; clarified paths and created the manual checklist |
| Copilot model | Auto in the interface | Exact model selected in Auto mode was not established |
| Core/Infrastructure implementation (T001–T014) | Antigravity CLI 1.4.9, Gemini 3.8 Flash High (gemini-3.8-flash-high) | Created the .sln, Core/Infrastructure projects, Web scaffold, and test csproj; implemented T001–T014 |
| Stage 1 build validation | Orchestrator in the user's terminal | Ran `dotnet build CurrencyConverter.sln`: 0 errors, 0 warnings. Checked live BNM XML: HTTP 200, ValCurs Date=28.10.2026, 40 Valute entries; rates were not hardcoded |
| UI implementation and fixes (T015–T019) | Antigravity CLI 1.4.9, Gemini 3.8 Flash High (gemini-3.8-flash-high) | Created the complete Razor Pages UI (Index.cshtml, Index.cshtml.cs, site.css, site.js, favicon.svg, Layout) and fixed first-iteration findings |
| Opera GX browser tests (M001–M007) | Orchestrator in Opera GX | Checked all 9 browser scenarios; screenshots recorded in docs/BROWSER_VALIDATION.md. Implementation PR #2 merged |
| xUnit tests and regression fixes (T020–T027) | Antigravity CLI 1.4.9, Gemini 3.8 Flash High (gemini-3.8-flash-high) | Created T020–T027 suites in `tests/CurrencyConverter.Tests`, fixtures/helpers (BnmXmlFixtures, FixedClock, TestHttpMessageHandler, TestDirectoryScope), and fixed confirmed defects |
| dotnet test execution | Codex assistant in the terminal | 162 tests passed, 0 failures, 0 skips |

## 3. Implemented components and first build

The first generated solution and projects compiled without errors or warnings.
This confirmed the scaffold and project references. Runtime correctness was
checked separately: browser testing exposed a CurrencyCode static-initialization
error, and review found validation/cache issues. The final components are
described below; actual fixes are listed in section 4.

- **Architecture and contracts (T001–T004):** `CurrencyConverter.sln`, `Directory.Build.props`, `CurrencyConverter.Core`, `CurrencyConverter.Infrastructure`, `CurrencyConverter.Web`, and `CurrencyConverter.Tests`. Domain models: `Amount`, `CurrencyRateEntry`, `RateSet`, `RateDate`, `RateLookupContext`, `ConversionRequest`, `ConversionResult`, `VerifiedCacheSnapshot`. Calendar dates use `DateOnly`; timestamps use `DateTimeOffset`. CurrencyCode required a static-initialization-order fix, described in section 4.
- **Calculations and conversion (T007–T008):** `CurrencyCalculator` uses only `decimal` and the formula `amount * (ValueSource / NominalSource) / (ValueTarget / NominalTarget)`, with final rounding to 2 places using `MidpointRounding.AwayFromZero`. Same-currency conversion immediately returns the original amount without network calls, cache reads, or fabricated rate metadata.
- **Validation (T005–T006):** `AmountValidator` accepts a dot/comma and rejects exponent notation, grouping separators/spaces, letters, zero, negative numbers, overflow, amounts above 1,000,000,000,000, and more than 4 decimal places. `RequestValidator` checks dates in Chisinau time (`ChisinauClock` on Windows/Linux) and rejects future/impossible dates.
- **BNM XML parser and HTTP client (T010–T011):** `BnmXmlParser` validates the `ValCurs` structure, date, positive nominal/rate, and unique currency codes. Additional valid currencies do not break parsing; MDL is added only after a nonempty response is verified. `BnmRateClient` uses a 10-second timeout and maps network/HTTP failures to domain exceptions.
- **Historical lookup (T009):** `RateLookupService` checks the selected day and up to 31 previous days only when a publication is missing. Network or XML failure stops lookup immediately instead of repeating it 32 times.
- **Persistent cache and orchestration (T012–T014):** `JsonCacheStore` uses atomic replacement through a temporary file in the same directory and `SemaphoreSlim` synchronization; corrupt files are handled safely. `ConversionService` requires explicit cache consent (`CacheConfirmationRequired`), avoids another network request after confirmation, rejects a cache newer than the requested date, and reports cache-write failure separately without discarding a successful calculation.
- **Razor Pages UI (T015–T018):**
  - Russian-language converter page with cards, large inputs, and an indigo accent (`#4f46e5`).
  - Required fields have stable HTML IDs: `amount`, `sourceCurrency`, `targetCurrency`, `rateDate`, `convertButton`, `swapCurrencies`, `conversionResult`, `useCachedButton`.
  - The swap button exchanges source and target currencies in one click.
  - Client validation in `site.js` matches the server's `AmountValidator` grammar. Invalid/empty input and active requests disable submission, with a loading indicator.
  - Same-currency conversion shows the exact amount and a rate-not-required badge, without invented dates or sources.
  - If the network is unavailable and a suitable cache exists, the app offers saved rates (`id="useCachedButton"`); confirmation submits another POST without an unnecessary network call.
  - The page does not depend on external fonts, CDNs, jQuery, or Bootstrap. It uses local `site.css`, `site.js`, and vector `favicon.svg`.
- **Test infrastructure and fixtures (T020–T027):** `CurrencyConverter.Tests` contains deterministic components and fixtures without real network requests or external dependencies:
  - `FixedClock`: replaceable Chisinau date/UTC time without changing system clocks.
  - `TestHttpMessageHandler`: intercepts requests and simulates status codes, timeouts, and network exceptions.
  - `TestDirectoryScope`: isolated temporary cache directories per test, cleaned automatically, avoiding conflicts with working `App_Data`.
  - `BnmXmlFixtures`: strict XML fixtures for valid responses, different nominal values, empty ValCurs, duplicate codes, DTD/XXE attacks, and invalid rates.

UI statuses in this English report are descriptions of the original Russian labels.

## 4. Agent errors and fixes

### Actual implementation fixes and clarifications

1. **Strict XML number syntax (`BnmXmlParser`)**
   - *Issue:* The initial `NumberStyles.Any` allowed a comma to be interpreted as a thousands separator and accepted exponential notation.
   - *Fix:* Added `TryParseXmlDecimal`, explicitly rejecting commas, exponents, and signs; only digits and one decimal point are allowed (`NumberStyles.AllowDecimalPoint`).
2. **XML parser security (`BnmXmlParser`)**
   - *Issue:* Standard `XDocument.Parse` was used without explicit DTD/external-entity restrictions or a size limit.
   - *Fix:* Switched to `XmlReader` with `DtdProcessing.Prohibit`, `XmlResolver = null`, and a 2 MB input limit.
3. **Missing publications and unsupported currencies (`BnmXmlParser`)**
   - *Issue:* An empty response could raise a format exception, while XML without supported currencies could become a fabricated MDL-only rate set.
   - *Fix:* An empty HTTP body or `ValCurs` without `Valute` returns `null`, signaling no publication to `RateLookupService`. If all XML currencies are unsupported, parsing fails without inventing an MDL-only set.
4. **HTTP resources and cancellation (`BnmRateClient`)**
   - *Issue:* `HttpResponseMessage` was not guaranteed to be disposed with `using`, `cts.Token` did not cover `ReadAsStringAsync`, and external cancellation could appear as a timeout.
   - *Fix:* Added `using (response)`, passed the timeout to `ReadAsStringAsync(cts.Token)`, and propagated external cancellation when `cancellationToken.IsCancellationRequested`.
5. **Strict cache validation and integrity (`JsonCacheStore`)**
   - *Issue:* Cache reads substituted the root date for corrupt `Entry.PublicationDate` values and automatically added `MDL=1` to invalid files; writes did not validate RateSet completeness.
   - *Fix:* Reads verify `Version == "1.0"`, nonempty `SourceUrl`/timestamps, matching entry/root dates, and an originally present `MDL=1` entry. Writes validate `RateSet` before creating a temporary file.
6. **Currency validation before saving (`ConversionService`)**
   - *Issue:* BNM responses were cached before checking the requested currencies, potentially replacing a valid cache with incomplete data.
   - *Fix:* Source/target currency checks now precede `SaveCacheAsync`.
7. **Matching validator grammars (`AmountValidator` and `site.js`)**
   - *Issue:* Server validation accepted leading `+` and `.5`, although the specification requires an explicit integer part.
   - *Fix:* Reject leading `+`, a missing integer part (`.5`), a separator without fractional digits (`1.`), and spaces. The same logic is used in client JavaScript.
8. **Shell environment restriction**
   - *Issue:* Permission checks rejected interactive `run_command` calls.
   - *Fix:* Core, Infrastructure, Web, and Tests were created using standard platform file tools without bypassing security restrictions. The orchestrator built the solution.
9. **Fractional amount formatting (`Index.cshtml`)**
   - *Issue:* `ToString("N2")` rounded same-currency source/target amounts, for example 123.4567 to 123.46.
   - *Fix:* Same-currency and original source amounts use up to 4 exact places (`ToString("0.####", CultureInfo.InvariantCulture)`); different-currency results use 2 places (`ToString("N2", CultureInfo.InvariantCulture)`).
10. **Removing implementation details from result cards (`Index.cshtml`)**
    - *Issue:* Same-currency cards contained unnecessary explanations about absent network/cache operations.
    - *Fix:* Removed these details, retaining the selected date and concise rate-not-required status.
11. **Future-date validation in JavaScript (`site.js`)**
    - *Issue:* Only nonempty dates were checked, so browsers without strict `max` enforcement could submit future dates.
    - *Fix:* Added `validateDate` checks for `validity.valid`, `rangeOverflow`, and `max`, a clear `#dateError`, and disabled `convertButton` when invalid.
12. **Safe errors and logging (`Index.cshtml.cs`, `Error.cshtml`, `Error.cshtml.cs`)**
    - *Issue:* Unexpected exception handlers displayed `ex.Message`; the standard `/Error` page was absent.
    - *Fix:* Added `Pages/Error.cshtml` and its model with safe Russian text and `RequestId`. Injected `ILogger<IndexModel>` into `Index.cshtml.cs`; unexpected errors are logged server-side, while users receive a generic message without internal details.
13. **CurrencyCode static initialization (`CurrencyCode.cs`)**
    - *Issue:* The first actual Opera GX conversion raised `TypeInitializationException: The type initializer for 'CurrencyConverter.Core.Models.CurrencyCode' threw an exception. -> System.NullReferenceException` at line 34 after a constructor call from `.cctor` at line 8.
    - *Cause:* Static instances `Mdl`, `Eur`, `Usd`, etc. preceded `SupportedCodes`. C# initializes static fields from top to bottom; `SupportedCodes` was `null` when `new("MDL")` ran.
    - *Fix:* Moved `SupportedCodes` before currency instances. CurrencyCode was removed from claims of first-attempt correctness.
14. **Strict cache schema version (`JsonCacheStore`)**
    - *Issue:* DTO initialization `public string Version { get; set; } = "1.0"` accepted JSON without a `"version"` field because the default remained.
    - *Fix:* Changed to `public string? Version { get; set; }` without a default; validation requires `dto.Version == "1.0"`.
15. **Null JSON cache entries (`JsonCacheStore`)**
    - *Issue:* `null` entries such as `[null]` in `"entries"` caused `NullReferenceException`.
    - *Fix:* Used `List<CurrencyRateEntryDto?>?`; null/invalid entries reject the file as corrupt and return `null` (cache miss) without crashing.
16. **Propagating external CancellationToken cancellation (`ConversionService`, `JsonCacheStore`)**
    - *Issue:* `catch (Exception ex)` during cache saves/general loads swallowed `OperationCanceledException`, disguising cancellation as a cache warning or service error.
    - *Fix:* Added `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` to propagate cancellation immediately.
17. **Date underflow in historical lookup (`RateLookupService`)**
    - *Issue:* `requestedDate.AddDays(-offset)` could throw `ArgumentOutOfRangeException` near `DateOnly.MinValue`.
    - *Fix:* Added `if (requestedDate.DayNumber < offset) break;` to stay within the DateOnly range.
18. **BNM publication-date limits (`RateLookupService`)**
    - *Issue:* Valid XML with a publication older than 31 calendar days relative to the requested date was accepted.
    - *Fix:* Added `if (rateSet.PublicationDate < requestedDate.AddDays(-MaxLookbackDays)) throw new RateSourceFormatException(...)` to enforce the 31-day lookup window.
19. **Strict MDL validation (`BnmXmlParser`)**
    - *Issue:* An explicit MDL entry with nominal/rate values other than 1 could enter the rate set.
    - *Fix:* Explicit MDL requires both values to equal 1, otherwise `RateSourceFormatException` is raised. XML with no supported foreign currency is also rejected.

## 5. Conformance to the initial specification

| Requirement / area | Task | Implementation file(s) | Status |
| --- | --- | --- | --- |
| Solution and projects | T001 | `CurrencyConverter.sln`, `src/*/*.csproj`, `tests/CurrencyConverter.Tests/CurrencyConverter.Tests.csproj` | Implemented |
| .NET 8 build conventions | T002 | `Directory.Build.props` | Implemented |
| Domain models and contracts | T003 | `src/CurrencyConverter.Core/Models/*.cs` | Implemented |
| Service interfaces | T004 | `src/CurrencyConverter.Core/Contracts/*.cs` | Implemented |
| Amount validation (10^12, dot/comma, no spaces/exponents) | T005 | `src/CurrencyConverter.Core/Validation/AmountValidator.cs`, `src/CurrencyConverter.Web/wwwroot/js/site.js` | Implemented |
| Currency and Chisinau-time validation | T006 | `src/CurrencyConverter.Core/Validation/RequestValidator.cs`, `src/CurrencyConverter.Infrastructure/Time/ChisinauClock.cs` | Implemented |
| MDL formula, nominal values, AwayFromZero | T007 | `src/CurrencyConverter.Core/Calculation/CurrencyCalculator.cs` | Implemented |
| Same currency without network/cache | T008 | `src/CurrencyConverter.Core/Calculation/ConversionService.cs`, `src/CurrencyConverter.Web/Pages/Index.cshtml` | Implemented |
| Backward lookup up to 31 days; stop on error | T009 | `src/CurrencyConverter.Core/Calculation/RateLookupService.cs` | Implemented |
| BNM HTTP (10-second timeout, CancellationToken) | T010 | `src/CurrencyConverter.Infrastructure/Bnm/BnmRateClient.cs` | Implemented |
| ValCurs XML, duplicates, nominal>0, MDL=1 | T011 | `src/CurrencyConverter.Infrastructure/Bnm/BnmXmlParser.cs` | Implemented |
| Atomic JSON cache and failure safety | T012 | `src/CurrencyConverter.Infrastructure/Caching/JsonCacheStore.cs` | Implemented |
| Cache suitability (not newer than request) | T013 | `src/CurrencyConverter.Core/Validation/CacheValidator.cs` | Implemented |
| Explicit cache consent and warnings | T014 | `src/CurrencyConverter.Core/Calculation/ConversionService.cs` | Implemented |
| Razor Pages UI, form, labels | T015 | `src/CurrencyConverter.Web/Pages/Index.cshtml`, `Index.cshtml.cs` | Implemented |
| Client validation, submit blocking, styles | T016 | `src/CurrencyConverter.Web/wwwroot/css/site.css`, `wwwroot/js/site.js` | Implemented |
| Server validation and result rendering | T017 | `src/CurrencyConverter.Web/Pages/Index.cshtml`, `Index.cshtml.cs`, `ViewModels/ConversionViewModel.cs` | Implemented |
| Cache confirmation and warnings | T018 | `src/CurrencyConverter.Web/Pages/Index.cshtml`, `Index.cshtml.cs` | Implemented |
| Launch documentation and reporting | T019 | `README.md`, `REPORT.md` | Implemented |
| BNM XML and negative cases | T020 | `tests/CurrencyConverter.Tests/Unit/BnmXmlParserTests.cs` | Implemented |
| Conversion, AwayFromZero, overflow | T021 | `tests/CurrencyConverter.Tests/Unit/CurrencyCalculatorTests.cs` | Implemented |
| Amount boundaries, formats, errors | T022 | `tests/CurrencyConverter.Tests/Unit/AmountValidatorTests.cs` | Implemented |
| Request validation, valid dates, IClock | T023 | `tests/CurrencyConverter.Tests/Unit/RequestValidatorTests.cs` | Implemented |
| Previous-rate lookup and stops | T024 | `tests/CurrencyConverter.Tests/Unit/RateLookupTests.cs` | Implemented |
| Bypass, offline, cache confirmation | T025 | `tests/CurrencyConverter.Tests/Unit/ConversionServiceTests.cs` | Implemented |
| JSON cache isolation, read/write, concurrency | T026 | `tests/CurrencyConverter.Tests/Unit/JsonCacheStoreTests.cs` | Implemented |
| Test commands and actual results | T027 | README.md, REPORT.md, docs/TEST_RESULTS.md | Verified |
| Additional HTTP client checks | T010 | tests/CurrencyConverter.Tests/Unit/BnmRateClientTests.cs | Verified |

## History and evidence

- `634a898`: separate repository and README before code.
- `feature/currency-converter`: PR #2 (T001–T019 and UI fixes) passed 9 Opera GX browser scenarios and was merged.
- Testing used `tests/currency-converter` and was merged into main through PR #3.
- Repository, initially created private: https://github.com/sam4ch/bnm-currency-converter-web.
- Specification PR: https://github.com/sam4ch/bnm-currency-converter-web/pull/1, committed and merged before code.
- Antigravity (Gemini 3.8 Flash High) implemented T020–T027 without Git operations; commit/push/merge remained the orchestrator's responsibility.
- Test files were created and registered in the solution; actual defects were fixed.
- The assistant ran `dotnet test`: 162/162 tests passed, with no skips.
- The manual UI checklist (M001–M007) remains a separate verified artifact from xUnit automation (`docs/BROWSER_VALIDATION.md`).

## Final validation and result boundaries

- `dotnet build CurrencyConverter.sln`: succeeded, 0 errors/warnings.
- `dotnet test`: 162 passed, 0 failures, 0 skips; [group breakdown](docs/TEST_RESULTS.md).
- Browser: Opera GX 138.0.6044.45, 10/10 scenarios, [report/screenshots](docs/BROWSER_VALIDATION.md). Checked live BNM, restart with cache, explicit consent, unsuitable dates, keyboard/loading behavior, and 1280/390 px widths.
- Source recovery was verified in the same interface with a local test HTTP source: 503 → 200, with a successful retry. This is separate from live BNM.
- Physical-phone/user acceptance was not performed; mobile width was emulated in the browser.
- Antigravity created the main application code and 134 tests. The Codex assistant added 28 regression checks and final fixes, and performed review, build/test execution, browser automation, visual inspection, final documentation, and Git operations.
- Stages are preserved through PRs: [specifications #1](https://github.com/sam4ch/bnm-currency-converter-web/pull/1), [implementation #2](https://github.com/sam4ch/bnm-currency-converter-web/pull/2), and [tests #3](https://github.com/sam4ch/bnm-currency-converter-web/pull/3).

### Antigravity quota and final fixes

Antigravity CLI reported `Individual quota reached` after producing the initial
134 passing tests. No account switching or quota bypass was performed. Remaining
review findings were fixed in the Codex chat: TimeProvider injection into
ChisinauClock for real winter/summer date-boundary tests; nullable warning CS8619;
a Nominal>1 fixture moved to supported EUR with effective-rate checks; and clearer
error-page text. The 28 additional checks cover corrupt JSON metadata, preservation
of valid cache files, missing currencies, same-currency cancellation, static
initialization, amount overflow, and avoiding premature rounding. This was Codex
work and must not be attributed to Antigravity.

The three actual Antigravity prompts are published in [docs/prompts](docs/prompts);
later clarifications and review results are recorded in this report.
