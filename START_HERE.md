# Начало работы: лабораторная №3

Приложение, спецификации и тесты находятся в этом репозитории.
Актуальные команды запуска, результаты проверок и структура:
**[README.md](README.md)**.

```powershell
Set-Location -LiteralPath 'C:\Downloads\Study\4 курс\PSI\lab3'
dotnet run --project .\src\CurrencyConverter.Web\CurrencyConverter.Web.csproj --urls http://localhost:5080
```

Откройте http://localhost:5080. Требуется .NET SDK 8+ с поддержкой .NET 8.

Материалы для защиты:

- [Отчёт и фактическое применение ИИ](REPORT.md).
- [Журнал этапов](docs/PROCESS_LOG.md).
- [Результаты браузерной проверки](docs/BROWSER_VALIDATION.md).
- [Конституция](.specify/memory/constitution.md), [спецификация](specs/001-currency-converter/spec.md), [план](specs/001-currency-converter/plan.md), [задачи](specs/001-currency-converter/tasks.md).
- PR: [спецификации №1](https://github.com/sam4ch/CurrencyConverter-Lab3/pull/1), [реализация №2](https://github.com/sam4ch/CurrencyConverter-Lab3/pull/2), [тесты №3](https://github.com/sam4ch/CurrencyConverter-Lab3/pull/3).

Spec Kit и Antigravity использовались при разработке. Для обычного запуска
готового приложения устанавливать их и повторять авторизацию не нужно.
Локальные инструменты `.tools/`, коды авторизации и рабочий кэш не публикуются.
