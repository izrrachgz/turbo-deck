using Api.Configuration;
using Api.Modules.Twitch;
using Microsoft.AspNetCore.DataProtection;

namespace Api.Test.Modules.Twitch;

public sealed class TwitchTokenStoreTest
{
    [Fact]
    public async Task OAuth_tokens_are_encrypted_at_rest_and_hard_purged()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"turbo-deck-twitch-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            IDataProtectionProvider provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(directory, "keys")));
            var store = new TwitchTokenStore(new BackendRuntime(directory, Path.Combine(directory, "settings.json")), provider);
            DateTime now = DateTime.UtcNow;
            var expected = new TwitchSession("client", "access-secret", "refresh-secret", now.AddHours(1), "123", "caster", now);

            await store.WriteAsync(expected, TestContext.Current.CancellationToken);
            string persisted = await File.ReadAllTextAsync(
                Path.Combine(directory, "Twitch", "oauth.dat"),
                TestContext.Current.CancellationToken);
            TwitchSession? restored = await store.ReadAsync(TestContext.Current.CancellationToken);

            Assert.DoesNotContain("access-secret", persisted, StringComparison.Ordinal);
            Assert.DoesNotContain("refresh-secret", persisted, StringComparison.Ordinal);
            Assert.Equal(expected, restored);

            await store.PurgeAsync(TestContext.Current.CancellationToken);
            Assert.Null(await store.ReadAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
