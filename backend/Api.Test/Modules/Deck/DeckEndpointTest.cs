using System.Net;
using System.Net.Http.Json;
using System.IO.Compression;
using Api.Test.Infrastructure;
using Application.Modules.Deck;
using Application.Modules.Media;
using Application.Modules.Twitch;
using Application.Modules.Playback;
using Application.Modules.Settings;
using Application.Modules.Deck.Transfers;

namespace Api.Test.Modules.Deck;

public sealed class DeckEndpointTest
{
    [Fact]
    public async Task Cell_configuration_rejects_the_removed_fourth_row()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        ProfileModel profile = (await PostAsync<ProfileModel>(client, "/api/v1/profiles", new { name = "Three rows" }))!;
        IReadOnlyList<DeckPageModel> pages = (await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{profile.Id}/pages",
            TestContext.Current.CancellationToken))!;

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/v1/pages/{pages[0].Id}/cells/3/0",
            new { label = "Removed row", enabled = true },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Profile_deletion_requires_its_name_and_preserves_media_shared_with_another_profile()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        MediaAssetModel shared = await UploadImageAsync(client);
        MediaAssetModel exclusive = await UploadImageAsync(client);
        ProfileModel target = (await PostAsync<ProfileModel>(client, "/api/v1/profiles", new { name = "Delete me" }))!;
        ProfileModel retained = (await PostAsync<ProfileModel>(client, "/api/v1/profiles", new { name = "Keep me" }))!;
        IReadOnlyList<DeckPageModel> targetPages = (await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{target.Id}/pages",
            TestContext.Current.CancellationToken))!;
        IReadOnlyList<DeckPageModel> retainedPages = (await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{retained.Id}/pages",
            TestContext.Current.CancellationToken))!;
        CellConfigModel sharedTargetCell = (await PutAsync<CellConfigModel>(
            client,
            $"/api/v1/pages/{targetPages[0].Id}/cells/0/0",
            new { label = "Shared", enabled = true, mediaAssetId = shared.Id }))!;
        CellConfigModel exclusiveTargetCell = (await PutAsync<CellConfigModel>(
            client,
            $"/api/v1/pages/{targetPages[0].Id}/cells/0/1",
            new { label = "Exclusive", enabled = true, mediaAssetId = exclusive.Id }))!;
        CellConfigModel retainedCell = (await PutAsync<CellConfigModel>(
            client,
            $"/api/v1/pages/{retainedPages[0].Id}/cells/0/0",
            new { label = "Shared", enabled = true, mediaAssetId = shared.Id }))!;
        HttpResponseMessage bindingResponse = await client.PutAsJsonAsync(
            "/api/v1/twitch/bindings/reward-delete",
            new { rewardTitle = "Delete reward", cellConfigId = exclusiveTargetCell.Id, enabled = true },
            TestContext.Current.CancellationToken);
        bindingResponse.EnsureSuccessStatusCode();
        await PostAsync<PlaybackQueueSnapshotModel>(client, "/api/v1/playback/queue", new { cellConfigId = sharedTargetCell.Id });
        await PostAsync<PlaybackQueueSnapshotModel>(client, "/api/v1/playback/queue", new { cellConfigId = retainedCell.Id });
        await PostAsync<PlaybackQueueSnapshotModel>(client, "/api/v1/playback/queue", new { cellConfigId = exclusiveTargetCell.Id });

        HttpResponseMessage rejected = await DeleteProfileAsync(client, target.Id, "delete me");
        IReadOnlyList<ProfileModel> beforeDeletion = (await client.GetFromJsonAsync<IReadOnlyList<ProfileModel>>(
            "/api/v1/profiles",
            TestContext.Current.CancellationToken))!;
        HttpResponseMessage deletedResponse = await DeleteProfileAsync(client, target.Id, target.Name);
        DeleteProfileResultModel deleted = (await deletedResponse.Content.ReadFromJsonAsync<DeleteProfileResultModel>(
            TestContext.Current.CancellationToken))!;

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(2, beforeDeletion.Count);
        Assert.Equal(HttpStatusCode.OK, deletedResponse.StatusCode);
        Assert.Equal(2, deleted.CellCount);
        Assert.Equal(1, deleted.MediaCount);
        Assert.Equal(1, deleted.FileCount);
        Assert.Equal(1, deleted.TwitchBindingCount);
        IReadOnlyList<ProfileModel> remaining = (await client.GetFromJsonAsync<IReadOnlyList<ProfileModel>>(
            "/api/v1/profiles",
            TestContext.Current.CancellationToken))!;
        Assert.Equal(retained.Id, Assert.Single(remaining).Id);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(
            $"/api/v1/media-assets/{shared.Id}/content",
            TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(
            $"/api/v1/media-assets/{exclusive.Id}/content",
            TestContext.Current.CancellationToken)).StatusCode);
        Assert.Single(Directory.GetFiles(Path.Combine(factory.DataDirectory, "files")));
        IReadOnlyList<TwitchRewardBindingModel> bindings = (await client.GetFromJsonAsync<IReadOnlyList<TwitchRewardBindingModel>>(
            "/api/v1/twitch/bindings",
            TestContext.Current.CancellationToken))!;
        Assert.Empty(bindings);
        PlaybackQueueSnapshotModel queue = (await client.GetFromJsonAsync<PlaybackQueueSnapshotModel>(
            "/api/v1/playback/queue",
            TestContext.Current.CancellationToken))!;
        Assert.Equal(retainedCell.Id, queue.Current?.CellConfigId);
        Assert.Empty(queue.Pending);
        ProfileModel replacement = (await PostAsync<ProfileModel>(client, "/api/v1/profiles", new { name = "Replacement" }))!;
        Assert.Equal(1, replacement.SortOrder);
        Assert.NotEqual(sharedTargetCell.Id, exclusiveTargetCell.Id);
    }

    [Fact]
    public async Task Profile_import_rejects_a_manifest_without_the_required_shape()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        using var package = new MemoryStream();
        using (var archive = new ZipArchive(package, ZipArchiveMode.Create, leaveOpen: true))
        {
            ZipArchiveEntry entry = archive.CreateEntry("manifest.json");
            await using Stream stream = entry.Open();
            await stream.WriteAsync("{}"u8.ToArray(), TestContext.Current.CancellationToken);
        }
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(package.ToArray()), "File", "invalid.turbo-deck");

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/profiles/import",
            form,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Profile_package_round_trips_grid_configuration_and_media()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        MediaAssetModel media = await UploadImageAsync(client);
        MediaAssetModel duplicateMedia = await UploadImageAsync(client);
        ProfileModel source = (await PostAsync<ProfileModel>(client, "/api/v1/profiles", new { name = "Portable" }))!;
        IReadOnlyList<DeckPageModel> sourcePages = (await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{source.Id}/pages",
            TestContext.Current.CancellationToken))!;
        HttpResponseMessage cellResponse = await client.PutAsJsonAsync(
            $"/api/v1/pages/{sourcePages[0].Id}/cells/1/2",
            new
            {
                label = "Portable scene",
                enabled = true,
                mediaAssetId = media.Id,
                thumbnailMediaAssetId = media.Id,
                displayPermanently = true,
                displaySize = "fullscreen",
                animationInType = "fade",
                animationInDurationMilliseconds = 500,
                animationOutType = "zoom",
                animationOutDurationMilliseconds = 750
            },
            TestContext.Current.CancellationToken);
        cellResponse.EnsureSuccessStatusCode();
        HttpResponseMessage duplicateCellResponse = await client.PutAsJsonAsync(
            $"/api/v1/pages/{sourcePages[0].Id}/cells/2/3",
            new { label = "Duplicate bytes", enabled = true, mediaAssetId = duplicateMedia.Id },
            TestContext.Current.CancellationToken);
        duplicateCellResponse.EnsureSuccessStatusCode();

        byte[] package = await client.GetByteArrayAsync(
            $"/api/v1/profiles/{source.Id}/export",
            TestContext.Current.CancellationToken);
        using (var packageStream = new MemoryStream(package))
        using (var archive = new ZipArchive(packageStream, ZipArchiveMode.Read))
            Assert.Equal(2, archive.Entries.Count);
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(package)
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.turbo-deck.profile+zip") }
        }, "File", "portable.turbo-deck");
        HttpResponseMessage importResponse = await client.PostAsync(
            "/api/v1/profiles/import",
            form,
            TestContext.Current.CancellationToken);
        ProfileImportResultModel? imported = await importResponse.Content.ReadFromJsonAsync<ProfileImportResultModel>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);
        Assert.NotNull(imported);
        Assert.Equal("Portable", imported.Profile.Name);
        Assert.Equal(2, imported.MediaCount);
        Assert.Equal(2, imported.CellCount);
        IReadOnlyList<DeckPageModel> importedPages = (await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{imported.Profile.Id}/pages",
            TestContext.Current.CancellationToken))!;
        DeckGridModel importedGrid = (await client.GetFromJsonAsync<DeckGridModel>(
            $"/api/v1/pages/{importedPages[0].Id}/grid",
            TestContext.Current.CancellationToken))!;
        Assert.Equal(2, importedGrid.Cells.Count);
        CellConfigModel importedCell = importedGrid.Cells.Single(cell => cell.RowIndex == 1 && cell.ColumnIndex == 2);
        Assert.Equal("Portable scene", importedCell.Label);
        Assert.Equal("fullscreen", importedCell.DisplaySize);
        Assert.True(importedCell.DisplayPermanently);
        Assert.NotEqual(media.Id, importedCell.MediaAssetId);
    }

    [Fact]
    public async Task Profile_page_and_cell_round_trip_through_the_API()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        MediaAssetModel thumbnail = await UploadImageAsync(client);

        HttpResponseMessage profileResponse = await client.PostAsJsonAsync(
            "/api/v1/profiles",
            new { name = "Main" },
            TestContext.Current.CancellationToken);
        ProfileModel? profile = await profileResponse.Content.ReadFromJsonAsync<ProfileModel>(TestContext.Current.CancellationToken);
        Assert.NotNull(profile);
        IReadOnlyList<DeckPageModel>? initialPages = await client.GetFromJsonAsync<IReadOnlyList<DeckPageModel>>(
            $"/api/v1/profiles/{profile.Id}/pages",
            TestContext.Current.CancellationToken);
        Assert.Equal(8, initialPages?.Count);
        DeckPageModel initialPage = initialPages![0];
        DeckPageModel secondPage = initialPages[1];
        HttpResponseMessage cellResponse = await client.PutAsJsonAsync(
            $"/api/v1/pages/{initialPage.Id}/cells/2/6",
            new
            {
                label = "Air horn",
                enabled = true,
                thumbnailMediaAssetId = thumbnail.Id,
                displayPermanently = true,
                displaySize = "large",
                animationInType = "fade",
                animationInDurationMilliseconds = 500,
                animationOutType = "zoom",
                animationOutDurationMilliseconds = 750
            },
            TestContext.Current.CancellationToken);
        CellConfigModel? cell = await cellResponse.Content.ReadFromJsonAsync<CellConfigModel>(TestContext.Current.CancellationToken);
        DeckGridModel? grid = await client.GetFromJsonAsync<DeckGridModel>(
            $"/api/v1/pages/{initialPage.Id}/grid",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, profileResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cellResponse.StatusCode);
        Assert.NotNull(profile);
        Assert.Equal("1", initialPage.Name);
        Assert.Equal(1, initialPage.Position);
        Assert.Equal("2", secondPage.Name);
        Assert.Equal(2, secondPage.Position);
        Assert.NotNull(cell);
        Assert.Equal(2, cell.RowIndex);
        Assert.Equal(6, cell.ColumnIndex);
        Assert.Equal("Air horn", cell.Label);
        Assert.Equal(thumbnail.Id, cell.ThumbnailMediaAssetId);
        Assert.True(cell.DisplayPermanently);
        Assert.Equal("large", cell.DisplaySize);
        Assert.Equal("fade", cell.AnimationInType);
        Assert.Equal(500, cell.AnimationInDurationMilliseconds);
        Assert.Equal("zoom", cell.AnimationOutType);
        Assert.Equal(750, cell.AnimationOutDurationMilliseconds);
        Assert.Single(grid?.Cells ?? []);
    }

    [Fact]
    public async Task Settings_have_local_defaults_and_can_be_updated()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();

        AppSettingsModel? defaults = await client.GetFromJsonAsync<AppSettingsModel>(
            "/api/v1/settings",
            TestContext.Current.CancellationToken);
        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            "/api/v1/settings",
            new
            {
                activeProfileId = (long?)null,
                culture = "es-ES",
                theme = "light",
                palette = "nord",
                timeZone = "America/Hermosillo",
                queueCapacity = 200,
                visualizerSafeMargin = 32
            },
            TestContext.Current.CancellationToken);
        AppSettingsModel? updated = await updateResponse.Content.ReadFromJsonAsync<AppSettingsModel>(TestContext.Current.CancellationToken);

        Assert.Equal("en-US", defaults?.Culture);
        Assert.Equal("midnight", defaults?.Palette);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("es-ES", updated?.Culture);
        Assert.Equal("America/Hermosillo", updated?.TimeZone);
    }

    [Fact]
    public async Task Settings_reject_a_palette_that_does_not_belong_to_the_theme()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/v1/settings",
            new
            {
                activeProfileId = (long?)null,
                culture = "en-US",
                theme = "dark",
                palette = "daylight",
                timeZone = "America/Hermosillo",
                queueCapacity = 100,
                visualizerSafeMargin = 24
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Settings_reject_an_invalid_IANA_time_zone()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/v1/settings",
            new
            {
                activeProfileId = (long?)null,
                culture = "en-US",
                theme = "dark",
                palette = "midnight",
                timeZone = "Not/A_TimeZone",
                queueCapacity = 100,
                visualizerSafeMargin = 24
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<MediaAssetModel> UploadImageAsync(HttpClient client)
    {
        byte[] pngHeader =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x02, 0x80, 0x00, 0x00, 0x01, 0x68
        ];
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(pngHeader)
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") }
        }, "File", "thumbnail.png");
        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/media-assets",
            form,
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MediaAssetModel>(TestContext.Current.CancellationToken))!;
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

    private static Task<HttpResponseMessage> DeleteProfileAsync(HttpClient client, long profileId, string confirmationName)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/profiles/{profileId}")
        {
            Content = JsonContent.Create(new { confirmationName })
        };
        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
