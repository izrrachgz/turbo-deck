namespace Api.Modules.Settings.Requests;

public sealed record UpdateAppSettingsRequest(
    long? ActiveProfileId,
    string Culture,
    string Theme,
    string Palette,
    string TimeZone,
    int QueueCapacity,
    int VisualizerSafeMargin);
