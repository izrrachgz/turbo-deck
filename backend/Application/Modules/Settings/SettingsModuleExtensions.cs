using FluentValidation;
using Application.Extensions;
using Application.Modules.Settings.Queries;
using Application.Modules.Settings.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules.Settings;

public static class SettingsModuleExtensions
{
    public static IServiceCollection AddSettingsModule(this IServiceCollection services)
    {
        services.AddApplicationRequest<GetAppSettingsQuery, AppSettingsModel, GetAppSettingsQueryHandler>();
        services.AddApplicationRequest<UpdateAppSettingsCommand, AppSettingsModel, UpdateAppSettingsCommandHandler>();
        services.AddScoped<IValidator<UpdateAppSettingsCommand>, UpdateAppSettingsCommandValidator>();
        return services;
    }
}
