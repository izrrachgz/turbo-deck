using Domain.Modules.Media;
using Application.Modules.Files;

namespace Application.Modules.Media;

internal static class MediaMapping
{
    public static MediaAssetModel ToModel(this MediaAsset asset, StoredFileModel file) => new(
        asset.Id,
        asset.DisplayName,
        asset.Kind,
        asset.InspectionStatus,
        asset.Width,
        asset.Height,
        asset.DurationMilliseconds,
        asset.VideoCodec,
        asset.AudioCodec,
        asset.HasAudio,
        asset.WaveformData,
        file,
        asset.CreatedUtc,
        asset.ModifiedUtc);
}
