namespace Api.Modules.Files.Requests;

public sealed class UploadFileRequest
{
    public IFormFile? File { get; init; }

    public string? Purpose { get; init; }
}
