using Cqrs.Abstractions;

namespace Application.Modules.Media.Commands;

public sealed record UploadMediaAssetCommand(
    string FileName,
    string ContentType,
    Stream Content,
    long MaximumSizeBytes,
    string? DisplayName) : ICommand<MediaAssetModel>;
