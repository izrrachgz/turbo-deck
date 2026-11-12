using Api.Models;
using Api.Services;
using Api.Constants;
using Api.Configuration;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Api.Modules.System.Logging;

namespace Api.Modules.System;

[ApiController]
[Route("system")]
public sealed class SystemController(
    IBackendSettingsStore settings,
    ISecurityTokenService tokens,
    IManagedHttpsCertificateService certificates,
    IHostApplicationLifetime lifetime,
    ILogger<SystemController> logger) : ControllerBase
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

        ManagedHttpsCertificate? certificate = null;
        if (request.Https.Enabled)
        {
            string[] identities = CertificateIdentityCatalog.GetSuggestedIdentities(request.Addresses);
            certificate = await certificates.EnsureAsync(identities, cancellationToken);
            request = request with
            {
                Https = new HttpsSettingsRequest(
                    Enabled: true,
                    certificate.CertificatePath,
                    certificate.CertificatePassword,
                    Managed: true,
                    certificate.CertificateAuthorityPath,
                    certificate.Identities,
                    certificate.ExpiresUtc)
            };
        }

        await settings.SaveNetworkAsync(request, cancellationToken);
        logger.NetworkSettingsChanged(request.Mode, request.Port, request.DiscoveryEnabled);
        ManagedHttpsCertificateResponse? certificateResponse = certificate is null
            ? null
            : new ManagedHttpsCertificateResponse(
                certificate.CertificatePath,
                certificate.CertificateAuthorityPath,
                certificate.Identities,
                certificate.ExpiresUtc,
                certificate.TrustedOnCurrentComputer);
        return Ok(new SettingsChangedResponse(RestartRequired: true, certificateResponse));
    }

    [HttpPost("network/api-key/rotate")]
    public async Task<ActionResult<LanApiKeyResponse>> RotateLanApiKey(CancellationToken cancellationToken)
    {
        GeneratedLanApiKey generated = tokens.GenerateLanApiKey();
        await settings.SaveLanApiKeyAsync(generated, cancellationToken);
        tokens.SetLanKey(generated.Salt, generated.Hash);
        logger.LanApiKeyRotated();
        return Ok(new LanApiKeyResponse(generated.Plaintext, RestartRequired: false));
    }

    [HttpPost("restart")]
    public ActionResult Restart()
    {
        logger.RestartRequested();
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
        logger.ShutdownRequested();
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
        if (request.Port is < 1 or > 65535)
            return "The port must be between 1 and 65535.";
        if (string.IsNullOrWhiteSpace(request.InstanceName) || request.InstanceName.Length > 100)
            return "The instance name is required and cannot exceed 100 characters.";
        string? addressError = ValidateAddresses(request.Addresses);
        if (addressError is not null)
            return addressError;
        if (request.Mode == NetworkMode.Lan && request.Addresses.Length == 0)
            return "At least one LAN address is required.";
        string? originError = ValidateBrowserOrigins(request.CorsAllowedOrigins);
        if (originError is not null)
            return originError;
        if (request.Mode == NetworkMode.Lan && !request.Https.Enabled && !request.AllowInsecureHttp)
            return "LAN mode requires HTTPS unless insecure HTTP is explicitly enabled.";
        return null;
    }

    private static string? ValidateAddresses(string[] addresses)
    {
        if (addresses.Length > 32)
            return "No more than 32 listener addresses can be configured.";

        var parsedAddresses = new List<IPAddress>(addresses.Length);
        foreach (string address in addresses)
        {
            if (!NetworkAddressCatalog.TryParse(address, out IPAddress parsed))
                return "Every listener address must be localhost or a valid IPv4 or IPv6 address.";
            if (parsedAddresses.Contains(parsed))
                return "Listener addresses must be unique and cannot resolve to the same interface.";
            parsedAddresses.Add(parsed);
        }

        if (parsedAddresses.Count > 1 && parsedAddresses.Any(NetworkAddressCatalog.IsWildcard))
            return "A wildcard listener cannot be combined with another listener address.";
        return null;
    }

    private static string? ValidateBrowserOrigins(string[] origins)
    {
        if (origins.Length > 64)
            return "No more than 64 browser origins can be configured.";

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string origin in origins)
        {
            if (!BrowserOriginCatalog.TryNormalize(origin, out string value))
                return "Every browser origin must be HTTP or HTTPS and cannot contain a path, query, fragment, or credentials.";
            if (!normalized.Add(value))
                return "Browser origins must be unique.";
        }
        return null;
    }
}
