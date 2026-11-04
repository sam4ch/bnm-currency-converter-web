# Getting started: Lab 3

The application, specifications, and tests are in this repository.
For current launch commands, validation results, and project structure, see
**[README.md](README.md)**.

Open a PowerShell terminal in the repository root:

```powershell
dotnet run --project .\src\CurrencyConverter.Web\CurrencyConverter.Web.csproj --urls http://localhost:5080
```

Open http://localhost:5080. Requires .NET SDK 8+ with .NET 8 support.

Materials for the lab presentation:

- [Report and actual AI use](REPORT.md).
- [Process log](docs/PROCESS_LOG.md).
- [Browser validation results](docs/BROWSER_VALIDATION.md).
- [Constitution](.specify/memory/constitution.md), [specification](specs/001-currency-converter/spec.md), [plan](specs/001-currency-converter/plan.md), and [tasks](specs/001-currency-converter/tasks.md).
- PRs: [specifications #1](https://github.com/sam4ch/bnm-currency-converter-web/pull/1), [implementation #2](https://github.com/sam4ch/bnm-currency-converter-web/pull/2), and [tests #3](https://github.com/sam4ch/bnm-currency-converter-web/pull/3).

Spec Kit and Antigravity were used during development. To run the completed
application normally, you do not need to install them or repeat authorization.
Local tools in `.tools/`, authorization codes, and runtime cache are not published.
