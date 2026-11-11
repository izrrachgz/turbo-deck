using Cqrs.Abstractions;

namespace Application.Modules.Playback.Queries;

internal sealed class GetPlaybackQueueQueryHandler(IPlaybackQueue queue) : IQueryHandler<GetPlaybackQueueQuery, PlaybackQueueSnapshotModel>
{
    public Task<PlaybackQueueSnapshotModel> Handle(GetPlaybackQueueQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(queue.GetSnapshot());
}
