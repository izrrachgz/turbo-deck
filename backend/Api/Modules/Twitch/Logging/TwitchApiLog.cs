namespace Api.Modules.Twitch.Logging;

internal static partial class TwitchApiLog
{
    [LoggerMessage(
        TwitchApiEventIds.Authorized,
        LogLevel.Information,
        "Twitch authorization completed for broadcaster {BroadcasterUserId}")]
    public static partial void TwitchAuthorized(this ILogger logger, string broadcasterUserId);

    [LoggerMessage(TwitchApiEventIds.Connected, LogLevel.Information, "Twitch EventSub WebSocket connected")]
    public static partial void TwitchConnected(this ILogger logger);

    [LoggerMessage(TwitchApiEventIds.Disconnected, LogLevel.Information, "Twitch integration disconnected and stored OAuth tokens purged")]
    public static partial void TwitchDisconnected(this ILogger logger);

    [LoggerMessage(TwitchApiEventIds.ConnectionFailed, LogLevel.Warning, "Twitch EventSub connection failed: {Reason}")]
    public static partial void TwitchConnectionFailed(this ILogger logger, string reason);

    [LoggerMessage(TwitchApiEventIds.RedemptionTriggered, LogLevel.Information, "Twitch reward {RewardId} triggered a bound deck cell")]
    public static partial void TwitchRedemptionTriggered(this ILogger logger, string rewardId);

    [LoggerMessage(
        TwitchApiEventIds.RevocationFailed,
        LogLevel.Warning,
        "Twitch token revocation could not be completed ({Reason}); local authorization will still be purged")]
    public static partial void TwitchRevocationFailed(this ILogger logger, string reason);
}
