using Api.Configuration;
using Application.Modules.Realtime;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace Api.Modules.Realtime;

public sealed class RealtimeHub(
    RealtimeRequestRegistry requests,
    IRealtimeConnectionRegistry connections,
    TimeProvider timeProvider,
    IConfiguration configuration) : Hub<IRealtimeClient>
{
    private static readonly Regex IdentifierPattern = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$", RegexOptions.CultureInvariant);

    public override async Task OnConnectedAsync()
    {
        string clientId = Context.GetHttpContext()?.Request.Query["clientId"].ToString() ?? string.Empty;
        ValidateIdentifier(clientId, "client ID");
        Context.Items[nameof(clientId)] = clientId;
        connections.Add(clientId, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, ClientGroup(clientId), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("clientId", out object? value) && value is string clientId)
            connections.Remove(clientId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Send(RealtimeSendRequest request)
    {
        ValidateRequest(request);
        string origin = GetClientId();
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (request.ExpiresUtc is DateTime expires && expires <= now)
            throw new HubException("The realtime message has expired.");

        if (request.Target == RealtimeTargetKind.Server)
        {
            await DispatchToServer(request, now);
            return;
        }

        var message = new RealtimeMessage(
            request.MessageId,
            request.CorrelationId,
            request.Kind,
            request.Name,
            origin,
            request.Payload.Clone(),
            now,
            request.ExpiresUtc);

        switch (request.Target)
        {
            case RealtimeTargetKind.Client:
                ValidateIdentifier(request.TargetId, "target client ID");
                await Clients.Group(ClientGroup(request.TargetId!)).Receive(message);
                break;
            case RealtimeTargetKind.Group:
                ValidateIdentifier(request.TargetId, "target group");
                await Clients.Group(UserGroup(request.TargetId!)).Receive(message);
                break;
            case RealtimeTargetKind.Broadcast:
                await Clients.Others.Receive(message);
                break;
            default:
                throw new HubException("The realtime target is not supported.");
        }
    }

    public async Task JoinGroup(string group)
    {
        ValidateIdentifier(group, "group");
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(group), Context.ConnectionAborted);
    }

    public async Task LeaveGroup(string group)
    {
        ValidateIdentifier(group, "group");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(group), Context.ConnectionAborted);
    }

    public IReadOnlyList<string> GetConnectedClients() => connections.GetConnectedClients();

    private async Task DispatchToServer(RealtimeSendRequest request, DateTime now)
    {
        if (request.Kind is not (RealtimeMessageKind.Command or RealtimeMessageKind.Query))
            throw new HubException("Only explicitly registered commands and queries can target the server.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted);
        timeout.CancelAfter(TimeSpan.FromSeconds(configuration.GetValue("Api:Realtime:RequestTimeoutSeconds", 30)));
        try
        {
            global::System.Text.Json.JsonElement? payload = await requests.DispatchAsync(
                request.Kind,
                request.Name,
                request.Payload,
                timeout.Token);
            await Clients.Caller.ReceiveResponse(new RealtimeResponse(
                Guid.NewGuid(),
                request.CorrelationId ?? request.MessageId,
                request.Name,
                true,
                payload,
                null,
                null,
                now));
        }
        catch (KeyNotFoundException)
        {
            await Clients.Caller.ReceiveResponse(new RealtimeResponse(
                Guid.NewGuid(),
                request.CorrelationId ?? request.MessageId,
                request.Name,
                false,
                null,
                "realtime_request_not_registered",
                "The requested command or query is not registered.",
                now));
        }
    }

    private string GetClientId() => Context.Items.TryGetValue("clientId", out object? value) && value is string clientId
        ? clientId
        : throw new HubException("The realtime client is not registered.");

    private static void ValidateRequest(RealtimeSendRequest request)
    {
        if (request.MessageId == Guid.Empty)
            throw new HubException("A message ID is required.");
        ValidateIdentifier(request.Name, "message name");
    }

    private static void ValidateIdentifier(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || !IdentifierPattern.IsMatch(value))
            throw new HubException($"The {field} is invalid.");
    }

    internal static string ClientGroup(string clientId) => $"client:{clientId.ToLowerInvariant()}";

    internal static string UserGroup(string group) => $"group:{group.ToLowerInvariant()}";
}
