using Microsoft.Extensions.Logging;

namespace Application.Modules.Media.Logging;

internal static partial class MediaApplicationLog
{
    [LoggerMessage(
        EventId = MediaApplicationEventIds.MediaAssetCreated,
        Level = LogLevel.Information,
        Message = "Media asset created. MediaAssetId: {MediaAssetId}. FileId: {FileId}. Kind: {Kind}. " +
            "InspectionStatus: {InspectionStatus}.")]
    public static partial void MediaAssetCreated(this ILogger logger, long mediaAssetId, long fileId, string kind, string inspectionStatus);

    [LoggerMessage(
        EventId = MediaApplicationEventIds.MediaAssetCreationFailed,
        Level = LogLevel.Warning,
        Message = "Media asset creation failed and file cleanup was requested. FileId: {FileId}.")]
    public static partial void MediaAssetCreationFailed(this ILogger logger, long fileId);
}
