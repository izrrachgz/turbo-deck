using Application.Modules.Deck;
using Application.Modules.Files;
using Application.Modules.Media;
using Application.Modules.Twitch;
using Application.Modules.Playback;
using Application.Modules.Realtime;
using Application.Modules.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules;

public static class ApplicationModuleExtensions
{
    public static IServiceCollection AddApplicationModules(this IServiceCollection services)
    {
        services.AddDeckModule();
        services.AddFilesModule();
        services.AddMediaModule();
        services.AddPlaybackModule();
        services.AddRealtimeModule();
        services.AddSettingsModule();
        services.AddTwitchModule();
        return services;
    }
}
