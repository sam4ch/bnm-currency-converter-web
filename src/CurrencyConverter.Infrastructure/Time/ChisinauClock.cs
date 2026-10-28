using CurrencyConverter.Core.Contracts;

namespace CurrencyConverter.Infrastructure.Time;

/// <summary>
/// Системные часы с поддержкой официального часового пояса Кишинёва (Молдова) на Windows и Linux.
/// </summary>
public sealed class ChisinauClock : IClock
{
    private static readonly TimeZoneInfo ChisinauTimeZone = ResolveChisinauTimeZone();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset NowInChisinau => TimeZoneInfo.ConvertTime(UtcNow, ChisinauTimeZone);

    public DateOnly TodayInChisinau => DateOnly.FromDateTime(NowInChisinau.DateTime);

    private static TimeZoneInfo ResolveChisinauTimeZone()
    {
        // Кандидаты для IANA (Linux/macOS) и Windows идентификаторов часового пояса Молдовы
        string[] candidates =
        [
            "Europe/Chisinau",
            "E. Europe Standard Time",
            "FLE Standard Time",
            "GTB Standard Time"
        ];

        foreach (string id in candidates)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Проверяем следующий идентификатор
            }
            catch (InvalidTimeZoneException)
            {
                // Невалидные данные реестра/системы, проверяем следующий
            }
        }

        // Если системные часовые пояса недоступны, используем стандартное смещение Восточной Европы (+2 / +3)
        // Но TimeZoneInfo.CreateCustomTimeZone позволяет безопасно создать фиксированный fallback при необходимости
        try
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                "Europe/Chisinau_Fallback",
                TimeSpan.FromHours(2),
                "Chisinau Standard Time",
                "Chisinau Standard Time");
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
