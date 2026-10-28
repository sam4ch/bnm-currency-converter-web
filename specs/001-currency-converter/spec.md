# Feature Specification: Currency Converter

**Git Branch (documentation)**: `specs/currency-converter`

**Implementation branch**: `feature/currency-converter`

**Test branch**: `tests/currency-converter`

**Created**: 2026-10-28

**Status**: Draft specification for implementation planning

## Project Structure

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

specs/
└── 001-currency-converter/
    ├── spec.md
    ├── plan.md
    ├── tasks.md
    ├── research.md
    ├── data-model.md
    ├── quickstart.md
    └── contracts/
```

**Important**: Git branch names are not directories. The folder layout above is the source tree; the branches `specs/currency-converter`, `feature/currency-converter`, and `tests/currency-converter` are separate Git workflow names.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Convert a valid amount on a selected date (Priority: P1)

A user opens the converter, enters a positive amount, selects the source and target currencies, chooses a date, and triggers conversion. The system must calculate the result using the published BNM exchange rates and show the actual course date, source reference, and transparent result.

**Why this priority**: This is the main product value: the user must be able to convert currencies reliably and understand what was used for the calculation.

**Independent Test**: The user can complete a valid conversion flow from empty form to result without needing any technical knowledge or external guidance.

**Acceptance Scenarios**:

1. **Given** the user has entered a valid positive amount, a supported source currency, a supported target currency, and a valid date in the past, **When** they submit the form, **Then** the system calculates the result using the official BNM rate table and shows the result with the date and source information.
2. **Given** the selected source and target currencies are the same, **When** the user submits the form, **Then** the system returns the original amount without making a network request and without altering the user input, and the result includes no invented course metadata.
3. **Given** the user enters a malformed amount, an invalid currency, or a future date, **When** they attempt conversion, **Then** the system prevents submission and shows a clear validation error in Russian.
4. **Given** at least one required field is empty or invalid, **When** the form is being completed, **Then** the submit button stays disabled until the form becomes valid and the user can submit it.

---

### User Story 2 - Handle historical data and fallback to the nearest valid rate (Priority: P1)

A user asks for a date for which BNM does not publish an official rate. The system must identify the last valid previous rate within the allowed window and show the actual course date with a warning instead of silently inventing a value.

**Why this priority**: Historical conversion is a core business requirement, and incorrect fallback behavior would cause incorrect financial output.

**Independent Test**: The flow can be tested by selecting a date without an official BNM publication and confirming that the system chooses the nearest valid earlier date or reports a clear error.

**Acceptance Scenarios**:

1. **Given** the selected date has no official BNM publication, **When** the system searches for a usable rate, **Then** it selects the latest available earlier publication within the permitted 31-day window and displays the actual date used.
2. **Given** there is no valid publication within the search window, **When** the conversion is attempted, **Then** the system displays an explicit error and suggests the use of the saved cached rate only when a valid cache exists.
3. **Given** the BNM service returns a rate for an earlier date than the user selected, **When** the result is displayed, **Then** the UI shows the actual rate date and warns that the displayed course corresponds to the last available rate rather than the requested date.

---

### User Story 3 - Recover gracefully from network and cache failures (Priority: P1)

A user attempts conversion when the network is unavailable or the BNM response is invalid. The system must never crash, must not display a false result, and must either use a verified cache after explicit confirmation or show an accurate error message.

**Why this priority**: Network instability and invalid source data are a normal operational risk for this feature, and graceful failure is essential for trust and correctness.

**Independent Test**: The system can be evaluated offline or with malformed source data while the user still receives understandable behavior instead of a broken page or incorrect conversion.

**Acceptance Scenarios**:

1. **Given** the live source is unavailable and a valid saved cache exists, **When** the user requests conversion, **Then** the system shows a clear message and asks for explicit confirmation before using the saved rate.
2. **Given** the saved cache contains a rate for an earlier date that is valid for the requested history, **When** the user explicitly confirms the cached fallback, **Then** the system may use the earlier saved course only after clear confirmation and must never use a cache dated after the requested date.
3. **Given** the system is trying to search earlier dates for a historical fallback and a network error or timeout occurs, **When** the fallback search is in progress, **Then** the system stops immediately, does not retry the lookup for every date, and shows a clear network error without inventing a result.
4. **Given** the cache file is missing, corrupted, contains a future or later-than-requested date, or the current request cannot be satisfied by the cache, **When** the system needs it, **Then** it treats it as invalid for that request and explains why conversion cannot proceed without a valid source.
5. **Given** the server receives malformed XML, empty data, invalid currencies, or an HTTP/network error, **When** the calculation is attempted, **Then** the system shows a user-readable error and preserves the current page state without producing a fake result.
6. **Given** the user requests conversion with the same source and target currency, **When** the calculation is processed, **Then** the system returns the original amount immediately, skips all network and cache lookup, and keeps the actual rate date empty because no course was requested or fetched.

---

### User Story 4 - Maintain a verified cache and protect data integrity (Priority: P2)

The system keeps the most recently verified exchange-rate response in a local file and reuses it between restarts when appropriate. It must never overwrite a valid cache with an invalid response and must handle concurrent writes safely.

**Why this priority**: A verified cache is required to support offline or degraded operation while preserving correctness and avoiding silent data corruption.

**Independent Test**: The user can perform a conversion after a successful fetch and then verify that the cached result is reused only when valid and explicitly confirmed.

**Acceptance Scenarios**:

1. **Given** a successful BNM response has been received, **When** the service processes it, **Then** it stores the response atomically in a local cache file without harming valid existing data.
2. **Given** the new response is invalid, incomplete, or malformed, **When** the system attempts to save it, **Then** it does not replace the last good cache and reports the save failure clearly to the user.
3. **Given** a valid conversion result has already been calculated, **When** the cache write fails during persistence, **Then** the conversion result remains valid for the user, the app reports the cache write issue, and the user is not told that the conversion itself failed.
4. **Given** the cache is damaged or contains a future or later-than-requested date, **When** the application starts or the user triggers conversion, **Then** it handles the problem gracefully and continues without crashing.

---

### User Story 5 - Provide a clear and usable browser interface (Priority: P3)

A user interacts with the converter via a browser on desktop or mobile. The form uses Russian labels, clear validation, loading states, and accessible keyboard behavior so the task can be completed without confusion.

**Why this priority**: A conversion tool is only useful if users can understand and operate it comfortably on common devices.

**Independent Test**: A user can complete the conversion flow on both a desktop browser and a phone-sized viewport without losing essential form controls or error messaging.

**Acceptance Scenarios**:

1. **Given** the user opens the page, **When** they inspect the form, **Then** the labels, error messages, and controls are in Russian and clearly identify the required input.
2. **Given** a request is in progress, **When** the form is submitted, **Then** the submit action is disabled and a loading state is shown until the response completes.
3. **Given** the user uses keyboard navigation or a small screen, **When** they interact with the form, **Then** controls remain readable, usable, and structurally clear.

### Edge Cases

- What happens when the amount has a comma or a dot as the decimal separator, or when the input contains grouping separators or scientific notation?
- How does the system handle empty input, letters, zero, negative values, overflow, unsupported currencies, and invalid dates?
- What happens when BNM returns duplicate currency codes, zero or negative nominal values, a missing date, or malformed XML?
- How does the system behave when the cache exists but is for a future or later-than-requested date?
- How does the system behave if the source or target currency is missing from the selected rate set?
- What happens when the authorization or network layer times out repeatedly, or when the request is retried across multiple dates?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow the user to enter an amount, a source currency, a target currency, and a selected rate date.
- **FR-002**: The system MUST validate the amount on both the client side and the server side before accepting a conversion request.
- **FR-003**: The system MUST reject empty values, letters, negative amounts, zero, overflow, and malformed numeric strings with clear Russian-language errors.
- **FR-004**: The system MUST accept decimal values using either a comma or a dot as the decimal separator, but MUST reject grouping separators and exponential notation.
- **FR-005**: The system MUST reject values above 1,000,000,000,000 and must limit the fractional precision to no more than four decimal places.
- **FR-006**: The system MUST reject an invalid date, a future date, and missing-date situations before performing a conversion.
- **FR-007**: The system MUST support the same-currency scenario and MUST return the original amount without a network request when source and target currencies are identical.
- **FR-008**: The system MUST use the official BNM XML exchange-rate feed as the authoritative data source for supported currencies.
- **FR-009**: The system MUST support the currency set MDL, EUR, USD, RON, and GBP as valid source and target currencies when they are present in the verified BNM response, and MDL MUST be included with a rate of 1 for the base currency model.
- **FR-010**: The system MUST interpret the exchange rates according to the published rate and nominal values and MUST calculate conversion through MDL rather than by direct multiplication against an unadjusted rate.
- **FR-011**: The system MUST calculate the result in decimal arithmetic and MUST round only at the end using the AwayFromZero midpoint rule to two decimal places.
- **FR-012**: For conversions that use exchange rates, the system MUST display the selected date, the actual date of the rate used, the source reference, and the successful retrieval time in the UI. Same-currency requests display the original amount and selected date but no rate-source metadata because no rate lookup occurred.
- **FR-013**: The system MUST detect when BNM returns a rate for an earlier date than the requested date and MUST show the real course date with a warning instead of replacing it silently.
- **FR-014**: When the selected date has no publication, the system MUST search backward for the most recent valid rate within 31 calendar days before reporting a failure.
- **FR-015**: The system MUST fail safely when the network is unavailable, the HTTP response is invalid, the XML is malformed, or the source data is incomplete.
- **FR-016**: The system MUST accept valid BNM XML responses that contain additional currencies beyond the minimum supported set, ignore unsupported entries while processing the supported subset, and reject unsupported user-selected currencies separately during validation. It MUST NOT reject the whole XML response solely because extra currency entries are present.
- **FR-017**: The system MUST reject invalid data sets containing missing dates, duplicate codes, zero or negative nominal values, or zero or negative rates.
- **FR-018**: The system MUST maintain a local persistent cache of the last successfully verified exchange-rate response across restarts.
- **FR-019**: The system MUST store the cache atomically and MUST ensure that an invalid response does not overwrite a valid cache file.
- **FR-020**: The system MUST gracefully handle a missing, corrupted, or stale cache file without crashing the application or producing a false conversion result.
- **FR-021**: If the live source is unavailable and a valid saved cache exists, the system MUST show a clear message with an explicit option to use the saved rate rather than applying the cache silently.
- **FR-022**: If the cache date is later than the requested date or otherwise not valid for the requested history, the system MUST refuse to use it for that conversion and explain why; a saved rate from an earlier date is allowed only after explicit user confirmation.
- **FR-023**: The HTTP client MUST use a bounded timeout so a network problem does not block the request indefinitely; on timeout or connectivity failure, the system stops the search immediately and reports the network issue.
- **FR-024**: The system MUST use `DateOnly` for calendar dates and MUST not introduce a separate date library dependency for date arithmetic.
- **FR-025**: When a valid conversion result is already available and the cache write fails, the system MUST keep the successful calculation result for the user and show a separate warning that saving the cache was unsuccessful.
- **FR-026**: The system MUST disable the submit button while required fields are empty or invalid and while a request is in progress.
- **FR-027**: The system MUST show user-visible errors in Russian and must keep the form state understandable after a failure.
- **FR-028**: The system MUST support browser use on both desktop and mobile layouts and MUST keep form controls and error messages usable across common screen sizes.
- **FR-029**: The system MUST provide keyboard-friendly controls and labels, including support for standard keyboard submission flows.

### Key Entities

- **Currency Rate Entry**: A single BNM rate record including the currency code, its nominal value, the rate value, and the date on which the rate was published.
- **Conversion Request**: A user request containing the amount, the source currency, the target currency, the selected rate date, and the desired conversion outcome.
- **Conversion Result**: A calculated amount, the actual date of the used rate, the source reference, and the warning or status associated with the retrieval path.
- **Verified Cache Snapshot**: The last successful response from the official source, stored locally with validation metadata, used only if it is valid for the current request scenario.
- **Rate Lookup Context**: The date search state that includes the requested date, current candidate date, max search window, and latest valid publication for the request.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can successfully complete a valid conversion flow from form entry to result in under one minute under normal conditions.
- **SC-002**: For all supported valid inputs, the system produces the mathematically correct result according to the BNM nominal-based MDL formula and rounds only at the final step.
- **SC-003**: The system rejects all of the following invalid cases: empty values, letters, zero, negative amounts, overflow beyond 1,000,000,000,000, malformed decimals, unsupported currencies, missing or future dates, invalid XML, duplicate codes, zero or negative nominal values, and missing or invalid rate data.
- **SC-004**: The system remains stable and does not crash when the network is unavailable, the XML is malformed, or the cache file is missing or corrupted.
- **SC-005**: When no live rate exists for the selected date, the system finds the latest valid earlier rate within the permitted period or clearly explains the failure instead of returning a fabricated amount.
- **SC-006**: A valid cache is preserved across restarts and is only reused when it is appropriate for the request; invalid cache entries are rejected without overwriting the last known good state.
- **SC-007**: The user can complete the conversion flow on both desktop and mobile layouts without loss of essential information or interaction clarity.
- **SC-008**: Same-currency requests return the original amount immediately and never show a mocked or invented rate date because no rate lookup was performed.
- **SC-009**: A conversion result remains visible even when the cache write fails, and the user receives a separate warning that the saved cache could not be updated.

## Assumptions

- Users operate in a modern browser and can access the application through a standard web interface.
- The official BNM exchange-rate feed is the primary source of truth and may sometimes be unavailable, return malformed data, or omit a publication for a requested date.
- Supported currencies are MDL, EUR, USD, RON, and GBP when they appear in a verified BNM response; MDL is treated as the base currency with an effective rate of 1.
- The maximum accepted input amount is 1,000,000,000,000, with up to four decimal places.
- The system is expected to operate in a local development environment and does not require authentication, premium APIs, or a database.
- The cache file is a local artifact and is not committed to version control; it is treated as runtime operational data and must never be silently used when it is invalid or later than the requested date.
- When source and target currencies are the same, no BNM request or rate lookup is performed and the UI displays no fake course metadata for that path.
- Calendar dates use `DateOnly`, while retrieval timestamps use `DateTimeOffset`.
