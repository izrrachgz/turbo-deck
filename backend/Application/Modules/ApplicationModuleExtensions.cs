using Application.Modules.Files;
using Application.Modules.Realtime;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules;

public static class ApplicationModuleExtensions
{
    public static IServiceCollection AddApplicationModules(this IServiceCollection services)
    {
        services.AddFilesModule();
        services.AddRealtimeModule();
        return services;
    }
}
