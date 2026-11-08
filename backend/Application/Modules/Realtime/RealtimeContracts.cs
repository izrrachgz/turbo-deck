using System.Text.Json;

namespace Application.Modules.Realtime;

public enum RealtimeMessageKind
{
    Event,
    Command,
    Query,
    Response
}

public enum RealtimeTargetKind
{
    Client,
    Group,
    Broadcast,
    Server
}

public sealed record RealtimeSendRequest(
    Guid MessageId,
    Guid? CorrelationId,
    RealtimeMessageKind Kind,
    string Name,
    RealtimeTargetKind Target,
    string? TargetId,
    JsonElement Payload,
    DateTime? ExpiresUtc);

public sealed record RealtimeMessage(
    Guid MessageId,
    Guid? CorrelationId,
    RealtimeMessageKind Kind,
    string Name,
    string OriginClientId,
    JsonElement Payload,
    DateTime CreatedUtc,
    DateTime? ExpiresUtc);

public sealed record RealtimeResponse(
    Guid MessageId,
    Guid CorrelationId,
    string Name,
    bool Success,
    JsonElement? Payload,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime CreatedUtc);

public sealed record RealtimePublishRequest(
    RealtimeTargetKind Target,
    string? TargetId,
    string Name,
    JsonElement Payload,
    Guid? CorrelationId = null,
    DateTime? ExpiresUtc = null);
