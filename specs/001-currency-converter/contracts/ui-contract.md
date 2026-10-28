# Contract: Razor Pages UI Contract

## Purpose

Define the server-to-page contract for the browser form and result handling.

## User-facing form fields

- amount: string
- sourceCurrency: string
- targetCurrency: string
- selectedDate: string

## UI state rules

- submit button is disabled when required fields are empty or invalid
- submit button is disabled while a request is in progress
- validation errors are shown in Russian
- decimal input accepts both `.` and `,` as valid separators
- same-currency conversion is handled as a direct path without network access
- when the cache is needed, the user sees a clear prompt and must confirm before proceeding
- same-currency requests show no invented rate metadata or source reference

## Result payload for the page model

- conversionStatus: success | validation-error | network-error | cache-fallback-required | source-unavailable
- convertedAmount: decimal or null
- actualRateDate: date or null
- selectedDate: date
- sourceReference: string or null
- warningText: string or null
- userMessage: Russian-language explanation
- sameCurrencyPath: bool

## Constraints

- no fake result on network failure
- no silent cache reuse
- does not expose internal stack traces
- must remain usable on desktop and mobile layouts
- if the cache write fails after a valid conversion, the page keeps the calculation result and shows a warning instead of reporting the conversion as failed
