using Microsoft.Extensions.Logging;

namespace Application.Modules.Deck.Logging;

internal static partial class DeckApplicationLog
{
    [LoggerMessage(
        EventId = DeckApplicationEventIds.ProfileCreated,
        Level = LogLevel.Information,
        Message = "Deck profile created. ProfileId: {ProfileId}. SortOrder: {SortOrder}.")]
    public static partial void ProfileCreated(this ILogger logger, long profileId, int sortOrder);

    [LoggerMessage(
        EventId = DeckApplicationEventIds.PageCreated,
        Level = LogLevel.Information,
        Message = "Deck page created. PageId: {PageId}. ProfileId: {ProfileId}. Position: {Position}.")]
    public static partial void PageCreated(this ILogger logger, long pageId, long profileId, int position);

    [LoggerMessage(
        EventId = DeckApplicationEventIds.CellConfigured,
        Level = LogLevel.Information,
        Message = "Deck cell configured. CellId: {CellId}. PageId: {PageId}. Row: {RowIndex}. Column: {ColumnIndex}.")]
    public static partial void CellConfigured(this ILogger logger, long cellId, long pageId, int rowIndex, int columnIndex);
}
