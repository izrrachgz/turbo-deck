using Cqrs.Abstractions;
using Domain.Modules.Deck;
using Domain.Modules.Files;
using Domain.Modules.Media;
using Domain.Modules.Twitch;
using Persistence.Abstractions;

namespace Application.Modules.Deck.Transfers;

internal sealed class GetProfileExportQueryHandler(IReadOnlyRepository repository, TimeProvider timeProvider)
    : IQueryHandler<GetProfileExportQuery, ProfileExportModel?>
{
    public async Task<ProfileExportModel?> Handle(GetProfileExportQuery request, CancellationToken cancellationToken)
    {
        Profile? profile = (await repository.GetAsync<Profile>(repository.Query(nameof(Profile))
            .Where(nameof(Profile.Id), request.ProfileId)
            .WhereNull(nameof(Profile.DeletedUtc)), cancellationToken)).FirstOrDefault();
        if (profile is null)
            return null;

        DeckPage[] pages = (await repository.GetAsync<DeckPage>(repository.Query(nameof(DeckPage))
            .Where(nameof(DeckPage.ProfileId), profile.Id)
            .WhereNull(nameof(DeckPage.DeletedUtc))
            .OrderBy(nameof(DeckPage.Position)), cancellationToken)).ToArray();
        long[] pageIds = pages.Select(page => page.Id).ToArray();
        CellConfig[] cells = pageIds.Length == 0
            ? []
            : (await repository.GetAsync<CellConfig>(repository.Query(nameof(CellConfig))
                .WhereIn(nameof(CellConfig.PageId), pageIds)
                .WhereNull(nameof(CellConfig.DeletedUtc))
                .OrderBy(nameof(CellConfig.RowIndex))
                .OrderBy(nameof(CellConfig.ColumnIndex)), cancellationToken)).ToArray();
        long[] assetIds = cells.SelectMany(cell => new long?[] { cell.MediaAssetId, cell.ThumbnailMediaAssetId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        MediaAsset[] assets = assetIds.Length == 0
            ? []
            : (await repository.GetAsync<MediaAsset>(repository.Query(nameof(MediaAsset))
                .WhereIn(nameof(MediaAsset.Id), assetIds)
                .WhereNull(nameof(MediaAsset.DeletedUtc)), cancellationToken)).ToArray();
        if (assets.Length != assetIds.Length)
            throw new FileNotFoundException("A media asset referenced by the profile was not found.");
        long[] fileIds = assets.Select(asset => asset.StoredFileId).Distinct().ToArray();
        StoredFile[] files = fileIds.Length == 0
            ? []
            : (await repository.GetAsync<StoredFile>(repository.Query(nameof(StoredFile))
                .WhereIn(nameof(StoredFile.Id), fileIds)
                .WhereNull(nameof(StoredFile.DeletedUtc)), cancellationToken)).ToArray();

        var filesById = files.ToDictionary(file => file.Id);
        if (assets.Any(asset => !filesById.ContainsKey(asset.StoredFileId)))
            throw new FileNotFoundException("A media file referenced by the profile was not found.");

        var assetKeys = assets.OrderBy(asset => asset.Id)
            .Select((asset, index) => (asset.Id, Key: $"media-{index + 1}"))
            .ToDictionary(item => item.Id, item => item.Key);
        var media = assets.OrderBy(asset => asset.Id).Select(asset =>
        {
            StoredFile file = filesById[asset.StoredFileId];
            string extension = NormalizeExtension(file.Extension);
            string archivePath = $"media/{file.Sha256.ToLowerInvariant()}{extension}";
            return new ProfileTransferMediaAsset(
                assetKeys[asset.Id],
                archivePath,
                file.OriginalFileName,
                file.ContentType,
                extension,
                file.Size,
                file.Sha256,
                asset.DisplayName,
                asset.Kind,
                asset.InspectionStatus,
                asset.Width,
                asset.Height,
                asset.DurationMilliseconds,
                asset.VideoCodec,
                asset.AudioCodec,
                asset.HasAudio,
                asset.WaveformData);
        }).ToArray();

        var cellKeys = cells.OrderBy(cell => cell.Id)
            .Select((cell, index) => (cell.Id, Key: $"cell-{index + 1}"))
            .ToDictionary(item => item.Id, item => item.Key);
        var transferPages = pages.Select(page => new ProfileTransferPage(
            page.Name,
            page.Position,
            cells.Where(cell => cell.PageId == page.Id).Select(cell => new ProfileTransferCell(
                cellKeys[cell.Id],
                cell.RowIndex,
                cell.ColumnIndex,
                cell.Label,
                cell.Enabled,
                cell.MediaAssetId is long mediaId ? assetKeys[mediaId] : null,
                cell.ThumbnailMediaAssetId is long thumbnailId ? assetKeys[thumbnailId] : null,
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
                cell.QueuePriority)).ToArray())).ToArray();

        long[] cellIds = cells.Select(cell => cell.Id).ToArray();
        TwitchRewardBinding[] bindings = cellIds.Length == 0
            ? []
            : (await repository.GetAsync<TwitchRewardBinding>(repository.Query(nameof(TwitchRewardBinding))
                .WhereIn(nameof(TwitchRewardBinding.CellConfigId), cellIds)
                .WhereNull(nameof(TwitchRewardBinding.DeletedUtc)), cancellationToken)).ToArray();
        var transferBindings = bindings.Select(binding => new ProfileTransferRewardBinding(
            binding.RewardId,
            binding.RewardTitle,
            cellKeys[binding.CellConfigId],
            binding.Enabled)).ToArray();
        var manifest = new ProfileTransferManifest(
            1,
            timeProvider.GetUtcNow().UtcDateTime,
            new ProfileTransferProfile(profile.Name, profile.SelectedPageNumber, transferPages),
            media,
            transferBindings);
        ProfileExportFile[] exportFiles = media
            .Select(item => new ProfileExportFile(item.ArchivePath, assets.Single(asset => assetKeys[asset.Id] == item.Key).StoredFileId))
            .DistinctBy(file => file.ArchivePath, StringComparer.Ordinal)
            .ToArray();
        return new ProfileExportModel(manifest, exportFiles);
    }

    private static string NormalizeExtension(string extension)
    {
        string normalized = extension.StartsWith('.') ? extension : $".{extension}";
        return normalized.ToLowerInvariant();
    }
}
