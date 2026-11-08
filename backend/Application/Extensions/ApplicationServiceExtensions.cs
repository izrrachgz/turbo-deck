using Application.Modules;
using Cqrs.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repository.Command.Extensions;
using Persistence.Repository.Extensions;
using Time.Extensions;

namespace Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCqrs();
        services.AddTimeServices();
        services.AddRepositories(configuration);
        services.AddCommandRunners(configuration);
        services.AddApplicationModules();
        return services;
    }
}
