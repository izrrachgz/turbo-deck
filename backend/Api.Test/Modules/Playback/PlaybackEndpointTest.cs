using System.Net;
using System.Net.Http.Json;
using Api.Test.Infrastructure;
using Application.Modules.Deck;
using Application.Modules.Media;
using Application.Modules.Playback;

namespace Api.Test.Modules.Playback;

public sealed class PlaybackEndpointTest
{
    [Fact]
    public async Task Assigned_cell_can_be_queued_and_completed()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        MediaAssetModel media = await UploadMediaAsync(client);
        ProfileModel profile = (await PostAsync<ProfileModel>(client, "/api/v1/profiles", new { name = "Main" }))!;
        IReadOnlyList<DeckPageModel>? pages = await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{profile.Id}/pages",
            TestContext.Current.CancellationToken);
        DeckPageModel page = pages![0];
        HttpResponseMessage cellResponse = await client.PutAsJsonAsync(
            $"/api/v1/pages/{page.Id}/cells/0/0",
            new
            {
                label = "Scene",
                enabled = true,
                mediaAssetId = media.Id,
                volume = 0.75m,
                playbackRate = 1.25m,
                pitchSemitones = -2m,
                displayPermanently = true,
                displaySize = "fullscreen",
                animationInType = "slide",
                animationInDurationMilliseconds = 400,
                animationOutType = "fade",
                animationOutDurationMilliseconds = 600,
                queuePriority = "override"
            },
            TestContext.Current.CancellationToken);
        CellConfigModel? cell = await cellResponse.Content.ReadFromJsonAsync<CellConfigModel>(TestContext.Current.CancellationToken);

        PlaybackQueueSnapshotModel queued = (await PostAsync<PlaybackQueueSnapshotModel>(
            client,
            "/api/v1/playback/queue",
            new { cellConfigId = cell!.Id }))!;
        PlaybackQueueSnapshotModel completed = (await PostAsync<PlaybackQueueSnapshotModel>(
            client,
            "/api/v1/playback/complete",
            new { itemId = queued.Current!.Id, success = true }))!;

        Assert.Equal(HttpStatusCode.OK, cellResponse.StatusCode);
        Assert.Equal(media.Id, queued.Current?.MediaAssetId);
        Assert.Equal("override", queued.Current?.Priority);
        Assert.Equal(0.75m, queued.Current?.Volume);
        Assert.True(queued.Current?.DisplayPermanently);
        Assert.Equal("fullscreen", queued.Current?.DisplaySize);
        Assert.Equal("slide", queued.Current?.AnimationInType);
        Assert.Equal(400, queued.Current?.AnimationInDurationMilliseconds);
        Assert.Null(completed.Current);
        Assert.Empty(completed.Pending);
    }

    [Fact]
    public async Task Override_cell_replaces_the_current_item_immediately()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        MediaAssetModel media = await UploadMediaAsync(client);
        ProfileModel profile = (await PostAsync<ProfileModel>(client, "/api/v1/profiles", new { name = "Main" }))!;
        IReadOnlyList<DeckPageModel>? pages = await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{profile.Id}/pages",
            TestContext.Current.CancellationToken);
        DeckPageModel page = pages![0];
        CellConfigModel normal = (await PutAsync<CellConfigModel>(
            client,
            $"/api/v1/pages/{page.Id}/cells/0/0",
            new { label = "Normal", enabled = true, mediaAssetId = media.Id }))!;
        CellConfigModel priority = (await PutAsync<CellConfigModel>(
            client,
            $"/api/v1/pages/{page.Id}/cells/0/2",
            new { label = "Priority", enabled = true, mediaAssetId = media.Id, queuePriority = "override" }))!;
        CellConfigModel pending = (await PutAsync<CellConfigModel>(
            client,
            $"/api/v1/pages/{page.Id}/cells/0/1",
            new { label = "Pending", enabled = true, mediaAssetId = media.Id }))!;

        await PostAsync<PlaybackQueueSnapshotModel>(client, "/api/v1/playback/queue", new { cellConfigId = normal.Id });
        PlaybackQueueSnapshotModel queued = (await PostAsync<PlaybackQueueSnapshotModel>(
            client,
            "/api/v1/playback/queue",
            new { cellConfigId = pending.Id }))!;
        PlaybackQueueSnapshotModel overridden = (await PostAsync<PlaybackQueueSnapshotModel>(
            client,
            "/api/v1/playback/queue",
            new { cellConfigId = priority.Id }))!;

        Assert.Equal(normal.Id, queued.Current?.CellConfigId);
        Assert.Equal(pending.Id, Assert.Single(queued.Pending).CellConfigId);
        Assert.Equal(priority.Id, overridden.Current?.CellConfigId);
        Assert.Equal(pending.Id, Assert.Single(overridden.Pending).CellConfigId);
    }

    [Fact]
    public async Task Queue_items_can_be_reordered_and_removed()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        MediaAssetModel media = await UploadMediaAsync(client);
        ProfileModel profile = (await PostAsync<ProfileModel>(client, "/api/v1/profiles", new { name = "Main" }))!;
        IReadOnlyList<DeckPageModel>? pages = await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{profile.Id}/pages",
            TestContext.Current.CancellationToken);
        DeckPageModel page = pages![0];
        var cells = new List<CellConfigModel>();
        for (int columnIndex = 0; columnIndex < 4; columnIndex++)
        {
            cells.Add((await PutAsync<CellConfigModel>(
                client,
                $"/api/v1/pages/{page.Id}/cells/0/{columnIndex}",
                new { label = $"Cell {columnIndex + 1}", enabled = true, mediaAssetId = media.Id }))!);
        }

        PlaybackQueueSnapshotModel snapshot = null!;
        foreach (CellConfigModel cell in cells)
        {
            snapshot = (await PostAsync<PlaybackQueueSnapshotModel>(
                client,
                "/api/v1/playback/queue",
                new { cellConfigId = cell.Id }))!;
        }
        Guid currentId = snapshot.Current!.Id;
        Guid middlePendingId = snapshot.Pending[1].Id;

        snapshot = (await PostAsync<PlaybackQueueSnapshotModel>(
            client,
            $"/api/v1/playback/queue/{middlePendingId}/move",
            new { offset = -1 }))!;
        Assert.Equal([cells[2].Id, cells[1].Id, cells[3].Id], snapshot.Pending.Select(item => item.CellConfigId));

        snapshot = (await PostAsync<PlaybackQueueSnapshotModel>(
            client,
            $"/api/v1/playback/queue/{middlePendingId}/move",
            new { offset = 1 }))!;
        Assert.Equal([cells[1].Id, cells[2].Id, cells[3].Id], snapshot.Pending.Select(item => item.CellConfigId));

        snapshot = (await DeleteAsync<PlaybackQueueSnapshotModel>(
            client,
            $"/api/v1/playback/queue/{middlePendingId}"))!;
        Assert.Equal([cells[1].Id, cells[3].Id], snapshot.Pending.Select(item => item.CellConfigId));

        snapshot = (await DeleteAsync<PlaybackQueueSnapshotModel>(client, $"/api/v1/playback/queue/{currentId}"))!;
        Assert.Equal(cells[1].Id, snapshot.Current?.CellConfigId);
        Assert.Equal(cells[3].Id, Assert.Single(snapshot.Pending).CellConfigId);
    }

    private static async Task<T?> PostAsync<T>(HttpClient client, string uri, object body)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(uri, body, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken);
    }

    private static async Task<T?> PutAsync<T>(HttpClient client, string uri, object body)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(uri, body, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken);
    }

    private static async Task<T?> DeleteAsync<T>(HttpClient client, string uri)
    {
        HttpResponseMessage response = await client.DeleteAsync(uri, TestContext.Current.CancellationToken);
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
