using Cqrs.Abstractions;

namespace Application.Modules.Playback.Commands;

public sealed record MovePlaybackQueueItemCommand(Guid ItemId, int Offset) : ICommand<PlaybackQueueSnapshotModel>;
