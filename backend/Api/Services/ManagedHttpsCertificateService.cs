using Api.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Api.Services;

public sealed record ManagedHttpsCertificate(
    string CertificatePath,
    string CertificatePassword,
    string CertificateAuthorityPath,
    string[] Identities,
    DateTime ExpiresUtc,
    bool TrustedOnCurrentComputer);

public interface IManagedHttpsCertificateService
{
    Task<ManagedHttpsCertificate> EnsureAsync(IEnumerable<string> requestedIdentities, CancellationToken cancellationToken);
}

internal sealed class ManagedHttpsCertificateException(string message, Exception innerException) : Exception(message, innerException);

internal sealed class ManagedHttpsCertificateService(
    BackendRuntime runtime,
    IWebHostEnvironment environment,
    Microsoft.Extensions.Options.IOptions<NetworkOptions> networkOptions) : IManagedHttpsCertificateService
{
    private const string CertificateDirectoryName = "Certificates";
    private const string AuthorityCertificateFileName = "turbo-deck-local-ca.cer";
    private const string AuthorityPrivateFileName = "turbo-deck-local-ca.pfx";
    private const string ServerCertificateFileName = "turbo-deck-server.pfx";
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
    private static readonly TimeSpan RenewalWindow = TimeSpan.FromDays(30);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly NetworkOptions _network = networkOptions.Value;
    private ManagedHttpsCertificate? _current;

    public async Task<ManagedHttpsCertificate> EnsureAsync(IEnumerable<string> requestedIdentities, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await EnsureCoreAsync(requestedIdentities, cancellationToken);
        }
        catch (Exception exception) when (exception is CryptographicException or IOException or UnauthorizedAccessException)
        {
            throw new ManagedHttpsCertificateException(
                $"Turbo Deck could not generate or trust the managed HTTPS certificate. {exception.Message}",
                exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ManagedHttpsCertificate> EnsureCoreAsync(
        IEnumerable<string> requestedIdentities,
        CancellationToken cancellationToken)
    {
        string[] identities = CertificateIdentityCatalog.NormalizeDistinct(requestedIdentities);
        if (identities.Length == 0)
            throw new InvalidOperationException("At least one concrete certificate identity is required.");

        string directory = Path.Combine(runtime.DataDirectory, CertificateDirectoryName);
        Directory.CreateDirectory(directory);
        string authorityCertificatePath = Path.Combine(directory, AuthorityCertificateFileName);
        string authorityPrivatePath = Path.Combine(directory, AuthorityPrivateFileName);
        string serverCertificatePath = Path.Combine(directory, ServerCertificateFileName);

        if (_current is not null && File.Exists(_current.CertificatePath) &&
            identities.SequenceEqual(_current.Identities, StringComparer.OrdinalIgnoreCase) &&
            _current.ExpiresUtc > DateTime.UtcNow.Add(RenewalWindow))
            return _current;

        string password = _network.Https.CertificatePassword;
        if (CanReuseServerCertificate(serverCertificatePath, password, identities, out DateTime expiresUtc))
        {
            bool trusted = TrustAuthorityIfSupported(authorityCertificatePath);
            _current = new ManagedHttpsCertificate(
                serverCertificatePath,
                password,
                authorityCertificatePath,
                identities,
                expiresUtc,
                trusted);
            return _current;
        }

        if (string.IsNullOrWhiteSpace(password))
            password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36));

        using X509Certificate2 authority = LoadOrCreateAuthority(
            authorityPrivatePath,
            authorityCertificatePath,
            password);
        using X509Certificate2 server = CreateServerCertificate(authority, identities);
        await WriteAtomicallyAsync(serverCertificatePath, server.Export(X509ContentType.Pfx, password), cancellationToken);
        bool authorityTrusted = TrustAuthorityIfSupported(authorityCertificatePath);

        _current = new ManagedHttpsCertificate(
            serverCertificatePath,
            password,
            authorityCertificatePath,
            identities,
            server.NotAfter.ToUniversalTime(),
            authorityTrusted);
        return _current;
    }

    private bool CanReuseServerCertificate(
        string certificatePath,
        string password,
        string[] identities,
        out DateTime expiresUtc)
    {
        expiresUtc = DateTime.MinValue;
        if (!_network.Https.Managed || string.IsNullOrWhiteSpace(password) || !File.Exists(certificatePath) ||
            !identities.SequenceEqual(_network.Https.CertificateIdentities, StringComparer.OrdinalIgnoreCase))
            return false;

        try
        {
            using X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                password,
                X509KeyStorageFlags.EphemeralKeySet);
            expiresUtc = certificate.NotAfter.ToUniversalTime();
            return certificate.HasPrivateKey && expiresUtc > DateTime.UtcNow.Add(RenewalWindow);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static X509Certificate2 LoadOrCreateAuthority(string privatePath, string publicPath, string password)
    {
        if (File.Exists(privatePath))
        {
            try
            {
                X509Certificate2 loadedAuthority = X509CertificateLoader.LoadPkcs12FromFile(
                    privatePath,
                    password,
                    X509KeyStorageFlags.EphemeralKeySet);
                if (!File.Exists(publicPath))
                    WriteAtomically(publicPath, loadedAuthority.Export(X509ContentType.Cert));
                return loadedAuthority;
            }
            catch (CryptographicException)
            {
                // A missing or invalid managed authority is replaced with a new local trust root.
            }
        }

        using RSA key = RSA.Create(4096);
        var request = new CertificateRequest("CN=Turbo Deck Local CA", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
            true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        X509Certificate2 authority = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(10));
        WriteAtomically(privatePath, authority.Export(X509ContentType.Pfx, password));
        WriteAtomically(publicPath, authority.Export(X509ContentType.Cert));
        return authority;
    }

    private static X509Certificate2 CreateServerCertificate(X509Certificate2 authority, string[] identities)
    {
        using RSA key = RSA.Create(3072);
        string commonName = identities.FirstOrDefault(identity => !IPAddress.TryParse(identity, out _)) ?? "Turbo Deck";
        var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new(ServerAuthenticationOid) },
            true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var alternativeNames = new SubjectAlternativeNameBuilder();
        foreach (string identity in identities)
        {
            if (IPAddress.TryParse(identity, out IPAddress? address))
                alternativeNames.AddIpAddress(address);
            else
                alternativeNames.AddDnsName(identity);
        }
        request.CertificateExtensions.Add(alternativeNames.Build());

        byte[] serialNumber = RandomNumberGenerator.GetBytes(16);
        serialNumber[0] &= 0x7F;
        if (serialNumber.All(value => value == 0))
            serialNumber[^1] = 1;
        using X509Certificate2 issued = request.Create(
            authority,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(365),
            serialNumber);
        return issued.CopyWithPrivateKey(key);
    }

    private bool TrustAuthorityIfSupported(string certificatePath)
    {
        if (environment.IsEnvironment("Testing") || !OperatingSystem.IsWindows() || !File.Exists(certificatePath))
            return false;

        using X509Certificate2 authority = X509CertificateLoader.LoadCertificateFromFile(certificatePath);
        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        X509Certificate2Collection matches = store.Certificates.Find(
            X509FindType.FindByThumbprint,
            authority.Thumbprint,
            validOnly: false);
        if (matches.Count == 0)
            store.Add(authority);
        return true;
    }

    private static async Task WriteAtomicallyAsync(string path, byte[] contents, CancellationToken cancellationToken)
    {
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, contents, cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static void WriteAtomically(string path, byte[] contents)
    {
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, contents);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
