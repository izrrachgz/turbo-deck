namespace Application.Modules.Playback;

public static class PlaybackRealtimeContract
{
    public const string Group = "playback";
    public const string QueueChanged = "playback.queue.changed";
    public const string Requested = "playback.requested";
    public const string StopRequested = "playback.stop.requested";
}

public sealed record PlaybackQueueItemModel(
    Guid Id,
    long CellConfigId,
    long MediaAssetId,
    string Label,
    string Kind,
    decimal Volume,
    decimal PlaybackRate,
    decimal PitchSemitones,
    int DisplayDurationMilliseconds,
    bool DisplayPermanently,
    string DisplaySize,
    string AnimationInType,
    int AnimationInDurationMilliseconds,
    string AnimationOutType,
    int AnimationOutDurationMilliseconds,
    string? WaveformData,
    int? Width,
    int? Height,
    long? SourceDurationMilliseconds,
    string Priority,
    DateTime QueuedUtc);

public sealed record PlaybackQueueSnapshotModel(
    PlaybackQueueItemModel? Current,
    IReadOnlyList<PlaybackQueueItemModel> Pending);
