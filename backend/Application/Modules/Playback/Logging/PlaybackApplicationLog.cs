using Microsoft.Extensions.Logging;

namespace Application.Modules.Playback.Logging;

internal static partial class PlaybackApplicationLog
{
    [LoggerMessage(
        EventId = PlaybackApplicationEventIds.Queued,
        Level = LogLevel.Information,
        Message = "Playback queued. QueueItemId: {QueueItemId}. CellId: {CellId}. MediaAssetId: {MediaAssetId}. Priority: {Priority}.")]
    public static partial void PlaybackQueued(this ILogger logger, Guid queueItemId, long cellId, long mediaAssetId, string priority);

    [LoggerMessage(
        EventId = PlaybackApplicationEventIds.Completed,
        Level = LogLevel.Information,
        Message = "Playback completed. QueueItemId: {QueueItemId}. Success: {Success}. ErrorCode: {ErrorCode}.")]
    public static partial void PlaybackCompleted(this ILogger logger, Guid queueItemId, bool success, string? errorCode);

    [LoggerMessage(
        EventId = PlaybackApplicationEventIds.QueueCleared,
        Level = LogLevel.Information,
        Message = "Playback queue cleared. StopCurrent: {StopCurrent}.")]
    public static partial void PlaybackQueueCleared(this ILogger logger, bool stopCurrent);

    [LoggerMessage(
        EventId = PlaybackApplicationEventIds.QueueItemRemoved,
        Level = LogLevel.Information,
        Message = "Playback queue item removed. QueueItemId: {QueueItemId}.")]
    public static partial void PlaybackQueueItemRemoved(this ILogger logger, Guid queueItemId);

    [LoggerMessage(
        EventId = PlaybackApplicationEventIds.QueueItemMoved,
        Level = LogLevel.Information,
        Message = "Playback queue item moved. QueueItemId: {QueueItemId}. Offset: {Offset}.")]
    public static partial void PlaybackQueueItemMoved(this ILogger logger, Guid queueItemId, int offset);
}
