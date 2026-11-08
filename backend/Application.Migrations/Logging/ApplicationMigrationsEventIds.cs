namespace Application.Migrations.Logging
{
    internal static class ApplicationMigrationsEventIds
    {
        public const int MigrationScanStarted = ApplicationMigrationsEventIdRanges.General + 0;
        public const int MigrationScriptsNotFound = ApplicationMigrationsEventIdRanges.General + 1;
        public const int MigrationLedgerUnavailable = ApplicationMigrationsEventIdRanges.General + 2;
        public const int MigrationsAlreadyCurrent = ApplicationMigrationsEventIdRanges.General + 3;
        public const int MigrationScriptMissing = ApplicationMigrationsEventIdRanges.General + 4;
        public const int ApplyingMigration = ApplicationMigrationsEventIdRanges.General + 5;
        public const int MigrationApplied = ApplicationMigrationsEventIdRanges.General + 6;
        public const int MigrationApplyFailed = ApplicationMigrationsEventIdRanges.General + 7;
        public const int MigrationScanCompleted = ApplicationMigrationsEventIdRanges.General + 8;
        public const int MigrationHistoryAhead = ApplicationMigrationsEventIdRanges.General + 9;
    }
}
