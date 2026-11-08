using Api.Configuration;
using Api.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Services;

public interface IBackendSettingsStore
{
    NetworkSettingsResponse GetNetwork();

    Task SaveNetworkAsync(NetworkSettingsRequest request, CancellationToken cancellationToken);

    Task SaveLanApiKeyAsync(GeneratedLanApiKey apiKey, CancellationToken cancellationToken);
}

internal sealed class BackendSettingsStore : IBackendSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly NetworkOptions _network;
    private readonly SecurityOptions _security;

    public BackendSettingsStore(
        BackendRuntime runtime,
        IOptions<NetworkOptions> network,
        IOptions<SecurityOptions> security)
    {
        _settingsPath = runtime.SettingsPath;
        _network = network.Value;
        _security = security.Value;
    }

    public NetworkSettingsResponse GetNetwork() => new(
        _network.Mode,
        _network.Addresses,
        _network.Port,
        _network.DiscoveryEnabled,
        _network.InstanceName,
        _network.InstanceId,
        _network.CorsAllowedOrigins,
        _network.AllowInsecureHttp,
        _network.Https.Enabled,
        _network.Https.CertificatePath,
        !string.IsNullOrWhiteSpace(_network.Https.CertificatePassword));

    public async Task SaveNetworkAsync(NetworkSettingsRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            PersistedBackendSettings settings = await ReadAsync(cancellationToken);
            string instanceId = Guid.TryParse(settings.Api.Network.InstanceId, out _)
                ? settings.Api.Network.InstanceId
                : Guid.NewGuid().ToString("D");
            settings.Api.Network = new PersistedNetworkSettings
            {
                Mode = request.Mode,
                Addresses = request.Addresses,
                Port = request.Port,
                DiscoveryEnabled = request.DiscoveryEnabled,
                InstanceName = request.InstanceName.Trim(),
                InstanceId = instanceId,
                CorsAllowedOrigins = request.CorsAllowedOrigins,
                AllowInsecureHttp = request.AllowInsecureHttp,
                Https = new HttpsOptions
                {
                    Enabled = request.Https.Enabled,
                    CertificatePath = request.Https.CertificatePath,
                    CertificatePassword = request.Https.CertificatePassword
                }
            };
            await WriteAsync(settings, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveLanApiKeyAsync(GeneratedLanApiKey apiKey, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            PersistedBackendSettings settings = await ReadAsync(cancellationToken);
            settings.Api.Security = new PersistedSecuritySettings
            {
                LanApiKeySalt = apiKey.Salt,
                LanApiKeyHash = apiKey.Hash
            };
            await WriteAsync(settings, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<PersistedBackendSettings> ReadAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_settingsPath))
        {
            await using var stream = File.OpenRead(_settingsPath);
            PersistedBackendSettings? existing = await JsonSerializer.DeserializeAsync<PersistedBackendSettings>(
                stream,
                SerializerOptions,
                cancellationToken);
            if (existing is not null)
                return existing;
        }

        return new PersistedBackendSettings
        {
            Api = new PersistedApiSettings
            {
                Network = PersistedNetworkSettings.From(_network),
                Security = new PersistedSecuritySettings
                {
                    LanApiKeySalt = _security.LanApiKeySalt,
                    LanApiKeyHash = _security.LanApiKeyHash
                }
            }
        };
    }

    private async Task WriteAsync(PersistedBackendSettings settings, CancellationToken cancellationToken)
    {
        string temporaryPath = $"{_settingsPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private sealed class PersistedBackendSettings
    {
        public PersistedApiSettings Api { get; set; } = new();
    }

    private sealed class PersistedApiSettings
    {
        public PersistedNetworkSettings Network { get; set; } = new();

        public PersistedSecuritySettings Security { get; set; } = new();
    }

    private sealed class PersistedNetworkSettings
    {
        public NetworkMode Mode { get; set; } = NetworkMode.Loopback;

        public string[] Addresses { get; set; } = ["127.0.0.1"];

        public int Port { get; set; }

        public bool DiscoveryEnabled { get; set; }

        public string InstanceName { get; set; } = "Turbo Deck Backend";

        public string InstanceId { get; set; } = Guid.NewGuid().ToString("D");

        public string[] CorsAllowedOrigins { get; set; } = [];

        public bool AllowInsecureHttp { get; set; }

        public HttpsOptions Https { get; set; } = new();

        public static PersistedNetworkSettings From(NetworkOptions options) => new()
        {
            Mode = options.Mode,
            Addresses = options.Addresses,
            Port = options.Port,
            DiscoveryEnabled = options.DiscoveryEnabled,
            InstanceName = options.InstanceName,
            InstanceId = Guid.TryParse(options.InstanceId, out _) ? options.InstanceId : Guid.NewGuid().ToString("D"),
            CorsAllowedOrigins = options.CorsAllowedOrigins,
            AllowInsecureHttp = options.AllowInsecureHttp,
            Https = options.Https
        };
    }

    private sealed class PersistedSecuritySettings
    {
        public string LanApiKeySalt { get; set; } = string.Empty;

        public string LanApiKeyHash { get; set; } = string.Empty;
    }
}
