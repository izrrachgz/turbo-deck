using Application.Modules.Media;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Infrastructure.Modules.Media;

public static class MediaInfrastructureExtensions
{
    public static IServiceCollection AddMediaInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICellThumbnailProcessor, CellThumbnailProcessor>();
        services.AddSingleton<IMediaInspector, BuiltInMediaInspector>();
        return services;
    }
}
