using Cqrs.Abstractions;

namespace Application.Modules.Playback.Commands;

public sealed record CompletePlaybackCommand(Guid ItemId, bool Success, string? ErrorCode) : ICommand<PlaybackQueueSnapshotModel>;
