using Cqrs.Abstractions;

namespace Application.Modules.Settings.Commands;

public sealed record UpdateAppSettingsCommand(
    long? ActiveProfileId,
    string Culture,
    string Theme,
    string Palette,
    string TimeZone,
    int QueueCapacity,
    int VisualizerSafeMargin) : ICommand<AppSettingsModel>;
