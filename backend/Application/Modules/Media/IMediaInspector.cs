namespace Application.Modules.Media;

public interface IMediaInspector
{
    Task<MediaInspectionResult> InspectAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
