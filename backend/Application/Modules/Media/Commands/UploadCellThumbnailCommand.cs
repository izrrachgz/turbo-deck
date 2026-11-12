using Cqrs.Abstractions;

namespace Application.Modules.Media.Commands;

public sealed record UploadCellThumbnailCommand(
    string FileName,
    Stream Content,
    string? DisplayName) : ICommand<MediaAssetModel>;
