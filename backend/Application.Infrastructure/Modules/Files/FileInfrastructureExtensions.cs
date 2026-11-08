using Application.Modules.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Infrastructure.Modules.Files;

public static class FileInfrastructureExtensions
{
    public static IServiceCollection AddFileInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection("Files"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.DataDirectory), "The file data directory is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RootPath), "The file storage root is required.")
            .ValidateOnStart();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        return services;
    }
}
