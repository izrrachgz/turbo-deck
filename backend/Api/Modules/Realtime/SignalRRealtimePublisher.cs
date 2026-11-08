using Api.Modules.Realtime.Logging;
using Application.Modules.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Api.Modules.Realtime;

internal sealed class SignalRRealtimePublisher(
    IHubContext<RealtimeHub, IRealtimeClient> hub,
    TimeProvider timeProvider,
    ILogger<SignalRRealtimePublisher> logger)
    : IRealtimePublisher
{
    public async Task PublishAsync(RealtimePublishRequest request, CancellationToken cancellationToken = default)
    {
        var message = new RealtimeMessage(
            Guid.NewGuid(),
            request.CorrelationId,
            RealtimeMessageKind.Event,
            request.Name,
            "server",
            request.Payload.Clone(),
            timeProvider.GetUtcNow().UtcDateTime,
            request.ExpiresUtc);

        IRealtimeClient client = request.Target switch
        {
            RealtimeTargetKind.Client when !string.IsNullOrWhiteSpace(request.TargetId) =>
                hub.Clients.Group(RealtimeHub.ClientGroup(request.TargetId)),
            RealtimeTargetKind.Group when !string.IsNullOrWhiteSpace(request.TargetId) =>
                hub.Clients.Group(RealtimeHub.UserGroup(request.TargetId)),
            RealtimeTargetKind.Broadcast => hub.Clients.All,
            _ => throw new InvalidOperationException("The realtime publication target is invalid.")
        };

        await client.Receive(message).WaitAsync(cancellationToken);
        logger.RealtimeEventPublished(request.Name, request.Target, request.TargetId);
    }
}
