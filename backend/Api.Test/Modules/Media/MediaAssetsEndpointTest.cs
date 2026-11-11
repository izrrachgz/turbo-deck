using System.Net;
using System.Net.Http.Json;
using Api.Test.Infrastructure;
using Application.Modules.Media;

namespace Api.Test.Modules.Media;

public sealed class MediaAssetsEndpointTest
{
    [Fact]
    public async Task Png_upload_is_inspected_and_content_round_trips()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
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
        }, "File", "scene.png");
        form.Add(new StringContent("Scene"), "DisplayName");

        HttpResponseMessage upload = await client.PostAsync(
            "/api/v1/media-assets",
            form,
            TestContext.Current.CancellationToken);
        MediaAssetModel? created = await upload.Content.ReadFromJsonAsync<MediaAssetModel>(TestContext.Current.CancellationToken);
        HttpResponseMessage content = await client.GetAsync(
            $"/api/v1/media-assets/{created?.Id}/content",
            TestContext.Current.CancellationToken);
        byte[] downloaded = await content.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Scene", created.DisplayName);
        Assert.Equal("image", created.Kind);
        Assert.Equal("inspected", created.InspectionStatus);
        Assert.Equal(640, created.Width);
        Assert.Equal(360, created.Height);
        Assert.Equal(pngHeader, downloaded);
    }
}
