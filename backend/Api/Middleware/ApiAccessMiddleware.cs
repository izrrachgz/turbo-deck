using System.Net;
using Api.Logging;
using Api.Services;
using Api.Constants;
using Api.Configuration;
using Microsoft.Extensions.Options;

namespace Api.Middleware;

internal sealed class ApiAccessMiddleware(
    RequestDelegate next,
    ISecurityTokenService tokens,
    IOptions<NetworkOptions> network,
    ILogger<ApiAccessMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        bool hostedClient = context.Request.Path.StartsWithSegments(ApiConstants.VisualizerPath) ||
            context.Request.Path.StartsWithSegments(ApiConstants.DeckExplorerPath);
        if (HttpMethods.IsGet(context.Request.Method) && hostedClient)
        {
            await next(context);
            return;
        }

        bool loopback = context.Connection.RemoteIpAddress is null || IPAddress.IsLoopback(context.Connection.RemoteIpAddress);
        bool management = context.Request.Path.StartsWithSegments("/system") || IsTwitchCredentialManagement(context.Request);
        string controlToken = context.Request.Headers[ApiConstants.ControlTokenHeader].ToString();
        string apiKey = context.Request.Headers[ApiConstants.ApiKeyHeader].ToString();
        string queryToken = context.Request.Query["access_token"].ToString();
        bool visualizerRealtime = context.Request.Path.StartsWithSegments(ApiConstants.VisualizerHubPath);
        bool deckExplorerRealtime = context.Request.Path.StartsWithSegments(ApiConstants.DeckExplorerHubPath);
        bool receiveOnlyRealtime = visualizerRealtime || deckExplorerRealtime;
        bool realtime = receiveOnlyRealtime || context.Request.Path.StartsWithSegments(ApiConstants.RealtimeHubPath);
        bool visualizerApi = context.Request.Path.StartsWithSegments(ApiConstants.VisualizerApiPath);
        bool deckExplorerApi = context.Request.Path.StartsWithSegments(ApiConstants.DeckExplorerApiPath);
        bool publicClientAccess = receiveOnlyRealtime || visualizerApi || deckExplorerApi;

        if (realtime && !string.IsNullOrWhiteSpace(queryToken))
        {
            apiKey = queryToken;
            controlToken = queryToken;
        }

        bool allowed = publicClientAccess || (management
            ? loopback && tokens.IsControlToken(controlToken)
            : loopback && tokens.IsControlToken(controlToken) || tokens.IsLanApiKey(apiKey));

        if (!allowed)
        {
            context.Response.StatusCode = management && !loopback
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status401Unauthorized;
            logger.AccessDenied(
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.Response.StatusCode);
            await context.Response.WriteAsJsonAsync(new
            {
                title = context.Response.StatusCode == StatusCodes.Status403Forbidden ? "Forbidden" : "Unauthorized",
                status = context.Response.StatusCode,
                errorCode = context.Response.StatusCode == StatusCodes.Status403Forbidden
                    ? "local_control_required"
                    : "invalid_api_credential",
                traceId = context.TraceIdentifier
            });
            return;
        }

        if (receiveOnlyRealtime)
            context.Items[ApiConstants.ReceiveOnlyPlaybackAccessItem] = true;

        if (deckExplorerApi &&
            !HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Headers.TryGetValue("Origin", out var deckOrigin) &&
            !IsAllowedRealtimeOrigin(context, deckOrigin.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            logger.AccessDenied(
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.Response.StatusCode);
            return;
        }

        if (network.Value.Mode == NetworkMode.Lan &&
            realtime &&
            context.Request.Headers.TryGetValue("Origin", out var origin) &&
            !IsAllowedRealtimeOrigin(context, origin.ToString()))
        {
            logger.RealtimeOriginDenied(
                origin.ToString(),
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }

    private bool IsAllowedRealtimeOrigin(HttpContext context, string origin)
    {
        if (!BrowserOriginCatalog.TryNormalize(origin, out string normalized))
            return false;

        string sameOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
        return normalized.Equals(sameOrigin, StringComparison.OrdinalIgnoreCase) ||
            BrowserOriginCatalog.NormalizeDistinct(network.Value.CorsAllowedOrigins)
                .Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsTwitchCredentialManagement(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method) || !request.Path.StartsWithSegments(ApiConstants.TwitchApiPath, out PathString remaining))
            return false;
        return remaining == "/device-authorization" ||
            remaining == "/device-authorization/poll" ||
            remaining == "/disconnect";
    }

}
