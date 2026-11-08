using Api.Test.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Api.Test.Modules.Health;

public sealed class HealthEndpointTest
{
    [Fact]
    public async Task Health_requires_a_valid_machine_credential()
    {
        await using var factory = new ApiFactory();
        using HttpClient anonymous = factory.CreateClient();
        using HttpClient authenticated = factory.CreateAuthenticatedClient();

        HttpResponseMessage denied = await anonymous.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        HttpResponseMessage allowed = await authenticated.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        var payload = await allowed.Content.ReadFromJsonAsync<Dictionary<string, string>>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal("ready", payload?["status"]);
    }
}
