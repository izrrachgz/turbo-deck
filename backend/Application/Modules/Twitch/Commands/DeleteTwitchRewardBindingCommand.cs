using Cqrs.Abstractions;

namespace Application.Modules.Twitch.Commands;

public sealed record DeleteTwitchRewardBindingCommand(string RewardId) : ICommand<bool>;
