using Api.Configuration;

namespace Api.Models;

public sealed record HttpsSettingsRequest(
    bool Enabled,
    string CertificatePath,
    string CertificatePassword,
    bool Managed = false,
    string CertificateAuthorityPath = "",
    string[]? CertificateIdentities = null,
    DateTime? CertificateExpiresUtc = null);

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
    bool LanApiKeyConfigured,
    bool HttpsManaged,
    bool CertificateConfigured,
    string CertificateAuthorityPath,
    string[] CertificateIdentities,
    string[] SuggestedCertificateIdentities,
    DateTime? CertificateExpiresUtc);

public sealed record ManagedHttpsCertificateResponse(
    string CertificatePath,
    string CertificateAuthorityPath,
    string[] Identities,
    DateTime ExpiresUtc,
    bool TrustedOnCurrentComputer);

public sealed record SettingsChangedResponse(
    bool RestartRequired,
    ManagedHttpsCertificateResponse? Certificate = null);

public sealed record LanApiKeyResponse(string ApiKey, bool RestartRequired);
