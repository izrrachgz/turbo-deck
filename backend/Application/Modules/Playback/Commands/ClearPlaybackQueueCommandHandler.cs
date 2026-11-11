using Cqrs.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Playback.Logging;

namespace Application.Modules.Playback.Commands;

internal sealed class ClearPlaybackQueueCommandHandler(
    IPlaybackQueue queue,
    ILogger<ClearPlaybackQueueCommandHandler> logger) : ICommandHandler<ClearPlaybackQueueCommand, PlaybackQueueSnapshotModel>
{
    public async Task<PlaybackQueueSnapshotModel> Handle(ClearPlaybackQueueCommand request, CancellationToken cancellationToken)
    {
        PlaybackQueueSnapshotModel snapshot = await queue.ClearAsync(request.StopCurrent, cancellationToken);
        logger.PlaybackQueueCleared(request.StopCurrent);
        return snapshot;
    }
}
