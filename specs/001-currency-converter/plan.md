# Implementation Plan: Currency Converter

**Date**: 2026-10-28
**Spec**: `specs/001-currency-converter/spec.md`

## Summary

The solution will be a Russian-language ASP.NET Core Razor Pages application on .NET 8 with a separate Core library for validation and conversion logic, an infrastructure layer for BNM XML retrieval and local cache persistence, and xUnit tests isolated from real network access. The feature must convert currencies using decimal arithmetic, nominal-aware MDL conversion, controlled fallback to earlier valid dates, explicit confirmation for cached fallback, and strict validation on both client and server.

## Technical Context

**Language/Version**: C# on .NET 8

**Primary Dependencies**: ASP.NET Core Razor Pages, System.Xml.Linq, HttpClient, JSON serialization, xUnit, dependency injection

**Storage**: local JSON file cache under a runtime data folder; no database required

**Testing**: xUnit for logic/unit validation, manual browser checklist for responsive design and keyboard behavior

**Target Platform**: local web application for desktop and mobile browsers, running on Windows or Linux development hosts

**Project Type**: web application with a shared library and test project

**Constraints**: decimal arithmetic only; source data must be BNM XML; no silent cache reuse; no real network in unit tests; source and target currency sets must remain limited to the supported list unless a higher-level response includes extra codes; a network error or timeout stops the date search immediately and does not retry the same date in a loop; calendar dates use `DateOnly`, retrieval timestamps use `DateTimeOffset`.

## Project Structure

### Documentation (this feature)

```text
specs/
└── 001-currency-converter/
    ├── spec.md
    ├── plan.md
    ├── tasks.md
    ├── research.md
    ├── data-model.md
    ├── quickstart.md
    ├── contracts/
    │   ├── bnm-provider.md
    │   ├── conversion-service.md
    │   ├── cache-store.md
    │   └── ui-contract.md
    └── checklists/
        └── manual-browser-checklist.md
```

### Repository root

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

**Structure Decision**: the source tree is split into a Razor Pages application, a Core library, an Infrastructure library, and a dedicated root-level test project. Branch names such as `specs/currency-converter`, `feature/currency-converter`, and `tests/currency-converter` are Git workflow labels and are not repository folders or source directories.

## Architecture Overview

### 1. Presentation Layer

The Razor Pages front end owns the browser form, Russian labels, client-side validation, loading state, and error presentation. It does not perform business calculations itself; instead, it sends a validated conversion request to a service layer and renders the response.

Responsibilities:
- show amount, source currency, target currency, selected date
- disable submit when required fields are invalid or loading
- display conversion result, source reference, rate date, and warning messages
- require explicit user confirmation before reusing an earlier cached rate

### 2. Core Layer

The Core library owns data contracts, validation, formula logic, and currency semantics. It should remain free of ASP.NET Core or HttpClient-specific types and be testable in isolation.

Responsibilities:
- parse and validate user input
- normalize decimal input from strings with dot/comma handling
- compute MDL-based conversion using nominal values
- validate BNM rate records and cache data
- determine fallback dates and rate selection logic

### 3. Infrastructure Layer

This layer handles network access, XML parsing, time abstraction, and file-based cache persistence. It converts external data to Core models and maps failures to well-defined domain errors.

Responsibilities:
- fetch BNM XML by date and API URL
- parse XML with `System.Xml.Linq`
- map the rate table into internal Core models
- persist cache atomically as JSON
- provide a clock abstraction and HTTP abstraction for tests

## Detailed Design

### Input and Validation Flow

1. User enters amount and selects currencies/date in Razor Pages.
2. Client-side validation blocks empty/invalid values and disables submit.
3. Server-side validation re-checks every request and rejects invalid input with a clear Russian error.
4. Same-currency conversion is short-circuited before network access.
5. For valid requests, the conversion service resolves the rate set and performs calculation using MDL.

Validation rules:
- amount must be positive, non-empty, and within `0 < amount <= 1_000_000_000_000`
- maximum four decimal places
- only dot or comma decimal separators are accepted
- no scientific notation, grouping separators, or whitespace-only values
- unsupported currencies are rejected at the selection layer; extra codes in a valid BNM response do not invalidate the whole document
- future dates and missing dates are rejected
- invalid XML or missing values raise domain-level errors
- same-currency requests skip the lookup and render the original amount without invented course metadata

### Rate Source and Parsing

The BNM source is the official XML endpoint:
`https://www.bnm.md/en/official_exchange_rates?get_xml=1&date=DD.MM.YYYY`

The parser must:
- request by date
- accept valid XML only
- ignore extra currency entries not used by the app when the XML is otherwise valid
- reject empty or malformed payloads, invalid nominal values, missing rate values, duplicate currency codes, or zero/negative denominators
- normalize codes to uppercase and validate against the supported set `MDL`, `EUR`, `USD`, `RON`, `GBP`

Important clarification:
- Additional currencies in a correct BNM XML document are not a reason to reject the entire response.
- The system may ignore unrelated codes while still using the supported subset.
- User-selected unsupported currencies are rejected separately by validation and UI state, not by failing the whole feed parse.

### Formula and Nominal Handling

The conversion logic must use the value/nominal ratio as the real per-unit MDL equivalent.

For a rate entry:
- `mdlPerUnit = rateValue / nominal`

For conversion from source currency to target currency:
- `result = amount * sourceMdlPerUnit / targetMdlPerUnit`

Where:
- `sourceMdlPerUnit` is derived from the selected source rate for the chosen date
- `targetMdlPerUnit` is derived from the selected target rate for the chosen date
- `amount` is decimal
- rounding occurs only at the final conversion result to two decimal places using `MidpointRounding.AwayFromZero`

Interpretation of MDL:
- `MDL` is included in the normalized rate table with `nominal = 1` and `rateValue = 1`
- the base model uses MDL as the denominator and numerator reference

### Date Search and Historical Fallback

The system must select the closest earlier valid date if the requested date lacks a publication. Search behavior:
- starts from the requested date
- checks earlier dates in descending order
- stops at 31 calendar days earlier
- if the previous valid date is found, the result uses that date and shows it as the actual rate date
- if no valid prior rate is found in the allowed window, the request fails with a clear error and does not fabricate an answer
- any network error or timeout during this search stops the process immediately and reports the failure without retrying the same date search repeatedly

The UI must show:
- selected date requested by the user
- actual date used for the calculation
- source URL
- last successful retrieval timestamp
- warning when the actual date differs from the requested date

### Cache Strategy

The cache is a local JSON file containing the last valid BNM response and metadata for validation.

Requirements:
- write atomically to a temporary file and replace the live cache only on success
- validate before use: ensure XML is parseable, date is valid, currencies are recognized, and values are positive
- do not overwrite a valid cache with invalid or incomplete data
- if the source is unavailable and a valid cache exists, show a user-readable warning and request explicit confirmation before reuse
- refuse to use a cache whose date is after the requested date or otherwise not valid for the request
- a missing, corrupted, or stale cache must not crash the app and must yield a clear explanation
- a cache write error must not invalidate an already computed successful conversion result; it must be reported as a warning separately from the conversion result

### Error Handling

The system must map all known failure modes into explicit domain errors, so user-facing messages are understandable and logs remain consistent.

Examples:
- network failures -> `NetworkUnavailableException` or equivalent domain error
- HTTP failures -> `RateSourceHttpException`
- malformed XML -> `RateSourceFormatException`
- empty rate set -> `RateSetEmptyException`
- unsupported currency selection -> `UnsupportedCurrencyException`
- invalid input -> `ValidationException`
- cache write failure -> `CacheWriteException`

All such errors must be shown to the user without leaking internal stack traces or producing a fake conversion result.

### Testability and Dependency Injection

To keep tests deterministic, the design will rely on interfaces and injected dependencies:
- `IClock` for the current date and time
- `IBnmRateClient` for HTTP and XML retrieval
- `IRateRepository` or `ICacheStore` for cache persistence
- `IConversionService` for business logic
- `IValidationService` for amount/date/currency checks

Unit tests will use:
- mocked/fake `HttpMessageHandler`
- fixed time provider
- fixture XML strings for valid and invalid BNM responses
- test cache files representing valid, corrupted, stale, and future-date scenarios

Important implementation clarification:
- use `DateOnly` for all calendar-date values and date comparisons
- use `DateTimeOffset` for retrieval timestamps and source last-success metadata
- set a bounded HTTP timeout to prevent indefinite waiting on network or proxy problems
- the same-currency branch must return the original amount with no rate fetch and no invented metadata

## Manual Browser Checklist

The following checks are not xUnit unit tests and must be performed manually in a browser: mobile responsiveness, keyboard interaction, focus order, visible form states, and end-to-end behavior with actual UI rendering.

- [ ] Verify the page remains usable on a phone-sized viewport and a desktop viewport.
- [ ] Verify Russian labels, messages, and form states remain clear and readable.
- [ ] Verify keyboard navigation works for amount, currencies, date, submit, and confirmation flows.
- [ ] Verify the loading state disables the form correctly and removes stale results.
- [ ] Verify the explicit cache confirmation flow appears when fallback is needed.
- [ ] Verify no fake rate metadata is displayed for same-currency requests.

## Phase Breakdown

### Phase 0: Research and decisions
- confirm the mapping between BNM XML structure and internal models
- confirm fallback search and cache semantics
- confirm testability strategy for isolated HTTP and time injection

### Phase 1: Data model and contracts
- define Core models for rate entries, request data, rate sets, response payloads, and cache snapshots
- define service contracts for validation, conversion, BNM fetch, and cache persistence
- define UI contract for server-side page handlers and status messages

### Phase 2: Implementation
- create Razor Pages form and page model
- create Core validation and conversion services
- create HTTP and XML provider with date-specific request logic
- create cache store with atomic write policy
- connect the UI and error handling paths

### Phase 3: Verification
- unit tests for parsing, conversion, invalid data, fallback behavior, and cache persistence
- manual browser validation for UI and mobile layout
- review against assignment requirements and constitution before release
