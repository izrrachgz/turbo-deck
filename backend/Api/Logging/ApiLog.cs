using Microsoft.Extensions.Logging;

namespace Api.Logging;

internal static partial class ApiLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Unhandled API exception for {TraceId}.")]
    public static partial void UnhandledException(this ILogger logger, Exception exception, string traceId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Advertising backend instance {InstanceId} through mDNS on port {Port}.")]
    public static partial void DiscoveryStarted(this ILogger logger, string instanceId, int port);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "The backend could not advertise its LAN service through mDNS.")]
    public static partial void DiscoveryFailed(this ILogger logger, Exception exception);
}
