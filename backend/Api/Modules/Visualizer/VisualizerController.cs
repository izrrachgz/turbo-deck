using Cqrs.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Application.Modules.Files;
using Application.Modules.Media;
using Application.Modules.Playback;
using Application.Modules.Settings;
using Application.Modules.Files.Queries;
using Application.Modules.Media.Queries;
using Application.Modules.Playback.Queries;
using Application.Modules.Settings.Queries;
using Application.Modules.Playback.Commands;

namespace Api.Modules.Visualizer;

[ApiController]
[Route("api/v1/visualizer")]
public sealed class VisualizerController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet("state")]
    public async Task<ActionResult<VisualizerStateResponse>> GetState(CancellationToken cancellationToken)
    {
        var queue = await dispatcher.Send(new GetPlaybackQueueQuery(), cancellationToken);
        AppSettingsModel settings = await dispatcher.Send(new GetAppSettingsQuery(), cancellationToken);
        return Ok(new VisualizerStateResponse(queue, settings.VisualizerSafeMargin));
    }

    [HttpPost("complete")]
    public async Task<ActionResult<PlaybackQueueSnapshotModel>> Complete(
        CompleteVisualizerPlaybackRequest request,
        CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(
            new CompletePlaybackCommand(request.ItemId, request.Success, request.ErrorCode),
            cancellationToken));

    [HttpGet("media/{id:long}")]
    public async Task<IActionResult> GetMedia(long id, CancellationToken cancellationToken)
    {
        MediaAssetModel? asset = await dispatcher.Send(new GetMediaAssetQuery(id), cancellationToken);
        if (asset is null)
            return NotFound();
        StoredFileContent? file = await dispatcher.Send(new OpenFileQuery(asset.File.Id), cancellationToken);
        return file is null ? NotFound() : File(file.Content, file.File.ContentType, enableRangeProcessing: true);
    }
}
