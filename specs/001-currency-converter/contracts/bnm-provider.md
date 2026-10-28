# Contract: BNM Rate Provider

## Purpose

Provide a verified exchange-rate source interface for the application without coupling the UI or Core layer to HTTP or XML implementation details.

## Responsibilities

- fetch BNM XML for a target date
- validate the HTTP response and XML payload
- build a normalized rate set for supported currencies
- return domain-level errors for network, HTTP, data, or format problems
- allow testing with fake handlers and fixed response payloads
- accept valid documents that contain extra currencies and ignore unsupported entries while preserving the supported subset

## Inputs

- requestedDate: the date for which the rates are required
- sourceUrl: the API endpoint, built in the infrastructure layer

## Outputs

- success: a validated `RateSet` with source metadata and retrieval timestamp
- failure: a domain error representing network, XML, or validation issues

## Constraints

- no real network calls in unit tests
- must never return a fabricated result for an invalid or missing data set
- must refuse invalid XML or unsupported currency structures without breaking the app
- a valid XML document with extra currencies is accepted as long as the supported subset is present and valid
- unsupported user-selected currencies are rejected separately during validation, not by failing the whole XML payload

## Data contract summary

- sourceUrl: string
- publicationDate: DateOnly
- retrievedAtUtc: DateTimeOffset
- currencyEntries: list of normalized rate entries
- warnings: list of informational warnings, if any
