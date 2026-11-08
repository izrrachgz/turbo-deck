using Api.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Test.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ControlToken = "test-control-token";
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), $"command-q-api-test-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_dataDirectory);
        string databasePath = Path.Combine(_dataDirectory, "test.db");
        builder.UseEnvironment("Testing");
        builder.UseSetting("Api:DataDirectory", _dataDirectory);
        builder.UseSetting("Api:Files:DataDirectory", _dataDirectory);
        builder.UseSetting("Api:Files:RootPath", "files");
        builder.UseSetting("Api:Security:ControlToken", ControlToken);
        builder.UseSetting("Api:ConnectionStrings:Engine", "SqLite");
        builder.UseSetting("Api:ConnectionStrings:Db", $"Data Source={databasePath};Version=3;Foreign Keys=True;");
        builder.UseSetting("Api:Network:Mode", "Loopback");
        builder.UseSetting("Api:Network:Port", "0");
    }

    public HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add(ApiConstants.ControlTokenHeader, ControlToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || !Directory.Exists(_dataDirectory))
            return;

        try
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Test runner file handles can close after the factory is disposed.
        }
        catch (UnauthorizedAccessException)
        {
            // The operating system will clean its temporary directory.
        }
    }
}
