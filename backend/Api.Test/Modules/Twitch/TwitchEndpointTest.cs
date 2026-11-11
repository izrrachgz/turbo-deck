using System.Net;
using System.Net.Http.Json;
using Api.Test.Infrastructure;
using Application.Modules.Deck;
using Application.Modules.Twitch;

namespace Api.Test.Modules.Twitch;

public sealed class TwitchEndpointTest
{
    [Fact]
    public async Task Reward_binding_round_trips_without_a_Twitch_connection()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        ProfileModel profile = (await PostAsync<ProfileModel>(client, "/api/v1/profiles", new { name = "Main" }))!;
        IReadOnlyList<DeckPageModel>? pages = await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{profile.Id}/pages",
            TestContext.Current.CancellationToken);
        DeckPageModel page = pages![0];
        HttpResponseMessage cellResponse = await client.PutAsJsonAsync(
            $"/api/v1/pages/{page.Id}/cells/0/0",
            new { label = "Reward", enabled = true },
            TestContext.Current.CancellationToken);
        CellConfigModel cell = (await cellResponse.Content.ReadFromJsonAsync<CellConfigModel>(TestContext.Current.CancellationToken))!;

        HttpResponseMessage saveResponse = await client.PutAsJsonAsync(
            "/api/v1/twitch/bindings/reward-123",
            new { rewardTitle = "Hydrate", cellConfigId = cell.Id, enabled = true },
            TestContext.Current.CancellationToken);
        IReadOnlyList<TwitchRewardBindingModel>? bindings = await client.GetFromJsonAsync<IReadOnlyList<TwitchRewardBindingModel>>(
            "/api/v1/twitch/bindings",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        TwitchRewardBindingModel binding = Assert.Single(bindings!);
        Assert.Equal("reward-123", binding.RewardId);
        Assert.Equal(cell.Id, binding.CellConfigId);
    }

    [Fact]
    public async Task Twitch_status_is_optional_by_default()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();

        TwitchConnectionStatusModel? status = await client.GetFromJsonAsync<TwitchConnectionStatusModel>(
            "/api/v1/twitch/status",
            TestContext.Current.CancellationToken);

        Assert.NotNull(status);
        Assert.False(status.Authorized);
        Assert.False(status.Connected);
    }

    private static async Task<T?> PostAsync<T>(HttpClient client, string uri, object body)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(uri, body, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken);
    }
}
