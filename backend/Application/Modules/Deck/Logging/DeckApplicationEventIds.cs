using Application.Logging;

namespace Application.Modules.Deck.Logging;

internal static class DeckApplicationEventIds
{
    public const int ProfileCreated = ApplicationEventIdRanges.Deck + 0;
    public const int PageCreated = ApplicationEventIdRanges.Deck + 1;
    public const int CellConfigured = ApplicationEventIdRanges.Deck + 2;
}
