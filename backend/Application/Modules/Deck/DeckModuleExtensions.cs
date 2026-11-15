using FluentValidation;
using Application.Extensions;
using Application.Modules.Deck.Queries;
using Application.Modules.Deck.Commands;
using Application.Modules.Deck.Transfers;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules.Deck;

public static class DeckModuleExtensions
{
    public static IServiceCollection AddDeckModule(this IServiceCollection services)
    {
        services.AddApplicationRequest<GetProfilesQuery, IReadOnlyList<ProfileModel>, GetProfilesQueryHandler>();
        services.AddApplicationRequest<GetProfilePagesQuery, IReadOnlyList<DeckPageModel>, GetProfilePagesQueryHandler>();
        services.AddApplicationRequest<GetDeckGridQuery, DeckGridModel?, GetDeckGridQueryHandler>();
        services.AddApplicationRequest<CreateProfileCommand, ProfileModel, CreateProfileCommandHandler>();
        services.AddApplicationRequest<DeleteProfileCommand, DeleteProfileResultModel?, DeleteProfileCommandHandler>();
        services.AddApplicationRequest<CreatePageCommand, DeckPageModel, CreatePageCommandHandler>();
        services.AddApplicationRequest<UpsertCellConfigCommand, CellConfigModel, UpsertCellConfigCommandHandler>();
        services.AddApplicationRequest<GetProfileExportQuery, ProfileExportModel?, GetProfileExportQueryHandler>();
        services.AddApplicationRequest<ImportProfileCommand, ProfileImportResultModel, ImportProfileCommandHandler>();

        services.AddScoped<IValidator<CreateProfileCommand>, CreateProfileCommandValidator>();
        services.AddScoped<IValidator<DeleteProfileCommand>, DeleteProfileCommandValidator>();
        services.AddScoped<IValidator<CreatePageCommand>, CreatePageCommandValidator>();
        services.AddScoped<IValidator<UpsertCellConfigCommand>, UpsertCellConfigCommandValidator>();
        services.AddScoped<IValidator<GetProfilePagesQuery>, GetProfilePagesQueryValidator>();
        services.AddScoped<IValidator<GetDeckGridQuery>, GetDeckGridQueryValidator>();
        return services;
    }
}
