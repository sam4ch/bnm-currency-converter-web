namespace CurrencyConverter.Core.Models;

/// <summary>
/// Ошибка валидации пользовательского ввода.
/// </summary>
public sealed class ValidationError
{
    public string Code { get; }
    public string MessageKey { get; }
    public string Message { get; }
    public string? FieldName { get; }

    public ValidationError(string code, string messageKey, string message, string? fieldName = null)
    {
        Code = code;
        MessageKey = messageKey;
        Message = message;
        FieldName = fieldName;
    }

    public override string ToString() => Message;
}
