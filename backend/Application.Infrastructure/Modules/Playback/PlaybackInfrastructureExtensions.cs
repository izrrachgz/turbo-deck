using Application.Modules.Playback;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Infrastructure.Modules.Playback;

public static class PlaybackInfrastructureExtensions
{
    public static IServiceCollection AddPlaybackInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPlaybackQueue, InMemoryPlaybackQueue>();
        return services;
    }
}
