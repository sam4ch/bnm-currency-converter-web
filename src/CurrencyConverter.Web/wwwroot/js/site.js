document.addEventListener('DOMContentLoaded', function () {
    const amountInput = document.getElementById('amount');
    const sourceSelect = document.getElementById('sourceCurrency');
    const targetSelect = document.getElementById('targetCurrency');
    const dateInput = document.getElementById('rateDate');
    const convertButton = document.getElementById('convertButton');
    const swapButton = document.getElementById('swapCurrencies');
    const amountError = document.getElementById('amountError');
    const dateError = document.getElementById('dateError');
    const form = document.getElementById('conversionForm');

    if (!amountInput || !convertButton || !dateInput) {
        return;
    }

    /**
     * Валидация суммы строго совпадает с грамматикой Core AmountValidator:
     * - Принимает цифры с одним необязательным десятичным разделителем (точка или запятая)
     * - Дробная часть: 1-4 цифры
     * - Запрещены: ведущий плюс, отрицательные числа, '.5', '1.', пробелы и группировка
     * - Ограничение: 0 < amount <= 1 000 000 000 000
     */
    function validateAmount(val) {
        if (!val) {
            return { valid: false, error: "Введите сумму для конвертации." };
        }

        const trimmed = val.trim();
        if (trimmed.length === 0) {
            return { valid: false, error: "Введите сумму для конвертации." };
        }

        // Проверка на пробелы или группировку разрядов
        if (/\s|_|'/.test(trimmed)) {
            return { valid: false, error: "Использование пробелов и разделителей групп разрядов не допускается." };
        }

        // Проверка на экспоненциальную запись
        if (/e/i.test(trimmed)) {
            return { valid: false, error: "Экспоненциальная запись не допускается." };
        }

        // Проверка на ведущие знаки плюс и минус
        if (trimmed.startsWith('+')) {
            return { valid: false, error: "Ведущий знак плюс не допускается." };
        }

        if (trimmed.startsWith('-')) {
            return { valid: false, error: "Сумма должна быть положительным числом больше нуля." };
        }

        // Проверка на отсутствие целой части (начинается с точки или запятой)
        if (trimmed.startsWith('.') || trimmed.startsWith(',')) {
            return { valid: false, error: "Сумма должна начинаться с цифры; запись без целой части не допускается." };
        }

        // Проверка на отсутствие дробной части (заканчивается точкой или запятой)
        if (trimmed.endsWith('.') || trimmed.endsWith(',')) {
            return { valid: false, error: "Укажите дробную часть после десятичного разделителя." };
        }

        // Подсчёт разделителей
        const dots = (trimmed.match(/\./g) || []).length;
        const commas = (trimmed.match(/,/g) || []).length;
        if (dots + commas > 1) {
            return { valid: false, error: "Некорректный формат суммы: обнаружено несколько десятичных разделителей." };
        }

        // Проверка символов: только цифры и один разделитель
        if (/[^0-9.,]/.test(trimmed)) {
            return { valid: false, error: "Сумма может содержать только цифры и десятичный разделитель (точку или запятую)." };
        }

        // Проверка дробной части
        const sepIdx = trimmed.search(/[.,]/);
        if (sepIdx !== -1) {
            const fraction = trimmed.substring(sepIdx + 1);
            if (fraction.length === 0) {
                return { valid: false, error: "Укажите дробную часть после десятичного разделителя." };
            }
            if (fraction.length > 4) {
                return { valid: false, error: "Количество знаков после десятичного разделителя не должно превышать 4." };
            }
        }

        // Парсинг числового значения
        const normalized = trimmed.replace(',', '.');
        const num = Number(normalized);
        if (isNaN(num)) {
            return { valid: false, error: "Значение суммы превышает допустимый предел точности." };
        }

        if (num <= 0) {
            return { valid: false, error: "Сумма должна быть положительным числом больше нуля." };
        }

        if (num > 1000000000000) {
            return { valid: false, error: "Сумма не должна превышать 1 000 000 000 000." };
        }

        return { valid: true, error: null };
    }

    /**
     * Валидация даты: проверяет непустоту, HTML5 validity и непревышение max (будущая дата)
     */
    function validateDate(dateEl) {
        if (!dateEl || !dateEl.value) {
            return { valid: false, error: "Укажите дату курса." };
        }

        const dateVal = dateEl.value.trim();
        if (dateVal.length === 0) {
            return { valid: false, error: "Укажите дату курса." };
        }

        if (dateEl.validity && !dateEl.validity.valid) {
            if (dateEl.validity.rangeOverflow) {
                return { valid: false, error: "Дата курса не может быть в будущем." };
            }
            return { valid: false, error: "Укажите корректную календарную дату." };
        }

        const maxVal = dateEl.getAttribute('max');
        if (maxVal && dateVal > maxVal) {
            return { valid: false, error: "Дата курса не может быть в будущем." };
        }

        return { valid: true, error: null };
    }

    function updateFormState() {
        const amountVal = amountInput.value;
        const amountCheck = validateAmount(amountVal);
        const dateCheck = validateDate(dateInput);

        // Обновление состояния поля суммы
        if (amountVal && amountVal.trim().length > 0) {
            if (!amountCheck.valid) {
                if (amountError) {
                    amountError.textContent = amountCheck.error;
                }
                amountInput.classList.add('input-error');
            } else {
                if (amountError) {
                    amountError.textContent = '';
                }
                amountInput.classList.remove('input-error');
            }
        } else {
            if (amountError) {
                amountError.textContent = '';
            }
            amountInput.classList.remove('input-error');
        }

        // Обновление состояния поля даты
        if (dateInput.value) {
            if (!dateCheck.valid) {
                if (dateError) {
                    dateError.textContent = dateCheck.error;
                }
                dateInput.classList.add('input-error');
            } else {
                if (dateError) {
                    dateError.textContent = '';
                }
                dateInput.classList.remove('input-error');
            }
        } else {
            if (dateError) {
                dateError.textContent = '';
            }
            dateInput.classList.remove('input-error');
        }

        convertButton.disabled = !(amountCheck.valid && dateCheck.valid);
    }

    // Слушатели событий формы
    amountInput.addEventListener('input', updateFormState);
    amountInput.addEventListener('change', updateFormState);
    amountInput.addEventListener('blur', updateFormState);

    dateInput.addEventListener('input', updateFormState);
    dateInput.addEventListener('change', updateFormState);
    dateInput.addEventListener('blur', updateFormState);

    if (swapButton && sourceSelect && targetSelect) {
        swapButton.addEventListener('click', function () {
            const currentSource = sourceSelect.value;
            sourceSelect.value = targetSelect.value;
            targetSelect.value = currentSource;
            updateFormState();
        });
    }

    if (form) {
        form.addEventListener('submit', function (e) {
            const amountCheck = validateAmount(amountInput.value);
            const dateCheck = validateDate(dateInput);
            if (!amountCheck.valid || !dateCheck.valid) {
                e.preventDefault();
                updateFormState();
                return;
            }

            // Индикация загрузки и защита от повторных кликов
            convertButton.disabled = true;
            convertButton.classList.add('is-loading');

            const btnText = convertButton.querySelector('.btn-text');
            if (btnText) {
                btnText.textContent = 'Выполняется расчёт...';
            }

            const oldResult = document.getElementById('conversionResult');
            if (oldResult) {
                oldResult.hidden = true;
                oldResult.style.display = 'none';
            }
        });
    }

    // Начальная валидация при загрузке страницы
    updateFormState();
});
