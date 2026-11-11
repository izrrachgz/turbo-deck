using FluentValidation;
using Application.Extensions;
using Application.Modules.Media.Queries;
using Application.Modules.Media.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules.Media;

public static class MediaModuleExtensions
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services)
    {
        services.AddApplicationRequest<UploadMediaAssetCommand, MediaAssetModel, UploadMediaAssetCommandHandler>();
        services.AddApplicationRequest<GetMediaAssetsQuery, IReadOnlyList<MediaAssetModel>, GetMediaAssetsQueryHandler>();
        services.AddApplicationRequest<GetMediaAssetQuery, MediaAssetModel?, GetMediaAssetQueryHandler>();
        services.AddScoped<IValidator<UploadMediaAssetCommand>, UploadMediaAssetCommandValidator>();
        services.AddScoped<IValidator<GetMediaAssetsQuery>, GetMediaAssetsQueryValidator>();
        return services;
    }
}
