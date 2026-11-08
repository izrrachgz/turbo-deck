using System.Buffers;
using Application.Modules.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Application.Infrastructure.Modules.Files.Logging;

namespace Application.Infrastructure.Modules.Files;

internal sealed class LocalFileStorage : IFileStorage
{
    private const int BufferSize = 81920;
    private readonly ILogger<LocalFileStorage> _logger;
    private readonly string _root;
    private readonly string _temporaryRoot;

    public LocalFileStorage(IOptions<FileStorageOptions> options, ILogger<LocalFileStorage> logger)
    {
        _logger = logger;
        _root = LocalStoragePath.ResolveRoot(options.Value);
        _temporaryRoot = Path.Combine(_root, ".tmp");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_temporaryRoot);
        _logger.FileStorageInitialized();
    }

    public async Task<FileStorageWriteResult> SaveAsync(
        Stream content,
        string extension,
        long maximumSizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        string normalizedExtension = NormalizeExtension(extension);
        string storageName = $"{Guid.NewGuid():N}{normalizedExtension}";
        string temporaryPath = Path.Combine(_temporaryRoot, $"{Guid.NewGuid():N}.upload");
        if (!LocalStoragePath.TryResolveFile(_root, storageName, out string finalPath))
            throw new InvalidOperationException("The generated storage path is invalid.");

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        long total = 0;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        try
        {
            await using (var output = new FileStream(temporaryPath, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                BufferSize = BufferSize,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            }))
            {
                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) != 0)
                {
                    total += read;
                    if (total > maximumSizeBytes)
                        throw new InvalidDataException($"The file exceeds the configured maximum of {maximumSizeBytes} bytes.");

                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }

                await output.FlushAsync(cancellationToken);
            }

            if (total == 0)
                throw new InvalidDataException("The file is empty.");

            File.Move(temporaryPath, finalPath, overwrite: false);
            _logger.FileContentWritten(storageName, total);
            return new FileStorageWriteResult(storageName, total, Convert.ToHexString(hash.GetHashAndReset()));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            TryDelete(temporaryPath);
        }
    }

    public ValueTask<Stream?> OpenReadAsync(string storageName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!LocalStoragePath.TryResolveFile(_root, storageName, out string path) || !File.Exists(path))
        {
            _logger.FileContentNotFound(storageName);
            return ValueTask.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read | FileShare.Delete,
            BufferSize = BufferSize,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
        return ValueTask.FromResult<Stream?>(stream);
    }

    public ValueTask DeleteAsync(string storageName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (LocalStoragePath.TryResolveFile(_root, storageName, out string path) && File.Exists(path))
        {
            File.Delete(path);
            _logger.FileContentDeleted(storageName);
        }

        return ValueTask.CompletedTask;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        string normalized = extension.StartsWith('.') ? extension : $".{extension}";
        return normalized.Length <= 16 && normalized.Skip(1).All(char.IsLetterOrDigit)
            ? normalized.ToLowerInvariant()
            : string.Empty;
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException exception)
        {
            _logger.TemporaryUploadCleanupFailed(exception);
            // A later cleanup pass can remove an in-use temporary file.
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.TemporaryUploadCleanupFailed(exception);
            // Preserve the original operation outcome when cleanup is denied.
        }
    }
}
