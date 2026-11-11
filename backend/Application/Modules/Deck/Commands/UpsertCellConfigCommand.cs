using Cqrs.Abstractions;

namespace Application.Modules.Deck.Commands;

public sealed record UpsertCellConfigCommand(
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
    string QueuePriority)
    : ICommand<CellConfigModel>;
