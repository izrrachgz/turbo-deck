using Api.Constants;
using Api.Test.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Api.Test.Modules.Realtime;

public sealed class RealtimeHubTest
{
    [Fact]
    public async Task Authenticated_client_registers_with_the_generic_hub()
    {
        await using var factory = new ApiFactory();
        using HttpClient startup = factory.CreateAuthenticatedClient();
        await startup.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/realtime?clientId=device-a"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.Headers.Add(ApiConstants.ControlTokenHeader, ApiFactory.ControlToken);
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();

        await connection.StartAsync(TestContext.Current.CancellationToken);
        string[] clients = await connection.InvokeAsync<string[]>("GetConnectedClients", TestContext.Current.CancellationToken);
        await connection.DisposeAsync();

        Assert.Contains("device-a", clients);
    }
}
