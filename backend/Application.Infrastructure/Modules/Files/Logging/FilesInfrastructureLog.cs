using Microsoft.Extensions.Logging;

namespace Application.Infrastructure.Modules.Files.Logging;

internal static partial class FilesInfrastructureLog
{
    [LoggerMessage(
        EventId = FilesInfrastructureEventIds.StorageInitialized,
        Level = LogLevel.Information,
        Message = "Local file storage initialized.")]
    public static partial void FileStorageInitialized(this ILogger logger);

    [LoggerMessage(
        EventId = FilesInfrastructureEventIds.ContentWritten,
        Level = LogLevel.Debug,
        Message = "File content written. StorageName: {StorageName}. Size: {Size}.")]
    public static partial void FileContentWritten(this ILogger logger, string storageName, long size);

    [LoggerMessage(
        EventId = FilesInfrastructureEventIds.ContentNotFound,
        Level = LogLevel.Debug,
        Message = "File content was not found. StorageName: {StorageName}.")]
    public static partial void FileContentNotFound(this ILogger logger, string storageName);

    [LoggerMessage(
        EventId = FilesInfrastructureEventIds.ContentDeleted,
        Level = LogLevel.Debug,
        Message = "File content deleted. StorageName: {StorageName}.")]
    public static partial void FileContentDeleted(this ILogger logger, string storageName);

    [LoggerMessage(
        EventId = FilesInfrastructureEventIds.TemporaryCleanupFailed,
        Level = LogLevel.Debug,
        Message = "Temporary upload cleanup failed; a later maintenance pass may remove it.")]
    public static partial void TemporaryUploadCleanupFailed(this ILogger logger, Exception exception);
}
