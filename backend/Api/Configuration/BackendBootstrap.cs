using Api.Constants;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace Api.Configuration;

internal sealed record BackendRuntime(string DataDirectory, string SettingsPath);

internal static class BackendBootstrap
{
    private const string SettingsFileName = "backendsettings.json";
    private const string DatabaseFileName = "turbo-deck.db";

    public static BackendRuntime Configure(WebApplicationBuilder builder)
    {
        string dataDirectory = ResolveDataDirectory(builder);
        Directory.CreateDirectory(dataDirectory);

        string settingsPath = Path.Combine(dataDirectory, SettingsFileName);
        builder.Configuration.AddJsonFile(settingsPath, optional: true, reloadOnChange: false);

        var derived = new Dictionary<string, string?>
        {
            [$"{ApiConstants.Section}:DataDirectory"] = dataDirectory,
            [$"{ApiConstants.Section}:Files:DataDirectory"] = dataDirectory
        };

        string? controlToken = Environment.GetEnvironmentVariable("TURBO_DECK_CONTROL_TOKEN");
        if (!string.IsNullOrWhiteSpace(controlToken))
            derived[$"{ApiConstants.Section}:Security:ControlToken"] = controlToken;

        string networkPortPath = $"{ApiConstants.Section}:Network:Port";
        int configuredPort = builder.Configuration.GetValue(networkPortPath, ApiConstants.DefaultPort);
        if (int.TryParse(builder.Configuration["Port"], out int commandLinePort))
            configuredPort = commandLinePort;
        if (configuredPort == 0 && !builder.Environment.IsEnvironment("Testing"))
            configuredPort = ApiConstants.DefaultPort;
        derived[networkPortPath] = configuredPort.ToString(System.Globalization.CultureInfo.InvariantCulture);

        string enginePath = $"{ApiConstants.Section}:ConnectionStrings:Engine";
        string databasePath = $"{ApiConstants.Section}:ConnectionStrings:Db";
        string engine = builder.Configuration[enginePath] ?? "SqLite";
        derived[enginePath] = engine;

        if (string.Equals(engine, "SqLite", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(builder.Configuration[databasePath]))
        {
            string path = Path.Combine(dataDirectory, DatabaseFileName);
            derived[databasePath] = $"Data Source={path};Version=3;Foreign Keys=True;Journal Mode=WAL;";
        }

        builder.Configuration.AddInMemoryCollection(derived);
        ConfigureKestrel(builder);
        return new BackendRuntime(dataDirectory, settingsPath);
    }

    private static string ResolveDataDirectory(WebApplicationBuilder builder)
    {
        string? configured = Environment.GetEnvironmentVariable("TURBO_DECK_DATA_DIR") ??
            builder.Configuration[$"{ApiConstants.Section}:DataDirectory"];
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        if (builder.Environment.IsDevelopment())
            return Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

        string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localData, "TurboDeck", "Backend");
    }

    private static void ConfigureKestrel(WebApplicationBuilder builder)
    {
        NetworkOptions network = builder.Configuration
            .GetRequiredSection($"{ApiConstants.Section}:Network")
            .Get<NetworkOptions>() ?? new NetworkOptions();
        long maximumFileSize = builder.Configuration.GetValue(
            $"{ApiConstants.Section}:Files:MaximumFileSizeBytes",
            200L * 1024 * 1024);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = maximumFileSize + 1024 * 1024;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);

            string[] addresses = ResolveListenAddresses(network);

            foreach (string address in addresses.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                IPAddress ipAddress = ParseAddress(address);
                options.Listen(ipAddress, network.Port, listen => ConfigureHttps(listen, network.Https));
            }

            if (network.Https.Enabled)
                options.Listen(IPAddress.Loopback, 0);
        });
    }

    private static string[] ResolveListenAddresses(NetworkOptions network)
    {
        if (network.Mode == NetworkMode.Loopback)
            return ["127.0.0.1"];

        string[] normalized = NetworkAddressCatalog.NormalizeDistinct(network.Addresses);
        string[] configured = normalized.Length == 0 ? ["0.0.0.0"] : normalized;
        bool includesLocalControl = configured.Any(address => address.Trim().ToLowerInvariant() is
            "localhost" or "127.0.0.1" or "::1" or "*" or "+" or "0.0.0.0" or "::");
        return includesLocalControl ? configured : [.. configured, "127.0.0.1"];
    }

    private static IPAddress ParseAddress(string address)
    {
        if (NetworkAddressCatalog.TryParse(address, out IPAddress parsed))
            return parsed;

        throw new InvalidOperationException($"Network address '{address}' is not a valid IP address.");
    }

    private static void ConfigureHttps(ListenOptions listen, HttpsOptions https)
    {
        if (!https.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(https.CertificatePath))
            throw new InvalidOperationException("LAN HTTPS requires a certificate path.");

        listen.UseHttps(Path.GetFullPath(https.CertificatePath), https.CertificatePassword);
    }
}
