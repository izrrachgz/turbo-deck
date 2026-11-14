using Api.Configuration;
using Api.Services;
using Makaretu.Dns;
using System.Net;

namespace Api.Test.Modules.SystemApi;

public sealed class DiscoveryHostedServiceTest
{
    [Fact]
    public void Discovery_profile_advertises_the_stable_friendly_hostname()
    {
        var network = new NetworkOptions
        {
            InstanceId = "test-instance",
            InstanceName = "Turbo Deck Backend",
            Https = new HttpsOptions { Enabled = true }
        };

        ServiceProfile profile = DiscoveryHostedService.CreateProfile(
            network,
            52024,
            [IPAddress.Parse("192.168.1.20")]);

        Assert.Equal("turbo-deck.local", profile.HostName.ToString());
        SRVRecord service = Assert.Single(profile.Resources.OfType<SRVRecord>());
        Assert.Equal(profile.HostName, service.Target);
        Makaretu.Dns.AddressRecord address = Assert.Single(profile.Resources.OfType<Makaretu.Dns.AddressRecord>());
        Assert.Equal(profile.HostName, address.Name);
        Assert.Equal(IPAddress.Parse("192.168.1.20"), address.Address);
    }
}
