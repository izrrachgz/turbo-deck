using System.Net;
using System.Net.Http.Json;
using Api.Test.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Application.Modules.Files;

namespace Api.Test.Modules.Files;

public sealed class FilesEndpointTest
{
    [Fact]
    public async Task Upload_validation_uses_the_requested_culture()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es-ES");
        using var form = new MultipartFormDataContent
        {
            { new StringContent("test"), "Purpose" }
        };

        HttpResponseMessage response = await client.PostAsync("/files", form, TestContext.Current.CancellationToken);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Se requiere un archivo que no esté vacío.", problem?.Title);
    }

    [Fact]
    public async Task File_lifecycle_round_trips_through_the_API()
    {
        await using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent("hello from the file module"u8.ToArray())
        {
            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain") }
        }, "File", "note.txt");
        form.Add(new StringContent("test"), "Purpose");

        HttpResponseMessage upload = await client.PostAsync("/files", form, TestContext.Current.CancellationToken);
        StoredFileModel? created = await upload.Content.ReadFromJsonAsync<StoredFileModel>(TestContext.Current.CancellationToken);
        HttpResponseMessage download = await client.GetAsync($"/files/{created?.Id}/content", TestContext.Current.CancellationToken);
        string content = await download.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        HttpResponseMessage delete = await client.DeleteAsync($"/files/{created?.Id}", TestContext.Current.CancellationToken);
        HttpResponseMessage missing = await client.GetAsync($"/files/{created?.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("note.txt", created.OriginalFileName);
        Assert.Equal("test", created.Purpose);
        Assert.Equal("hello from the file module", content);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
