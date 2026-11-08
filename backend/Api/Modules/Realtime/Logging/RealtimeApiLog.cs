using Microsoft.Extensions.Logging;

namespace Api.Modules.Realtime.Logging;

internal static partial class RealtimeApiLog
{
    [LoggerMessage(
        EventId = RealtimeApiEventIds.ClientConnected,
        Level = LogLevel.Debug,
        Message = "Realtime client connected. ClientId: {ClientId}. ConnectionId: {ConnectionId}.")]
    public static partial void RealtimeClientConnected(this ILogger logger, string clientId, string connectionId);

    [LoggerMessage(
        EventId = RealtimeApiEventIds.ClientDisconnected,
        Level = LogLevel.Debug,
        Message = "Realtime client disconnected. ClientId: {ClientId}. ConnectionId: {ConnectionId}.")]
    public static partial void RealtimeClientDisconnected(this ILogger logger, string clientId, string connectionId);

    [LoggerMessage(
        EventId = RealtimeApiEventIds.ClientDisconnectedWithError,
        Level = LogLevel.Warning,
        Message = "Realtime client disconnected with an error. ClientId: {ClientId}. ConnectionId: {ConnectionId}.")]
    public static partial void RealtimeClientDisconnectedWithError(
        this ILogger logger,
        Exception exception,
        string clientId,
        string connectionId);

    [LoggerMessage(
        EventId = RealtimeApiEventIds.MessageRouted,
        Level = LogLevel.Debug,
        Message = "Realtime message routed. Kind: {Kind}. Name: {Name}. Origin: {Origin}. Target: {Target}. TargetId: {TargetId}.")]
    public static partial void RealtimeMessageRouted(
        this ILogger logger,
        object kind,
        string name,
        string origin,
        object target,
        string? targetId);

    [LoggerMessage(
        EventId = RealtimeApiEventIds.ServerRequestNotRegistered,
        Level = LogLevel.Warning,
        Message = "Realtime server request is not registered. Kind: {Kind}. Name: {Name}. Origin: {Origin}.")]
    public static partial void RealtimeServerRequestNotRegistered(this ILogger logger, object kind, string name, string origin);

    [LoggerMessage(
        EventId = RealtimeApiEventIds.EventPublished,
        Level = LogLevel.Debug,
        Message = "Realtime event published. Name: {Name}. Target: {Target}. TargetId: {TargetId}.")]
    public static partial void RealtimeEventPublished(this ILogger logger, string name, object target, string? targetId);
}
