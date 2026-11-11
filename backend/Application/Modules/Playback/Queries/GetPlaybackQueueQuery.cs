using Cqrs.Abstractions;

namespace Application.Modules.Playback.Queries;

public sealed record GetPlaybackQueueQuery : IQuery<PlaybackQueueSnapshotModel>;
