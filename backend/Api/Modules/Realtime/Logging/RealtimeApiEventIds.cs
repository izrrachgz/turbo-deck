using Api.Logging;

namespace Api.Modules.Realtime.Logging;

internal static class RealtimeApiEventIds
{
    public const int ClientConnected = ApiEventIdRanges.Realtime + 0;
    public const int ClientDisconnected = ApiEventIdRanges.Realtime + 1;
    public const int ClientDisconnectedWithError = ApiEventIdRanges.Realtime + 2;
    public const int MessageRouted = ApiEventIdRanges.Realtime + 3;
    public const int ServerRequestNotRegistered = ApiEventIdRanges.Realtime + 4;
    public const int EventPublished = ApiEventIdRanges.Realtime + 5;
}
