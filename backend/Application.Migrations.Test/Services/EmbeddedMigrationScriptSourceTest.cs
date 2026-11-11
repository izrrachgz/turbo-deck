using Common.Enumerations;
using Application.Migrations.Services;

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
    public async Task Every_engine_has_a_contiguous_base_and_deck_schema(DatabaseEngine engine)
    {
        var source = new EmbeddedMigrationScriptSource();

        var scripts = await source.GetScriptsAsync(engine, TestContext.Current.CancellationToken);

        Assert.Equal(9, scripts.Count);
        Assert.Equal([1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L], scripts.Select(script => script.MigrationId));
        Assert.Equal(["Core", "Deck", "Media", "Playback", "Twitch", "Deck", "Settings", "Deck", "Deck"],
            scripts.Select(script => script.Module));
        Assert.Contains("MigrationRecord", scripts[0].Content, StringComparison.Ordinal);
        Assert.Contains("StoredFile", scripts[0].Content, StringComparison.Ordinal);
        Assert.Contains("Profile", scripts[1].Content, StringComparison.Ordinal);
        Assert.Contains("DeckPage", scripts[1].Content, StringComparison.Ordinal);
        Assert.Contains("CellConfig", scripts[1].Content, StringComparison.Ordinal);
        Assert.Contains("AppSettings", scripts[1].Content, StringComparison.Ordinal);
        Assert.Contains("MediaAsset", scripts[2].Content, StringComparison.Ordinal);
        Assert.Contains("QueuePriority", scripts[2].Content, StringComparison.Ordinal);
        Assert.Contains("DisplayDurationMilliseconds", scripts[3].Content, StringComparison.Ordinal);
        Assert.Contains("TwitchRewardBinding", scripts[4].Content, StringComparison.Ordinal);
        Assert.Contains("ThumbnailMediaAssetId", scripts[5].Content, StringComparison.Ordinal);
        Assert.Contains("TimeZone", scripts[6].Content, StringComparison.Ordinal);
        Assert.Contains("DeckPage", scripts[7].Content, StringComparison.Ordinal);
        Assert.Contains("AnimationInType", scripts[8].Content, StringComparison.Ordinal);
        Assert.Contains("DisplayPermanently", scripts[8].Content, StringComparison.Ordinal);
        Assert.All(scripts, script => Assert.Equal(64, script.Checksum.Length));
        Assert.All(scripts, script => Assert.DoesNotContain("Account", script.Content, StringComparison.OrdinalIgnoreCase));
        Assert.All(scripts, script => Assert.DoesNotContain("Chat", script.Content, StringComparison.OrdinalIgnoreCase));
        Assert.All(scripts, script => Assert.DoesNotContain("Notification", script.Content, StringComparison.OrdinalIgnoreCase));
    }
}
