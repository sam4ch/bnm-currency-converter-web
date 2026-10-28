# Data Model

## Core value objects

### Amount

Represents the user-entered decimal amount.

Fields:
- rawText: string
- normalizedValue: decimal
- decimalPlaces: int

Validation rules:
- must not be empty
- must be positive
- must be within `0 < value <= 1_000_000_000_000`
- must accept either a dot or a comma as the decimal separator
- must not use scientific notation or grouping separators
- must not exceed four decimal places

### CurrencyCode

Represents a normalized ISO-like code for supported currencies.

Allowed values:
- `MDL`
- `EUR`
- `USD`
- `RON`
- `GBP`

Validation rules:
- uppercase normalized form
- accepted only if present in the supported set
- unsupported user-selected code is rejected at validation time

### RateDate

Represents a valid BNM publication date used for lookup and display.

Fields:
- requestedDate: DateOnly
- actualRateDate: DateOnly?
- retrievalTimestampUtc: DateTimeOffset?

Validation rules:
- requested date must be present
- requested date cannot be in the future
- actual rate date may be null when same-currency conversion bypasses the rate lookup
- actual rate date must be valid and within the historical search window when fallback is used

## Domain entities

### ConversionRequest

Represents the user action to convert one amount from one currency to another for a target date.

Fields:
- amount: Amount
- sourceCurrency: CurrencyCode
- targetCurrency: CurrencyCode
- requestedDate: DateOnly
- confirmationRequired: bool
- cacheFallbackApproved: bool

Relationships:
- belongs to one user session
- resolves to one `RateSet` during processing

### CurrencyRateEntry

Represents one BNM rate record for a single currency on a date.

Fields:
- currencyCode: CurrencyCode
- nominal: decimal
- rateValue: decimal
- effectiveMdlPerUnit: decimal
- publicationDate: DateOnly

Validation rules:
- currency code must be present and valid
- nominal must be positive
- rate value must be positive
- publication date must be valid
- source data must not contain duplicate currency rows for the same code on the same publication date
- `nominal` greater than 1 is valid and must be respected in formula calculation

### RateSet

Represents the complete validated set of rates for a particular publication date.

Fields:
- publicationDate: DateOnly
- entries: list of `CurrencyRateEntry`
- sourceUrl: string
- retrievedAtUtc: DateTimeOffset

Validation rules:
- must contain entries for all supported currencies when available
- the upstream XML may contain extra currency codes; the parser ignores these before constructing the supported, normalized Core entries
- empty rate set is invalid
- missing or corrupt metadata is invalid

### ConversionResult

Represents the final result shown to the user.

Fields:
- sourceAmount: decimal
- sourceCurrency: CurrencyCode
- targetCurrency: CurrencyCode
- convertedAmount: decimal
- rateDateUsed: DateOnly?
- requestedDate: DateOnly
- sourceReference: string?
- retrievalTimestampUtc: DateTimeOffset?
- warningText: string or null
- usedCacheFallback: bool

Validation rules:
- converted amount is decimal and rounded only at the final step; the same-currency short-circuit preserves the original amount
- `rateDateUsed`, `sourceReference`, and `retrievalTimestampUtc` are null or absent when the same-currency path is used because no source lookup occurred
- warning text is present when the actual rate date differs from the requested date
- cache fallback usage is explicitly marked

### VerifiedCacheSnapshot

Represents the last valid response that has been accepted and persisted.

Fields:
- snapshotDateUtc: DateTimeOffset
- rateSet: RateSet
- hashOrVersion: string
- cacheSourceLabel: string

Validation rules:
- must have a valid and parseable rate set
- must never be replaced by invalid data
- must never be reused if the date is later than the requested date or otherwise invalid for the request

### ValidationError

Represents a user-facing validation issue.

Fields:
- code: string
- messageKey: string
- message: string
- fieldName: string or null

Representative examples:
- amount-empty
- amount-invalid-decimal
- date-future
- currency-unsupported
- cache-invalid-for-request
- network-unavailable

## Relationships

- A different-currency `ConversionRequest` resolves to one `RateSet` or an error; a valid same-currency request resolves directly without a rate set.
- One `RateSet` contains multiple `CurrencyRateEntry` records.
- A rate-based `ConversionResult` is produced from a `RateSet` and a `ConversionRequest`; a same-currency result uses only the validated request.
- One `VerifiedCacheSnapshot` stores one valid `RateSet`.
- `RateDate` may be the requested date or the actual date used after fallback logic.

## Business rules captured in the model

- `MDL` is always base currency and must be represented with nominal `1` and rate `1` in the normalized rate set.
- `Value / Nominal` is the MDL-per-unit conversion factor.
- A request with same source and target currency is treated specially and bypasses the network and rate lookup.
- A cache entry may be used only after explicit user confirmation and only if it is valid for the requested date scenario.
- If the rate date differs from the selected date, the result must include user-visible warning text.
- Same-currency conversions must not produce fake rate metadata; the relevant course fields remain empty or null.
- `DateOnly` is used for calendar dates; `DateTimeOffset` is used for retrieval timestamps and cache metadata.
