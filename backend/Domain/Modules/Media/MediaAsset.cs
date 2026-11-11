using Domain.Entities;

namespace Domain.Modules.Media;

public sealed class MediaAsset : BaseEntity
{
    public long StoredFileId { get; set; }

    public required string DisplayName { get; set; }

    public required string Kind { get; set; }

    public required string InspectionStatus { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public long? DurationMilliseconds { get; set; }

    public string? VideoCodec { get; set; }

    public string? AudioCodec { get; set; }

    public bool HasAudio { get; set; }

    public string? WaveformData { get; set; }
}
