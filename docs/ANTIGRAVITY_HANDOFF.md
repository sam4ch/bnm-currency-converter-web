# Handoff to Google Antigravity

This document contains a prompt for a future stage, not evidence of a completed build.
Start only after Copilot specifications and analysis are complete and their PR is merged.

## Preparation

1. Verify that Git contains a specification commit but no application code yet.
2. Open this repository in Google Antigravity and sign in to your account.
3. Record the exact selected model name from the interface in REPORT.md.
4. Create `feature/currency-converter` from the current `main`.

## Implementation prompt

```text
Implement Lab 3, Currency Converter, from the Spec Kit artifacts.
First read .specify/memory/constitution.md, .specify/feature.json,
spec.md, plan.md, research.md, data-model.md, quickstart.md, contracts,
tasks.md, and analysis.md for the active specification, and docs/ASSIGNMENT_REQUIREMENTS.md.
If feature.json is missing, find the single current folder in specs/;
if the choice is ambiguous, stop and explain what needs to be selected.

Work in feature/currency-converter. Complete implementation tasks in order
without changing the meaning of approved requirements. Do not invent successful completion.
The app must use C#/.NET 8 with ASP.NET Core Razor Pages, official BNM XML,
decimal and Nominal, and a clear graphical interface. Handle invalid input,
missing connectivity/publications, and same-currency conversion. Persist a
validated cache across restarts. Never apply cached rates silently.

Keep meaningful intermediate commits. Check the build and UI.
If you find a discrepancy with the documents, record the concrete case,
observation, cause, fix, and verification method in REPORT.md.
Do not claim an absence of errors before checking. Do not invent the model name:
if you cannot determine it, ask the student to copy it from the interface.
Update README with actual, verified launch commands.
Do not push directly to main or merge branches locally instead of a GitHub PR.
Unit-test tasks belong to the separate next stage in tests/currency-converter.
```

## Test prompt after the implementation PR

```text
Read the constitution, specification, plan, tasks, input requirements, and REPORT.md.
In tests/currency-converter, branched from main, implement the remaining xUnit tasks:
BNM XML and Nominal parsing; direct, inverse, cross-rate, and same-currency calculations;
rounding and overflow; empty/invalid responses; validation; offline behavior;
cache confirmation, writing, corruption, and restart; historical dates; and lookup
of the previous published rate.
Tests must be reproducible without internet access: fixtures, fake HTTP, and a fake clock.
Run dotnet test from the root and fix actual failures. Do not replace checks with
constants, exclude failing tests, or declare unexecuted tests successful.
Record actual results, test count, SDK version, and fixes in README/REPORT.md.
Update tasks.md statuses only for completed tasks.
Make separate commits for tests and discovered fixes, then create a PR.
```

After testing, check acceptance criteria manually in the browser, record the
results, and complete the report. Do not replace actual errors with invented examples.
