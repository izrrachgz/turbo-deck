using System.Net;
using System.Net.Http.Json;
using Api.Test.Infrastructure;
using Application.Modules.Deck;
using Application.Modules.Media;
using Application.Modules.Settings;

namespace Api.Test.Modules.Deck;

public sealed class DeckEndpointTest
{
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
}
