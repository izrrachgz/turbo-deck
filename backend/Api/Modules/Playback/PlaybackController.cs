using Cqrs.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Application.Modules.Playback;
using Api.Modules.Playback.Requests;
using Application.Modules.Playback.Queries;
using Application.Modules.Playback.Commands;

namespace Api.Modules.Playback;

[ApiController]
[Route("api/v1/playback")]
public sealed class PlaybackController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet("queue")]
    public async Task<ActionResult<PlaybackQueueSnapshotModel>> GetQueue(CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new GetPlaybackQueueQuery(), cancellationToken));

    [HttpPost("queue")]
    public async Task<ActionResult<PlaybackQueueSnapshotModel>> Queue(
        QueueCellRequest request,
        CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new QueueCellCommand(request.CellConfigId), cancellationToken));

    [HttpPost("complete")]
    public async Task<ActionResult<PlaybackQueueSnapshotModel>> Complete(
        CompletePlaybackRequest request,
        CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(
            new CompletePlaybackCommand(request.ItemId, request.Success, request.ErrorCode),
            cancellationToken));

    [HttpDelete("queue/{itemId:guid}")]
    public async Task<ActionResult<PlaybackQueueSnapshotModel>> Remove(Guid itemId, CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new RemovePlaybackQueueItemCommand(itemId), cancellationToken));

    [HttpPost("queue/{itemId:guid}/move")]
    public async Task<ActionResult<PlaybackQueueSnapshotModel>> Move(
        Guid itemId,
        MovePlaybackQueueItemRequest request,
        CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new MovePlaybackQueueItemCommand(itemId, request.Offset), cancellationToken));

    [HttpPost("clear")]
    public async Task<ActionResult<PlaybackQueueSnapshotModel>> Clear(
        ClearPlaybackQueueRequest request,
        CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new ClearPlaybackQueueCommand(request.StopCurrent), cancellationToken));
}
