# Lab 3 Input Requirements

These are inputs for Copilot, not a generated Spec Kit specification.
Source: the Lab 3 PDF supplied by the student, read in full.
The selected main assignment is the currency converter, not the alternative NASA NEO Tracker.

## Process and evidence

1. Create a separate GitHub repository, distinct from Labs 1/2.
2. Initialize Spec Kit with the `copilot` integration.
3. Use Copilot in VS Code for the constitution, specification, plan, tasks,
   and consistency analysis, checking intermediate results.
4. Commit specification documents before writing implementation code.
5. Use Google Antigravity to implement the saved tasks.
6. Use separate specification, implementation, and test branches; merge all
   changes into `main` through pull requests. Commits must reflect the actual process.
7. Base REPORT.md on facts: source, actual Antigravity models and their stages,
   successful results, specific errors and fixes, and conformance to the initial specification.
8. Do not invent tool/model use, tests, agent errors, screenshots, or completed
   stages. State explicitly when a stage has not been performed.

## User features

- Russian-language graphical web UI: amount, source/target currencies, selected
  rate date, conversion button, result, source, and rate date.
- Disable the button while required fields are empty/invalid and during requests.
  Validate on the server as well.
- Empty input, letters, negative or zero amounts, overflow, unsupported currencies,
  and invalid dates must produce clear errors.
- Accept a dot or comma as the decimal separator, without thousands grouping or
  exponential notation. Use a reasonable limit of 1,000,000,000,000 and at most
  four decimal places. Reject ambiguous forms.
- Same-currency conversion returns the original amount without division or a network request.
- Default to today's date in Chisinau time. Reject future dates.
- Include currency swapping, clear loading/error states, usable mobile/desktop
  layouts, field labels, and keyboard navigation.

## Source and calculations

- Source: official BNM XML over HTTPS, with no API key:
  `https://www.bnm.md/en/official_exchange_rates?get_xml=1&date=DD.MM.YYYY`.
- Required currencies: MDL, EUR, USD, RON, GBP, using only currencies actually
  present in a validated response. Add MDL as the base currency at a rate of 1.
- `Value / Nominal` is the number of MDL per unit of a currency.
- Formula: `amount * sourceMdlPerUnit / targetMdlPerUnit`.
- Perform all calculations with `decimal`; round the result to two places only
  at the end using `MidpointRounding.AwayFromZero`.
- Calculate the inverse direction from the same rate table.
- Show the actual rate date, selected date, source link, and successful retrieval
  timestamp. Explain that the rate is for reference.
- If BNM returns an earlier rate immediately, show its actual date and a warning;
  do not substitute the user's requested date.
- For an empty response or no publication on the selected date, search backward
  for the latest available rate, no more than 31 calendar days. If exhausted,
  show an error and offer a suitable cache.
- Network/HTTP failures, timeouts, malformed XML, or invalid data must not crash
  the app or produce a fabricated result.
- Reject empty currency sets, missing dates, dates later than the requested date,
  duplicate codes, and zero/negative nominal values or rates.
- Network timeouts must not accumulate into a minutes-long wait while offline:
  stop the lookup on network failure instead of repeating it for every date.

## Persistent cache and offline behavior

- Save the latest successfully validated response to a local file that survives
  server restarts. Do not commit the cache folder to Git.
- Write atomically so concurrent requests cannot corrupt the file. An invalid
  response must not overwrite valid cached data. Write failure must preserve
  the fetched rate, while informing the user that the cache could not be saved.
- If the source is unavailable, show a clear message and an explicit offer to
  use the saved rate with its date. Never apply the cache silently.
- Handle a corrupt cache file without crashing.
- For historical requests, do not use a cache newer than the requested date.
- If no suitable cache exists, explain that conversion between different
  currencies is currently unavailable and allow retrying.

## Technical choices for the planning stage

C#, .NET 8, ASP.NET Core Razor Pages, a small Core library with pure calculation
and validation logic, HttpClient, System.Xml.Linq, a JSON cache file, and xUnit.
No mandatory Node.js, database, authentication, or paid services.
The data provider and clock must be replaceable in tests.

## Required validation

- Unit tests for valid BNM XML parsing, including Nominal > 1 and MDL.
- Unit tests for direct, inverse, cross-rate, and same-currency calculations,
  rounding, and overflow.
- Unit tests for empty/corrupt XML, nonpositive values, duplicate codes,
  missing dates, and missing currencies.
- Unit tests for input validation and both decimal separator forms.
- Offline tests with/without a cache, unsuitable dates/corrupt cache, previous
  publication lookup, and cache persistence across service instances.
- Use fixtures and fake HTTP handlers, not live BNM, in tests.
- One command from the root: `dotnet test`. Document the final command and web
  launch instructions in README after checking them against the actual project.
- Check the browser UI manually as a separate activity; do not present this as unit testing.
