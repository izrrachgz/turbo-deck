namespace Application.Modules.Settings;

public sealed record AppSettingsModel(
    long Id,
    long? ActiveProfileId,
    string Culture,
    string Theme,
    string Palette,
    string TimeZone,
    int QueueCapacity,
    int VisualizerSafeMargin,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);
