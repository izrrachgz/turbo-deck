using Application.Migrations.Models;
using Application.Migrations.Services;
using Common.Enumerations;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.Repository;
using SqlKata.Execution;

namespace Application.Migrations.Test.Services;

public sealed class ApplicationMigrationRunnerTest
{
    [Fact]
    public async Task SQLite_baseline_is_idempotent_and_creates_only_the_base_schema()
    {
        using var repository = new WritableRepository(
            "Data Source=:memory:;Version=3;New=True;",
            DatabaseEngine.SqLite,
            NullLogger<WritableRepository>.Instance);
        var runner = new ApplicationMigrationRunner(
            repository,
            new EmbeddedMigrationScriptSource(),
            TimeProvider.System,
            NullLogger<ApplicationMigrationRunner>.Instance);

        await runner.RunAsync(TestContext.Current.CancellationToken);
        await runner.RunAsync(TestContext.Current.CancellationToken);

        var records = await repository.Db.Query(nameof(MigrationRecord))
            .GetAsync<MigrationRecord>(cancellationToken: TestContext.Current.CancellationToken);
        string[] tables = (await repository.Db.SelectAsync<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';",
            cancellationToken: TestContext.Current.CancellationToken)).Order(StringComparer.Ordinal).ToArray();

        Assert.Single(records);
        Assert.Equal(["MigrationRecord", "StoredFile"], tables);
    }
}
