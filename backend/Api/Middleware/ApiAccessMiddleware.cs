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

        bool loopback = context.Connection.RemoteIpAddress is null || IPAddress.IsLoopback(context.Connection.RemoteIpAddress);
        bool management = context.Request.Path.StartsWithSegments("/system");
        string controlToken = context.Request.Headers[ApiConstants.ControlTokenHeader].ToString();
        string apiKey = context.Request.Headers[ApiConstants.ApiKeyHeader].ToString();

        if (context.Request.Path.StartsWithSegments(ApiConstants.RealtimeHubPath) &&
            context.Request.Query.TryGetValue("access_token", out var queryToken))
        {
            apiKey = queryToken.ToString();
            controlToken = queryToken.ToString();
        }

        bool allowed = management
            ? loopback && tokens.IsControlToken(controlToken)
            : loopback && tokens.IsControlToken(controlToken) || tokens.IsLanApiKey(apiKey);

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

        if (network.Value.Mode == NetworkMode.Lan &&
            context.Request.Path.StartsWithSegments(ApiConstants.RealtimeHubPath) &&
            context.Request.Headers.TryGetValue("Origin", out var origin) &&
            !network.Value.CorsAllowedOrigins.Contains(origin.ToString().TrimEnd('/'), StringComparer.OrdinalIgnoreCase))
        {
            logger.RealtimeOriginDenied(
                origin.ToString(),
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}
