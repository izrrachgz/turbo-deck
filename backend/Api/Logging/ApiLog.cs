using Microsoft.Extensions.Logging;

namespace Api.Logging;

internal static partial class ApiLog
{
    [LoggerMessage(
        EventId = ApiEventIds.BackendReady,
        Level = LogLevel.Information,
        Message = "Backend ready. ProcessId: {ProcessId}. LoopbackUrl: {LoopbackUrl}. ListenerCount: {ListenerCount}.")]
    public static partial void BackendReady(this ILogger logger, int processId, string loopbackUrl, int listenerCount);

    [LoggerMessage(EventId = ApiEventIds.UnhandledException, Level = LogLevel.Error, Message = "Unhandled API exception for {TraceId}.")]
    public static partial void UnhandledException(this ILogger logger, Exception exception, string traceId);

    [LoggerMessage(
        EventId = ApiEventIds.AccessDenied,
        Level = LogLevel.Warning,
        Message = "API access denied. Method: {Method}. Path: {Path}. RemoteAddress: {RemoteAddress}. StatusCode: {StatusCode}.")]
    public static partial void AccessDenied(this ILogger logger, string method, string path, string remoteAddress, int statusCode);

    [LoggerMessage(
        EventId = ApiEventIds.RealtimeOriginDenied,
        Level = LogLevel.Warning,
        Message = "Realtime connection origin denied. Origin: {Origin}. RemoteAddress: {RemoteAddress}.")]
    public static partial void RealtimeOriginDenied(this ILogger logger, string origin, string remoteAddress);

    [LoggerMessage(
        EventId = ApiEventIds.ParentProcessMonitoringStarted,
        Level = LogLevel.Information,
        Message = "Parent process monitoring started. ParentProcessId: {ParentProcessId}.")]
    public static partial void ParentProcessMonitoringStarted(this ILogger logger, int parentProcessId);

    [LoggerMessage(
        EventId = ApiEventIds.ParentProcessExited,
        Level = LogLevel.Information,
        Message = "Parent process exited; backend shutdown requested. ParentProcessId: {ParentProcessId}.")]
    public static partial void ParentProcessExited(this ILogger logger, int parentProcessId);

    [LoggerMessage(
        EventId = ApiEventIds.DiscoveryStarted,
        Level = LogLevel.Information,
        Message = "Advertising backend instance {InstanceId} through mDNS on port {Port}.")]
    public static partial void DiscoveryStarted(this ILogger logger, string instanceId, int port);

    [LoggerMessage(
        EventId = ApiEventIds.DiscoveryFailed,
        Level = LogLevel.Warning,
        Message = "The backend could not advertise its LAN service through mDNS.")]
    public static partial void DiscoveryFailed(this ILogger logger, Exception exception);
}
