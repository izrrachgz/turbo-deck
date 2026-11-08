using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules.Realtime;

public static class RealtimeModuleExtensions
{
    public static IServiceCollection AddRealtimeModule(this IServiceCollection services)
    {
        services.AddScoped<RealtimeRequestRegistry>();
        return services;
    }

    public static IServiceCollection AddRealtimeRequestHandler<THandler>(this IServiceCollection services)
        where THandler : class, IRealtimeRequestHandler
    {
        services.AddScoped<IRealtimeRequestHandler, THandler>();
        return services;
    }
}
