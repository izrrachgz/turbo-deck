using Microsoft.Extensions.Logging;

namespace Application.Migrations.Logging
{
    internal static partial class ApplicationMigrationsLog
    {
        [LoggerMessage(EventId = ApplicationMigrationsEventIds.MigrationScanStarted, Level = LogLevel.Information, Message = "Application migration scan started. Engine: {Engine}. Available scripts: {ScriptCount}.")]
        public static partial void MigrationScanStarted(this ILogger logger, object engine, int scriptCount);

        [LoggerMessage(EventId = ApplicationMigrationsEventIds.MigrationScriptsNotFound, Level = LogLevel.Error, Message = "No application migration scripts were found for engine {Engine}.")]
        public static partial void MigrationScriptsNotFound(this ILogger logger, object engine);

        [LoggerMessage(EventId = ApplicationMigrationsEventIds.MigrationLedgerUnavailable, Level = LogLevel.Debug, Message = "The application migration ledger is unavailable. Migration processing will begin from migration 1.")]
        public static partial void MigrationLedgerUnavailable(this ILogger logger, Exception exception);

        [LoggerMessage(EventId = ApplicationMigrationsEventIds.MigrationsAlreadyCurrent, Level = LogLevel.Information, Message = "Application migrations are already current. Last applied migration: {LastMigrationId}. Latest available migration: {LatestMigrationId}.")]
        public static partial void MigrationsAlreadyCurrent(this ILogger logger, long lastMigrationId, long latestMigrationId);

        [LoggerMessage(EventId = ApplicationMigrationsEventIds.MigrationScriptMissing, Level = LogLevel.Error, Message = "Expected application migration {MigrationId} was not found.")]
        public static partial void MigrationScriptMissing(this ILogger logger, long migrationId);

        [LoggerMessage(EventId = ApplicationMigrationsEventIds.ApplyingMigration, Level = LogLevel.Information, Message = "Applying application migration {MigrationId}. Module: {Module}. Script: {MigrationName}.")]
        public static partial void ApplyingMigration(this ILogger logger, long migrationId, string module, string migrationName);

        [LoggerMessage(EventId = ApplicationMigrationsEventIds.MigrationApplied, Level = LogLevel.Information, Message = "Application migration {MigrationId} applied successfully. Module: {Module}. Script: {MigrationName}.")]
        public static partial void MigrationApplied(this ILogger logger, long migrationId, string module, string migrationName);

        [LoggerMessage(EventId = ApplicationMigrationsEventIds.MigrationApplyFailed, Level = LogLevel.Error, Message = "Application migration {MigrationId} failed. Module: {Module}. Script: {MigrationName}.")]
        public static partial void MigrationApplyFailed(this ILogger logger, Exception exception, long migrationId, string module, string migrationName);

        [LoggerMessage(EventId = ApplicationMigrationsEventIds.MigrationScanCompleted, Level = LogLevel.Information, Message = "Application migration scan completed. Engine: {Engine}. Latest migration: {LatestMigrationId}.")]
        public static partial void MigrationScanCompleted(this ILogger logger, object engine, long latestMigrationId);

        [LoggerMessage(EventId = ApplicationMigrationsEventIds.MigrationHistoryAhead, Level = LogLevel.Error, Message = "The database migration ledger is ahead of the deployed application scripts. Last applied migration: {LastMigrationId}. Latest available migration: {LatestMigrationId}.")]
        public static partial void MigrationHistoryAhead(this ILogger logger, long lastMigrationId, long latestMigrationId);

    }
}
