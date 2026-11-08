using Application.Migrations.Services;
using Common.Enumerations;

namespace Application.Migrations.Test.Services;

public sealed class EmbeddedMigrationScriptSourceTest
{
    public static TheoryData<DatabaseEngine> SupportedEngines => new()
    {
        DatabaseEngine.MsSql,
        DatabaseEngine.PgSql,
        DatabaseEngine.MySql,
        DatabaseEngine.SqLite
    };

    [Theory]
    [MemberData(nameof(SupportedEngines))]
    public async Task Every_engine_has_a_clean_version_one_baseline(DatabaseEngine engine)
    {
        var source = new EmbeddedMigrationScriptSource();

        var scripts = await source.GetScriptsAsync(engine, TestContext.Current.CancellationToken);

        var script = Assert.Single(scripts);
        Assert.Equal(1, script.MigrationId);
        Assert.Equal("Core", script.Module);
        Assert.Equal("migration-1.sql", script.Name);
        Assert.Contains("MigrationRecord", script.Content, StringComparison.Ordinal);
        Assert.Contains("StoredFile", script.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("Account", script.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Chat", script.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Notification", script.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(64, script.Checksum.Length);
    }
}
