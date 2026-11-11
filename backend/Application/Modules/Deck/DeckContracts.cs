namespace Application.Modules.Deck;

public sealed record ProfileModel(
    long Id,
    string Name,
    int SortOrder,
    int SelectedPageNumber,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);

public sealed record DeckPageModel(
    long Id,
    long ProfileId,
    string Name,
    int Position,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);

public sealed record CellConfigModel(
    long Id,
    long PageId,
    int RowIndex,
    int ColumnIndex,
    string? Label,
    bool Enabled,
    long? MediaAssetId,
    long? ThumbnailMediaAssetId,
    decimal Volume,
    decimal PlaybackRate,
    decimal PitchSemitones,
    int DisplayDurationMilliseconds,
    bool DisplayPermanently,
    string DisplaySize,
    string AnimationInType,
    int AnimationInDurationMilliseconds,
    string AnimationOutType,
    int AnimationOutDurationMilliseconds,
    string QueuePriority,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);

public sealed record DeckGridModel(DeckPageModel Page, IReadOnlyList<CellConfigModel> Cells);
