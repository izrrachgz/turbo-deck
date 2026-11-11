namespace Api.Modules.Media.Requests;

public sealed class UploadMediaAssetRequest
{
    public IFormFile? File { get; init; }

    public string? DisplayName { get; init; }
}
