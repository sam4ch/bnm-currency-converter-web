# Research and Design Decisions

## Decision 1: Use the official BNM XML feed and parse it with LINQ to XML

**Decision**: The application will fetch the official BNM XML by date and parse it with `System.Xml.Linq` (XDocument/XElement) into a normalized internal model.

**Rationale**: The assignment explicitly requires the official BNM XML feed and calls out XML parsing, nominal handling, and MDL conversion. LINQ to XML is a lightweight and reliable match for a structured RSS-like XML feed and supports test fixtures well.

**Alternatives considered**:
- DOM-based XML parsing: heavier and less concise for this feature
- direct string parsing: fragile and error-prone for malformed XML
- non-BNM data sources: rejected because the project definition requires official BNM data

## Decision 2: Validate and compute using decimal arithmetic with nominal-aware MDL conversion

**Decision**: All calculations will use `decimal`; conversion will use `amount * sourceMdlPerUnit / targetMdlPerUnit` after computing `mdlPerUnit = rateValue / nominal`.

**Rationale**: This matches the assignment and prevents floating-point drift. It also preserves the denomination semantics required by BNM data.

**Alternatives considered**:
- using `double` or `float`: rejected due to rounding risk and assignment requirements
- skipping nominal handling: rejected because the lab explicitly requires nominal-aware conversion

## Decision 3: Keep the supported currency set explicit and ignore unrelated XML entries

**Decision**: Supported currencies are `MDL`, `EUR`, `USD`, `RON`, and `GBP` when present in a valid BNM response. The parser may ignore extra currencies in a valid feed, but the user cannot select unsupported currencies.

**Rationale**: This satisfies the requirement that additional currencies in a correct BNM XML response do not invalidate the whole response and preserves robust handling of real-world output that may contain more than the minimum set.

**Alternatives considered**:
- rejecting any feed containing unknown codes: rejected because it would discard valid BNM data
- allowing arbitrary currencies from the feed: rejected because the UI and validation must remain consistent and intentionally limited

## Decision 4: Use a local JSON cache with atomic write strategy

**Decision**: The application will persist the last valid BNM response to a local JSON file and write it atomically by staging to a temporary file and replacing the active file on success.

**Rationale**: This satisfies the requirement for persistent verified cache across restarts and avoids file corruption under concurrent requests or partial write failures.

**Alternatives considered**:
- direct in-place write: rejected because it risks corruption
- caching in memory only: rejected because the requirement calls for persistence across restarts
- caching invalid responses: rejected because it violates the verified-cache policy

## Decision 5: Search backward within a 31-day window and require explicit consent for older cached rate reuse

**Decision**: If the chosen date is missing, the app will search backwards for the most recent valid earlier publication within 31 days. If the only viable path is a saved cached rate from an earlier date, the user must explicitly confirm the fallback before it is used.

**Rationale**: This matches the lab requirement and preserves user trust and correctness. It also prevents silent use of stale or time-invalid data.

**Alternatives considered**:
- automatically using the cache without prompting: rejected because the constitution and requirement require explicit user confirmation
- using a rate from a later date than the requested date: rejected as invalid and prohibited

## Decision 6: Inject time and HTTP dependencies for deterministic unit testing

**Decision**: The design will inject `IClock` and a rate-client abstraction so tests can provide fixed time and synthetic HTTP responses without real network access.

**Rationale**: The assignment demands unit tests without a live BNM feed and calls out time and HTTP substitution in tests.

**Alternatives considered**:
- direct `DateTime.Now` and `HttpClient` usage in production code: rejected because it prevents stable tests
- static global date/time hooks: rejected as harder to control and more brittle

## Decision 7: Distinguish user-input rejection from source-data rejection

**Decision**: User-selected unsupported currencies are rejected separately from malformed BNM feeds. The feed parser is resilient to additional XML entries, but the UI and server ensure only supported selections are accepted.

**Rationale**: This aligns with the clarification of FR-016 and preserves compatibility with real BNM XML that may include additional codes beyond the app’s minimum set.

**Alternatives considered**:
- rejecting the whole XML if any unsupported codes exist: rejected because it would break valid BNM responses unnecessarily
- silently accepting any code from the feed: rejected because user selection must remain controlled and validated externally

## Decision 8: Use explicit domain errors for all failure modes

**Decision**: The app will normalize errors into domain-level exceptions and user-facing messages rather than generic exceptions or fake output.

**Rationale**: This keeps the UI clear and ensures errors remain predictable across network, parsing, validation, and cache scenarios.

**Alternatives considered**:
- raw exceptions to the page model: rejected because it makes UI logic brittle and user messages inconsistent
- silently falling back to previous values: rejected because it violates the requirement for explicit confirmation and correctness

## Open issues resolved from the clarification flow

- Maximum amount limit: `1,000,000,000,000`
- Supported currencies: `MDL`, `EUR`, `USD`, `RON`, `GBP`
- MDL base rate: included with `nominal = 1` and `rate = 1`
- Cache rule: valid cache may be used only when explicitly confirmed and never when it is after the requested date
- Button state: disabled when required fields are empty or invalid; disabled while request is in progress
- Rejection criterion: all invalid cases listed in the spec must be rejected, not only a statistical subset
