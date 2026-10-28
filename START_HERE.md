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

Это подготовка, не готовая лабораторная. Шаблон конституции и копии skills
сами по себе не являются готовыми спецификациями.

## Текущий порядок действий

1. Открыть эту папку в VS Code, открыть Copilot Chat (`Ctrl+Alt+I`).
2. Проверить завершение уже отправленного запроса `/speckit-constitution`.
   Сейчас Copilot ожидает подтверждения `Run pwsh command?` для команды:

   ```powershell
   pwsh -NoLogo -NoProfile -File .specify/scripts/powershell/resolve-template.ps1 constitution-template -Json
   ```

   Она определяет путь к шаблону конституции. После проверки запроса нажать
   `Allow` в Copilot, затем дождаться результата. Не включать глобальное
   разрешение всех команд ради одного запроса.
3. Пройти [команды Copilot](docs/COPILOT_PROMPTS.md) по одной.
4. Проверить и закоммитить документы до создания кода, открыть PR спецификаций.
5. После слияния PR перейти к [Antigravity](docs/ANTIGRAVITY_HANDOFF.md).

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
