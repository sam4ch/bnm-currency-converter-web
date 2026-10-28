using System.Globalization;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Exceptions;
using CurrencyConverter.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CurrencyConverter.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IClock _clock;
    private readonly IValidationService _validationService;
    private readonly IConversionService _conversionService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IClock clock,
        IValidationService validationService,
        IConversionService conversionService,
        ILogger<IndexModel> logger)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty]
    public string? Amount { get; set; }

    [BindProperty]
    public string SourceCurrency { get; set; } = "EUR";

    [BindProperty]
    public string TargetCurrency { get; set; } = "MDL";

    [BindProperty]
    public string? RateDate { get; set; }

    [BindProperty]
    public bool CacheApproved { get; set; }

    public IReadOnlyList<string> SupportedCurrencies { get; } = ["MDL", "EUR", "USD", "RON", "GBP"];

    public string TodayDate => _clock.TodayInChisinau.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public bool HasResult { get; private set; }
    public ConversionResult? Result { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? WarningMessage { get; private set; }
    public bool ShowCacheConfirmation { get; private set; }
    public string? CacheConfirmationMessage { get; private set; }
    public DateOnly? CacheDate { get; private set; }
    public DateTimeOffset? CacheTimestamp { get; private set; }
    public bool IsSameCurrency => string.Equals(SourceCurrency, TargetCurrency, StringComparison.OrdinalIgnoreCase);

    public void OnGet()
    {
        RateDate ??= TodayDate;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await ProcessConversionAsync(cacheFallbackApproved: CacheApproved);
    }

    public async Task<IActionResult> OnPostUseCachedAsync()
    {
        return await ProcessConversionAsync(cacheFallbackApproved: true);
    }

    private async Task<IActionResult> ProcessConversionAsync(bool cacheFallbackApproved)
    {
        HasResult = false;
        Result = null;
        ErrorMessage = null;
        WarningMessage = null;
        ShowCacheConfirmation = false;
        CacheConfirmationMessage = null;

        // Валидация даты
        if (string.IsNullOrWhiteSpace(RateDate) ||
            !DateOnly.TryParseExact(RateDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedDate))
        {
            ErrorMessage = "Укажите корректную календарную дату курса.";
            return Page();
        }

        ConversionRequest request;
        try
        {
            request = _validationService.ValidateRequest(
                Amount,
                SourceCurrency,
                TargetCurrency,
                parsedDate,
                cacheFallbackApproved);
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Непредвиденная ошибка валидации параметров конвертации.");
            ErrorMessage = "Произошла ошибка при проверке введённых данных. Пожалуйста, проверьте значения полей.";
            return Page();
        }

        try
        {
            var result = await _conversionService.ConvertAsync(request, HttpContext.RequestAborted);
            Result = result;

            switch (result.Status)
            {
                case ConversionStatus.Success:
                    HasResult = true;
                    WarningMessage = result.WarningText;
                    break;

                case ConversionStatus.CacheConfirmationRequired:
                    ShowCacheConfirmation = true;
                    CacheConfirmationMessage = result.UserMessage;
                    CacheDate = result.RateDateUsed;
                    CacheTimestamp = result.RetrievalTimestampUtc;
                    break;

                case ConversionStatus.SourceUnavailable:
                case ConversionStatus.ValidationError:
                    ErrorMessage = result.UserMessage;
                    break;
            }
        }
        catch (CalculationOverflowException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Непредвиденная ошибка во время выполнения конвертации.");
            ErrorMessage = "Произошла непредвиденная ошибка при обработке запроса. Пожалуйста, повторите попытку позже.";
        }

        return Page();
    }
}
