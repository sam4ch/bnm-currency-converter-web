# Contract: Local Cache Store

## Purpose

Persist the last verified BNM response and provide safe retrieval for offline or degraded conditions.

## Responsibilities

- save the latest valid snapshot atomically
- read the cached document safely and validate it before reuse
- preserve a valid cache when a new response is invalid
- reject stale, future, or otherwise invalid cache usage for a given request
- surface file-read or file-write errors without crashing the application
- keep the current conversion result valid even if the cache write fails

## Inputs

- validated `RateSet` to store
- requestedDate for cache suitability checks
- optional explicit approval flag for cached fallback

## Outputs

- success: valid cache entry or fallback result
- failure: cache miss, corruption, stale-date error, or write failure message

## Constraints

- invalid responses do not overwrite the previous good cache
- the cache file is runtime-only and not committed to Git
- cache reuse is explicit and user-visible
- a corrupt cache file must be handled gracefully without causing a false result
- if a valid calculation has already completed and the cache write fails, the result remains valid and the cache issue is reported as a warning rather than a calculation failure
- cache entries dated after the requested date are rejected for that request

## Storage format

- local JSON file
- atomic write pattern with temporary file + replace
- metadata includes retrieval timestamp and source date
