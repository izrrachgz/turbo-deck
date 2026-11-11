using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using Domain.Modules.Media;
using Persistence.Abstractions;
using Application.Modules.Files;
using Microsoft.Extensions.Logging;
using Application.Modules.Files.Queries;
using Application.Modules.Media.Logging;
using Application.Modules.Files.Commands;

namespace Application.Modules.Media.Commands;

internal sealed class UploadMediaAssetCommandHandler(
    IRequestDispatcher dispatcher,
    IWritableRepository repository,
    IMediaInspector inspector,
    TimeProvider timeProvider,
    ILogger<UploadMediaAssetCommandHandler> logger) : ICommandHandler<UploadMediaAssetCommand, MediaAssetModel>
{
    public async Task<MediaAssetModel> Handle(UploadMediaAssetCommand request, CancellationToken cancellationToken)
    {
        var file = await dispatcher.Send(new UploadFileCommand(
            request.FileName,
            request.ContentType,
            request.Content,
            request.MaximumSizeBytes,
            "media"), cancellationToken);

        try
        {
            StoredFileContent stored = await dispatcher.Send(new OpenFileQuery(file.Id), cancellationToken)
                ?? throw new FileNotFoundException("The stored media content was not found.");
            await using Stream content = stored.Content;
            MediaInspectionResult inspection = await inspector.InspectAsync(
                content,
                file.OriginalFileName,
                file.ContentType,
                cancellationToken);
            DateTime now = timeProvider.GetCurrentUtcDateTime();
            string displayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? Path.GetFileNameWithoutExtension(file.OriginalFileName)
                : request.DisplayName.Trim();
            long id = await repository.Db.Query(nameof(MediaAsset)).InsertGetIdAsync<long>(new
            {
                StoredFileId = file.Id,
                DisplayName = displayName,
                inspection.Kind,
                InspectionStatus = inspection.Status,
                inspection.Width,
                inspection.Height,
                inspection.DurationMilliseconds,
                inspection.VideoCodec,
                inspection.AudioCodec,
                inspection.HasAudio,
                inspection.WaveformData,
                CreatedUtc = now,
                ModifiedUtc = now,
                DeletedUtc = (DateTime?)null
            }, cancellationToken: cancellationToken);
            logger.MediaAssetCreated(id, file.Id, inspection.Kind, inspection.Status);
            return new MediaAsset
            {
                Id = id,
                StoredFileId = file.Id,
                DisplayName = displayName,
                Kind = inspection.Kind,
                InspectionStatus = inspection.Status,
                Width = inspection.Width,
                Height = inspection.Height,
                DurationMilliseconds = inspection.DurationMilliseconds,
                VideoCodec = inspection.VideoCodec,
                AudioCodec = inspection.AudioCodec,
                HasAudio = inspection.HasAudio,
                WaveformData = inspection.WaveformData,
                CreatedUtc = now,
                ModifiedUtc = now
            }.ToModel(file);
        }
        catch
        {
            await dispatcher.Send(new DeleteFileCommand(file.Id), CancellationToken.None);
            logger.MediaAssetCreationFailed(file.Id);
            throw;
        }
    }
}
