using Cqrs.Abstractions;

namespace Application.Modules.Playback.Commands;

public sealed record ClearPlaybackQueueCommand(bool StopCurrent) : ICommand<PlaybackQueueSnapshotModel>;
