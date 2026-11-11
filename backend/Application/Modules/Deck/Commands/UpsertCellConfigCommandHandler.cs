using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using Domain.Modules.Deck;
using Domain.Modules.Media;
using Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Deck.Logging;

namespace Application.Modules.Deck.Commands;

internal sealed class UpsertCellConfigCommandHandler(
    IWritableRepository repository,
    TimeProvider timeProvider,
    ILogger<UpsertCellConfigCommandHandler> logger) : ICommandHandler<UpsertCellConfigCommand, CellConfigModel>
{
    public async Task<CellConfigModel> Handle(UpsertCellConfigCommand request, CancellationToken cancellationToken)
    {
        DeckPage? page = await repository.Db.Query(nameof(DeckPage))
            .Where(nameof(DeckPage.Id), request.PageId)
            .WhereNull(nameof(DeckPage.DeletedUtc))
            .FirstOrDefaultAsync<DeckPage>(cancellationToken: cancellationToken);
        if (page is null)
            throw new FileNotFoundException("The deck page was not found.");

        if (request.MediaAssetId is long mediaAssetId)
        {
            MediaAsset? mediaAsset = await repository.Db.Query(nameof(MediaAsset))
                .Where(nameof(MediaAsset.Id), mediaAssetId)
                .WhereNull(nameof(MediaAsset.DeletedUtc))
                .FirstOrDefaultAsync<MediaAsset>(cancellationToken: cancellationToken);
            if (mediaAsset is null)
                throw new FileNotFoundException("The media asset was not found.");
        }

        if (request.ThumbnailMediaAssetId is long thumbnailMediaAssetId)
        {
            MediaAsset? thumbnail = await repository.Db.Query(nameof(MediaAsset))
                .Where(nameof(MediaAsset.Id), thumbnailMediaAssetId)
                .WhereNull(nameof(MediaAsset.DeletedUtc))
                .FirstOrDefaultAsync<MediaAsset>(cancellationToken: cancellationToken);
            if (thumbnail is null)
                throw new FileNotFoundException("The thumbnail media asset was not found.");
            if (!string.Equals(thumbnail.Kind, "image", StringComparison.Ordinal))
                throw new InvalidDataException("A cell thumbnail must use an image media asset.");
        }

        CellConfig? existing = await repository.Db.Query(nameof(CellConfig))
            .Where(nameof(CellConfig.PageId), request.PageId)
            .Where(nameof(CellConfig.RowIndex), request.RowIndex)
            .Where(nameof(CellConfig.ColumnIndex), request.ColumnIndex)
            .WhereNull(nameof(CellConfig.DeletedUtc))
            .FirstOrDefaultAsync<CellConfig>(cancellationToken: cancellationToken);
        DateTime now = timeProvider.GetCurrentUtcDateTime();
        string? label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim();

        if (existing is not null)
        {
            await repository.Db.Query(nameof(CellConfig))
                .Where(nameof(CellConfig.Id), existing.Id)
                .UpdateAsync(new
                {
                    Label = label,
                    request.Enabled,
                    request.MediaAssetId,
                    request.ThumbnailMediaAssetId,
                    request.Volume,
                    request.PlaybackRate,
                    request.PitchSemitones,
                    request.DisplayDurationMilliseconds,
                    request.DisplayPermanently,
                    request.DisplaySize,
                    request.AnimationInType,
                    request.AnimationInDurationMilliseconds,
                    request.AnimationOutType,
                    request.AnimationOutDurationMilliseconds,
                    request.QueuePriority,
                    ModifiedUtc = now
                }, cancellationToken: cancellationToken);
            existing.Label = label;
            existing.Enabled = request.Enabled;
            existing.MediaAssetId = request.MediaAssetId;
            existing.ThumbnailMediaAssetId = request.ThumbnailMediaAssetId;
            existing.Volume = request.Volume;
            existing.PlaybackRate = request.PlaybackRate;
            existing.PitchSemitones = request.PitchSemitones;
            existing.DisplayDurationMilliseconds = request.DisplayDurationMilliseconds;
            existing.DisplayPermanently = request.DisplayPermanently;
            existing.DisplaySize = request.DisplaySize;
            existing.AnimationInType = request.AnimationInType;
            existing.AnimationInDurationMilliseconds = request.AnimationInDurationMilliseconds;
            existing.AnimationOutType = request.AnimationOutType;
            existing.AnimationOutDurationMilliseconds = request.AnimationOutDurationMilliseconds;
            existing.QueuePriority = request.QueuePriority;
            existing.ModifiedUtc = now;
            logger.CellConfigured(existing.Id, request.PageId, request.RowIndex, request.ColumnIndex);
            return existing.ToModel();
        }

        long id = await repository.Db.Query(nameof(CellConfig)).InsertGetIdAsync<long>(new
        {
            request.PageId,
            request.RowIndex,
            request.ColumnIndex,
            Label = label,
            request.Enabled,
            request.MediaAssetId,
            request.ThumbnailMediaAssetId,
            request.Volume,
            request.PlaybackRate,
            request.PitchSemitones,
            request.DisplayDurationMilliseconds,
            request.DisplayPermanently,
            request.DisplaySize,
            request.AnimationInType,
            request.AnimationInDurationMilliseconds,
            request.AnimationOutType,
            request.AnimationOutDurationMilliseconds,
            request.QueuePriority,
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: cancellationToken);
        logger.CellConfigured(id, request.PageId, request.RowIndex, request.ColumnIndex);
        return new CellConfig
        {
            Id = id,
            PageId = request.PageId,
            RowIndex = request.RowIndex,
            ColumnIndex = request.ColumnIndex,
            Label = label,
            Enabled = request.Enabled,
            MediaAssetId = request.MediaAssetId,
            ThumbnailMediaAssetId = request.ThumbnailMediaAssetId,
            Volume = request.Volume,
            PlaybackRate = request.PlaybackRate,
            PitchSemitones = request.PitchSemitones,
            DisplayDurationMilliseconds = request.DisplayDurationMilliseconds,
            DisplayPermanently = request.DisplayPermanently,
            DisplaySize = request.DisplaySize,
            AnimationInType = request.AnimationInType,
            AnimationInDurationMilliseconds = request.AnimationInDurationMilliseconds,
            AnimationOutType = request.AnimationOutType,
            AnimationOutDurationMilliseconds = request.AnimationOutDurationMilliseconds,
            QueuePriority = request.QueuePriority,
            CreatedUtc = now,
            ModifiedUtc = now
        }.ToModel();
    }
}
