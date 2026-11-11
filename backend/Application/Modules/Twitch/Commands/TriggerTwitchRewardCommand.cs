using Cqrs.Abstractions;
using Application.Modules.Playback;

namespace Application.Modules.Twitch.Commands;

public sealed record TriggerTwitchRewardCommand(string RewardId) : ICommand<PlaybackQueueSnapshotModel?>;
