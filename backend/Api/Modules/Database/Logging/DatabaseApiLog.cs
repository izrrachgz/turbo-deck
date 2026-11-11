using Common.Enumerations;

namespace Api.Modules.Database.Logging;

internal static partial class DatabaseApiLog
{
    [LoggerMessage(DatabaseApiEventIds.ConnectionTestSucceeded, LogLevel.Information, "Database connection test succeeded for {Engine}")]
    public static partial void ConnectionTestSucceeded(this ILogger logger, DatabaseEngine engine);

    [LoggerMessage(DatabaseApiEventIds.ConnectionTestFailed, LogLevel.Warning, "Database connection test failed for {Engine}: {Reason}")]
    public static partial void ConnectionTestFailed(this ILogger logger, DatabaseEngine engine, string reason);

    [LoggerMessage(DatabaseApiEventIds.EngineSwitched, LogLevel.Information, "Active database engine switched to {Engine}")]
    public static partial void EngineSwitched(this ILogger logger, DatabaseEngine engine);

    [LoggerMessage(DatabaseApiEventIds.EngineSwitchFailed, LogLevel.Error, "Database engine switch failed for {Engine}: {Reason}")]
    public static partial void EngineSwitchFailed(this ILogger logger, DatabaseEngine engine, string reason);
}
