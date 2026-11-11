using Domain.Entities;

namespace Domain.Modules.Deck;

public sealed class CellConfig : BaseEntity
{
    public long PageId { get; set; }

    public int RowIndex { get; set; }

    public int ColumnIndex { get; set; }

    public string? Label { get; set; }

    public bool Enabled { get; set; } = true;

    public long? MediaAssetId { get; set; }

    public long? ThumbnailMediaAssetId { get; set; }

    public decimal Volume { get; set; } = 1m;

    public decimal PlaybackRate { get; set; } = 1m;

    public decimal PitchSemitones { get; set; }

    public int DisplayDurationMilliseconds { get; set; } = 5000;

    public bool DisplayPermanently { get; set; }

    public required string DisplaySize { get; set; }

    public required string AnimationInType { get; set; }

    public int AnimationInDurationMilliseconds { get; set; }

    public required string AnimationOutType { get; set; }

    public int AnimationOutDurationMilliseconds { get; set; }

    public required string QueuePriority { get; set; }
}
