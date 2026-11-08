using Application.Logging;

namespace Application.Modules.Realtime.Logging;

internal static class RealtimeApplicationEventIds
{
    public const int RequestDispatching = ApplicationEventIdRanges.Realtime + 0;
    public const int RequestNotRegistered = ApplicationEventIdRanges.Realtime + 1;
}
