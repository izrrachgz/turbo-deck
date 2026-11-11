using Cqrs.Abstractions;

namespace Application.Modules.Twitch.Commands;

public sealed record UpsertTwitchRewardBindingCommand(
    string RewardId,
    string RewardTitle,
    long CellConfigId,
    bool Enabled) : ICommand<TwitchRewardBindingModel>;
