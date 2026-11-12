namespace Application.Modules.Media;

public interface ICellThumbnailProcessor
{
    Task<byte[]> CreatePngAsync(Stream source, CancellationToken cancellationToken = default);
}
