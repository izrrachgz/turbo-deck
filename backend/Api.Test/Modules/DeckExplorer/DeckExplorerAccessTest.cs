using Api.Models;
using System.Net;
using System.Net.Http.Json;
using Api.Test.Infrastructure;
using Application.Modules.Deck;
using Application.Modules.Media;
using Application.Modules.Playback;
using Application.Modules.Realtime;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;

namespace Api.Test.Modules.DeckExplorer;

public sealed class DeckExplorerAccessTest
{
    [Fact]
    public async Task Explorer_pages_cannot_be_framed()
    {
        await using var factory = new ApiFactory();
        using HttpClient browser = factory.CreateClient();

        HttpResponseMessage response = await browser.GetAsync(
            "/deck/not-found",
            TestContext.Current.CancellationToken);

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out IEnumerable<string>? values));
        Assert.Contains("frame-ancestors 'none'", Assert.Single(values), StringComparison.Ordinal);
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
    }

    [Fact]
    public async Task Anonymous_explorer_can_read_the_deck_and_trigger_the_shared_queue_only()
    {
        await using var factory = new ApiFactory();
        using HttpClient control = factory.CreateAuthenticatedClient();
        await control.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        CellConfigModel cell = await CreateCellAsync(control);
        using HttpClient browser = factory.CreateClient();

        HttpResponseMessage profiles = await browser.GetAsync(
            "/api/v1/deck-explorer/profiles",
            TestContext.Current.CancellationToken);
        using var crossOriginRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/deck-explorer/cells/{cell.Id}/trigger");
        crossOriginRequest.Headers.Add("Origin", "https://untrusted.example");
        HttpResponseMessage crossOriginTrigger = await browser.SendAsync(
            crossOriginRequest,
            TestContext.Current.CancellationToken);
        HttpResponseMessage triggered = await browser.PostAsync(
            $"/api/v1/deck-explorer/cells/{cell.Id}/trigger",
            null,
            TestContext.Current.CancellationToken);
        PlaybackQueueSnapshotModel? publicQueue = await triggered.Content.ReadFromJsonAsync<PlaybackQueueSnapshotModel>(
            TestContext.Current.CancellationToken);
        PlaybackQueueSnapshotModel? controlQueue = await control.GetFromJsonAsync<PlaybackQueueSnapshotModel>(
            "/api/v1/playback/queue",
            TestContext.Current.CancellationToken);
        HttpResponseMessage privateProfiles = await browser.GetAsync(
            "/api/v1/profiles",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, profiles.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, crossOriginTrigger.StatusCode);
        Assert.Equal(HttpStatusCode.OK, triggered.StatusCode);
        Assert.Equal(cell.Id, publicQueue?.Current?.CellConfigId);
        Assert.Equal(publicQueue?.Current?.Id, controlQueue?.Current?.Id);
        Assert.Equal(HttpStatusCode.Unauthorized, privateProfiles.StatusCode);
    }

    [Fact]
    public async Task Explorer_clients_receive_shared_queue_events_but_cannot_invoke_interactive_hub_operations()
    {
        await using var factory = new ApiFactory();
        using HttpClient control = factory.CreateAuthenticatedClient();
        await control.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        CellConfigModel cell = await CreateCellAsync(control);
        var received = new TaskCompletionSource<RealtimeMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/deck-explorer?clientId=deck-explorer-test"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();
        connection.On<RealtimeMessage>("Receive", message =>
        {
            if (message.Name == PlaybackRealtimeContract.Requested)
                received.TrySetResult(message);
        });
        await connection.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient browser = factory.CreateClient();

        await browser.PostAsync(
            $"/api/v1/deck-explorer/cells/{cell.Id}/trigger",
            null,
            TestContext.Current.CancellationToken);
        RealtimeMessage message = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Exception? exception = await Record.ExceptionAsync(() =>
            connection.InvokeAsync<string[]>("GetConnectedClients", TestContext.Current.CancellationToken));
        await connection.DisposeAsync();

        Assert.Equal(PlaybackRealtimeContract.Requested, message.Name);
        Assert.NotNull(exception);
        Assert.Contains("only receive playback events", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<CellConfigModel> CreateCellAsync(HttpClient control)
    {
        MediaAssetModel media = await UploadMediaAsync(control);
        HttpResponseMessage profileResponse = await control.PostAsJsonAsync(
            "/api/v1/profiles",
            new { name = "Mobile" },
            TestContext.Current.CancellationToken);
        profileResponse.EnsureSuccessStatusCode();
        ProfileModel profile = (await profileResponse.Content.ReadFromJsonAsync<ProfileModel>(TestContext.Current.CancellationToken))!;
        IReadOnlyList<DeckPageModel>? pages = await control.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{profile.Id}/pages",
            TestContext.Current.CancellationToken);
        HttpResponseMessage cellResponse = await control.PutAsJsonAsync(
            $"/api/v1/pages/{pages![0].Id}/cells/0/0",
            new { label = "Scene", enabled = true, mediaAssetId = media.Id, thumbnailMediaAssetId = media.Id },
            TestContext.Current.CancellationToken);
        cellResponse.EnsureSuccessStatusCode();
        return (await cellResponse.Content.ReadFromJsonAsync<CellConfigModel>(TestContext.Current.CancellationToken))!;
    }

    private static async Task<MediaAssetModel> UploadMediaAsync(HttpClient client)
    {
        byte[] pngHeader =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x20
        ];
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(pngHeader)
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") }
        }, "File", "scene.png");
        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/media-assets",
            form,
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MediaAssetModel>(TestContext.Current.CancellationToken))!;
    }
}
