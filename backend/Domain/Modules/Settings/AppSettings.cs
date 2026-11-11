using Domain.Entities;

namespace Domain.Modules.Settings;

public sealed class AppSettings : BaseEntity
{
    public long? ActiveProfileId { get; set; }

    public required string Culture { get; set; }

    public required string Theme { get; set; }

    public required string Palette { get; set; }

    public required string TimeZone { get; set; }

    public int QueueCapacity { get; set; }

    public int VisualizerSafeMargin { get; set; }
}
