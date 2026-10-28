# Currency Converter · Лабораторная №3

Конвертер валют с графическим веб-интерфейсом, разработка по спецификации
с помощью GitHub Spec Kit, GitHub Copilot и Google Antigravity.

> Статус: спецификации подготовлены и проверены до реализации.
> Приложение и unit-тесты ещё не созданы; следующий этап — Google Antigravity.

Этап документов: [PR №1](https://github.com/sam4ch/CurrencyConverter-Lab3/pull/1).
Подготовлены 29 функциональных требований, 9 критериев успеха и 27 задач.

## Выбранное решение

| Часть | Решение |
| --- | --- |
| Язык | C# |
| Платформа | .NET 8, ASP.NET Core Razor Pages |
| Интерфейс | Русскоязычная страница в браузере |
| Источник | Официальный XML Национального банка Молдовы (BNM) |
| Расчёты | `decimal`, пересчёт через MDL с учётом номинала |
| Кэш | Локальный файл с последним успешно проверенным ответом |
| Unit-тесты | xUnit; планируемая команда `dotnet test` из корня |

## Порядок выполнения

1. [Открыть пошаговую инструкцию](START_HERE.md).
2. В Copilot отдельно выполнить команды Spec Kit из
   [набора промптов](docs/COPILOT_PROMPTS.md).
3. Проверить документы, закоммитить их в ветке спецификаций и создать PR.
4. После слияния PR передать спецификации Antigravity в отдельной ветке.
5. Реализовать тесты в отдельной ветке, проверить приложение и оформить отчёт.

## Документация

- [Спецификация приложения](specs/001-currency-converter/spec.md)
- [Технический план](specs/001-currency-converter/plan.md)
- [Задачи и покрытие требований](specs/001-currency-converter/tasks.md)
- [Анализ перед реализацией](specs/001-currency-converter/analysis.md)
- [Ручной чек-лист UI — пока не выполнен](specs/001-currency-converter/checklists/manual-browser-checklist.md)
- [Требования к приложению и критерии приёмки](docs/ASSIGNMENT_REQUIREMENTS.md)
- [Промпты для этапа Copilot](docs/COPILOT_PROMPTS.md)
- [Передача проекта в Antigravity](docs/ANTIGRAVITY_HANDOFF.md)
- [Журнал фактического выполнения](docs/PROCESS_LOG.md)
- [Отчёт — заполняется по результатам работы](REPORT.md)

Команды запуска приложения будут добавлены после его фактической реализации
и проверки. До этого `dotnet run` в этой папке не запустит конвертер.

## Первоисточники

- [GitHub Spec Kit: установка](https://github.github.io/spec-kit/installation.html)
- [Spec Kit: последовательность этапов](https://github.github.io/spec-kit/quickstart.html)
- [Интеграция Copilot](https://github.github.io/spec-kit/integrations/copilot.html)
- [Google Antigravity: начало работы](https://antigravity.google/docs/getting-started)
- [BNM: официальные курсы](https://www.bnm.md/en/official_exchange_rates)
