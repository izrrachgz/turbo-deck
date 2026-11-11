namespace Application.Modules.Twitch;

public interface ITwitchConnection
{
    TwitchConnectionStatusModel GetStatus();

    Task<TwitchDeviceAuthorizationModel> StartDeviceAuthorizationAsync(string clientId, CancellationToken cancellationToken);

    Task<TwitchAuthorizationPollModel> PollDeviceAuthorizationAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<TwitchRewardModel>> GetRewardsAsync(CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);
}
