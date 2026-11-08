namespace Application.Modules.Files;

public interface IFileStorage
{
    Task<FileStorageWriteResult> SaveAsync(
        Stream content,
        string extension,
        long maximumSizeBytes,
        CancellationToken cancellationToken = default);

    ValueTask<Stream?> OpenReadAsync(string storageName, CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(string storageName, CancellationToken cancellationToken = default);
}
