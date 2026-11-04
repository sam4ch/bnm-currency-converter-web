# Specification stage in GitHub Copilot

Open the `lab3` repository root in VS Code and use Copilot chat in Agent mode.
Send each command separately, wait for completion, and verify its result.
The names below match the installed Spec Kit 1.2.4 skills layout.
These are agent prompts, not PowerShell commands.
The prompts below are English translations of the documented requests.

## 1. Constitution

```text
/speckit-constitution Read docs/ASSIGNMENT_REQUIREMENTS.md. Create a constitution for the currency converter project: decimal precision with BNM nominal values, a graphical Russian-language web interface, fault tolerance and a persistent cache, client/server input validation, and isolated unit tests. Specifications must precede code; require separate branches and PRs, implementation handoff to Antigravity, and a factual report. Documentation only for now: no implementation or git commit/push.
```

Expected result: a completed `.specify/memory/constitution.md`,
not the original template containing `[PROJECT_NAME]`.

## 2. Specification

```text
/speckit-specify Read docs/ASSIGNMENT_REQUIREMENTS.md and the constitution. Describe a currency converter with a graphical Russian-language interface for a student in Moldova. Include user scenarios, identified requirements, acceptance criteria, and edge cases: validation, same-currency conversion, rate date and BNM, nominal values, weekends, offline behavior, explicit confirmation to use a persistent cache, corrupt cache, and historical dates. Include every constraint from the input requirements. Create standard Spec Kit artifacts; do not write application/test code, commit/push, or switch Git branches.
```

## 3. Clarification

```text
/speckit-clarify Check the specification for ambiguity. Use the decisions from docs/ASSIGNMENT_REQUIREMENTS.md: BNM XML, selected dates in Chisinau time, backward lookup up to 31 days only when publication is missing, explicit cache confirmation, no cache newer than the requested date, decimal arithmetic, and final-only rounding. If a material question remains, ask it. Do not start implementation.
```

## 4. Technical plan

```text
/speckit-plan Read docs/ASSIGNMENT_REQUIREMENTS.md and the specification. Use C#/.NET 8, ASP.NET Core Razor Pages, a separate Core library for calculations and validation, HttpClient, System.Xml.Linq, a persistent JSON cache with atomic writes, and xUnit. Separate the parser, fetching/available-date lookup, cache, and calculations; HTTP and time must be replaceable in tests. Prepare plan.md, research.md, data-model.md, quickstart.md, and the required contracts. Verify the BNM source and Value/Nominal semantics against the primary source. Do not create application code, csproj files, or tests yet. No git commit/push.
```

## 5. Requirements checklist

```text
/speckit-checklist Create a requirements-quality checklist for calculation accuracy and Nominal, XML errors, network failures, previous-rate lookup, persistent cache with explicit confirmation, historical dates, UI validation, and compliance with the lab process. This checks requirements; it is not evidence of completed implementation.
```

## 6. Tasks

```text
/speckit-tasks Create concrete tasks with identifiers, dependencies, and file paths. Separate implementation tasks in feature/currency-converter from tests in tests/currency-converter. Cover every criterion from docs/ASSIGNMENT_REQUIREMENTS.md and map tasks to requirements. Create tasks.md only for now; do not execute tasks or perform git commit/push.
```

## 7. Pre-implementation analysis

```text
/speckit-analyze Check constitution.md, spec.md, plan.md, tasks.md, and docs/ASSIGNMENT_REQUIREMENTS.md for consistency. Save the factual report as analysis.md in the active specification folder. Identify requirement-to-task coverage, contradictions, and unresolved questions. Do not claim code is implemented or tests have passed. Implementation is prohibited at this stage.
```

Fix actual contradictions and rerun analysis if necessary.
After verification, commit documents in `specs/currency-converter`,
create a PR, and merge it into `main`. Implementation must not start before this.

## Next agent

After the PR is merged, use [ANTIGRAVITY_HANDOFF.md](ANTIGRAVITY_HANDOFF.md).
Do not run `/speckit-implement` in Copilot for this lab: the assignment requires
Google Antigravity to build the application from the documents.
