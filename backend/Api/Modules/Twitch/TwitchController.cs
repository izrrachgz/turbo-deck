using Cqrs.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Application.Modules.Twitch;
using Api.Modules.Twitch.Requests;
using Application.Modules.Twitch.Queries;
using Application.Modules.Twitch.Commands;

namespace Api.Modules.Twitch;

[ApiController]
[Route("api/v1/twitch")]
public sealed class TwitchController(ITwitchConnection twitch, IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<TwitchConnectionStatusModel> GetStatus() => Ok(twitch.GetStatus());

    [HttpPost("device-authorization")]
    public async Task<ActionResult<TwitchDeviceAuthorizationModel>> StartAuthorization(
        StartTwitchDeviceAuthorizationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await twitch.StartDeviceAuthorizationAsync(request.ClientId, cancellationToken));

    [HttpPost("device-authorization/poll")]
    public async Task<ActionResult<TwitchAuthorizationPollModel>> PollAuthorization(CancellationToken cancellationToken) =>
        Ok(await twitch.PollDeviceAuthorizationAsync(cancellationToken));

    [HttpPost("disconnect")]
    public async Task<ActionResult> Disconnect(CancellationToken cancellationToken)
    {
        await twitch.DisconnectAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("rewards")]
    public async Task<ActionResult<IReadOnlyList<TwitchRewardModel>>> GetRewards(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await twitch.GetRewardsAsync(cancellationToken));
        }
        catch (TwitchReauthorizationRequiredException exception)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = exception.Message
            };
            problem.Extensions["errorCode"] = "twitch_reauthorization_required";
            return Conflict(problem);
        }
    }

    [HttpGet("bindings")]
    public async Task<ActionResult<IReadOnlyList<TwitchRewardBindingModel>>> GetBindings(CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(new GetTwitchRewardBindingsQuery(), cancellationToken));

    [HttpPut("bindings/{rewardId}")]
    public async Task<ActionResult<TwitchRewardBindingModel>> UpsertBinding(
        string rewardId,
        UpsertTwitchRewardBindingRequest request,
        CancellationToken cancellationToken) =>
        Ok(await dispatcher.Send(
            new UpsertTwitchRewardBindingCommand(rewardId, request.RewardTitle, request.CellConfigId, request.Enabled),
            cancellationToken));

    [HttpDelete("bindings/{rewardId}")]
    public async Task<ActionResult> DeleteBinding(string rewardId, CancellationToken cancellationToken)
    {
        bool deleted = await dispatcher.Send(new DeleteTwitchRewardBindingCommand(rewardId), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
