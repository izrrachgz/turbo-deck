using Cqrs.Abstractions;

namespace Application.Modules.Playback.Commands;

public sealed record RemovePlaybackQueueItemCommand(Guid ItemId) : ICommand<PlaybackQueueSnapshotModel>;
