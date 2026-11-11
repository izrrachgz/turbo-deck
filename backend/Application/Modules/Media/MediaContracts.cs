using Application.Modules.Files;

namespace Application.Modules.Media;

public sealed record MediaInspectionResult(
    string Kind,
    string Status,
    int? Width,
    int? Height,
    long? DurationMilliseconds,
    string? VideoCodec,
    string? AudioCodec,
    bool HasAudio,
    string? WaveformData);

public sealed record MediaAssetModel(
    long Id,
    string DisplayName,
    string Kind,
    string InspectionStatus,
    int? Width,
    int? Height,
    long? DurationMilliseconds,
    string? VideoCodec,
    string? AudioCodec,
    bool HasAudio,
    string? WaveformData,
    StoredFileModel File,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);
