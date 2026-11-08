using Time.Extensions;
using Application.Modules;
using Localization.Extensions;
using Cqrs.DependencyInjection;
using Persistence.Repository.Extensions;
using Application.Localization.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Repository.Command.Extensions;

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
        services
            .AddCommandQLocalization(configuration.GetSection("Localization"))
            .AddJsonFiles()
            .AddResx(Messages.ResourceManager)
            .AddResx(ValidationText.ResourceManager)
            .AddResx(FieldNames.ResourceManager);
        return services;
    }
}
