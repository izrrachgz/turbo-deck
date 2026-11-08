using Api.Configuration;
using Api.Extensions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

BackendRuntime runtime = BackendBootstrap.Configure(builder);
builder.ConfigureServices(runtime);

var app = builder.Build();
app.ConfigurePipeline();

await app.StartAsync();
IReadOnlyCollection<string> addresses = app.Services.GetRequiredService<IServer>()
    .Features.Get<IServerAddressesFeature>()?.Addresses.ToArray() ?? [];
string? url = addresses.FirstOrDefault(address => address.Contains("127.0.0.1", StringComparison.Ordinal)) ?? addresses.FirstOrDefault();
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
