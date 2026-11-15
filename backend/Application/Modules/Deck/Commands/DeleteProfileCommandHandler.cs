using System.Data;
using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using System.Data.Common;
using Domain.Modules.Deck;
using Domain.Modules.Files;
using Domain.Modules.Media;
using Domain.Modules.Twitch;
using Domain.Modules.Settings;
using Persistence.Abstractions;
using Application.Modules.Files;
using Application.Modules.Playback;
using Microsoft.Extensions.Logging;
using Application.Modules.Deck.Logging;

namespace Application.Modules.Deck.Commands;

internal sealed class DeleteProfileCommandHandler(
    IWritableRepository repository,
    IFileStorage storage,
    IPlaybackQueue playbackQueue,
    TimeProvider timeProvider,
    ILogger<DeleteProfileCommandHandler> logger) : ICommandHandler<DeleteProfileCommand, DeleteProfileResultModel?>
{
    public async Task<DeleteProfileResultModel?> Handle(DeleteProfileCommand request, CancellationToken cancellationToken)
    {
        DbConnection connection = (DbConnection)repository.Db.Connection;
        bool closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        var storageNames = new List<string>();
        long[] deletedCellIds = [];
        DeleteProfileResultModel? result;
        await using DbTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            Profile? profile = await repository.Db.Query(nameof(Profile))
                .Where(nameof(Profile.Id), request.ProfileId)
                .WhereNull(nameof(Profile.DeletedUtc))
                .FirstOrDefaultAsync<Profile>(transaction: transaction, cancellationToken: cancellationToken);
            if (profile is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }
            if (!string.Equals(profile.Name, request.ConfirmationName, StringComparison.Ordinal))
                throw new InvalidDataException("Type the profile name exactly to confirm deletion.");

            Profile[] activeProfiles = (await repository.Db.Query(nameof(Profile))
                .WhereNull(nameof(Profile.DeletedUtc))
                .GetAsync<Profile>(transaction: transaction, cancellationToken: cancellationToken)).ToArray();
            long[] activeProfileIds = activeProfiles.Select(item => item.Id).ToArray();
            DeckPage[] activePages = (await repository.Db.Query(nameof(DeckPage))
                .WhereIn(nameof(DeckPage.ProfileId), activeProfileIds)
                .WhereNull(nameof(DeckPage.DeletedUtc))
                .GetAsync<DeckPage>(transaction: transaction, cancellationToken: cancellationToken)).ToArray();
            long[] targetPageIds = activePages.Where(page => page.ProfileId == profile.Id).Select(page => page.Id).ToArray();
            long[] externalPageIds = activePages.Where(page => page.ProfileId != profile.Id).Select(page => page.Id).ToArray();
            CellConfig[] targetCells = targetPageIds.Length == 0
                ? []
                : (await repository.Db.Query(nameof(CellConfig))
                    .WhereIn(nameof(CellConfig.PageId), targetPageIds)
                    .WhereNull(nameof(CellConfig.DeletedUtc))
                    .GetAsync<CellConfig>(transaction: transaction, cancellationToken: cancellationToken)).ToArray();
            CellConfig[] externalCells = externalPageIds.Length == 0
                ? []
                : (await repository.Db.Query(nameof(CellConfig))
                    .WhereIn(nameof(CellConfig.PageId), externalPageIds)
                    .WhereNull(nameof(CellConfig.DeletedUtc))
                    .GetAsync<CellConfig>(transaction: transaction, cancellationToken: cancellationToken)).ToArray();

            long[] targetAssetIds = AssetIds(targetCells);
            HashSet<long> externalAssetIds = AssetIds(externalCells).ToHashSet();
            long[] exclusiveAssetIds = targetAssetIds.Where(id => !externalAssetIds.Contains(id)).ToArray();
            MediaAsset[] exclusiveAssets = exclusiveAssetIds.Length == 0
                ? []
                : (await repository.Db.Query(nameof(MediaAsset))
                    .WhereIn(nameof(MediaAsset.Id), exclusiveAssetIds)
                    .WhereNull(nameof(MediaAsset.DeletedUtc))
                    .GetAsync<MediaAsset>(transaction: transaction, cancellationToken: cancellationToken)).ToArray();
            long[] candidateFileIds = exclusiveAssets.Select(asset => asset.StoredFileId).Distinct().ToArray();
            long[] retainedFileIds = candidateFileIds.Length == 0
                ? []
                : (await repository.Db.Query(nameof(MediaAsset))
                    .WhereIn(nameof(MediaAsset.StoredFileId), candidateFileIds)
                    .WhereNotIn(nameof(MediaAsset.Id), exclusiveAssetIds)
                    .WhereNull(nameof(MediaAsset.DeletedUtc))
                    .GetAsync<MediaAsset>(transaction: transaction, cancellationToken: cancellationToken))
                    .Select(asset => asset.StoredFileId)
                    .Distinct()
                    .ToArray();
            long[] fileIds = candidateFileIds.Except(retainedFileIds).ToArray();
            StoredFile[] files = fileIds.Length == 0
                ? []
                : (await repository.Db.Query(nameof(StoredFile))
                    .WhereIn(nameof(StoredFile.Id), fileIds)
                    .WhereNull(nameof(StoredFile.DeletedUtc))
                    .GetAsync<StoredFile>(transaction: transaction, cancellationToken: cancellationToken)).ToArray();
            storageNames.AddRange(files.Select(file => file.StorageName));

            DateTime now = timeProvider.GetCurrentUtcDateTime();
            long[] cellIds = targetCells.Select(cell => cell.Id).ToArray();
            deletedCellIds = cellIds;
            int bindingCount = cellIds.Length == 0
                ? 0
                : await repository.Db.Query(nameof(TwitchRewardBinding))
                    .WhereIn(nameof(TwitchRewardBinding.CellConfigId), cellIds)
                    .WhereNull(nameof(TwitchRewardBinding.DeletedUtc))
                    .UpdateAsync(new { DeletedUtc = now, ModifiedUtc = now }, transaction: transaction, cancellationToken: cancellationToken);
            if (cellIds.Length != 0)
                await repository.Db.Query(nameof(CellConfig))
                    .WhereIn(nameof(CellConfig.Id), cellIds)
                    .WhereNull(nameof(CellConfig.DeletedUtc))
                    .UpdateAsync(new { DeletedUtc = now, ModifiedUtc = now }, transaction: transaction, cancellationToken: cancellationToken);
            if (targetPageIds.Length != 0)
                await repository.Db.Query(nameof(DeckPage))
                    .WhereIn(nameof(DeckPage.Id), targetPageIds)
                    .WhereNull(nameof(DeckPage.DeletedUtc))
                    .UpdateAsync(new { DeletedUtc = now, ModifiedUtc = now }, transaction: transaction, cancellationToken: cancellationToken);
            await repository.Db.Query(nameof(Profile))
                .Where(nameof(Profile.Id), profile.Id)
                .WhereNull(nameof(Profile.DeletedUtc))
                .UpdateAsync(new { DeletedUtc = now, ModifiedUtc = now }, transaction: transaction, cancellationToken: cancellationToken);
            await repository.Db.Query(nameof(AppSettings))
                .Where(nameof(AppSettings.ActiveProfileId), profile.Id)
                .WhereNull(nameof(AppSettings.DeletedUtc))
                .UpdateAsync(new { ActiveProfileId = (long?)null, ModifiedUtc = now }, transaction: transaction, cancellationToken: cancellationToken);
            if (exclusiveAssetIds.Length != 0)
                await repository.Db.Query(nameof(MediaAsset))
                    .WhereIn(nameof(MediaAsset.Id), exclusiveAssetIds)
                    .WhereNull(nameof(MediaAsset.DeletedUtc))
                    .UpdateAsync(new { DeletedUtc = now, ModifiedUtc = now }, transaction: transaction, cancellationToken: cancellationToken);
            if (fileIds.Length != 0)
                await repository.Db.Query(nameof(StoredFile))
                    .WhereIn(nameof(StoredFile.Id), fileIds)
                    .WhereNull(nameof(StoredFile.DeletedUtc))
                    .UpdateAsync(new { DeletedUtc = now, ModifiedUtc = now }, transaction: transaction, cancellationToken: cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            result = new DeleteProfileResultModel(profile.Id, profile.Name, targetCells.Length, exclusiveAssets.Length, files.Length, bindingCount);
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

        try
        {
            await playbackQueue.RemoveCellsAsync(deletedCellIds.ToHashSet(), CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.ProfileQueueCleanupFailed(exception, result!.ProfileId);
        }
        foreach (string storageName in storageNames)
        {
            try
            {
                await storage.DeleteAsync(storageName, CancellationToken.None);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.ProfileMediaCleanupFailed(exception, result!.ProfileId);
            }
        }
        logger.ProfileDeleted(result!.ProfileId, result.CellCount, result.MediaCount);
        return result;
    }

    private static long[] AssetIds(IEnumerable<CellConfig> cells) => cells
        .SelectMany(cell => new long?[] { cell.MediaAssetId, cell.ThumbnailMediaAssetId })
        .Where(id => id.HasValue)
        .Select(id => id!.Value)
        .Distinct()
        .ToArray();
}
