using Api.Configuration;
using Api.Logging;
using Makaretu.Dns;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

namespace Api.Services;

internal sealed class DiscoveryHostedService(
    IHostApplicationLifetime lifetime,
    IServer server,
    IOptions<NetworkOptions> options,
    ILogger<DiscoveryHostedService> logger) : IHostedService, IDisposable
{
    private ServiceDiscovery? _discovery;
    private IDisposable? _startedRegistration;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.Mode == NetworkMode.Lan && options.Value.DiscoveryEnabled)
            _startedRegistration = lifetime.ApplicationStarted.Register(StartAdvertising);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _startedRegistration?.Dispose();
        _discovery?.Dispose();
        _discovery = null;
    }

    private void StartAdvertising()
    {
        try
        {
            string? address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
            if (address is null || !Uri.TryCreate(address, UriKind.Absolute, out Uri? uri))
                return;

            var profile = new ServiceProfile(options.Value.InstanceName, "_turbo-deck._tcp", (ushort)uri.Port);
            profile.AddProperty("id", options.Value.InstanceId);
            profile.AddProperty("api", "1");
            profile.AddProperty("realtime", "1");
            profile.AddProperty("hub", "/hubs/realtime");
            profile.AddProperty("tls", options.Value.Https.Enabled ? "1" : "0");

            _discovery = new ServiceDiscovery();
            _discovery.Advertise(profile);
            logger.DiscoveryStarted(options.Value.InstanceId, uri.Port);
        }
        catch (Exception exception)
        {
            logger.DiscoveryFailed(exception);
        }
    }
}
