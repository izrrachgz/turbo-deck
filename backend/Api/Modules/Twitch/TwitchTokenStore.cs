using System.Text.Json;
using Api.Configuration;
using Microsoft.AspNetCore.DataProtection;

namespace Api.Modules.Twitch;

internal sealed record TwitchSession(
    string ClientId,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresUtc,
    string BroadcasterUserId,
    string BroadcasterDisplayName,
    DateTime LastValidatedUtc);

internal interface ITwitchTokenStore
{
    Task<TwitchSession?> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(TwitchSession session, CancellationToken cancellationToken);

    Task PurgeAsync(CancellationToken cancellationToken);
}

internal sealed class TwitchTokenStore(BackendRuntime runtime, IDataProtectionProvider protectionProvider) : ITwitchTokenStore
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("TurboDeck.Twitch.OAuth.v1");
    private readonly string _path = Path.Combine(runtime.DataDirectory, "Twitch", "oauth.dat");

    public async Task<TwitchSession?> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return null;

        string protectedValue = await File.ReadAllTextAsync(_path, cancellationToken);
        string json = _protector.Unprotect(protectedValue);
        return JsonSerializer.Deserialize<TwitchSession>(json);
    }

    public async Task WriteAsync(TwitchSession session, CancellationToken cancellationToken)
    {
        string directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            string protectedValue = _protector.Protect(JsonSerializer.Serialize(session));
            await File.WriteAllTextAsync(temporaryPath, protectedValue, cancellationToken);
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public Task PurgeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(_path))
            File.Delete(_path);
        return Task.CompletedTask;
    }
}
