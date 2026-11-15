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

        Assert.Equal(12, records.Count());
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

    [Fact]
    public async Task Migration_10_preserves_cells_and_reward_bindings_and_accepts_advanced_animations()
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
            new StaticMigrationScriptSource(scripts.Take(9).ToArray()),
            TimeProvider.System,
            NullLogger<ApplicationMigrationRunner>.Instance);
        await initialRunner.RunAsync(TestContext.Current.CancellationToken);

        DateTime now = DateTime.UtcNow;
        long profileId = await repository.Db.Query("Profile").InsertGetIdAsync<long>(new
        {
            Name = "Animation test",
            SortOrder = 1,
            SelectedPageNumber = 1,
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: TestContext.Current.CancellationToken);
        long pageId = await repository.Db.Query("DeckPage").InsertGetIdAsync<long>(new
        {
            ProfileId = profileId,
            Name = "1",
            Position = 1,
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: TestContext.Current.CancellationToken);
        long cellId = await repository.Db.Query("CellConfig").InsertGetIdAsync<long>(new
        {
            PageId = pageId,
            RowIndex = 0,
            ColumnIndex = 0,
            Label = "Advanced animation",
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: TestContext.Current.CancellationToken);
        await repository.Db.Query("TwitchRewardBinding").InsertAsync(new
        {
            RewardId = "reward-1",
            RewardTitle = "Animate",
            CellConfigId = cellId,
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
        await repository.Db.Query("CellConfig").Where("Id", cellId).UpdateAsync(new
        {
            AnimationInType = "orbit",
            AnimationOutType = "morph"
        }, cancellationToken: TestContext.Current.CancellationToken);

        long preservedCellId = await repository.Db.Query("TwitchRewardBinding")
            .Where("RewardId", "reward-1")
            .Select("CellConfigId")
            .FirstAsync<long>(cancellationToken: TestContext.Current.CancellationToken);
        AdvancedAnimationTypes animations = await repository.Db.Query("CellConfig")
            .Where("Id", cellId)
            .FirstAsync<AdvancedAnimationTypes>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(cellId, preservedCellId);
        Assert.Equal("orbit", animations.AnimationInType);
        Assert.Equal("morph", animations.AnimationOutType);
    }

    [Fact]
    public async Task Migration_12_removes_fourth_row_cells_and_preserves_the_first_three_rows()
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
            new StaticMigrationScriptSource(scripts.Take(11).ToArray()),
            TimeProvider.System,
            NullLogger<ApplicationMigrationRunner>.Instance);
        await initialRunner.RunAsync(TestContext.Current.CancellationToken);

        DateTime now = DateTime.UtcNow;
        long profileId = await repository.Db.Query("Profile").InsertGetIdAsync<long>(new
        {
            Name = "Grid migration",
            SortOrder = 1,
            SelectedPageNumber = 1,
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: TestContext.Current.CancellationToken);
        long pageId = await repository.Db.Query("DeckPage").InsertGetIdAsync<long>(new
        {
            ProfileId = profileId,
            Name = "1",
            Position = 1,
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: TestContext.Current.CancellationToken);
        long retainedCellId = await InsertCellAsync(repository, pageId, 2, now);
        long removedCellId = await InsertCellAsync(repository, pageId, 3, now);
        await InsertBindingAsync(repository, retainedCellId, "retained", now);
        await InsertBindingAsync(repository, removedCellId, "removed", now);

        var finalRunner = new ApplicationMigrationRunner(
            repository,
            embeddedSource,
            TimeProvider.System,
            NullLogger<ApplicationMigrationRunner>.Instance);
        await finalRunner.RunAsync(TestContext.Current.CancellationToken);

        long[] cellIds = (await repository.Db.Query("CellConfig")
            .Select("Id")
            .GetAsync<long>(cancellationToken: TestContext.Current.CancellationToken)).ToArray();
        string[] rewardIds = (await repository.Db.Query("TwitchRewardBinding")
            .Select("RewardId")
            .GetAsync<string>(cancellationToken: TestContext.Current.CancellationToken)).ToArray();
        string tableSql = await repository.Db.Query("sqlite_master")
            .Where("type", "table")
            .Where("name", "CellConfig")
            .Select("sql")
            .FirstAsync<string>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([retainedCellId], cellIds);
        Assert.Equal(["retained"], rewardIds);
        Assert.Contains("RowIndex BETWEEN 0 AND 2", tableSql, StringComparison.Ordinal);
    }

    private static Task<long> InsertCellAsync(WritableRepository repository, long pageId, int rowIndex, DateTime now) =>
        repository.Db.Query("CellConfig").InsertGetIdAsync<long>(new
        {
            PageId = pageId,
            RowIndex = rowIndex,
            ColumnIndex = 0,
            Label = $"Row {rowIndex}",
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: TestContext.Current.CancellationToken);

    private static Task<int> InsertBindingAsync(
        WritableRepository repository,
        long cellId,
        string rewardId,
        DateTime now) => repository.Db.Query("TwitchRewardBinding").InsertAsync(new
        {
            RewardId = rewardId,
            RewardTitle = rewardId,
            CellConfigId = cellId,
            CreatedUtc = now,
            ModifiedUtc = now,
            DeletedUtc = (DateTime?)null
        }, cancellationToken: TestContext.Current.CancellationToken);

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

    private sealed class AdvancedAnimationTypes
    {
        public string AnimationInType { get; init; } = string.Empty;

        public string AnimationOutType { get; init; } = string.Empty;
    }
}
