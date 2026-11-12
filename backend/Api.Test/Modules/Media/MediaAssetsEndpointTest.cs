using System.Net;
using SkiaSharp;
using System.Net.Http.Json;
using Api.Test.Infrastructure;
using Application.Modules.Files;
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

    [Fact]
    public async Task Thumbnail_upload_persists_only_a_square_optimized_png()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        byte[] original = CreatePng(800, 400);
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(original)
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") }
        }, "File", "scene.png");
        form.Add(new StringContent("Scene"), "DisplayName");

        HttpResponseMessage upload = await client.PostAsync(
            "/api/v1/media-assets/thumbnails",
            form,
            TestContext.Current.CancellationToken);
        MediaAssetModel? created = await upload.Content.ReadFromJsonAsync<MediaAssetModel>(TestContext.Current.CancellationToken);
        HttpResponseMessage content = await client.GetAsync(
            $"/api/v1/media-assets/{created?.Id}/content",
            TestContext.Current.CancellationToken);
        byte[] downloaded = await content.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        StoredFileModel[]? files = await client.GetFromJsonAsync<StoredFileModel[]>(
            "/files?limit=100",
            TestContext.Current.CancellationToken);
        using SKBitmap decoded = SKBitmap.Decode(downloaded);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Scene", created.DisplayName);
        Assert.Equal("image", created.Kind);
        Assert.Equal(CellThumbnailSpecifications.OutputSize, created.Width);
        Assert.Equal(CellThumbnailSpecifications.OutputSize, created.Height);
        Assert.Equal("image/png", created.File.ContentType);
        Assert.Equal(".png", created.File.Extension);
        Assert.Equal("scene-thumbnail.png", created.File.OriginalFileName);
        Assert.Equal(CellThumbnailSpecifications.OutputSize, decoded.Width);
        Assert.Equal(CellThumbnailSpecifications.OutputSize, decoded.Height);
        StoredFileModel stored = Assert.Single(files!);
        Assert.Equal(created.File.Id, stored.Id);
    }

    [Fact]
    public async Task Invalid_thumbnail_does_not_persist_the_source_upload()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        byte[] incompletePng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(incompletePng)
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") }
        }, "File", "broken.png");

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/media-assets/thumbnails",
            form,
            TestContext.Current.CancellationToken);
        StoredFileModel[]? files = await client.GetFromJsonAsync<StoredFileModel[]>(
            "/files?limit=100",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(files!);
    }

    private static byte[] CreatePng(int width, int height)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.CornflowerBlue);
            canvas.Flush();
        }
        using SKPixmap pixels = bitmap.PeekPixels();
        using SKData png = pixels.Encode(new SKPngEncoderOptions(SKPngEncoderFilterFlags.AllFilters, 6))!;
        return png.ToArray();
    }
}
