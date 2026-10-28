# Начало работы: лабораторная №3

## Что уже подготовлено

- Отдельный локальный репозиторий в `C:\Downloads\Study\4 курс\PSI\lab3`.
- Отдельный закрытый репозиторий GitHub: [CurrencyConverter-Lab3](https://github.com/sam4ch/CurrencyConverter-Lab3).
- Начальный коммит `634a898` без реализации приложения.
- Ветка `specs/currency-converter` для этапа спецификаций.
- GitHub Spec Kit 1.2.4, инициализированный с `--integration copilot --script ps`.
- Штатные skills Copilot в `.github/skills/` и шаблоны `.specify/`.
- Локальный Antigravity CLI 1.4.9; SHA512 загрузки проверен.
- Входные требования и запросы для двух агентов в `docs/`.

Copilot уже создал конституцию, спецификацию, план, модели, контракты и задачи.
Итоговая проверка описана в specs/001-currency-converter/analysis.md.
Это завершение подготовки документов, а не готовое приложение.

## Текущий порядок действий

1. Проверить состояние [PR спецификаций №1](https://github.com/sam4ch/CurrencyConverter-Lab3/pull/1).
2. После сохранения документов и слияния PR открыть актуальный main в Antigravity.
3. Войти в свой Google-аккаунт и записать точное имя выбранной модели.
4. Создать ветку `feature/currency-converter` от main и передать агенту
   [подготовленную инструкцию](docs/ANTIGRAVITY_HANDOFF.md).
5. После PR реализации создать `tests/currency-converter`, выполнить тесты,
   ручной checklist и закончить REPORT.md. В Copilot реализацию не запускать.

Версия установленного Spec Kit проверяется из PowerShell в папке `lab3`:

```powershell
& ".\.tools\spec-kit\Scripts\specify.exe" version
& ".\.tools\spec-kit\Scripts\specify.exe" integration status --json
```

Локальный CLI Antigravity запускается так:

```powershell
& ".\.tools\antigravity\agy.exe"
```

При первом запуске может потребоваться вход в Google. Авторизацию выполняет
студент в браузере; пароль и коды не нужно записывать в файлы или присылать в чат.
Можно использовать и официальное [приложение Antigravity](https://antigravity.google/download).

Проверка `agy models` в пользовательском контексте уже подтвердила:
`Please sign in to view available models`. Для продолжения вход действительно нужен.

## Ветки и Pull Request

| Этап | Ветка | Что попадает в PR |
| --- | --- | --- |
| Спецификации | `specs/currency-converter` | Конституция, спецификация, план, задачи, анализ |
| Реализация | `feature/currency-converter` | Код приложения и проверенные команды запуска |
| Тесты | `tests/currency-converter` | Unit-тесты, исправления по результатам тестов, отчёт |

Каждая следующая ветка создаётся от `main` после слияния предыдущего PR.
Использовать обычное слияние или rebase с сохранением осмысленных коммитов,
чтобы история показывала процесс. Не собирать всё в единственный финальный коммит.
