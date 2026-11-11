using Time.Extensions;
using Cqrs.Abstractions;
using Domain.Modules.Deck;
using Domain.Modules.Media;
using Domain.Modules.Settings;
using Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Playback.Logging;

namespace Application.Modules.Playback.Commands;

internal sealed class QueueCellCommandHandler(
    IReadOnlyRepository repository,
    IPlaybackQueue queue,
    TimeProvider timeProvider,
    ILogger<QueueCellCommandHandler> logger) : ICommandHandler<QueueCellCommand, PlaybackQueueSnapshotModel>
{
    public async Task<PlaybackQueueSnapshotModel> Handle(QueueCellCommand request, CancellationToken cancellationToken)
    {
        var cellQuery = repository.Query(nameof(CellConfig))
            .Where(nameof(CellConfig.Id), request.CellConfigId)
            .Where(nameof(CellConfig.Enabled), true)
            .WhereNull(nameof(CellConfig.DeletedUtc));
        CellConfig? cell = (await repository.GetAsync<CellConfig>(cellQuery, cancellationToken)).FirstOrDefault();
        if (cell?.MediaAssetId is not long mediaAssetId)
            throw new FileNotFoundException("The enabled deck cell or its media assignment was not found.");

        var assetQuery = repository.Query(nameof(MediaAsset))
            .Where(nameof(MediaAsset.Id), mediaAssetId)
            .WhereNull(nameof(MediaAsset.DeletedUtc));
        MediaAsset? asset = (await repository.GetAsync<MediaAsset>(assetQuery, cancellationToken)).FirstOrDefault();
        if (asset is null)
            throw new FileNotFoundException("The media asset was not found.");

        var settingsQuery = repository.Query(nameof(AppSettings))
            .Where(nameof(AppSettings.Id), 1)
            .WhereNull(nameof(AppSettings.DeletedUtc));
        AppSettings? settings = (await repository.GetAsync<AppSettings>(settingsQuery, cancellationToken)).FirstOrDefault();
        int capacity = settings?.QueueCapacity ?? 100;

        var item = new PlaybackQueueItemModel(
            Guid.NewGuid(),
            cell.Id,
            asset.Id,
            cell.Label ?? asset.DisplayName,
            asset.Kind,
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
            asset.WaveformData,
            asset.Width,
            asset.Height,
            asset.DurationMilliseconds,
            cell.QueuePriority,
            timeProvider.GetCurrentUtcDateTime());
        PlaybackQueueSnapshotModel snapshot = await queue.EnqueueAsync(item, capacity, cancellationToken);
        logger.PlaybackQueued(item.Id, item.CellConfigId, item.MediaAssetId, item.Priority);
        return snapshot;
    }
}
