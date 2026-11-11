using Api.Models;
using System.Net;
using System.Net.Http.Json;
using Api.Test.Infrastructure;

namespace Api.Test.Modules.SystemApi;

public sealed class SystemNetworkEndpointTest
{
    [Fact]
    public async Task Network_settings_include_safe_local_address_suggestions()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();

        NetworkSettingsResponse? settings = await client.GetFromJsonAsync<NetworkSettingsResponse>(
            "/system/network",
            TestContext.Current.CancellationToken);

        Assert.NotNull(settings);
        Assert.Contains("localhost", settings.SuggestedAddresses);
        Assert.Contains("127.0.0.1", settings.SuggestedAddresses);
        Assert.Contains("0.0.0.0", settings.SuggestedAddresses);
    }

    [Fact]
    public async Task Network_settings_repair_equivalent_legacy_listener_entries()
    {
        await using var factory = new ApiFactory(["127.0.0.1", "localhost", "127.0.0.1"]);
        using HttpClient client = factory.CreateAuthenticatedClient();

        NetworkSettingsResponse? settings = await client.GetFromJsonAsync<NetworkSettingsResponse>(
            "/system/network",
            TestContext.Current.CancellationToken);

        Assert.NotNull(settings);
        Assert.Equal(["127.0.0.1"], settings.Addresses);
    }

    [Fact]
    public async Task Network_settings_reject_invalid_duplicate_and_overlapping_listeners()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        string[][] invalidAddresses =
        [
            ["deck.local"],
            ["localhost", "127.0.0.1"],
            ["0.0.0.0", "192.168.1.20"]
        ];

        foreach (string[] addresses in invalidAddresses)
        {
            HttpResponseMessage response = await client.PutAsJsonAsync(
                "/system/network",
                CreateRequest(addresses),
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    private static object CreateRequest(string[] addresses) => new
    {
        mode = 0,
        addresses,
        port = 52024,
        discoveryEnabled = false,
        instanceName = "Turbo Deck Backend",
        corsAllowedOrigins = Array.Empty<string>(),
        allowInsecureHttp = false,
        https = new { enabled = false, certificatePath = string.Empty, certificatePassword = string.Empty }
    };
}
