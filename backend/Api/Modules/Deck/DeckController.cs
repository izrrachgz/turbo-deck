using Cqrs.Abstractions;
using Application.Modules.Deck;
using Microsoft.AspNetCore.Mvc;
using Api.Modules.Deck.Requests;
using Application.Modules.Deck.Queries;
using Application.Modules.Deck.Commands;

namespace Api.Modules.Deck;

[ApiController]
[Route("api/v1")]
public sealed class DeckController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet("profiles")]
    public async Task<ActionResult<IReadOnlyList<ProfileModel>>> GetProfiles(CancellationToken cancellationToken)
        => Ok(await dispatcher.Send(new GetProfilesQuery(), cancellationToken));

    [HttpPost("profiles")]
    public async Task<ActionResult<ProfileModel>> CreateProfile(CreateProfileRequest request, CancellationToken cancellationToken)
    {
        ProfileModel created = await dispatcher.Send(new CreateProfileCommand(request.Name), cancellationToken);
        return Created($"/api/v1/profiles/{created.Id}", created);
    }

    [HttpGet("profiles/{profileId:long}/pages")]
    public async Task<ActionResult<IReadOnlyList<DeckPageModel>>> GetPages(long profileId, CancellationToken cancellationToken)
        => Ok(await dispatcher.Send(new GetProfilePagesQuery(profileId), cancellationToken));

    [HttpPost("profiles/{profileId:long}/pages")]
    public async Task<ActionResult<DeckPageModel>> CreatePage(long profileId, CancellationToken cancellationToken)
    {
        DeckPageModel created = await dispatcher.Send(new CreatePageCommand(profileId), cancellationToken);
        return Created($"/api/v1/pages/{created.Id}", created);
    }

    [HttpGet("pages/{pageId:long}/grid")]
    public async Task<ActionResult<DeckGridModel>> GetGrid(long pageId, CancellationToken cancellationToken)
    {
        DeckGridModel? grid = await dispatcher.Send(new GetDeckGridQuery(pageId), cancellationToken);
        return grid is null ? NotFound() : Ok(grid);
    }

    [HttpPut("pages/{pageId:long}/cells/{rowIndex:int}/{columnIndex:int}")]
    public async Task<ActionResult<CellConfigModel>> UpsertCell(
        long pageId,
        int rowIndex,
        int columnIndex,
        UpsertCellConfigRequest request,
        CancellationToken cancellationToken)
        => Ok(await dispatcher.Send(
            new UpsertCellConfigCommand(
                pageId,
                rowIndex,
                columnIndex,
                request.Label,
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
                request.QueuePriority),
            cancellationToken));
}
