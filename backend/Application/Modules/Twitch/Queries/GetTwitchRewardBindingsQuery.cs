using Cqrs.Abstractions;

namespace Application.Modules.Twitch.Queries;

public sealed record GetTwitchRewardBindingsQuery : IQuery<IReadOnlyList<TwitchRewardBindingModel>>;
