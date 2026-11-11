using Application.Extensions;
using Application.Modules.Playback.Queries;
using Application.Modules.Playback.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules.Playback;

public static class PlaybackModuleExtensions
{
    public static IServiceCollection AddPlaybackModule(this IServiceCollection services)
    {
        services.AddApplicationRequest<QueueCellCommand, PlaybackQueueSnapshotModel, QueueCellCommandHandler>();
        services.AddApplicationRequest<CompletePlaybackCommand, PlaybackQueueSnapshotModel, CompletePlaybackCommandHandler>();
        services.AddApplicationRequest<RemovePlaybackQueueItemCommand, PlaybackQueueSnapshotModel, RemovePlaybackQueueItemCommandHandler>();
        services.AddApplicationRequest<MovePlaybackQueueItemCommand, PlaybackQueueSnapshotModel, MovePlaybackQueueItemCommandHandler>();
        services.AddApplicationRequest<ClearPlaybackQueueCommand, PlaybackQueueSnapshotModel, ClearPlaybackQueueCommandHandler>();
        services.AddApplicationRequest<GetPlaybackQueueQuery, PlaybackQueueSnapshotModel, GetPlaybackQueueQueryHandler>();
        return services;
    }
}
