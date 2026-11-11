using Time.Extensions;
using Persistence.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application.Migrations.Services;
using Application.Migrations.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

    public static async Task RunApplicationMigrations(
        this IServiceProvider services,
        IWritableRepository repository,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        var runner = new ApplicationMigrationRunner(
            repository,
            scope.ServiceProvider.GetRequiredService<IMigrationScriptSource>(),
            scope.ServiceProvider.GetRequiredService<TimeProvider>(),
            scope.ServiceProvider.GetRequiredService<ILogger<ApplicationMigrationRunner>>());
        await runner.RunAsync(cancellationToken);
    }
}
