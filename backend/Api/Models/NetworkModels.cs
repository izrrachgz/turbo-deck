using Api.Configuration;

namespace Api.Models;

public sealed record HttpsSettingsRequest(bool Enabled, string CertificatePath, string CertificatePassword);

public sealed record NetworkSettingsRequest(
    NetworkMode Mode,
    string[] Addresses,
    int Port,
    bool DiscoveryEnabled,
    string InstanceName,
    string[] CorsAllowedOrigins,
    bool AllowInsecureHttp,
    HttpsSettingsRequest Https);

public sealed record NetworkSettingsResponse(
    NetworkMode Mode,
    string[] Addresses,
    string[] SuggestedAddresses,
    int Port,
    bool DiscoveryEnabled,
    string InstanceName,
    string InstanceId,
    string[] CorsAllowedOrigins,
    bool AllowInsecureHttp,
    bool HttpsEnabled,
    string CertificatePath,
    bool CertificatePasswordConfigured,
    bool LanApiKeyConfigured);

public sealed record SettingsChangedResponse(bool RestartRequired);

public sealed record LanApiKeyResponse(string ApiKey, bool RestartRequired);
