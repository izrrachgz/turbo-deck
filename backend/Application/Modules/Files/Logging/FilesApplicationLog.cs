using Microsoft.Extensions.Logging;

namespace Application.Modules.Files.Logging;

internal static partial class FilesApplicationLog
{
    [LoggerMessage(
        EventId = FilesApplicationEventIds.FileStored,
        Level = LogLevel.Information,
        Message = "File stored. FileId: {FileId}. Size: {Size}. ContentType: {ContentType}.")]
    public static partial void FileStored(this ILogger logger, long fileId, long size, string contentType);

    [LoggerMessage(
        EventId = FilesApplicationEventIds.FileMetadataSaveFailed,
        Level = LogLevel.Error,
        Message = "File metadata could not be saved; stored bytes will be cleaned up. StorageName: {StorageName}.")]
    public static partial void FileMetadataSaveFailed(this ILogger logger, Exception exception, string storageName);

    [LoggerMessage(
        EventId = FilesApplicationEventIds.FileCleanupFailed,
        Level = LogLevel.Warning,
        Message = "File cleanup failed after metadata persistence failed. StorageName: {StorageName}.")]
    public static partial void FileCleanupFailed(this ILogger logger, Exception exception, string storageName);

    [LoggerMessage(
        EventId = FilesApplicationEventIds.FileDeleteNotFound,
        Level = LogLevel.Debug,
        Message = "File delete found no active metadata. FileId: {FileId}.")]
    public static partial void FileDeleteNotFound(this ILogger logger, long fileId);

    [LoggerMessage(EventId = FilesApplicationEventIds.FileDeleted, Level = LogLevel.Information, Message = "File deleted. FileId: {FileId}.")]
    public static partial void FileDeleted(this ILogger logger, long fileId);

    [LoggerMessage(
        EventId = FilesApplicationEventIds.FileContentMissing,
        Level = LogLevel.Warning,
        Message = "File metadata exists but stored content is missing. FileId: {FileId}.")]
    public static partial void FileContentMissing(this ILogger logger, long fileId);
}
