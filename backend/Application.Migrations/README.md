# Application.Migrations

Embedded, forward-only application schema migrations for `SqLite`, `MsSql`, `PgSql`, and `MySql`.

Scripts live under `Migrations/{Engine}/{Module}/migration-X.sql`. IDs are unique and contiguous per engine, and every
schema change must be represented equivalently in all four trees. Startup reads the ledger and applies pending scripts
before the API emits readiness. Migration 1 contains `MigrationRecord` and `StoredFile`; migration 2 adds the initial
Turbo Deck profile, page, cell, and application-settings schema. Migration 3 adds media metadata and cell playback
configuration. Migration 4 adds bounded image display duration to cell playback configuration; playback queue state
remains in memory and has no table.
Migration 5 adds `TwitchRewardBinding` for optional Channel Point reward-to-cell mappings; OAuth tokens remain encrypted
host files and never enter the database.
Migration 6 adds the optional image-only cell thumbnail reference. Migration 7 adds the persisted IANA timezone used by
application preferences.
Migration 8 backfills missing numbered pages so every active profile owns all eight page positions.
Migration 9 adds permanent-image behavior, display-size presets, and bounded enter/exit transition configuration to
cells.
Migration 10 extends the animation constraints with the advanced animation set.
Migration 11 is an intentionally empty compatibility checkpoint. It prevents the identifier from being reused after an unreleased experimental migration was reverted.
