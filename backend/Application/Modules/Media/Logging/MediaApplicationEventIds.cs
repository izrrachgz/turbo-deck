using Application.Logging;

namespace Application.Modules.Media.Logging;

internal static class MediaApplicationEventIds
{
    public const int MediaAssetCreated = ApplicationEventIdRanges.Media + 0;
    public const int MediaAssetCreationFailed = ApplicationEventIdRanges.Media + 1;
}
