using Api.Logging;

namespace Api.Modules.Twitch.Logging;

internal static class TwitchApiEventIds
{
    public const int Authorized = ApiEventIdRanges.Twitch + 1;
    public const int Connected = ApiEventIdRanges.Twitch + 2;
    public const int Disconnected = ApiEventIdRanges.Twitch + 3;
    public const int ConnectionFailed = ApiEventIdRanges.Twitch + 4;
    public const int RedemptionTriggered = ApiEventIdRanges.Twitch + 5;
    public const int RevocationFailed = ApiEventIdRanges.Twitch + 6;
}
