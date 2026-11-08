namespace Application.Infrastructure.Modules.Files;

internal sealed class FileStorageOptions
{
    public string DataDirectory { get; init; } = string.Empty;

    public string RootPath { get; init; } = "files";
}
