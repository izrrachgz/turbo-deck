namespace Api.Modules.Twitch.Requests;

public sealed record StartTwitchDeviceAuthorizationRequest(string ClientId);

public sealed record UpsertTwitchRewardBindingRequest(string RewardTitle, long CellConfigId, bool Enabled);
