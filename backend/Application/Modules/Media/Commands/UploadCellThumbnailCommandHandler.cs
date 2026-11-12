using Cqrs.Abstractions;

namespace Application.Modules.Media.Commands;

internal sealed class UploadCellThumbnailCommandHandler(
    IRequestDispatcher dispatcher,
    ICellThumbnailProcessor processor) : ICommandHandler<UploadCellThumbnailCommand, MediaAssetModel>
{
    public async Task<MediaAssetModel> Handle(UploadCellThumbnailCommand request, CancellationToken cancellationToken)
    {
        byte[] png = await processor.CreatePngAsync(request.Content, cancellationToken);
        if (png.LongLength > CellThumbnailSpecifications.MaximumOutputBytes)
            throw new InvalidDataException("The optimized thumbnail exceeds the supported output size.");

        string baseName = Path.GetFileNameWithoutExtension(request.FileName.Trim());
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "cell";
        string displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? baseName : request.DisplayName.Trim();

        await using var content = new MemoryStream(png, writable: false);
        return await dispatcher.Send(new UploadMediaAssetCommand(
            $"{baseName}-thumbnail.png",
            "image/png",
            content,
            CellThumbnailSpecifications.MaximumOutputBytes,
            displayName), cancellationToken);
    }
}
