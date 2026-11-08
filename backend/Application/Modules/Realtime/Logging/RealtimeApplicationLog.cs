using Microsoft.Extensions.Logging;

namespace Application.Modules.Realtime.Logging;

internal static partial class RealtimeApplicationLog
{
    [LoggerMessage(
        EventId = RealtimeApplicationEventIds.RequestDispatching,
        Level = LogLevel.Debug,
        Message = "Dispatching explicitly registered realtime request. Kind: {Kind}. Name: {Name}.")]
    public static partial void RealtimeRequestDispatching(this ILogger logger, object kind, string name);

    [LoggerMessage(
        EventId = RealtimeApplicationEventIds.RequestNotRegistered,
        Level = LogLevel.Debug,
        Message = "Realtime request was not registered. Kind: {Kind}. Name: {Name}.")]
    public static partial void RealtimeRequestNotRegistered(this ILogger logger, object kind, string name);
}
