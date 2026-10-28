# Contract: Conversion Service

## Purpose

Accept a validated user conversion request and return the correct output or a precise domain error.

## Responsibilities

- validate request semantics before conversion
- detect same-currency scenario and return the original amount without any source call
- resolve source and target MDL-per-unit factors
- apply decimal arithmetic and final rounding only at the end
- support fallback to earlier valid rates within the lookup window
- track whether the result used live data, fallback data, or no rate lookup because the currencies match
- keep the calculation result valid even when the cache write fails, reporting the cache issue as a warning instead of a conversion failure

## Inputs

- amount: decimal-like string or normalized amount value
- sourceCurrency: supported code
- targetCurrency: supported code
- requestedDate: DateOnly
- optional cache fallback approval flag

## Outputs

- success: `ConversionResult` with converted amount, actual rate date, warnings, and source metadata when applicable
- failure: `ValidationError` or domain error describing missing rates, invalid data, or explicit cache rejection

## Constraints

- no floating-point math
- no silent cache reuse without explicit user confirmation
- no use of a cache where the cached date is later than the requested date
- no fabricated fallback when no valid earlier rate exists within the allowed window
- same-currency requests bypass both the BNM source lookup and the local cache lookup
- same-currency results must not invent rate metadata; actual course date and source reference remain empty or null
- cache write failure is surfaced as a warning and does not invalidate an otherwise successful conversion result

## Key result fields

- convertedAmount
- rateDateUsed
- sourceReference
- retrievedAtUtc
- warningText
- usedCacheFallback
- sameCurrencyPath: bool
