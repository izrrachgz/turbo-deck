using Cqrs.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Playback.Logging;

namespace Application.Modules.Playback.Commands;

internal sealed class RemovePlaybackQueueItemCommandHandler(
    IPlaybackQueue queue,
    ILogger<RemovePlaybackQueueItemCommandHandler> logger)
    : ICommandHandler<RemovePlaybackQueueItemCommand, PlaybackQueueSnapshotModel>
{
    public async Task<PlaybackQueueSnapshotModel> Handle(
        RemovePlaybackQueueItemCommand request,
        CancellationToken cancellationToken)
    {
        PlaybackQueueSnapshotModel snapshot = await queue.RemoveAsync(request.ItemId, cancellationToken);
        logger.PlaybackQueueItemRemoved(request.ItemId);
        return snapshot;
    }
}
