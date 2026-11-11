using Api.Models;
using Api.Constants;
using System.Text.Json;
using Api.Configuration;
using Common.Enumerations;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;

namespace Api.Services;

public interface IBackendSettingsStore
{
    NetworkSettingsResponse GetNetwork();

    Task SaveNetworkAsync(NetworkSettingsRequest request, CancellationToken cancellationToken);

    Task SaveLanApiKeyAsync(GeneratedLanApiKey apiKey, CancellationToken cancellationToken);

    DatabaseConnectionConfiguration GetDatabase(DatabaseEngine engine);

    DatabaseConnectionConfiguration GetActiveDatabase();

    Task SaveDatabaseAsync(DatabaseEngine engine, string connectionString, CancellationToken cancellationToken);
}

public sealed record DatabaseConnectionConfiguration(DatabaseEngine Engine, string ConnectionString, bool Configured);

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
    private readonly DatabaseOptions _database;
    private readonly IDataProtector _databaseProtector;
    private readonly object _databaseLock = new();
    private DatabaseEngine _activeDatabaseEngine;
    private Dictionary<DatabaseEngine, string> _databaseConnectionStrings;

    public BackendSettingsStore(
        BackendRuntime runtime,
        IOptions<NetworkOptions> network,
        IOptions<SecurityOptions> security,
        IOptions<DatabaseOptions> database,
        IDataProtectionProvider dataProtectionProvider)
    {
        _settingsPath = runtime.SettingsPath;
        _network = network.Value;
        _security = security.Value;
        _database = database.Value;
        _databaseProtector = dataProtectionProvider.CreateProtector("TurboDeck.Database.ConnectionStrings.v1");
        (_activeDatabaseEngine, _databaseConnectionStrings) = LoadDatabaseSettings();
    }

    public NetworkSettingsResponse GetNetwork() => new(
        _network.Mode,
        NetworkAddressCatalog.NormalizeDistinct(_network.Addresses),
        NetworkAddressCatalog.GetSuggestions(),
        _network.Port,
        _network.DiscoveryEnabled,
        _network.InstanceName,
        _network.InstanceId,
        _network.CorsAllowedOrigins,
        _network.AllowInsecureHttp,
        _network.Https.Enabled,
        _network.Https.CertificatePath,
        !string.IsNullOrWhiteSpace(_network.Https.CertificatePassword),
        !string.IsNullOrWhiteSpace(_security.LanApiKeyHash));

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
                Addresses = NetworkAddressCatalog.NormalizeDistinct(request.Addresses),
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
                    CertificatePassword = string.IsNullOrEmpty(request.Https.CertificatePassword)
                        ? _network.Https.CertificatePassword
                        : request.Https.CertificatePassword
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
            settings.Api.Security.LanApiKeySalt = apiKey.Salt;
            settings.Api.Security.LanApiKeyHash = apiKey.Hash;
            await WriteAsync(settings, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public DatabaseConnectionConfiguration GetDatabase(DatabaseEngine engine)
    {
        lock (_databaseLock)
        {
            bool configured = _databaseConnectionStrings.TryGetValue(engine, out string? connectionString) &&
                !string.IsNullOrWhiteSpace(connectionString);
            return new DatabaseConnectionConfiguration(engine, connectionString ?? string.Empty, configured);
        }
    }

    public DatabaseConnectionConfiguration GetActiveDatabase()
    {
        lock (_databaseLock)
        {
            bool configured = _databaseConnectionStrings.TryGetValue(_activeDatabaseEngine, out string? connectionString) &&
                !string.IsNullOrWhiteSpace(connectionString);
            return new DatabaseConnectionConfiguration(_activeDatabaseEngine, connectionString ?? string.Empty, configured);
        }
    }

    public async Task SaveDatabaseAsync(DatabaseEngine engine, string connectionString, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            PersistedBackendSettings settings = await ReadAsync(cancellationToken);
            settings.Api.Database.ActiveEngine = engine.ToString();
            settings.Api.Database.ProtectedConnectionStrings[engine.ToString()] = _databaseProtector.Protect(connectionString);
            await WriteAsync(settings, cancellationToken);

            lock (_databaseLock)
            {
                _activeDatabaseEngine = engine;
                _databaseConnectionStrings[engine] = connectionString;
            }
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
                },
                Database = CreatePersistedDatabaseSettings()
            }
        };
    }

    private (DatabaseEngine ActiveEngine, Dictionary<DatabaseEngine, string> ConnectionStrings) LoadDatabaseSettings()
    {
        var connectionStrings = new Dictionary<DatabaseEngine, string>();
        if (!string.IsNullOrWhiteSpace(_database.Db))
            connectionStrings[_database.Engine] = _database.Db;

        if (!File.Exists(_settingsPath))
            return (_database.Engine, connectionStrings);

        try
        {
            string json = File.ReadAllText(_settingsPath);
            PersistedBackendSettings? settings = JsonSerializer.Deserialize<PersistedBackendSettings>(json, SerializerOptions);
            if (settings is null)
                return (_database.Engine, connectionStrings);

            foreach ((string engineName, string protectedConnectionString) in settings.Api.Database.ProtectedConnectionStrings)
            {
                if (Enum.TryParse(engineName, ignoreCase: true, out DatabaseEngine engine) && !string.IsNullOrWhiteSpace(protectedConnectionString))
                    connectionStrings[engine] = _databaseProtector.Unprotect(protectedConnectionString);
            }

            DatabaseEngine activeEngine = Enum.TryParse(
                settings.Api.Database.ActiveEngine,
                ignoreCase: true,
                out DatabaseEngine storedEngine)
                ? storedEngine
                : _database.Engine;
            return (activeEngine, connectionStrings);
        }
        catch (Exception exception) when (exception is JsonException or System.Security.Cryptography.CryptographicException or IOException)
        {
            return (_database.Engine, connectionStrings);
        }
    }

    private PersistedDatabaseSettings CreatePersistedDatabaseSettings()
    {
        var persisted = new PersistedDatabaseSettings { ActiveEngine = _activeDatabaseEngine.ToString() };
        lock (_databaseLock)
        {
            foreach ((DatabaseEngine engine, string connectionString) in _databaseConnectionStrings)
                persisted.ProtectedConnectionStrings[engine.ToString()] = _databaseProtector.Protect(connectionString);
        }
        return persisted;
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

        public PersistedDatabaseSettings Database { get; set; } = new();
    }

    private sealed class PersistedNetworkSettings
    {
        public NetworkMode Mode { get; set; } = NetworkMode.Loopback;

        public string[] Addresses { get; set; } = ["127.0.0.1"];

        public int Port { get; set; } = ApiConstants.DefaultPort;

        public bool DiscoveryEnabled { get; set; }

        public string InstanceName { get; set; } = "Turbo Deck Backend";

        public string InstanceId { get; set; } = Guid.NewGuid().ToString("D");

        public string[] CorsAllowedOrigins { get; set; } = [];

        public bool AllowInsecureHttp { get; set; }

        public HttpsOptions Https { get; set; } = new();

        public static PersistedNetworkSettings From(NetworkOptions options) => new()
        {
            Mode = options.Mode,
            Addresses = NetworkAddressCatalog.NormalizeDistinct(options.Addresses),
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

    private sealed class PersistedDatabaseSettings
    {
        public string ActiveEngine { get; set; } = DatabaseEngine.SqLite.ToString();

        public Dictionary<string, string> ProtectedConnectionStrings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
