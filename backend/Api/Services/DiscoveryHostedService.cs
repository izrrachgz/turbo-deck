using Api.Configuration;
using Api.Logging;
using Makaretu.Dns;
using System.Net;
using System.Net.Sockets;
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

            IPAddress[] addresses = NetworkAddressCatalog.GetSuggestions()
                .Select(value => NetworkAddressCatalog.TryParse(value, out IPAddress address) ? address : IPAddress.None)
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address) &&
                    !NetworkAddressCatalog.IsWildcard(address))
                .Distinct()
                .ToArray();
            ServiceProfile profile = CreateProfile(options.Value, (ushort)uri.Port, addresses);

            _discovery = new ServiceDiscovery();
            _discovery.Advertise(profile);
            logger.DiscoveryStarted(options.Value.InstanceId, uri.Port);
        }
        catch (Exception exception)
        {
            logger.DiscoveryFailed(exception);
        }
    }

    internal static ServiceProfile CreateProfile(NetworkOptions network, ushort port, IEnumerable<IPAddress> addresses)
    {
        var profile = new ServiceProfile(network.InstanceName, "_turbo-deck._tcp", port, addresses);
        DomainName originalHostName = profile.HostName;
        profile.HostName = CertificateIdentityCatalog.FriendlyHostName;
        foreach (ResourceRecord resource in profile.Resources)
        {
            if (resource is SRVRecord service)
                service.Target = profile.HostName;
            if (resource.Name.Equals(originalHostName))
                resource.Name = profile.HostName;
        }
        profile.AddProperty("id", network.InstanceId);
        profile.AddProperty("api", "1");
        profile.AddProperty("realtime", "1");
        profile.AddProperty("hub", "/hubs/realtime");
        profile.AddProperty("tls", network.Https.Enabled ? "1" : "0");
        profile.AddProperty("host", CertificateIdentityCatalog.FriendlyHostName);
        return profile;
    }
}
