# Currency Converter · Лабораторная №3

Конвертер валют с графическим веб-интерфейсом на ASP.NET Core Razor Pages (.NET 8), разработанный по утверждённой спецификации в рамках лабораторной работы №3.

> **Статус**: Реализация задач T001–T019 (Core, Infrastructure, Web Razor Pages UI) завершена в ветке `feature/currency-converter`.
> Unit-тесты T020–T027 планируются в отдельной ветке `tests/currency-converter` после завершения этапа реализации.

## Архитектура и стек технологий

| Компонент | Технологическое решение |
| --- | --- |
| Платформа и язык | C# на .NET 8 (C# 12) |
| Пользовательский интерфейс | ASP.NET Core Razor Pages, русскоязычный интерфейс, адаптивный дизайн (ПК и мобильные устройства), без внешних CDN/библиотек |
| Источник данных | Официальный XML Национального банка Молдовы: `https://www.bnm.md/en/official_exchange_rates?get_xml=1&date=DD.MM.YYYY` |
| Поддерживаемые валюты | `MDL` (базовая валюта, курс 1), `EUR`, `USD`, `RON`, `GBP` |
| Математика пересчёта | Строго в `decimal`: `amount * (ValueSource / NominalSource) / (ValueTarget / NominalTarget)`, округление середины от нуля (`MidpointRounding.AwayFromZero`) до 2 знаков |
| Одинаковые валюты | Мгновенный возврат суммы без обращения к источнику и кэшу, без фиктивных метаданных курса |
| Постоянный кэш | Локальный атомарный JSON-файл в `App_Data/rates_cache.json`, сохраняемый между перезапусками сервера |
| Автономный режим (offline) | Предложение использовать проверенный кэш с явным подтверждением пользователя (`CacheConfirmationRequired`), запрет кэша с датой позже запрошенной |

## Запуск приложения

### 1. Сборка решения
```powershell
dotnet build CurrencyConverter.sln
```

### 2. Запуск веб-приложения
```powershell
dotnet run --project src/CurrencyConverter.Web/CurrencyConverter.Web.csproj
```
После запуска откройте в браузере локальный адрес (обычно `http://localhost:5000` или `https://localhost:5001`).

### 3. Автоматизированные тесты (будущий этап)
```powershell
dotnet test CurrencyConverter.sln
```
> Примечание: тела xUnit-тестов разрабатываются на следующем Git-этапе в ветке `tests/currency-converter`.

## Локальное хранилище кэша

Файл кэша сохраняется по пути:
`src/CurrencyConverter.Web/App_Data/rates_cache.json`
- Папка `App_Data` исключена из системы контроля версий Git через `.gitignore`.
- Запись выполняется атомарно через временный файл в том же каталоге с синхронизацией `SemaphoreSlim`.
- При недоступности сервера BNM приложение предлагает использовать этот файл только после явного подтверждения пользователем.

## Документация проекта

- [Спецификация приложения](specs/001-currency-converter/spec.md)
- [Технический план реализации](specs/001-currency-converter/plan.md)
- [Задачи и статус (T001–T027)](specs/001-currency-converter/tasks.md)
- [Модель данных](specs/001-currency-converter/data-model.md)
- [Контракт UI Razor Pages](specs/001-currency-converter/contracts/ui-contract.md)
- [Ручной чек-лист UI (M001–M007)](specs/001-currency-converter/checklists/manual-browser-checklist.md)
- [Отчёт о ходе разработки](REPORT.md)
