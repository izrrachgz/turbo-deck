using System.Text.Json;
using Application.Modules.Realtime;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Test.Modules.Realtime;

public sealed class RealtimeRequestRegistryTest
{
    [Fact]
    public async Task Dispatches_only_explicitly_registered_requests()
    {
        var registry = new RealtimeRequestRegistry([new EchoHandler()], NullLogger<RealtimeRequestRegistry>.Instance);
        JsonElement payload = JsonSerializer.SerializeToElement(new { value = "hello" });

        JsonElement? response = await registry.DispatchAsync(
            RealtimeMessageKind.Query,
            "echo.get",
            payload,
            TestContext.Current.CancellationToken);

        Assert.Equal("hello", response?.GetProperty("value").GetString());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => registry.DispatchAsync(
            RealtimeMessageKind.Command,
            "unknown.run",
            payload,
            TestContext.Current.CancellationToken));
    }

    private sealed class EchoHandler : IRealtimeRequestHandler
    {
        public string Name => "echo.get";

        public RealtimeMessageKind Kind => RealtimeMessageKind.Query;

        public Task<JsonElement?> HandleAsync(JsonElement payload, CancellationToken cancellationToken) =>
            Task.FromResult<JsonElement?>(payload.Clone());
    }
}
