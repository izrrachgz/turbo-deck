using Api.Modules.Twitch;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Test.Modules.Twitch;

public sealed class TwitchConnectionServiceTest
{
    [Fact]
    public async Task Disconnect_purges_local_tokens_even_when_the_request_is_already_cancelled()
    {
        var tokenStore = new RecordingTokenStore();
        using var service = new TwitchConnectionService(
            null!,
            tokenStore,
            null!,
            TimeProvider.System,
            NullLogger<TwitchConnectionService>.Instance);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await service.DisconnectAsync(cancelled.Token);

        Assert.True(tokenStore.Purged);
        Assert.False(tokenStore.PurgeTokenCanBeCancelled);
        Assert.False(service.GetStatus().Authorized);
    }

    private sealed class RecordingTokenStore : ITwitchTokenStore
    {
        public bool Purged { get; private set; }

        public bool PurgeTokenCanBeCancelled { get; private set; }

        public Task<TwitchSession?> ReadAsync(CancellationToken cancellationToken) => Task.FromResult<TwitchSession?>(null);

        public Task WriteAsync(TwitchSession session, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task PurgeAsync(CancellationToken cancellationToken)
        {
            Purged = true;
            PurgeTokenCanBeCancelled = cancellationToken.CanBeCanceled;
            return Task.CompletedTask;
        }
    }
}
