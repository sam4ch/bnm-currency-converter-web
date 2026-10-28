# Currency Converter · Лабораторная №3

Конвертер валют с графическим веб-интерфейсом, разработка по спецификации
с помощью GitHub Spec Kit, GitHub Copilot и Google Antigravity.

> Статус: подготовка окружения. Приложение ещё не реализовано.
> Наличие шаблонов Spec Kit не означает, что Copilot уже создал спецификацию.

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
