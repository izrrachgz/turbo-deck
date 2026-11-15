using System.Data;
using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using System.Data.Common;
using Domain.Modules.Deck;
using Domain.Modules.Files;
using Domain.Modules.Media;
using Domain.Modules.Twitch;
using Persistence.Abstractions;
using Application.Modules.Files;
using Application.Modules.Media;

namespace Application.Modules.Deck.Transfers;

internal sealed class ImportProfileCommandHandler(
    IWritableRepository repository,
    IFileStorage storage,
    IMediaInspector inspector,
    TimeProvider timeProvider) : ICommandHandler<ImportProfileCommand, ProfileImportResultModel>
{
    private sealed record WrittenMedia(ProfileTransferMediaAsset Asset, FileStorageWriteResult File, MediaInspectionResult Inspection);

    public async Task<ProfileImportResultModel> Handle(ImportProfileCommand request, CancellationToken cancellationToken)
    {
        Validate(request);
        Profile[] activeProfiles = (await repository.Db.Query(nameof(Profile))
            .WhereNull(nameof(Profile.DeletedUtc))
            .OrderBy(nameof(Profile.SortOrder))
            .GetAsync<Profile>(cancellationToken: cancellationToken)).ToArray();
        if (activeProfiles.Length >= 8)
            throw new InvalidDataException("A maximum of eight profiles is supported.");

        var streamsByKey = request.Media.ToDictionary(media => media.Key, StringComparer.Ordinal);
        var written = new List<WrittenMedia>(request.Manifest.Media.Count);
        try
        {
            foreach (ProfileTransferMediaAsset asset in request.Manifest.Media)
            {
                FileStorageWriteResult file = await storage.SaveAsync(
                    streamsByKey[asset.Key].Content,
                    asset.Extension,
                    request.MaximumMediaSizeBytes,
                    cancellationToken);
                if (file.Size != asset.Size || !string.Equals(file.Sha256, asset.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    await storage.DeleteAsync(file.StorageName, CancellationToken.None);
                    throw new InvalidDataException($"Media '{asset.OriginalFileName}' failed its integrity check.");
                }
                try
                {
                    Stream? storedContent = await storage.OpenReadAsync(file.StorageName, cancellationToken);
                    if (storedContent is null)
                        throw new FileNotFoundException("Imported media could not be reopened for inspection.");
                    await using (storedContent)
                    {
                        MediaInspectionResult inspection = await inspector.InspectAsync(
                            storedContent,
                            asset.OriginalFileName,
                            asset.ContentType,
                            cancellationToken);
                        written.Add(new WrittenMedia(asset, file, inspection));
                    }
                }
                catch
                {
                    await storage.DeleteAsync(file.StorageName, CancellationToken.None);
                    throw;
                }
            }

            HashSet<string> thumbnailKeys = request.Manifest.Profile.Pages.SelectMany(page => page.Cells)
                .Select(cell => cell.ThumbnailMediaKey)
                .Where(key => key is not null)
                .Select(key => key!)
                .ToHashSet(StringComparer.Ordinal);
            if (written.Any(media => thumbnailKeys.Contains(media.Asset.Key) && media.Inspection.Kind != "image"))
                throw new InvalidDataException("An imported cell thumbnail is not a valid image.");

            return await SaveGraphAsync(request.Manifest, written, activeProfiles, cancellationToken);
        }
        catch
        {
            foreach (WrittenMedia media in written)
            {
                try
                {
                    await storage.DeleteAsync(media.File.StorageName, CancellationToken.None);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // Preserve the import failure; startup maintenance can remove an orphaned file.
                }
            }
            throw;
        }
    }

    private async Task<ProfileImportResultModel> SaveGraphAsync(
        ProfileTransferManifest manifest,
        IReadOnlyList<WrittenMedia> written,
        IReadOnlyList<Profile> activeProfiles,
        CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetCurrentUtcDateTime();
        int sortOrder = Enumerable.Range(1, 8).First(order => activeProfiles.All(profile => profile.SortOrder != order));
        DbConnection connection = (DbConnection)repository.Db.Connection;
        bool closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var mediaIds = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (WrittenMedia media in written)
            {
                long fileId = await repository.Db.Query(nameof(StoredFile)).InsertGetIdAsync<long>(new
                {
                    OriginalFileName = Path.GetFileName(media.Asset.OriginalFileName),
                    media.File.StorageName,
                    media.Asset.ContentType,
                    media.Asset.Extension,
                    media.File.Size,
                    media.File.Sha256,
                    Purpose = "media",
                    CreatedUtc = now,
                    ModifiedUtc = now,
                    DeletedUtc = (DateTime?)null
                }, transaction: transaction, cancellationToken: cancellationToken);
                long mediaId = await repository.Db.Query(nameof(MediaAsset)).InsertGetIdAsync<long>(new
                {
                    StoredFileId = fileId,
                    media.Asset.DisplayName,
                    media.Inspection.Kind,
                    InspectionStatus = media.Inspection.Status,
                    media.Inspection.Width,
                    media.Inspection.Height,
                    media.Inspection.DurationMilliseconds,
                    media.Inspection.VideoCodec,
                    media.Inspection.AudioCodec,
                    media.Inspection.HasAudio,
                    media.Inspection.WaveformData,
                    CreatedUtc = now,
                    ModifiedUtc = now,
                    DeletedUtc = (DateTime?)null
                }, transaction: transaction, cancellationToken: cancellationToken);
                mediaIds[media.Asset.Key] = mediaId;
            }

            long profileId = await repository.Db.Query(nameof(Profile)).InsertGetIdAsync<long>(new
            {
                Name = manifest.Profile.Name.Trim(),
                SortOrder = sortOrder,
                manifest.Profile.SelectedPageNumber,
                CreatedUtc = now,
                ModifiedUtc = now,
                DeletedUtc = (DateTime?)null
            }, transaction: transaction, cancellationToken: cancellationToken);
            var cellIds = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (ProfileTransferPage page in manifest.Profile.Pages.OrderBy(page => page.Position))
            {
                long pageId = await repository.Db.Query(nameof(DeckPage)).InsertGetIdAsync<long>(new
                {
                    ProfileId = profileId,
                    Name = page.Name.Trim(),
                    page.Position,
                    CreatedUtc = now,
                    ModifiedUtc = now,
                    DeletedUtc = (DateTime?)null
                }, transaction: transaction, cancellationToken: cancellationToken);
                foreach (ProfileTransferCell cell in page.Cells)
                {
                    long cellId = await repository.Db.Query(nameof(CellConfig)).InsertGetIdAsync<long>(new
                    {
                        PageId = pageId,
                        cell.RowIndex,
                        cell.ColumnIndex,
                        Label = string.IsNullOrWhiteSpace(cell.Label) ? null : cell.Label.Trim(),
                        cell.Enabled,
                        MediaAssetId = cell.MediaKey is null ? (long?)null : mediaIds[cell.MediaKey],
                        ThumbnailMediaAssetId = cell.ThumbnailMediaKey is null ? (long?)null : mediaIds[cell.ThumbnailMediaKey],
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
                        CreatedUtc = now,
                        ModifiedUtc = now,
                        DeletedUtc = (DateTime?)null
                    }, transaction: transaction, cancellationToken: cancellationToken);
                    cellIds[cell.Key] = cellId;
                }
            }

            int importedBindings = 0;
            foreach (ProfileTransferRewardBinding binding in manifest.TwitchRewardBindings)
            {
                TwitchRewardBinding? existing = await repository.Db.Query(nameof(TwitchRewardBinding))
                    .Where(nameof(TwitchRewardBinding.RewardId), binding.RewardId)
                    .WhereNull(nameof(TwitchRewardBinding.DeletedUtc))
                    .FirstOrDefaultAsync<TwitchRewardBinding>(transaction: transaction, cancellationToken: cancellationToken);
                if (existing is not null)
                    continue;
                await repository.Db.Query(nameof(TwitchRewardBinding)).InsertAsync(new
                {
                    binding.RewardId,
                    binding.RewardTitle,
                    CellConfigId = cellIds[binding.CellKey],
                    Enabled = false,
                    CreatedUtc = now,
                    ModifiedUtc = now,
                    DeletedUtc = (DateTime?)null
                }, transaction: transaction, cancellationToken: cancellationToken);
                importedBindings++;
            }

            await transaction.CommitAsync(cancellationToken);
            var profile = new ProfileModel(
                profileId,
                manifest.Profile.Name.Trim(),
                sortOrder,
                manifest.Profile.SelectedPageNumber,
                now,
                now);
            int cellCount = manifest.Profile.Pages.Sum(page => page.Cells.Count);
            return new ProfileImportResultModel(profile, written.Count, cellCount, importedBindings);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }
    }

    private static void Validate(ImportProfileCommand request)
    {
        ProfileTransferManifest manifest = request.Manifest;
        if (manifest.FormatVersion != 1)
            throw new InvalidDataException("This profile package version is not supported.");
        if (string.IsNullOrWhiteSpace(manifest.Profile.Name) || manifest.Profile.Name.Trim().Length > 100)
            throw new InvalidDataException("The imported profile name is invalid.");
        if (manifest.Profile.SelectedPageNumber is < 1 or > 8 || manifest.Profile.Pages.Count != 8 ||
            !manifest.Profile.Pages.Select(page => page.Position).Order().SequenceEqual(Enumerable.Range(1, 8)))
            throw new InvalidDataException("The imported profile must contain pages 1 through 8.");

        if (manifest.Media.Select(asset => asset.Key).Distinct(StringComparer.Ordinal).Count() != manifest.Media.Count)
            throw new InvalidDataException("The profile package media index is invalid.");
        var assets = manifest.Media.ToDictionary(asset => asset.Key, StringComparer.Ordinal);
        if (request.Media.Count != manifest.Media.Count ||
            request.Media.Select(media => media.Key).Distinct(StringComparer.Ordinal).Count() != request.Media.Count ||
            request.Media.Any(media => !assets.ContainsKey(media.Key)))
            throw new InvalidDataException("The profile package media index is invalid.");
        foreach (ProfileTransferMediaAsset asset in manifest.Media)
        {
            if (string.IsNullOrWhiteSpace(asset.Key) || asset.Size is < 1 || asset.Size > request.MaximumMediaSizeBytes ||
                asset.Sha256.Length != 64 || !asset.Sha256.All(Uri.IsHexDigit) ||
                asset.Kind is not ("image" or "audio" or "video" or "unknown") ||
                string.IsNullOrWhiteSpace(asset.DisplayName) || asset.DisplayName.Length > 160)
                throw new InvalidDataException("The profile package contains invalid media metadata.");
        }

        ProfileTransferCell[] cells = manifest.Profile.Pages.SelectMany(page => page.Cells).ToArray();
        if (cells.Select(cell => cell.Key).Distinct(StringComparer.Ordinal).Count() != cells.Length)
            throw new InvalidDataException("The profile package contains duplicate cell identifiers.");
        foreach (ProfileTransferPage page in manifest.Profile.Pages)
        {
            if (string.IsNullOrWhiteSpace(page.Name) || page.Name.Trim().Length > 100 || page.Cells.Count > 24 ||
                page.Cells.Select(cell => (cell.RowIndex, cell.ColumnIndex)).Distinct().Count() != page.Cells.Count)
                throw new InvalidDataException("The profile package contains an invalid page.");
            foreach (ProfileTransferCell cell in page.Cells)
            {
                if (cell.RowIndex is < 0 or > 2 || cell.ColumnIndex is < 0 or > 7 || cell.Label?.Length > 80 ||
                    cell.Volume is < 0 or > 1 || cell.PlaybackRate is < 0.25m or > 4m ||
                    cell.PitchSemitones is < -24 or > 24 || cell.DisplayDurationMilliseconds is < 250 or > 600000 ||
                    cell.DisplaySize is not ("small" or "medium" or "large" or "fullscreen") ||
                    !IsAnimation(cell.AnimationInType) || !IsAnimation(cell.AnimationOutType) ||
                    cell.AnimationInDurationMilliseconds is < 0 or > 10000 ||
                    cell.AnimationOutDurationMilliseconds is < 0 or > 10000 ||
                    cell.QueuePriority is not ("normal" or "override") ||
                    cell.MediaKey is not null && !assets.ContainsKey(cell.MediaKey) ||
                    cell.ThumbnailMediaKey is not null && (!assets.TryGetValue(cell.ThumbnailMediaKey, out ProfileTransferMediaAsset? thumbnail) || thumbnail.Kind != "image"))
                    throw new InvalidDataException("The profile package contains an invalid cell configuration.");
            }
        }

        var cellKeys = cells.Select(cell => cell.Key).ToHashSet(StringComparer.Ordinal);
        if (manifest.TwitchRewardBindings.Any(binding =>
            string.IsNullOrWhiteSpace(binding.RewardId) || binding.RewardId.Length > 128 ||
            string.IsNullOrWhiteSpace(binding.RewardTitle) || binding.RewardTitle.Length > 160 ||
            !cellKeys.Contains(binding.CellKey)))
            throw new InvalidDataException("The profile package contains an invalid Twitch reward binding.");
    }

    private static bool IsAnimation(string value) => value is
        "none" or "fade" or "slide" or "zoom" or "spin" or "flip" or "bounce" or "morph" or "orbit" or "blur";
}
