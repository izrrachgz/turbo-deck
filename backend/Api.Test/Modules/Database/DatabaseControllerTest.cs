using Api.Models;
using System.Net;
using System.Text.Json;
using System.Data.SQLite;
using System.Net.Http.Json;
using Api.Test.Infrastructure;

namespace Api.Test.Modules.Database;

public sealed class DatabaseControllerTest
{
    [Fact]
    public async Task Settings_expose_status_without_exposing_connection_credentials()
    {
        using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();

        DatabaseSettingsResponse? response = await client.GetFromJsonAsync<DatabaseSettingsResponse>(
            "/system/database",
            TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal("sqlite", response.ActiveEngine);
        Assert.Equal(4, response.Engines.Count);
        DatabaseEngineStateResponse sqlite = Assert.Single(response.Engines, engine => engine.Engine == "sqlite");
        Assert.True(sqlite.Configured);
        Assert.Equal("Connected", sqlite.Status);
        Assert.DoesNotContain("Data Source", sqlite.ConnectionSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Switch_to_fresh_sqlite_database_migrates_and_encrypts_the_saved_connection()
    {
        using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        string databasePath = Path.Combine(factory.DataDirectory, "alternate.db");
        string connectionString = $"Data Source={databasePath};Version=3;Foreign Keys=True;";

        HttpResponseMessage testResponse = await client.PostAsJsonAsync(
            "/system/database/test",
            new DatabaseConnectionRequest("sqlite", connectionString),
            TestContext.Current.CancellationToken);
        DatabaseConnectionTestResponse? test = await testResponse.Content.ReadFromJsonAsync<DatabaseConnectionTestResponse>(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, testResponse.StatusCode);
        Assert.True(test?.Success);

        HttpResponseMessage switchResponse = await client.PostAsJsonAsync(
            "/system/database/switch",
            new DatabaseConnectionRequest("sqlite", connectionString),
            TestContext.Current.CancellationToken);
        DatabaseSwitchResponse? switched = await switchResponse.Content.ReadFromJsonAsync<DatabaseSwitchResponse>(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, switchResponse.StatusCode);
        Assert.Equal("sqlite", switched?.ActiveEngine);
        Assert.True(File.Exists(databasePath));
        HttpResponseMessage profilesResponse = await client.GetAsync(
            "/api/v1/profiles",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, profilesResponse.StatusCode);

        string settingsJson = await File.ReadAllTextAsync(
            Path.Combine(factory.DataDirectory, "backendsettings.json"),
            TestContext.Current.CancellationToken);
        Assert.DoesNotContain(databasePath, settingsJson, StringComparison.OrdinalIgnoreCase);
        using JsonDocument document = JsonDocument.Parse(settingsJson);
        string protectedValue = document.RootElement
            .GetProperty("api")
            .GetProperty("database")
            .GetProperty("protectedConnectionStrings")
            .GetProperty("SqLite")
            .GetString()!;
        Assert.NotEmpty(protectedValue);
    }

    [Fact]
    public async Task Switch_rejects_a_nonempty_database_without_the_application_ledger()
    {
        using var factory = new ApiFactory();
        using HttpClient client = factory.CreateAuthenticatedClient();
        string databasePath = Path.Combine(factory.DataDirectory, "foreign.db");
        string connectionString = $"Data Source={databasePath};Version=3;Foreign Keys=True;";
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using SQLiteCommand command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE ForeignData (Id INTEGER NOT NULL PRIMARY KEY);";
            command.ExecuteNonQuery();
        }

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/system/database/switch",
            new DatabaseConnectionRequest("sqlite", connectionString),
            TestContext.Current.CancellationToken);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("migration ledger", body, StringComparison.OrdinalIgnoreCase);
    }
}
