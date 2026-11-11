using Domain.Modules.Deck;

namespace Application.Modules.Deck;

internal static class DeckMapping
{
    public static ProfileModel ToModel(this Profile profile) => new(
        profile.Id,
        profile.Name,
        profile.SortOrder,
        profile.SelectedPageNumber,
        profile.CreatedUtc,
        profile.ModifiedUtc);

    public static DeckPageModel ToModel(this DeckPage page) => new(
        page.Id,
        page.ProfileId,
        page.Name,
        page.Position,
        page.CreatedUtc,
        page.ModifiedUtc);

    public static CellConfigModel ToModel(this CellConfig cell) => new(
        cell.Id,
        cell.PageId,
        cell.RowIndex,
        cell.ColumnIndex,
        cell.Label,
        cell.Enabled,
        cell.MediaAssetId,
        cell.ThumbnailMediaAssetId,
        cell.Volume,
        cell.PlaybackRate,
        cell.PitchSemitones,
        cell.DisplayDurationMilliseconds,
        cell.DisplayPermanently,
        cell.DisplaySize,
        cell.AnimationInType,
        cell.AnimationInDurationMilliseconds,
        cell.AnimationOutType,
        cell.AnimationOutDurationMilliseconds,
        cell.QueuePriority,
        cell.CreatedUtc,
        cell.ModifiedUtc);
}
