using Application.Logging;

namespace Application.Modules.Files.Logging;

internal static class FilesApplicationEventIds
{
    public const int FileStored = ApplicationEventIdRanges.Files + 0;
    public const int FileMetadataSaveFailed = ApplicationEventIdRanges.Files + 1;
    public const int FileCleanupFailed = ApplicationEventIdRanges.Files + 2;
    public const int FileDeleteNotFound = ApplicationEventIdRanges.Files + 3;
    public const int FileDeleted = ApplicationEventIdRanges.Files + 4;
    public const int FileContentMissing = ApplicationEventIdRanges.Files + 5;
}
