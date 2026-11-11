namespace Api.Modules.Deck.Requests;

public sealed record UpsertCellConfigRequest(
    string? Label,
    bool Enabled = true,
    long? MediaAssetId = null,
    long? ThumbnailMediaAssetId = null,
    decimal Volume = 1m,
    decimal PlaybackRate = 1m,
    decimal PitchSemitones = 0m,
    int DisplayDurationMilliseconds = 5000,
    bool DisplayPermanently = false,
    string DisplaySize = "medium",
    string AnimationInType = "none",
    int AnimationInDurationMilliseconds = 0,
    string AnimationOutType = "none",
    int AnimationOutDurationMilliseconds = 0,
    string QueuePriority = "normal");
