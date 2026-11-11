using Application.Extensions;
using Application.Modules.Twitch.Queries;
using Application.Modules.Twitch.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules.Twitch;

public static class TwitchModuleExtensions
{
    public static IServiceCollection AddTwitchModule(this IServiceCollection services)
    {
        services.AddApplicationRequest<GetTwitchRewardBindingsQuery, IReadOnlyList<TwitchRewardBindingModel>,
            GetTwitchRewardBindingsQueryHandler>();
        services.AddApplicationRequest<UpsertTwitchRewardBindingCommand, TwitchRewardBindingModel,
            UpsertTwitchRewardBindingCommandHandler>();
        services.AddApplicationRequest<DeleteTwitchRewardBindingCommand, bool, DeleteTwitchRewardBindingCommandHandler>();
        services.AddApplicationRequest<TriggerTwitchRewardCommand, Playback.PlaybackQueueSnapshotModel?,
            TriggerTwitchRewardCommandHandler>();
        return services;
    }
}
