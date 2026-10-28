# Quickstart and Validation Guide

## Purpose

This guide describes how the implemented feature should be validated once the code is written. It is a documentation artifact only; no implementation or execution is performed here.

## Prerequisites

- .NET 8 SDK installed
- local environment capable of running ASP.NET Core applications
- browser access to the local Razor Pages application
- optional: file access permissions for the local cache folder

## Planned project startup

1. Restore dependencies with the .NET CLI.
2. Start the web application from the repository root.
3. Open the local browser page for the conversion form.
4. Verify the page loads in Russian and includes labels and validation feedback.

## Planned validation scenarios

### Valid conversion path

1. Enter a positive amount such as `100.50`.
2. Select `USD` as source and `EUR` as target.
3. Choose a valid historical date in the past.
4. Submit the form.
5. Confirm the result is displayed with the actual rate date, source URL, retrieval time, and no warning if the date matches.

### Same currency path

1. Choose the same source and target currency.
2. Submit the form.
3. Confirm the system returns the same amount without network fetch.

### Historical fallback path

1. Select a date without a publication.
2. Submit the form.
3. Confirm the system searches the previous valid publication within the 31-day limit, then displays the actual course date and warning if needed.

### Error path for invalid input

1. Submit a blank form.
2. Try letters, zero, negative values, or malformed decimals.
3. Confirm the submit button is disabled and clear Russian-language errors are shown.

### Cache fallback path

1. Simulate a network failure while a valid cache exists.
2. Confirm the system asks for explicit user confirmation before using the saved rate.
3. Confirm a later-than-requested cached date is rejected.

### Malformed source and corrupted cache

1. Simulate invalid BNM XML or missing fields.
2. Confirm the app never returns a fake result.
3. Confirm a corrupted cache is handled gracefully and no crash occurs.

## Acceptance evidence checklist

- UI in Russian and readable on both desktop and mobile widths
- consistent validation on client and server
- decimal arithmetic and nominal-based formula correctness
- no silent acceptance of unsupported or malformed values
- no fake result shown when data is missing or the network fails
- no cache overwrite of valid data with invalid data
- explicit prompt before reusing earlier cached data
- final result includes actual rate date, source reference, and retrieval timestamp

## Notes

This document is intentionally implementation-agnostic. The exact commands and file names may vary with the final project structure, but the scenarios above reflect the requirements and constitution and should be used as design validation criteria.
