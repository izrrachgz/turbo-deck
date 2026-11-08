using Api.Configuration;
using Api.Constants;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.System;

[ApiController]
[Route("system")]
public sealed class SystemController(
    IBackendSettingsStore settings,
    ISecurityTokenService tokens,
    IHostApplicationLifetime lifetime) : ControllerBase
{
    [HttpGet("network")]
    public ActionResult<NetworkSettingsResponse> GetNetwork() => Ok(settings.GetNetwork());

    [HttpPut("network")]
    public async Task<ActionResult<SettingsChangedResponse>> UpdateNetwork(
        NetworkSettingsRequest request,
        CancellationToken cancellationToken)
    {
        string? error = Validate(request);
        if (error is not null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: error);

        if (request.Mode == NetworkMode.Lan && !tokens.HasLanApiKey)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Generate a LAN API key before enabling LAN mode.");

        await settings.SaveNetworkAsync(request, cancellationToken);
        return Ok(new SettingsChangedResponse(RestartRequired: true));
    }

    [HttpPost("network/api-key/rotate")]
    public async Task<ActionResult<LanApiKeyResponse>> RotateLanApiKey(CancellationToken cancellationToken)
    {
        GeneratedLanApiKey generated = tokens.GenerateLanApiKey();
        await settings.SaveLanApiKeyAsync(generated, cancellationToken);
        tokens.SetLanKey(generated.Salt, generated.Hash);
        return Ok(new LanApiKeyResponse(generated.Plaintext, RestartRequired: false));
    }

    [HttpPost("restart")]
    public ActionResult Restart()
    {
        Environment.ExitCode = ApiConstants.RestartRequestedExitCode;
        Response.OnCompleted(() =>
        {
            lifetime.StopApplication();
            return Task.CompletedTask;
        });
        return Accepted();
    }

    [HttpPost("shutdown")]
    public ActionResult Shutdown()
    {
        Environment.ExitCode = 0;
        Response.OnCompleted(() =>
        {
            lifetime.StopApplication();
            return Task.CompletedTask;
        });
        return Accepted();
    }

    private static string? Validate(NetworkSettingsRequest request)
    {
        if (request.Port is < 0 or > 65535)
            return "The port must be between 0 and 65535.";
        if (string.IsNullOrWhiteSpace(request.InstanceName) || request.InstanceName.Length > 100)
            return "The instance name is required and cannot exceed 100 characters.";
        if (request.Mode == NetworkMode.Lan && request.Addresses.Length == 0)
            return "At least one LAN address is required.";
        if (request.CorsAllowedOrigins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme is not ("http" or "https")))
        {
            return "Every CORS origin must be an absolute HTTP or HTTPS origin.";
        }
        if (request.Mode == NetworkMode.Lan && request.Https.Enabled && string.IsNullOrWhiteSpace(request.Https.CertificatePath))
            return "A certificate path is required when LAN HTTPS is enabled.";
        if (request.Mode == NetworkMode.Lan && !request.Https.Enabled && !request.AllowInsecureHttp)
            return "LAN mode requires HTTPS unless insecure HTTP is explicitly enabled.";
        return null;
    }
}
