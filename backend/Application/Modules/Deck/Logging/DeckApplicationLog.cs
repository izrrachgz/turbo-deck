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

    [LoggerMessage(
        EventId = DeckApplicationEventIds.ProfileDeleted,
        Level = LogLevel.Information,
        Message = "Deck profile deleted. ProfileId: {ProfileId}. CellCount: {CellCount}. MediaCount: {MediaCount}.")]
    public static partial void ProfileDeleted(this ILogger logger, long profileId, int cellCount, int mediaCount);

    [LoggerMessage(
        EventId = DeckApplicationEventIds.ProfileMediaCleanupFailed,
        Level = LogLevel.Warning,
        Message = "Profile media cleanup failed after deletion. ProfileId: {ProfileId}.")]
    public static partial void ProfileMediaCleanupFailed(this ILogger logger, Exception exception, long profileId);

    [LoggerMessage(
        EventId = DeckApplicationEventIds.ProfileQueueCleanupFailed,
        Level = LogLevel.Warning,
        Message = "Profile playback queue cleanup failed after deletion. ProfileId: {ProfileId}.")]
    public static partial void ProfileQueueCleanupFailed(this ILogger logger, Exception exception, long profileId);
}
