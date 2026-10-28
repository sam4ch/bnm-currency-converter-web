# Tasks: Currency Converter

**Feature**: `001-currency-converter`
**Status**: Draft implementation backlog for documentation-only planning
**Date**: 2026-10-28

## Repository layout

```text
CurrencyConverter.sln
src/
├── CurrencyConverter.Web/
│   ├── Pages/
│   ├── wwwroot/
│   ├── ViewModels/
│   └── Services/
├── CurrencyConverter.Core/
│   ├── Models/
│   ├── Validation/
│   ├── Calculation/
│   └── Contracts/
└── CurrencyConverter.Infrastructure/
    ├── Bnm/
    ├── Caching/
    └── Time/

tests/
└── CurrencyConverter.Tests/
    ├── Unit/
    ├── Fixtures/
    └── Helpers/
```

Notes:
- `specs/currency-converter`, `feature/currency-converter`, and `tests/currency-converter` are Git branch names only; they are not folders in the repository tree.
- The implementation branch is `feature/currency-converter` for application code.
- The testing branch is `tests/currency-converter` for tests and validation work after the implementation PR is merged.

## Task sequence

### Phase 0 — Initial solution and contracts

- [x] T001: Create the solution and project skeleton for `CurrencyConverter.Web`, `CurrencyConverter.Core`, `CurrencyConverter.Infrastructure`, and `tests/CurrencyConverter.Tests` under the repository root layout above. Dependencies: none. Phase: implementation. Scope: `CurrencyConverter.sln`, `src/**`, `tests/**`. FR: FR-001, FR-008, FR-026, FR-027, FR-028, FR-029. SC: SC-001, SC-007.
- [x] T002: Add project-level conventions for nullable, implicit usings, LangVersion, and deterministic build behavior for the .NET 8 solution; keep the settings consistent across the solution. Dependencies: T001. Phase: implementation. Scope: `Directory.Build.props` or equivalent project-level settings. FR: FR-001, FR-008. SC: SC-001.
- [x] T003: Define the Core domain contracts and value objects for `CurrencyCode`, `Amount`, `ConversionRequest`, `ConversionResult`, `RateSet`, `CurrencyRateEntry`, `RateLookupContext`, and cache snapshot metadata, using `DateOnly` for calendar dates and `DateTimeOffset` for retrieval timestamps. Dependencies: T001, T002. Phase: implementation. Scope: `src/CurrencyConverter.Core/**`. FR: FR-001, FR-009, FR-012, FR-018, FR-024. SC: SC-002, SC-006, SC-008.
- [x] T004: Define infrastructure and service contracts for BNM fetch, conversion orchestration, validation, and JSON cache persistence. Dependencies: T003. Phase: implementation. Scope: `src/CurrencyConverter.Core/Contracts/**`, `src/CurrencyConverter.Infrastructure/**`. FR: FR-008, FR-015, FR-018, FR-021, FR-022, FR-023, FR-025. SC: SC-004, SC-006, SC-009.

### Phase 1 — Core validation and conversion rules

- [x] T005: Implement amount validation for positive decimal input, accepted `.` and `,` separators, rejection of empty strings, letters, zero, negative values, grouping separators, exponent notation, overflow, and more than four decimal places. Dependencies: T003. Phase: implementation. Scope: `src/CurrencyConverter.Core/Validation/**`, `src/CurrencyConverter.Core/Models/**`. FR: FR-002, FR-003, FR-004, FR-005, FR-026, FR-027. SC: SC-003.
- [x] T006: Implement currency and date validation that rejects unsupported user-selected currencies, forbids missing or future dates, and preserves the supported set `MDL`, `EUR`, `USD`, `RON`, `GBP` without rejecting valid BNM XML containing extra currencies. Dependencies: T003, T005. Phase: implementation. Scope: `src/CurrencyConverter.Core/Validation/**`. FR: FR-006, FR-009, FR-016, FR-027. SC: SC-003, SC-004.
- [x] T007: Implement nominal-aware MDL conversion logic using `rateValue / nominal` and `amount * sourceMdlPerUnit / targetMdlPerUnit`, including final `MidpointRounding.AwayFromZero` rounding only at the end of the conversion. Dependencies: T003, T006. Phase: implementation. Scope: `src/CurrencyConverter.Core/Calculation/**`. FR: FR-010, FR-011, FR-017. SC: SC-002, SC-008.
- [x] T008: Implement same-currency short-circuit behavior so identical source and target currencies return the original amount without network lookup, cache lookup, or invented rate metadata. Dependencies: T003, T006, T007. Phase: implementation. Scope: `src/CurrencyConverter.Core/**`, `src/CurrencyConverter.Web/**`. FR: FR-007, FR-012, FR-021, FR-022. SC: SC-008.
- [x] T009: Implement the historical fallback selection logic for the nearest previous valid publication within the 31-day window, and fail cleanly when no valid earlier date exists. Dependencies: T003, T004, T006, T007. Phase: implementation. Scope: `src/CurrencyConverter.Core/**`, `src/CurrencyConverter.Infrastructure/**`. FR: FR-013, FR-014, FR-015, FR-023. SC: SC-005.

### Phase 2 — Infrastructure: BNM, time, and cache

- [x] T010: Implement the BNM XML HTTP client with a 10-second timeout and injectable `HttpClient` / handler for testability; ensure network, HTTP, and timeout failures are mapped to domain-level errors. Dependencies: T004. Phase: implementation. Scope: `src/CurrencyConverter.Infrastructure/Bnm/**`, `src/CurrencyConverter.Web/**`. FR: FR-008, FR-015, FR-023. SC: SC-004.
- [x] T011: Implement BNM XML parsing for valid documents, normalization to uppercase currency codes, acceptance of extra supported/unsupported currencies in valid payloads, and rejection of empty or malformed XML, missing date values, duplicate codes, zero/negative nominal values, and invalid rate values. Dependencies: T004, T010. Phase: implementation. Scope: `src/CurrencyConverter.Infrastructure/Bnm/**`. FR: FR-008, FR-009, FR-016, FR-017, FR-023. SC: SC-003, SC-004.
- [x] T012: Implement the local JSON cache store with atomic write semantics using a temporary file and replace-on-success strategy, plus validation before reuse. Dependencies: T004, T011. Phase: implementation. Scope: `src/CurrencyConverter.Infrastructure/Caching/**`. FR: FR-018, FR-019, FR-020, FR-021, FR-022, FR-025. SC: SC-004, SC-006, SC-009.
- [x] T013: Implement cache suitability checks so a cached response is rejected when it is later than the requested date, corrupted, invalid, or otherwise unsuitable; record the cache snapshot metadata with `DateTimeOffset` retrieval timestamps. Dependencies: T012. Phase: implementation. Scope: `src/CurrencyConverter.Infrastructure/Caching/**`, `src/CurrencyConverter.Core/**`. FR: FR-018, FR-019, FR-020, FR-021, FR-022. SC: SC-004, SC-006.
- [x] T014: Implement the orchestration layer that resolves rate source and cache behavior, including explicit consent before reusing an earlier cache result, and keeps a successful conversion result valid even when cache write fails. Dependencies: T008, T009, T012, T013. Phase: implementation. Scope: `src/CurrencyConverter.Core/**`, `src/CurrencyConverter.Infrastructure/**`. FR: FR-018, FR-021, FR-022, FR-025. SC: SC-004, SC-006, SC-009.

### Phase 3 — Razor Pages application and UI

- [x] T015: Build the Razor Pages conversion form, Russian labels, form fields, server-side request object, and status messaging for success, validation error, network issues, and cached backup usage. Dependencies: T004, T005, T006, T014. Phase: implementation. Scope: `src/CurrencyConverter.Web/**`. FR: FR-001, FR-012, FR-021, FR-026, FR-027, FR-028, FR-029. SC: SC-001, SC-007.
- [x] T016: Add client-side validation, disabled submit state while required fields are invalid or a request is pending, and mobile/desktop-friendly styling and focus behavior. Dependencies: T015. Phase: implementation. Scope: `src/CurrencyConverter.Web/wwwroot/**`, `src/CurrencyConverter.Web/Pages/**`. FR: FR-002, FR-026, FR-028, FR-029. SC: SC-001, SC-007.
- [x] T017: Add server-side validation, result rendering, warning states, and same-currency handling without invented rate metadata or fake source references. Dependencies: T015, T016, T008. Phase: implementation. Scope: `src/CurrencyConverter.Web/Pages/**`, `src/CurrencyConverter.Web/ViewModels/**`. FR: FR-007, FR-012, FR-026, FR-027, FR-028. SC: SC-001, SC-008.
- [x] T018: Add the explicit confirmation flow for cached fallback and UI messages that explain stale, missing, future, or invalid cache cases without silent reuse. Dependencies: T013, T014, T015, T017. Phase: implementation. Scope: `src/CurrencyConverter.Web/**`. FR: FR-020, FR-021, FR-022, FR-027. SC: SC-004, SC-006.
- [x] T019: Document project usage and status in the repository root documentation, with future execution commands described as planned actions rather than claims of a completed run. Dependencies: T015, T016, T017. Phase: implementation. Scope: `README.md`, `REPORT.md`. FR: FR-001, FR-012, FR-018, FR-027. SC: SC-001.

## Test backlog

### Phase 4 — Automated tests in `tests/currency-converter` branch

- [ ] T020: Add parsing tests for valid BNM XML, including `MDL` base rate and `Nominal > 1`, valid extra currencies, and negative cases covering empty/invalid XML, missing date/value, duplicate codes, and invalid nominal/rate values. Dependencies: T011. Phase: tests. Scope: `tests/CurrencyConverter.Tests/**`. FR: FR-008, FR-009, FR-016, FR-017, FR-023. SC: SC-003, SC-004.
- [ ] T021: Add conversion calculation tests covering direct, reverse, and cross-currency conversions on known fixtures, same-currency behavior, nominal handling, final rounding with `AwayFromZero`, and overflow. Dependencies: T007, T008. Phase: tests. Scope: `tests/CurrencyConverter.Tests/**`. FR: FR-007, FR-010, FR-011. SC: SC-002, SC-008.
- [ ] T022: Add amount validation tests for both decimal forms, accepted values, rejected empty input, letters, zero, negatives, bounds, precision errors, grouping separators, exponent notation, and overflow; include exact boundary checks. Dependencies: T005. Phase: tests. Scope: `tests/CurrencyConverter.Tests/**`. FR: FR-002, FR-003, FR-004, FR-005. SC: SC-003.
- [ ] T023: Add request validation tests for unsupported currencies, missing or future dates, and the Moldova local-date boundary with fixed clock values. Dependencies: T006. Phase: tests. Scope: `tests/CurrencyConverter.Tests/**`. FR: FR-006, FR-009, FR-016, FR-027. SC: SC-003.
- [ ] T024: Add lookup tests for nearest previous publication, when the actual response date is earlier than requested, search window exhaustion, and immediate stop on timeout or network error with call-count verification. Dependencies: T009, T010. Phase: tests. Scope: `tests/CurrencyConverter.Tests/**`. FR: FR-013, FR-014, FR-015, FR-023. SC: SC-004, SC-005.
- [ ] T025: Add orchestration tests covering identical-currency bypass, offline behavior with no cache and with valid cache requiring explicit consent, later-than-requested cache rejection, and cache write warning behavior without invalidating a successful result. Dependencies: T008, T012, T013, T014. Phase: tests. Scope: `tests/CurrencyConverter.Tests/**`. FR: FR-007, FR-018, FR-020, FR-021, FR-022, FR-025. SC: SC-004, SC-006, SC-008, SC-009.
- [ ] T026: Add JSON cache store tests for atomic persistence, restart-safe read behavior, corrupt or missing file handling, stale/bad data not replacing valid cache, and concurrent-write safety keeping the document intact. Dependencies: T012, T013. Phase: tests. Scope: `tests/CurrencyConverter.Tests/**`. FR: FR-018, FR-019, FR-020. SC: SC-004, SC-006.
- [ ] T027: Add test execution and CI-ready verification instructions that call `dotnet test` from the repository root, without claiming completion before those checks are actually run. Dependencies: T020, T021, T022, T023, T024, T025, T026. Phase: tests. Scope: `README.md`, `REPORT.md`, `tests/**`. FR: FR-008, FR-018, FR-027. SC: SC-001, SC-004, SC-006.

## Manual browser validation checklist

This checklist is separate from xUnit tests and must be performed manually in a browser or device emulator.

- [ ] M001: Verify the page remains usable on mobile and desktop widths.
- [ ] M002: Verify Russian labels, validation messages, and form states are clear and readable.
- [ ] M003: Verify keyboard navigation and focus order work for the amount, currencies, date, and submit controls.
- [ ] M004: Verify the loading state disables form submission and removes stale results during a request.
- [ ] M005: Verify the explicit cache confirmation flow appears only when required.
- [ ] M006: Verify same-currency requests display no fake course metadata or source reference.
- [ ] M007: Verify the page remains stable across retries, invalid inputs, and confirmation flows.

## Traceability table

Each relevant FR maps to at least one implementation task and one test or manual-check task.

| FR | Implementation task(s) | Test / manual-check task(s) |
| --- | --- | --- |
| FR-001 | T001, T015 | M001, M002, M003 |
| FR-002 | T005, T016 | T022, M004 |
| FR-003 | T005 | T022 |
| FR-004 | T005 | T022 |
| FR-005 | T005 | T022 |
| FR-006 | T006 | T023 |
| FR-007 | T008, T017 | T021, T025, M006 |
| FR-008 | T001, T010, T011 | T020, T027 |
| FR-009 | T003, T006, T011 | T020, T023 |
| FR-010 | T007 | T021 |
| FR-011 | T007 | T021 |
| FR-012 | T003, T015, T017 | T025, M002 |
| FR-013 | T009 | T024 |
| FR-014 | T009 | T024 |
| FR-015 | T010, T011 | T024 |
| FR-016 | T006, T011 | T020, T023 |
| FR-017 | T007, T011 | T020, T021 |
| FR-018 | T003, T012, T013, T014 | T025, T026, T027 |
| FR-019 | T012, T013 | T026 |
| FR-020 | T012, T013, T018 | T025, T026 |
| FR-021 | T012, T013, T014, T018 | T025 |
| FR-022 | T013, T014, T018 | T025 |
| FR-023 | T010 | T024 |
| FR-024 | T003 | T023 |
| FR-025 | T014, T019 | T025, T027 |
| FR-026 | T005, T015, T016 | M004 |
| FR-027 | T005, T015, T017, T018 | T023, M002 |
| FR-028 | T015, T016 | M001, M007 |
| FR-029 | T015, T016 | M003 |

## Success criteria linkage

| SC | Relevant tasks |
| --- | --- |
| SC-001 | T001, T015, T016, T019, T027 |
| SC-002 | T003, T007, T021 |
| SC-003 | T005, T006, T020, T022, T023 |
| SC-004 | T010, T011, T012, T013, T024, T026 |
| SC-005 | T009, T024 |
| SC-006 | T003, T012, T013, T014, T025, T026 |
| SC-007 | T001, T015, T016, T017, M001, M003 |
| SC-008 | T007, T008, T017, T021, T025 |
| SC-009 | T014, T019, T025, T027 |


## Concrete file targets

These are planned file paths, not existing implementation files. The module
scopes above do not authorize using branch names as source directories.

| Task | Planned file(s) |
| --- | --- |
| T001 | `CurrencyConverter.sln`; `src/CurrencyConverter.Web/CurrencyConverter.Web.csproj`; `src/CurrencyConverter.Core/CurrencyConverter.Core.csproj`; `src/CurrencyConverter.Infrastructure/CurrencyConverter.Infrastructure.csproj`; `tests/CurrencyConverter.Tests/CurrencyConverter.Tests.csproj` |
| T002 | `Directory.Build.props` |
| T003 | `src/CurrencyConverter.Core/Models/CurrencyCode.cs`, `Amount.cs`, `CurrencyRateEntry.cs`, `RateSet.cs`, `RateDate.cs`, `RateLookupContext.cs`, `ConversionRequest.cs`, `ConversionResult.cs`, `VerifiedCacheSnapshot.cs` in the same Models directory |
| T004 | `src/CurrencyConverter.Core/Contracts/IClock.cs`, `IBnmRateClient.cs`, `ICacheStore.cs`, `IConversionService.cs`, `IValidationService.cs` in the same Contracts directory |
| T005 | `src/CurrencyConverter.Core/Validation/AmountValidator.cs` |
| T006 | `src/CurrencyConverter.Core/Validation/RequestValidator.cs`; `src/CurrencyConverter.Infrastructure/Time/ChisinauClock.cs` |
| T007 | `src/CurrencyConverter.Core/Calculation/CurrencyCalculator.cs` |
| T008 | `src/CurrencyConverter.Core/Calculation/ConversionService.cs` |
| T009 | `src/CurrencyConverter.Core/Calculation/RateLookupService.cs` |
| T010 | `src/CurrencyConverter.Infrastructure/Bnm/BnmRateClient.cs`; `src/CurrencyConverter.Web/Program.cs` |
| T011 | `src/CurrencyConverter.Infrastructure/Bnm/BnmXmlParser.cs` |
| T012 | `src/CurrencyConverter.Infrastructure/Caching/JsonCacheStore.cs` |
| T013 | `src/CurrencyConverter.Infrastructure/Caching/CacheValidator.cs` |
| T014 | `src/CurrencyConverter.Core/Calculation/ConversionService.cs` |
| T015 | `src/CurrencyConverter.Web/Pages/Index.cshtml`, `Index.cshtml.cs`; `src/CurrencyConverter.Web/Program.cs` |
| T016 | `src/CurrencyConverter.Web/wwwroot/css/site.css`; `src/CurrencyConverter.Web/wwwroot/js/site.js` |
| T017 | `src/CurrencyConverter.Web/Pages/Index.cshtml`, `Index.cshtml.cs`; `src/CurrencyConverter.Web/ViewModels/ConversionViewModel.cs` |
| T018 | `src/CurrencyConverter.Web/Pages/Index.cshtml`, `Index.cshtml.cs` |
| T019 | `README.md`; `REPORT.md` |
| T020 | `tests/CurrencyConverter.Tests/Unit/BnmXmlParserTests.cs`; `tests/CurrencyConverter.Tests/Fixtures/bnm-valid.xml` and invalid XML fixtures |
| T021 | `tests/CurrencyConverter.Tests/Unit/CurrencyCalculatorTests.cs` |
| T022 | `tests/CurrencyConverter.Tests/Unit/AmountValidatorTests.cs` |
| T023 | `tests/CurrencyConverter.Tests/Unit/RequestValidatorTests.cs`; `tests/CurrencyConverter.Tests/Helpers/FixedClock.cs` |
| T024 | `tests/CurrencyConverter.Tests/Unit/RateLookupTests.cs`; `tests/CurrencyConverter.Tests/Helpers/FakeHttpMessageHandler.cs` |
| T025 | `tests/CurrencyConverter.Tests/Unit/ConversionServiceTests.cs` |
| T026 | `tests/CurrencyConverter.Tests/Unit/JsonCacheStoreTests.cs` |
| T027 | `README.md`; `REPORT.md` |

M001–M007 are maintained in `checklists/manual-browser-checklist.md`.
The inline list is a summary of the same checks, not additional completed tests.

### Execution clarifications

- The date validation tests include impossible dates, not only future or missing dates.
- T025 must verify that the confirmed cached fallback actually calculates, while the unconfirmed path does not.
- T026 must instantiate a new cache service using the same temporary file to verify restart persistence.
- A cache is not invalid solely because its date is earlier than the requested date; the explicit-consent rule still applies.
- Documentation commands are reported as verified only after actual execution; until then they remain planned.

- T001 must complete before any feature work.
- T003 must be defined before T005, T006, T007, T008, and T009.
- T010 and T012 must be complete before the UI service layer can be fully wired in T015 and T018.
- T020 through T026 are test tasks to be executed after the implementation branch is merged into the test branch workflow, not before implementation is complete.
- Manual validation items are separate from xUnit tasks and must not be mixed into automated test coverage.
- No `[P]` markers are used because the task list deliberately avoids claiming independent workstreams without proof of actual source-file independence and dependency safety.
- This backlog is a planning artifact only; it does not claim implementation execution or passing tests.
