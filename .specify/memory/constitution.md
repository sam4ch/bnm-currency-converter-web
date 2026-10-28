<!--
Sync Impact Report
- Version change: 0.0.0 -> 1.0.0
- Modified principles: new constitution initialized for the project
- Added sections: Additional Constraints; Development Workflow
- Removed sections: template placeholder sections and bracketed tokens
- Deferred TODOs: none
-->

# Конституция проекта конвертера валют

## Core Principles

### I. Russian-First User Experience
The application MUST present a graphical web interface in Russian, with clear labels, field hints, validation messages, and error states. Users MUST be able to understand the conversion flow without referring to English-only instructions or hidden assumptions. This keeps the product aligned with the assignment and reduces user confusion during real-world use.

### II. .NET 8 Razor Pages Architecture
The project MUST be implemented with C# on .NET 8 using ASP.NET Core Razor Pages. The UI, request handling, and server-side validation MUST remain within the Razor Pages model unless a requirement explicitly demands a different approach and documents it. This preserves a single, traceable architecture consistent with the lab constraints.

### III. Official Data Source and Exact Decimal Arithmetic
The application MUST use the official BNM XML exchange-rate feed as the source of truth for currency values. All conversion arithmetic MUST use decimal data types instead of floating-point arithmetic. When converting through MDL, the calculation MUST include the published nominal value for each currency and must not ignore denomination semantics. This prevents rounding drift and ensures mathematically correct conversion results.

### IV. Input Validation and Operational Safety
The application MUST validate that the monetary amount is a positive decimal value on both the client side and the server side. Empty, zero, negative, or malformed values MUST be rejected with explicit Russian-language validation errors. The server MUST also protect against invalid requests and unexplained data issues by returning clear errors for malformed XML, missing currency data, unavailable rate dates, and network failures. This guarantees safe calculation paths and understandable user-facing feedback.

### V. Reliable Rates, Verified Cache, and Testability
The application MUST select the last valid available exchange rate for the required date and MUST maintain a verified local cache across restarts. Cached values MUST only be reused after successful validation, and stale or invalid data MUST trigger a refresh or an explicit error. Unit tests MUST validate business logic and parsing behavior without depending on real network access; they MUST isolate external dependencies through controlled fixtures or mocks. This keeps the service reliable while preserving test determinism.

## Additional Constraints

- The project MUST use the BNM XML feed as the authoritative currency source and MUST not substitute unofficial or manual data.
- Monetary calculations MUST be performed with decimal arithmetic and MUST include nominal-based conversion through MDL when required by the exchange-rate data model.
- The application MUST keep the last successfully verified rate data in a local cache and reuse it between restarts when the source is unavailable.
- The user interface MUST be a graphical browser interface in Russian and MUST provide understandable validation and error text.
- The converter MUST reject impossible inputs, missing data, network errors, and malformed XML with explicit, user-readable messages.
- No fabricated models, completed tests, or reported results may be claimed before they are actually created and validated.

## Development Workflow

- Before implementation, the team MUST complete and review the specification, plan, tasks, and consistency analysis for the feature.
- The specification and planning artifacts MUST be checked for internal consistency before any code is written.
- All unit tests MUST be written to validate expected behavior without real network calls or production data dependencies.
- Implementation and testing MUST remain aligned with the constitution: no undocumented shortcuts, no silent data assumptions, and no hidden logic changes.
- Review gates MUST confirm adherence to the project requirements and the approved design before accepting code.

## Governance

This constitution governs all project decisions related to the currency converter laboratory. It supersedes informal practices when there is a conflict with the stated requirements, architecture, data validity rules, or documentation expectations.

Amendments MUST be documented in the project constitution, reviewed for impact, and versioned according to the rules below. Any change that affects the architecture, validation policy, data-source rules, or testing requirements MUST be explicitly justified before approval. A change is not valid unless the corresponding documentation and review record are updated.

Versioning policy:
- MAJOR: backward-incompatible governance, principle removal, or material redefinition of a rule
- MINOR: a new principle or section is added, or an existing rule is materially expanded
- PATCH: clarifying wording, typo fixes, formatting, or non-semantic improvements

Compliance review expectations:
- All code, tests, and documentation MUST remain consistent with this constitution.
- Any implementation decision that changes the approved architecture, data source, or validation model MUST be reviewed before merge.
- The project MUST avoid claiming completed work, test coverage, or results that were not actually produced and verified.

**Version**: 1.0.0 | **Ratified**: 2026-10-28 | **Last Amended**: 2026-10-28
