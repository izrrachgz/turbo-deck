using Cqrs.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Playback.Logging;

namespace Application.Modules.Playback.Commands;

internal sealed class MovePlaybackQueueItemCommandHandler(
    IPlaybackQueue queue,
    ILogger<MovePlaybackQueueItemCommandHandler> logger)
    : ICommandHandler<MovePlaybackQueueItemCommand, PlaybackQueueSnapshotModel>
{
    public async Task<PlaybackQueueSnapshotModel> Handle(
        MovePlaybackQueueItemCommand request,
        CancellationToken cancellationToken)
    {
        PlaybackQueueSnapshotModel snapshot = await queue.MoveAsync(request.ItemId, request.Offset, cancellationToken);
        logger.PlaybackQueueItemMoved(request.ItemId, request.Offset);
        return snapshot;
    }
}
