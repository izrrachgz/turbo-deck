using Domain.Modules.Settings;

namespace Application.Modules.Settings;

internal static class SettingsMapping
{
    public static AppSettingsModel ToModel(this AppSettings settings) => new(
        settings.Id,
        settings.ActiveProfileId,
        settings.Culture,
        settings.Theme,
        settings.Palette,
        settings.TimeZone,
        settings.QueueCapacity,
        settings.VisualizerSafeMargin,
        settings.CreatedUtc,
        settings.ModifiedUtc);
}
