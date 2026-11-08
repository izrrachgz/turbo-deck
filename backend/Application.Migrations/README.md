# Application.Migrations

Embedded, forward-only application schema migrations for `SqLite`, `MsSql`, `PgSql`, and `MySql`.

Scripts live under `Migrations/{Engine}/{Module}/migration-X.sql`. IDs are unique and contiguous per engine, and every
schema change must be represented equivalently in all four trees. Startup reads the ledger and applies pending scripts
before the API emits readiness. The baseline contains only `MigrationRecord` and `StoredFile`.
