using Microsoft.Extensions.Logging;

namespace Application.Modules.Settings.Logging;

internal static partial class SettingsApplicationLog
{
    [LoggerMessage(
        EventId = SettingsApplicationEventIds.AppSettingsUpdated,
        Level = LogLevel.Information,
        Message = "Application settings updated. Culture: {Culture}. Theme: {Theme}. TimeZone: {TimeZone}.")]
    public static partial void AppSettingsUpdated(this ILogger logger, string culture, string theme, string timeZone);
}
