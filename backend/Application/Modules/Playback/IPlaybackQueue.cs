namespace Application.Modules.Playback;

public interface IPlaybackQueue
{
    PlaybackQueueSnapshotModel GetSnapshot();

    Task<PlaybackQueueSnapshotModel> EnqueueAsync(
        PlaybackQueueItemModel item,
        int capacity,
        CancellationToken cancellationToken = default);

    Task<PlaybackQueueSnapshotModel> CompleteAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<PlaybackQueueSnapshotModel> RemoveAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<PlaybackQueueSnapshotModel> MoveAsync(Guid itemId, int offset, CancellationToken cancellationToken = default);

    Task<PlaybackQueueSnapshotModel> ClearAsync(bool stopCurrent, CancellationToken cancellationToken = default);
}
