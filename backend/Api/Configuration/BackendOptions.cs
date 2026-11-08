namespace Api.Configuration;

public enum NetworkMode
{
    Loopback,
    Lan
}

internal sealed class NetworkOptions
{
    public NetworkMode Mode { get; init; } = NetworkMode.Loopback;

    public string[] Addresses { get; init; } = ["127.0.0.1"];

    public int Port { get; init; }

    public bool DiscoveryEnabled { get; init; }

    public string InstanceName { get; init; } = "Turbo Deck Backend";

    public string InstanceId { get; init; } = Guid.Empty.ToString("D");

    public string[] CorsAllowedOrigins { get; init; } = [];

    public bool AllowInsecureHttp { get; init; }

    public HttpsOptions Https { get; init; } = new();
}

internal sealed class HttpsOptions
{
    public bool Enabled { get; init; }

    public string CertificatePath { get; init; } = string.Empty;

    public string CertificatePassword { get; init; } = string.Empty;
}

internal sealed class SecurityOptions
{
    public string ControlToken { get; init; } = string.Empty;

    public string LanApiKeySalt { get; init; } = string.Empty;

    public string LanApiKeyHash { get; init; } = string.Empty;
}

internal sealed class FilesOptions
{
    public string DataDirectory { get; init; } = string.Empty;

    public string RootPath { get; init; } = "files";

    public long MaximumFileSizeBytes { get; init; } = 200L * 1024 * 1024;
}

internal sealed class RealtimeOptions
{
    public int MaximumReceiveMessageSizeBytes { get; init; } = 64 * 1024;

    public int RequestTimeoutSeconds { get; init; } = 30;

    public string RedisBackplaneConnectionString { get; init; } = string.Empty;

    public string RedisChannelPrefix { get; init; } = "turbo-deck-realtime";
}
