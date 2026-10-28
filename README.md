# Конвертер валют BNM

**Лабораторная №3 · C# / .NET 8 · Spec Kit → Copilot → Antigravity**

Русскоязычное веб-приложение для конвертации **MDL, EUR, USD, RON и GBP** по
официальным курсам Национального банка Молдовы. Можно выбрать дату,
поменять валюты местами и подтвердить использование сохранённого курса,
если источник временно недоступен.

![Интерфейс приложения](docs/screenshots/desktop.png)

## Быстрый запуск

Нужен **.NET SDK 8 или новее** с поддержкой .NET 8. Для расчёта новых курсов
нужен интернет; API-ключ, Node.js и база данных не требуются.

Откройте папку проекта в VS Code и выполните в PowerShell:

```powershell
Set-Location -LiteralPath 'C:\Downloads\Study\4 курс\PSI\lab3'
dotnet restore
dotnet run --project .\src\CurrencyConverter.Web\CurrencyConverter.Web.csproj --urls http://localhost:5080
```

Откройте **http://localhost:5080**. Остановка сервера — `Ctrl+C`.
Если проект скачан в другую папку, укажите её в `Set-Location`.
В текущем каталоге должен находиться `CurrencyConverter.sln`.

## Возможности

| Возможность | Поведение |
| --- | --- |
| Ввод суммы | Точка или запятая; от 0.0001 до 1 000 000 000 000; до четырёх дробных знаков |
| Проверка данных | На клиенте и сервере; неверный ввод блокирует расчёт |
| Выбор даты | По умолчанию сегодня в Кишинёве; будущие даты запрещены |
| Исторический курс | Поиск ближайшей публикации в пределах 31 предыдущего дня; показана реальная дата |
| Формула | `amount × (ValueSource / NominalSource) / (ValueTarget / NominalTarget)` |
| Точность | `decimal`; округление результата до двух знаков, середина от нуля (`AwayFromZero`) |
| Одинаковые валюты | Точная исходная сумма до четырёх знаков; запрос курса не нужен |
| Кэш | JSON-файл сохраняется между перезапусками; атомарная запись |
| Нет сети | Подходящий кэш предлагается с датой и применяется после явного подтверждения |
| Исторический запрос | Кэш с датой позже выбранной использовать нельзя |
| Интерфейс | Адаптивная вёрстка, управление клавиатурой, состояния загрузки и ошибок |

Источник: [официальный BNM XML](https://www.bnm.md/en/official_exchange_rates?get_xml=1&date=28.10.2026).
Официальный курс является справочным. Курсы получаются по сети, в код приложения не зашиты.

## Проверка

Из корня репозитория:

```powershell
dotnet build CurrencyConverter.sln
dotnet test
```

**Проверенный результат:** 162 тестов прошли, 0 ошибок, 0 пропусков.
Unit-тесты используют фикстуры и подменённые HTTP-обработчики, работают без BNM и интернета.
Сценарии Opera GX, мобильная вёрстка и реальные скриншоты описаны отдельно в
[протоколе проверки интерфейса](docs/BROWSER_VALIDATION.md).

## Где хранится кэш

```text
src/CurrencyConverter.Web/App_Data/rates_cache.json
```

Файл появляется после успешного получения курса. `App_Data` исключён из Git.
Перезапуск приложения файл не удаляет. Путь можно изменить параметром
`Cache:FilePath` в `appsettings.json` либо переменной окружения `Cache__FilePath`.
Повреждённый кэш отвергается; ошибка записи показывается предупреждением,
при этом полученный результат остаётся доступен.

## Структура проекта

```text
CurrencyConverter.sln
src/
  CurrencyConverter.Core/            # модели, валидация, расчёты
  CurrencyConverter.Infrastructure/  # BNM XML, JSON-кэш, время Кишинёва
  CurrencyConverter.Web/             # Razor Pages, локальные CSS/JS
tests/
  CurrencyConverter.Tests/           # xUnit, фикстуры, подмены
specs/001-currency-converter/         # spec, plan, tasks, contracts
docs/                               # требования, процесс, браузерные проверки
REPORT.md                           # отчёт о работе ИИ, ошибках и исправлениях
```

## Как выполнена лабораторная

| Этап | Инструмент и результат | Pull Request |
| --- | --- | --- |
| Спецификации | Spec Kit 1.2.4 + Copilot в VS Code: конституция, спецификация, план, задачи, анализ | [№1](https://github.com/sam4ch/CurrencyConverter-Lab3/pull/1) |
| Реализация | Antigravity CLI 1.4.9, **gemini-3.8-flash-high**: Core, Infrastructure, Razor Pages | [№2](https://github.com/sam4ch/CurrencyConverter-Lab3/pull/2) |
| Тестирование | Antigravity: основной набор xUnit; ассистент Codex: дополнительные regression-тесты, доработки по ревью и запуск проверок | [№3](https://github.com/sam4ch/CurrencyConverter-Lab3/pull/3) |

Работа ведётся через отдельные ветки `specs/currency-converter`,
`feature/currency-converter`, `tests/currency-converter` и слияния PR в `main`.
История отражает последовательность этапов; документы спецификации созданы до кода.
Точное имя модели Copilot в режиме Auto не установлено; это отмечено в отчёте.
Antigravity исчерпал квоту после создания основного набора тестов;
заключительные доработки выполнены в текущем чате Codex и отдельно указаны в отчёте.

## Документация

- [Отчёт о лабораторной: модели, ошибки, исправления и результаты](REPORT.md)
- [Исходные требования из PDF](docs/ASSIGNMENT_REQUIREMENTS.md)
- [Спецификация](specs/001-currency-converter/spec.md) · [План](specs/001-currency-converter/plan.md) · [Задачи](specs/001-currency-converter/tasks.md)
- [Конституция проекта](.specify/memory/constitution.md)
- [Модель данных](specs/001-currency-converter/data-model.md) · [Контракты](specs/001-currency-converter/contracts)
- [Браузерная проверка и скриншоты](docs/BROWSER_VALIDATION.md)
- [Результаты тестов](docs/TEST_RESULTS.md) · [Использованные промпты Antigravity](docs/prompts)
- [Чек-лист UI](specs/001-currency-converter/checklists/manual-browser-checklist.md)
