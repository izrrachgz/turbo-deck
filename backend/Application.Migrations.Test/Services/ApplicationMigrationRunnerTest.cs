using SqlKata.Execution;
using Common.Enumerations;
using Persistence.Repository;
using Application.Migrations.Models;
using Application.Migrations.Services;
using Application.Migrations.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Migrations.Test.Services;

public sealed class ApplicationMigrationRunnerTest
{
    [Fact]
    public async Task SQLite_migrations_are_idempotent_and_create_the_deck_schema()
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
        string timeZone = await repository.Db.Query("AppSettings")
            .Select("TimeZone")
            .FirstAsync<string>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(9, records.Count());
        Assert.Equal(
            ["AppSettings", "CellConfig", "DeckPage", "MediaAsset", "MigrationRecord", "Profile", "StoredFile", "TwitchRewardBinding"],
            tables);
        Assert.Equal("UTC", timeZone);
    }

    [Fact]
    public async Task Migration_8_backfills_all_page_positions_for_an_existing_profile()
    {
        using var repository = new WritableRepository(
            "Data Source=:memory:;Version=3;New=True;",
            DatabaseEngine.SqLite,
            NullLogger<WritableRepository>.Instance);
        var embeddedSource = new EmbeddedMigrationScriptSource();
        IReadOnlyList<MigrationScript> scripts = await embeddedSource.GetScriptsAsync(
            DatabaseEngine.SqLite,
            TestContext.Current.CancellationToken);
        var initialRunner = new ApplicationMigrationRunner(
            repository,
            new StaticMigrationScriptSource(scripts.Take(7).ToArray()),
            TimeProvider.System,
            NullLogger<ApplicationMigrationRunner>.Instance);
        await initialRunner.RunAsync(TestContext.Current.CancellationToken);

        DateTime now = DateTime.UtcNow;
        long profileId = await repository.Db.Query("Profile").InsertGetIdAsync<long>(new
        {
            Name = "Existing",
            SortOrder = 1,
            SelectedPageNumber = 1,
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: TestContext.Current.CancellationToken);
        await repository.Db.Query("DeckPage").InsertAsync(new
        {
            ProfileId = profileId,
            Name = "1",
            Position = 1,
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: TestContext.Current.CancellationToken);

        var finalRunner = new ApplicationMigrationRunner(
            repository,
            embeddedSource,
            TimeProvider.System,
            NullLogger<ApplicationMigrationRunner>.Instance);
        await finalRunner.RunAsync(TestContext.Current.CancellationToken);
        IEnumerable<PagePosition> pages = await repository.Db.Query("DeckPage")
            .Where("ProfileId", profileId)
            .WhereNull("DeletedUtc")
            .OrderBy("Position")
            .GetAsync<PagePosition>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(Enumerable.Range(1, 8), pages.Select(page => page.Position));
    }

    private sealed class StaticMigrationScriptSource : IMigrationScriptSource
    {
        private readonly IReadOnlyList<MigrationScript> _scripts;

        public StaticMigrationScriptSource(IReadOnlyList<MigrationScript> scripts)
        {
            _scripts = scripts;
        }

        public Task<IReadOnlyList<MigrationScript>> GetScriptsAsync(
            DatabaseEngine engine,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_scripts);
        }
    }

    private sealed class PagePosition
    {
        public int Position { get; init; }
    }
}
