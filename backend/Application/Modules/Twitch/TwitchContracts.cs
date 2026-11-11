namespace Application.Modules.Twitch;

public sealed record TwitchConnectionStatusModel(
    bool Configured,
    bool Authorized,
    bool Connected,
    string? BroadcasterDisplayName,
    string? LastError);

public sealed record TwitchDeviceAuthorizationModel(
    string UserCode,
    string VerificationUri,
    DateTime ExpiresUtc,
    int PollIntervalSeconds);

public sealed record TwitchAuthorizationPollModel(bool Pending, TwitchConnectionStatusModel Status);

public sealed record TwitchRewardModel(string Id, string Title, int Cost, bool Enabled, string? Prompt);

public sealed record TwitchRewardBindingModel(
    long Id,
    string RewardId,
    string RewardTitle,
    long CellConfigId,
    bool Enabled,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);
