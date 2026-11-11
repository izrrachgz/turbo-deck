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

namespace Api.Test.Modules.Visualizer;

public sealed class VisualizerAccessTest
{
    [Fact]
    public async Task Visualizer_policy_allows_blob_workers_without_allowing_inline_scripts()
    {
        await using var factory = new ApiFactory();
        using HttpClient browser = factory.CreateClient();

        HttpResponseMessage response = await browser.GetAsync(
            "/visualizer/not-found",
            TestContext.Current.CancellationToken);

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out IEnumerable<string>? values));
        string policy = Assert.Single(values);
        Assert.Contains("script-src 'self';", policy, StringComparison.Ordinal);
        Assert.Contains("worker-src 'self' blob:;", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", policy, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Anonymous_client_can_read_visualizer_state_but_not_general_API_routes()
    {
        await using var factory = new ApiFactory();
        using HttpClient browser = factory.CreateClient();

        HttpResponseMessage state = await browser.GetAsync(
            "/api/v1/visualizer/state",
            TestContext.Current.CancellationToken);
        HttpResponseMessage profiles = await browser.GetAsync(
            "/api/v1/profiles",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, state.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profiles.StatusCode);
    }

    [Fact]
    public async Task Visualizer_hub_client_cannot_invoke_interactive_hub_operations()
    {
        await using var factory = new ApiFactory();
        using HttpClient control = factory.CreateAuthenticatedClient();
        await control.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        const string path = "/hubs/visualizer?clientId=visualizer-test";
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, path), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();

        await connection.StartAsync(TestContext.Current.CancellationToken);
        Exception? exception = await Record.ExceptionAsync(() =>
            connection.InvokeAsync<string[]>("GetConnectedClients", TestContext.Current.CancellationToken));
        await connection.DisposeAsync();

        Assert.NotNull(exception);
        Assert.Contains("only receive playback events", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Visualizer_receives_playback_events_and_can_complete_the_current_item()
    {
        await using var factory = new ApiFactory();
        using HttpClient control = factory.CreateAuthenticatedClient();
        var received = new TaskCompletionSource<RealtimeMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        const string hubPath = "/hubs/visualizer?clientId=visualizer-playback";
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, hubPath), options =>
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

        MediaAssetModel media = await UploadMediaAsync(control);
        ProfileModel profile = (await PostAsync<ProfileModel>(control, "/api/v1/profiles", new { name = "Main" }))!;
        IReadOnlyList<DeckPageModel>? pages = await control.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{profile.Id}/pages",
            TestContext.Current.CancellationToken);
        DeckPageModel page = pages![0];
        HttpResponseMessage cellResponse = await control.PutAsJsonAsync(
            $"/api/v1/pages/{page.Id}/cells/0/0",
            new { label = "Scene", enabled = true, mediaAssetId = media.Id },
            TestContext.Current.CancellationToken);
        CellConfigModel cell = (await cellResponse.Content.ReadFromJsonAsync<CellConfigModel>(
            TestContext.Current.CancellationToken))!;
        PlaybackQueueSnapshotModel queued = (await PostAsync<PlaybackQueueSnapshotModel>(
            control,
            "/api/v1/playback/queue",
            new { cellConfigId = cell.Id }))!;

        RealtimeMessage message = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        using HttpClient browser = factory.CreateClient();
        HttpResponseMessage completed = await browser.PostAsJsonAsync(
            "/api/v1/visualizer/complete",
            new { itemId = queued.Current!.Id, success = true },
            TestContext.Current.CancellationToken);
        PlaybackQueueSnapshotModel? final = await completed.Content.ReadFromJsonAsync<PlaybackQueueSnapshotModel>(
            TestContext.Current.CancellationToken);
        await connection.DisposeAsync();

        Assert.Equal(PlaybackRealtimeContract.Requested, message.Name);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Null(final?.Current);
    }

    private static async Task<T?> PostAsync<T>(HttpClient client, string uri, object body)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(uri, body, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken);
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
