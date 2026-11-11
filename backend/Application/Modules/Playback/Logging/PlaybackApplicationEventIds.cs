using Application.Logging;

namespace Application.Modules.Playback.Logging;

internal static class PlaybackApplicationEventIds
{
    public const int Queued = ApplicationEventIdRanges.Playback + 0;
    public const int Completed = ApplicationEventIdRanges.Playback + 1;
    public const int QueueCleared = ApplicationEventIdRanges.Playback + 2;
    public const int QueueItemRemoved = ApplicationEventIdRanges.Playback + 3;
    public const int QueueItemMoved = ApplicationEventIdRanges.Playback + 4;
}
