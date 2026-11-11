using Api.Modules.Twitch;
using Microsoft.AspNetCore.Mvc;
using Application.Modules.Twitch;

namespace Api.Test.Modules.Twitch;

public sealed class TwitchControllerTest
{
    [Fact]
    public async Task Rewards_returns_an_actionable_conflict_when_public_client_reauthorization_is_required()
    {
        var controller = new TwitchController(new ReauthorizationRequiredConnection(), null!);

        ActionResult<IReadOnlyList<TwitchRewardModel>> result = await controller.GetRewards(TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(409, conflict.StatusCode);
        Assert.Equal(TwitchReauthorizationRequiredException.UserMessage, problem.Title);
        Assert.Equal("twitch_reauthorization_required", problem.Extensions["errorCode"]);
    }

    private sealed class ReauthorizationRequiredConnection : ITwitchConnection
    {
        public TwitchConnectionStatusModel GetStatus() => throw new NotSupportedException();

        public Task<TwitchDeviceAuthorizationModel> StartDeviceAuthorizationAsync(string clientId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<TwitchAuthorizationPollModel> PollDeviceAuthorizationAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TwitchRewardModel>> GetRewardsAsync(CancellationToken cancellationToken) =>
            throw new TwitchReauthorizationRequiredException(new HttpRequestException("missing client secret"));

        public Task DisconnectAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
