using Application.Modules.Playback;

namespace Api.Modules.Visualizer;

public sealed record VisualizerStateResponse(
    PlaybackQueueSnapshotModel Queue,
    int SafeMargin);

public sealed record CompleteVisualizerPlaybackRequest(
    Guid ItemId,
    bool Success = true,
    string? ErrorCode = null);
