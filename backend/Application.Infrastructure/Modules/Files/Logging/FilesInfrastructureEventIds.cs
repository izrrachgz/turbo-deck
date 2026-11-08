using Application.Infrastructure.Logging;

namespace Application.Infrastructure.Modules.Files.Logging;

internal static class FilesInfrastructureEventIds
{
    public const int StorageInitialized = InfrastructureEventIdRanges.Files + 0;
    public const int ContentWritten = InfrastructureEventIdRanges.Files + 1;
    public const int ContentNotFound = InfrastructureEventIdRanges.Files + 2;
    public const int ContentDeleted = InfrastructureEventIdRanges.Files + 3;
    public const int TemporaryCleanupFailed = InfrastructureEventIdRanges.Files + 4;
}
