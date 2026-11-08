# Database engines

SQLite is selected when no engine is configured. Its database file is created as `turbo-deck.db` beneath
`TURBO_DECK_DATA_DIR`; foreign keys and WAL mode are enabled. File bytes remain in the filesystem and the database stores
only `StoredFile` metadata.

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
applies pending migrations before emitting readiness. The version-one baseline contains only `MigrationRecord` and
`StoredFile`.
