using Application.Logging;

namespace Application.Modules.Deck.Logging;

internal static class DeckApplicationEventIds
{
    public const int ProfileCreated = ApplicationEventIdRanges.Deck + 0;
    public const int PageCreated = ApplicationEventIdRanges.Deck + 1;
    public const int CellConfigured = ApplicationEventIdRanges.Deck + 2;
    public const int ProfileDeleted = ApplicationEventIdRanges.Deck + 3;
    public const int ProfileMediaCleanupFailed = ApplicationEventIdRanges.Deck + 4;
    public const int ProfileQueueCleanupFailed = ApplicationEventIdRanges.Deck + 5;
}
