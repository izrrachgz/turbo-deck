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

The desktop Advanced Settings page can test and switch engines while the backend is running. The system API exposes
only configured/status metadata and a sanitized server/database summary; it never returns a connection string.
Connection strings saved through the UI are encrypted with ASP.NET Core Data Protection before they are written to
`backendsettings.json`. On Windows, DPAPI also protects the local key ring. Startup configuration remains a supported
way to provide the initial engine and connection.

`GET /system/database` returns the active engine and the status of all providers. `POST /system/database/test` opens a
temporary connection with a five-second timeout. `POST /system/database/switch` repeats that preflight, verifies that
the database is either empty or already owns a Turbo Deck migration ledger, applies pending migrations, and only then
persists the new active engine. Switching never copies or deletes data in another database.

SQL Server, PostgreSQL, and MySQL databases must already exist; Turbo Deck provisions its tables but does not create an
external database. SQLite creates its file when necessary. A non-empty database without `MigrationRecord` is rejected
instead of merging the Turbo Deck schema into an unknown schema.

`Application.Migrations/Migrations/{Engine}` contains a numerically ordered, forward-only script sequence for every
engine. Migration IDs must remain contiguous and equivalent schema changes must be added to all four trees. Startup
and runtime switches apply pending migrations before the selected database becomes active. Each migration and its
ledger update use a provider transaction (subject to the database engine's DDL transaction semantics). Migration 1
contains `MigrationRecord` and `StoredFile`.
Migration 2 adds `Profile`, `DeckPage`, `CellConfig`, and singleton `AppSettings` with equivalent scripts for all four
engines. Migration 3 adds `MediaAsset` and cell media/playback configuration.
Migration 4 adds `CellConfig.DisplayDurationMilliseconds` with equivalent bounds for each provider.
Migration 5 adds optional Twitch reward bindings. Migration 6 adds the image-only cell thumbnail media reference.
Migration 7 adds the validated IANA `AppSettings.TimeZone` value; legacy mode/offset columns remain untouched so the
forward-only migration is safe for existing installations.
Migration 8 fills any missing numbered pages so every active profile has positions 1 through 8.
Migration 9 adds cell permanence, display-size, and enter/exit transition fields with equivalent defaults and bounds
for all four engines.
