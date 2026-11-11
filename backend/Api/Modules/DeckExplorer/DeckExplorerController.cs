using Cqrs.Abstractions;
using Application.Modules.Deck;
using Microsoft.AspNetCore.Mvc;
using Application.Modules.Files;
using Application.Modules.Media;
using Application.Modules.Playback;
using Application.Modules.Deck.Queries;
using Application.Modules.Files.Queries;
using Application.Modules.Media.Queries;
using Application.Modules.Playback.Queries;
using Application.Modules.Playback.Commands;

namespace Api.Modules.DeckExplorer;

[ApiController]
[Route("api/v1/deck-explorer")]
public sealed class DeckExplorerController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet("profiles")]
    public async Task<ActionResult<IReadOnlyList<ProfileModel>>> GetProfiles(CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new GetProfilesQuery(), cancellationToken));

    [HttpGet("profiles/{profileId:long}/pages")]
    public async Task<ActionResult<IReadOnlyList<DeckPageModel>>> GetPages(long profileId, CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new GetProfilePagesQuery(profileId), cancellationToken));

    [HttpGet("pages/{pageId:long}/grid")]
    public async Task<ActionResult<DeckGridModel>> GetGrid(long pageId, CancellationToken cancellationToken)
    {
        DeckGridModel? grid = await dispatcher.Send(new GetDeckGridQuery(pageId), cancellationToken);
        return grid is null ? NotFound() : Ok(grid);
    }

    [HttpGet("queue")]
    public async Task<ActionResult<PlaybackQueueSnapshotModel>> GetQueue(CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new GetPlaybackQueueQuery(), cancellationToken));

    [HttpPost("cells/{cellConfigId:long}/trigger")]
    public async Task<ActionResult<PlaybackQueueSnapshotModel>> Trigger(long cellConfigId, CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new QueueCellCommand(cellConfigId), cancellationToken));

    [HttpGet("thumbnails/{mediaAssetId:long}")]
    public async Task<IActionResult> GetThumbnail(long mediaAssetId, CancellationToken cancellationToken)
    {
        MediaAssetModel? asset = await dispatcher.Send(new GetMediaAssetQuery(mediaAssetId), cancellationToken);
        if (asset is null || !asset.Kind.Equals("image", StringComparison.OrdinalIgnoreCase))
            return NotFound();
        StoredFileContent? file = await dispatcher.Send(new OpenFileQuery(asset.File.Id), cancellationToken);
        return file is null ? NotFound() : File(file.Content, file.File.ContentType, enableRangeProcessing: true);
    }
}
