using Cqrs.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Playback.Logging;

namespace Application.Modules.Playback.Commands;

internal sealed class CompletePlaybackCommandHandler(
    IPlaybackQueue queue,
    ILogger<CompletePlaybackCommandHandler> logger) : ICommandHandler<CompletePlaybackCommand, PlaybackQueueSnapshotModel>
{
    public async Task<PlaybackQueueSnapshotModel> Handle(CompletePlaybackCommand request, CancellationToken cancellationToken)
    {
        PlaybackQueueSnapshotModel snapshot = await queue.CompleteAsync(request.ItemId, cancellationToken);
        logger.PlaybackCompleted(request.ItemId, request.Success, request.ErrorCode);
        return snapshot;
    }
}
