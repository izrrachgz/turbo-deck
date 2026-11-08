using Application.Extensions;
using Application.Modules.Files.Commands;
using Application.Modules.Files.Queries;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Modules.Files;

public static class FilesModuleExtensions
{
    public static IServiceCollection AddFilesModule(this IServiceCollection services)
    {
        services.AddApplicationRequest<UploadFileCommand, StoredFileModel, UploadFileCommandHandler>();
        services.AddApplicationRequest<DeleteFileCommand, bool, DeleteFileCommandHandler>();
        services.AddApplicationRequest<GetFilesQuery, IReadOnlyList<StoredFileModel>, GetFilesQueryHandler>();
        services.AddApplicationRequest<GetFileQuery, StoredFileModel?, GetFileQueryHandler>();
        services.AddApplicationRequest<OpenFileQuery, StoredFileContent?, OpenFileQueryHandler>();

        services.AddScoped<IValidator<UploadFileCommand>, UploadFileCommandValidator>();
        services.AddScoped<IValidator<DeleteFileCommand>, DeleteFileCommandValidator>();
        services.AddScoped<IValidator<GetFilesQuery>, GetFilesQueryValidator>();
        return services;
    }
}
