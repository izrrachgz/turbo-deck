using System.Net;
using System.Text;
using System.Text.Json;
using Cqrs.Abstractions;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Net.Http.Headers;
using Api.Modules.Twitch.Logging;
using Application.Modules.Twitch;
using Application.Modules.Twitch.Commands;

namespace Api.Modules.Twitch;

internal sealed class TwitchConnectionService(
    IHttpClientFactory httpClientFactory,
    ITwitchTokenStore tokenStore,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<TwitchConnectionService> logger) : BackgroundService, ITwitchConnection
{
    private static readonly Uri DeviceUri = new("https://id.twitch.tv/oauth2/device");
    private static readonly Uri TokenUri = new("https://id.twitch.tv/oauth2/token");
    private static readonly Uri ValidateUri = new("https://id.twitch.tv/oauth2/validate");
    private static readonly Uri RevokeUri = new("https://id.twitch.tv/oauth2/revoke");
    private static readonly Uri RewardsUri = new("https://api.twitch.tv/helix/channel_points/custom_rewards");
    private static readonly Uri SubscriptionsUri = new("https://api.twitch.tv/helix/eventsub/subscriptions");
    private static readonly Uri EventSubUri = new("wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30");
    private const string Scope = "channel:read:redemptions";

    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    private readonly SemaphoreSlim _wakeSignal = new(0, 1);
    private readonly object _statusGate = new();
    private readonly Queue<string> _messageOrder = new();
    private readonly HashSet<string> _messageIds = new(StringComparer.Ordinal);
    private TwitchSession? _session;
    private PendingAuthorization? _pendingAuthorization;
    private ClientWebSocket? _activeSocket;
    private bool _connected;
    private string? _lastError;

    public TwitchConnectionStatusModel GetStatus()
    {
        lock (_statusGate)
        {
            return new TwitchConnectionStatusModel(
                _session is not null || _pendingAuthorization is not null,
                _session is not null,
                _connected,
                _session?.BroadcasterDisplayName,
                _lastError);
        }
    }

    public async Task<TwitchDeviceAuthorizationModel> StartDeviceAuthorizationAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        string normalizedClientId = clientId.Trim();
        if (normalizedClientId.Length is < 10 or > 64 || normalizedClientId.Any(character => !char.IsLetterOrDigit(character)))
            throw new InvalidDataException("A valid Twitch application client ID is required.");

        using var request = new HttpRequestMessage(HttpMethod.Post, DeviceUri)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = normalizedClientId,
                ["scopes"] = Scope
            })
        };
        using HttpResponseMessage response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using JsonDocument document = await ReadJsonAsync(response, cancellationToken);
        JsonElement root = document.RootElement;
        int expiresIn = root.GetProperty("expires_in").GetInt32();
        int interval = root.GetProperty("interval").GetInt32();
        var pending = new PendingAuthorization(
            normalizedClientId,
            root.GetProperty("device_code").GetString()!,
            root.GetProperty("user_code").GetString()!,
            root.GetProperty("verification_uri").GetString()!,
            timeProvider.GetUtcNow().AddSeconds(expiresIn).UtcDateTime,
            Math.Max(interval, 1),
            DateTime.MinValue);
        lock (_statusGate)
        {
            _pendingAuthorization = pending;
            _lastError = null;
        }
        return new TwitchDeviceAuthorizationModel(pending.UserCode, pending.VerificationUri, pending.ExpiresUtc, pending.IntervalSeconds);
    }

    public async Task<TwitchAuthorizationPollModel> PollDeviceAuthorizationAsync(CancellationToken cancellationToken)
    {
        PendingAuthorization pending;
        lock (_statusGate)
            pending = _pendingAuthorization ?? throw new InvalidOperationException("No Twitch device authorization is pending.");

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (pending.ExpiresUtc <= now)
        {
            lock (_statusGate)
                _pendingAuthorization = null;
            throw new InvalidOperationException("The Twitch device authorization code expired.");
        }
        if (pending.NextPollUtc > now)
            return new TwitchAuthorizationPollModel(true, GetStatus());

        lock (_statusGate)
            _pendingAuthorization = pending with { NextPollUtc = now.AddSeconds(pending.IntervalSeconds) };

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUri)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = pending.ClientId,
                ["scopes"] = Scope,
                ["device_code"] = pending.DeviceCode,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            })
        };
        using HttpResponseMessage response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            using JsonDocument error = await ReadJsonAsync(response, cancellationToken);
            if (string.Equals(
                error.RootElement.GetProperty("message").GetString(),
                "authorization_pending",
                StringComparison.OrdinalIgnoreCase))
                return new TwitchAuthorizationPollModel(true, GetStatus());
        }
        await EnsureSuccessAsync(response, cancellationToken);
        using JsonDocument tokenDocument = await ReadJsonAsync(response, cancellationToken);
        string accessToken = tokenDocument.RootElement.GetProperty("access_token").GetString()!;
        string refreshToken = tokenDocument.RootElement.GetProperty("refresh_token").GetString()!;
        TwitchSession session = await ValidateSessionAsync(
            pending.ClientId,
            accessToken,
            refreshToken,
            cancellationToken);
        await tokenStore.WriteAsync(session, cancellationToken);
        lock (_statusGate)
        {
            _session = session;
            _pendingAuthorization = null;
            _lastError = null;
        }
        logger.TwitchAuthorized(session.BroadcasterUserId);
        WakeConnectionLoop();
        return new TwitchAuthorizationPollModel(false, GetStatus());
    }

    public async Task<IReadOnlyList<TwitchRewardModel>> GetRewardsAsync(CancellationToken cancellationToken)
    {
        TwitchSession session = await GetCurrentSessionAsync(cancellationToken);
        var uri = new UriBuilder(RewardsUri) { Query = $"broadcaster_id={Uri.EscapeDataString(session.BroadcasterUserId)}" }.Uri;
        using var request = CreateHelixRequest(HttpMethod.Get, uri, session);
        using HttpResponseMessage response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using JsonDocument document = await ReadJsonAsync(response, cancellationToken);
        return document.RootElement.GetProperty("data").EnumerateArray()
            .Select(reward => new TwitchRewardModel(
                reward.GetProperty("id").GetString()!,
                reward.GetProperty("title").GetString()!,
                reward.GetProperty("cost").GetInt32(),
                reward.GetProperty("is_enabled").GetBoolean(),
                reward.TryGetProperty("prompt", out JsonElement prompt) ? prompt.GetString() : null))
            .ToArray();
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        TwitchSession? session;
        lock (_statusGate)
        {
            session = _session;
            _session = null;
            _pendingAuthorization = null;
            _connected = false;
            _lastError = null;
        }
        ClientWebSocket? socket = Interlocked.Exchange(ref _activeSocket, null);
        socket?.Abort();
        socket?.Dispose();
        if (session is not null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, RevokeUri)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = session.ClientId,
                        ["token"] = session.AccessToken
                    })
                };
                using HttpResponseMessage _ = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Local purge is authoritative even when Twitch is unavailable.
            }
        }
        await tokenStore.PurgeAsync(cancellationToken);
        logger.TwitchDisconnected();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            TwitchSession? stored = await tokenStore.ReadAsync(stoppingToken);
            lock (_statusGate)
                _session = stored;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            lock (_statusGate)
                _lastError = "Stored Twitch authorization could not be read. Disconnect to purge it.";
            logger.TwitchConnectionFailed(exception.GetType().Name);
        }

        Uri connectionUri = EventSubUri;
        bool resumedConnection = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            if (GetStatus().Authorized)
            {
                try
                {
                    TwitchSession session = await GetCurrentSessionAsync(stoppingToken);
                    Uri? reconnectUri = await RunWebSocketAsync(connectionUri, session, resumedConnection, stoppingToken);
                    connectionUri = reconnectUri ?? EventSubUri;
                    resumedConnection = reconnectUri is not null;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    lock (_statusGate)
                    {
                        _connected = false;
                        _lastError = "Twitch EventSub is reconnecting.";
                    }
                    logger.TwitchConnectionFailed(exception.GetType().Name);
                    connectionUri = EventSubUri;
                    resumedConnection = false;
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            else
            {
                await _wakeSignal.WaitAsync(stoppingToken);
            }
        }
    }

    private async Task<Uri?> RunWebSocketAsync(
        Uri connectionUri,
        TwitchSession session,
        bool resumedConnection,
        CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        Interlocked.Exchange(ref _activeSocket, socket)?.Dispose();
        await socket.ConnectAsync(connectionUri, cancellationToken);
        int timeoutSeconds = 45;
        while (socket.State == WebSocketState.Open)
        {
            using var receiveTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            receiveTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            string message = await ReceiveMessageAsync(socket, receiveTimeout.Token);
            using JsonDocument document = JsonDocument.Parse(message);
            JsonElement root = document.RootElement;
            if (session.LastValidatedUtc < timeProvider.GetUtcNow().AddHours(-1).UtcDateTime)
                session = await GetCurrentSessionAsync(cancellationToken);
            string messageType = root.GetProperty("metadata").GetProperty("message_type").GetString()!;
            if (messageType == "session_welcome")
            {
                JsonElement sessionElement = root.GetProperty("payload").GetProperty("session");
                timeoutSeconds = sessionElement.GetProperty("keepalive_timeout_seconds").GetInt32() + 10;
                if (!resumedConnection)
                    await SubscribeAsync(sessionElement.GetProperty("id").GetString()!, session, cancellationToken);
                lock (_statusGate)
                {
                    _connected = true;
                    _lastError = null;
                }
                logger.TwitchConnected();
            }
            else if (messageType == "session_reconnect")
            {
                string reconnectUrl = root.GetProperty("payload").GetProperty("session").GetProperty("reconnect_url").GetString()!;
                return new Uri(reconnectUrl);
            }
            else if (messageType == "notification")
            {
                string messageId = root.GetProperty("metadata").GetProperty("message_id").GetString()!;
                if (RememberMessage(messageId))
                    await HandleNotificationAsync(root.GetProperty("payload"), cancellationToken);
            }
            else if (messageType == "revocation")
            {
                throw new InvalidOperationException("Twitch revoked the EventSub subscription.");
            }
        }
        return null;
    }

    private async Task SubscribeAsync(string sessionId, TwitchSession session, CancellationToken cancellationToken)
    {
        using var request = CreateHelixRequest(HttpMethod.Post, SubscriptionsUri, session);
        request.Content = JsonContent.Create(new
        {
            type = "channel.channel_points_custom_reward_redemption.add",
            version = "1",
            condition = new { broadcaster_user_id = session.BroadcasterUserId },
            transport = new { method = "websocket", session_id = sessionId }
        });
        using HttpResponseMessage response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task HandleNotificationAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        JsonElement subscription = payload.GetProperty("subscription");
        if (subscription.GetProperty("type").GetString() != "channel.channel_points_custom_reward_redemption.add")
            return;
        string rewardId = payload.GetProperty("event").GetProperty("reward").GetProperty("id").GetString()!;
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IRequestDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IRequestDispatcher>();
        if (await dispatcher.Send(new TriggerTwitchRewardCommand(rewardId), cancellationToken) is not null)
            logger.TwitchRedemptionTriggered(rewardId);
    }

    private async Task<TwitchSession> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        await _sessionGate.WaitAsync(cancellationToken);
        try
        {
            TwitchSession current;
            lock (_statusGate)
                current = _session ?? throw new InvalidOperationException("Twitch is not authorized.");
            DateTime now = timeProvider.GetUtcNow().UtcDateTime;
            if (current.ExpiresUtc > now.AddMinutes(5))
            {
                if (current.LastValidatedUtc >= now.AddHours(-1))
                    return current;

                TwitchSession validated = await ValidateSessionAsync(
                    current.ClientId,
                    current.AccessToken,
                    current.RefreshToken,
                    cancellationToken);
                await tokenStore.WriteAsync(validated, cancellationToken);
                lock (_statusGate)
                    _session = validated;
                return validated;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUri)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = current.ClientId,
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = current.RefreshToken
                })
            };
            using HttpResponseMessage response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            using JsonDocument document = await ReadJsonAsync(response, cancellationToken);
            string accessToken = document.RootElement.GetProperty("access_token").GetString()!;
            string refreshToken = document.RootElement.GetProperty("refresh_token").GetString()!;
            TwitchSession refreshed = current with
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresUtc = timeProvider.GetUtcNow().AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32()).UtcDateTime,
                LastValidatedUtc = timeProvider.GetUtcNow().UtcDateTime
            };
            await tokenStore.WriteAsync(refreshed, cancellationToken);
            lock (_statusGate)
                _session = refreshed;
            return refreshed;
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    private async Task<TwitchSession> ValidateSessionAsync(
        string clientId,
        string accessToken,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ValidateUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);
        using HttpResponseMessage response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using JsonDocument document = await ReadJsonAsync(response, cancellationToken);
        string validatedClientId = document.RootElement.GetProperty("client_id").GetString()!;
        if (!string.Equals(clientId, validatedClientId, StringComparison.Ordinal))
            throw new InvalidOperationException("Twitch returned a token for another client ID.");
        return new TwitchSession(
            clientId,
            accessToken,
            refreshToken,
            timeProvider.GetUtcNow().AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32()).UtcDateTime,
            document.RootElement.GetProperty("user_id").GetString()!,
            document.RootElement.GetProperty("login").GetString()!,
            timeProvider.GetUtcNow().UtcDateTime);
    }

    private static HttpRequestMessage CreateHelixRequest(HttpMethod method, Uri uri, TwitchSession session)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Client-Id", session.ClientId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return request;
    }

    private static async Task<string> ReceiveMessageAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new WebSocketException("Twitch closed the EventSub connection.");
            await stream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
            if (stream.Length > 256 * 1024)
                throw new InvalidDataException("The Twitch EventSub message exceeded the allowed size.");
        } while (!result.EndOfMessage);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private bool RememberMessage(string messageId)
    {
        lock (_messageIds)
        {
            if (!_messageIds.Add(messageId))
                return false;
            _messageOrder.Enqueue(messageId);
            while (_messageOrder.Count > 256)
                _messageIds.Remove(_messageOrder.Dequeue());
            return true;
        }
    }

    private void WakeConnectionLoop()
    {
        if (_wakeSignal.CurrentCount == 0)
            _wakeSignal.Release();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        string reason = response.ReasonPhrase ?? "Twitch request failed";
        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("message", out JsonElement message))
                reason = message.GetString() ?? reason;
        }
        catch (JsonException)
        {
            // Do not surface arbitrary response bodies.
        }
        throw new HttpRequestException(reason, null, response.StatusCode);
    }

    private sealed record PendingAuthorization(
        string ClientId,
        string DeviceCode,
        string UserCode,
        string VerificationUri,
        DateTime ExpiresUtc,
        int IntervalSeconds,
        DateTime NextPollUtc);
}
