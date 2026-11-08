using Application.Migrations.Abstractions;
using Application.Migrations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Time.Extensions;

namespace Application.Migrations.Extensions;

public static class ApplicationMigrationExtensions
{
    public static IServiceCollection AddApplicationMigrations(this IServiceCollection services)
    {
        services.AddTimeServices();
        services.TryAddSingleton<IMigrationScriptSource, EmbeddedMigrationScriptSource>();
        services.TryAddScoped<ApplicationMigrationRunner>();
        return services;
    }

    public static async Task RunApplicationMigrations(this IHost host, CancellationToken cancellationToken = default)
    {
        await host.Services.RunApplicationMigrations(cancellationToken);
    }

    public static async Task RunApplicationMigrations(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<ApplicationMigrationRunner>();
        await runner.RunAsync(cancellationToken);
    }
}
