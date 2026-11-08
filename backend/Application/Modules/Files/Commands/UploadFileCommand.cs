using Cqrs.Abstractions;

namespace Application.Modules.Files.Commands;

public sealed record UploadFileCommand(
    string FileName,
    string ContentType,
    Stream Content,
    long MaximumSizeBytes,
    string? Purpose) : ICommand<StoredFileModel>;
