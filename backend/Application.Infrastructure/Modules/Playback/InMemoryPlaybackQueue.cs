using System.Text.Json;
using Application.Modules.Playback;
using Application.Modules.Realtime;

namespace Application.Infrastructure.Modules.Playback;

internal sealed class InMemoryPlaybackQueue(IRealtimePublisher realtimePublisher) : IPlaybackQueue
{
    private readonly object _sync = new();
    private readonly LinkedList<PlaybackQueueItemModel> _pending = new();
    private PlaybackQueueItemModel? _current;

    public PlaybackQueueSnapshotModel GetSnapshot()
    {
        lock (_sync)
            return Snapshot();
    }

    public async Task<PlaybackQueueSnapshotModel> EnqueueAsync(
        PlaybackQueueItemModel item,
        int capacity,
        CancellationToken cancellationToken = default)
    {
        PlaybackQueueSnapshotModel snapshot;
        bool started;
        bool interrupted;
        lock (_sync)
        {
            if (_pending.Count + (_current is null ? 0 : 1) >= capacity)
                throw new InvalidDataException("The playback queue has reached its configured capacity.");

            started = _current is null;
            interrupted = !started && item.Priority.Equals("override", StringComparison.OrdinalIgnoreCase);
            if (started)
                _current = item;
            else if (interrupted)
                _current = item;
            else
                _pending.AddLast(item);
            snapshot = Snapshot();
        }

        if (interrupted)
            await PublishAsync(PlaybackRealtimeContract.StopRequested, new { reason = "overridden" }, cancellationToken);
        if (started || interrupted)
            await PublishAsync(PlaybackRealtimeContract.Requested, item, cancellationToken);
        await PublishAsync(PlaybackRealtimeContract.QueueChanged, snapshot, cancellationToken);
        return snapshot;
    }

    public async Task<PlaybackQueueSnapshotModel> CompleteAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        PlaybackQueueItemModel? next = null;
        PlaybackQueueSnapshotModel snapshot;
        lock (_sync)
        {
            if (_current?.Id != itemId)
                throw new InvalidDataException("Only the current playback item can be completed.");
            _current = null;
            if (_pending.First is not null)
            {
                next = _pending.First.Value;
                _pending.RemoveFirst();
                _current = next;
            }
            snapshot = Snapshot();
        }

        if (next is not null)
            await PublishAsync(PlaybackRealtimeContract.Requested, next, cancellationToken);
        await PublishAsync(PlaybackRealtimeContract.QueueChanged, snapshot, cancellationToken);
        return snapshot;
    }

    public async Task<PlaybackQueueSnapshotModel> RemoveAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        PlaybackQueueItemModel? next = null;
        PlaybackQueueSnapshotModel snapshot;
        bool removedCurrent;
        lock (_sync)
        {
            removedCurrent = _current?.Id == itemId;
            if (removedCurrent)
            {
                _current = null;
                if (_pending.First is not null)
                {
                    next = _pending.First.Value;
                    _pending.RemoveFirst();
                    _current = next;
                }
            }
            else
            {
                LinkedListNode<PlaybackQueueItemModel>? node = FindPending(itemId);
                if (node is null)
                    throw new FileNotFoundException("The playback queue item was not found.");
                _pending.Remove(node);
            }
            snapshot = Snapshot();
        }

        if (removedCurrent)
            await PublishAsync(PlaybackRealtimeContract.StopRequested, new { reason = "queue-item-removed" }, cancellationToken);
        if (next is not null)
            await PublishAsync(PlaybackRealtimeContract.Requested, next, cancellationToken);
        await PublishAsync(PlaybackRealtimeContract.QueueChanged, snapshot, cancellationToken);
        return snapshot;
    }

    public async Task<PlaybackQueueSnapshotModel> MoveAsync(
        Guid itemId,
        int offset,
        CancellationToken cancellationToken = default)
    {
        if (offset is not (-1 or 1))
            throw new InvalidDataException("The queue move offset must be -1 or 1.");

        PlaybackQueueSnapshotModel snapshot;
        bool changed = false;
        lock (_sync)
        {
            LinkedListNode<PlaybackQueueItemModel>? node = FindPending(itemId);
            if (node is null)
                throw new FileNotFoundException("The pending playback queue item was not found.");
            if (offset < 0 && node.Previous is LinkedListNode<PlaybackQueueItemModel> previous)
            {
                _pending.Remove(node);
                _pending.AddBefore(previous, node);
                changed = true;
            }
            else if (offset > 0 && node.Next is LinkedListNode<PlaybackQueueItemModel> next)
            {
                _pending.Remove(node);
                _pending.AddAfter(next, node);
                changed = true;
            }
            snapshot = Snapshot();
        }

        if (changed)
            await PublishAsync(PlaybackRealtimeContract.QueueChanged, snapshot, cancellationToken);
        return snapshot;
    }

    public async Task<PlaybackQueueSnapshotModel> ClearAsync(bool stopCurrent, CancellationToken cancellationToken = default)
    {
        PlaybackQueueSnapshotModel snapshot;
        lock (_sync)
        {
            _pending.Clear();
            if (stopCurrent)
                _current = null;
            snapshot = Snapshot();
        }

        if (stopCurrent)
            await PublishAsync(PlaybackRealtimeContract.StopRequested, new { reason = "queue-cleared" }, cancellationToken);
        await PublishAsync(PlaybackRealtimeContract.QueueChanged, snapshot, cancellationToken);
        return snapshot;
    }

    private PlaybackQueueSnapshotModel Snapshot() => new(_current, _pending.ToArray());

    private LinkedListNode<PlaybackQueueItemModel>? FindPending(Guid itemId)
    {
        for (LinkedListNode<PlaybackQueueItemModel>? node = _pending.First; node is not null; node = node.Next)
        {
            if (node.Value.Id == itemId)
                return node;
        }
        return null;
    }

    private Task PublishAsync(string name, object payload, CancellationToken cancellationToken) =>
        realtimePublisher.PublishAsync(new RealtimePublishRequest(
            RealtimeTargetKind.Group,
            PlaybackRealtimeContract.Group,
            name,
            JsonSerializer.SerializeToElement(payload)), cancellationToken);
}
