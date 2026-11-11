# Database engines

SQLite is selected when no engine is configured. Its database file is created as `turbo-deck.db` beneath
`TURBO_DECK_DATA_DIR`; foreign keys and WAL mode are enabled. File bytes remain in the filesystem. The database stores
`StoredFile` and `MediaAsset` metadata plus product-owned deck/settings records, never uploaded file bytes or transient
playback queue state.

Supported engine values are:

```text
SqLite
MsSql
PgSql
MySql
```

Select another engine with configuration or environment variables:

```text
Api__ConnectionStrings__Engine=PgSql
Api__ConnectionStrings__Db=Host=...;Database=...;Username=...;Password=...;
```

Connection strings are startup configuration: they are never returned from the management API, discovery records, or
readiness output. Changing the engine or connection string requires restarting the process.

`Application.Migrations/Migrations/{Engine}` contains a numerically ordered, forward-only script sequence for every
engine. Migration IDs must remain contiguous and equivalent schema changes must be added to all four trees. Startup
applies pending migrations before emitting readiness. Migration 1 contains `MigrationRecord` and `StoredFile`.
Migration 2 adds `Profile`, `DeckPage`, `CellConfig`, and singleton `AppSettings` with equivalent scripts for all four
engines. Migration 3 adds `MediaAsset` and cell media/playback configuration.
Migration 4 adds `CellConfig.DisplayDurationMilliseconds` with equivalent bounds for each provider.
Migration 5 adds optional Twitch reward bindings. Migration 6 adds the image-only cell thumbnail media reference.
Migration 7 adds the validated IANA `AppSettings.TimeZone` value; legacy mode/offset columns remain untouched so the
forward-only migration is safe for existing installations.
Migration 8 fills any missing numbered pages so every active profile has positions 1 through 8.
Migration 9 adds cell permanence, display-size, and enter/exit transition fields with equivalent defaults and bounds
for all four engines.
