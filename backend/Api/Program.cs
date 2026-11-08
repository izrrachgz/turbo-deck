using Api.Logging;
using Api.Extensions;
using System.Text.Json;
using Api.Configuration;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);

BackendRuntime runtime = BackendBootstrap.Configure(builder);
builder.ConfigureLogging(runtime);
builder.ConfigureServices(runtime);

var app = builder.Build();
app.ConfigurePipeline();

await app.StartAsync();
IReadOnlyCollection<string> addresses = app.Services.GetRequiredService<IServer>()
    .Features.Get<IServerAddressesFeature>()?.Addresses.ToArray() ?? [];
string? url = addresses.FirstOrDefault(address => address.Contains("127.0.0.1", StringComparison.Ordinal)) ?? addresses.FirstOrDefault();
app.Logger.BackendReady(Environment.ProcessId, url ?? string.Empty, addresses.Count);
Console.Out.WriteLine(JsonSerializer.Serialize(new
{
    @event = "ready",
    url,
    addresses,
    pid = Environment.ProcessId
}));
Console.Out.Flush();
await app.WaitForShutdownAsync();
return Environment.ExitCode;

public partial class Program;
