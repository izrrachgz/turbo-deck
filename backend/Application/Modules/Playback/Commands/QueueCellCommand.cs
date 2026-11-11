using Cqrs.Abstractions;

namespace Application.Modules.Playback.Commands;

public sealed record QueueCellCommand(long CellConfigId) : ICommand<PlaybackQueueSnapshotModel>;
