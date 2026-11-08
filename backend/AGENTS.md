# Turbo Deck backend - AGENTS

## Solution

- Framework: .NET 10.
- Solution: `turbo-deck-backend.sln`.
- Restore/build/test: `dotnet restore`, `dotnet build .\turbo-deck-backend.sln`, `dotnet test`.
- Helper scripts: `scripts/build.ps1`, `scripts/test.ps1`, and `scripts/run-api.ps1`.
- Package versions are centrally managed in `Directory.Packages.props`; never add inline `Version` attributes.
- Versioned `CommandQ.*` packages come from the sibling `command-q` repository through `..\..\command-q-build-output`.
- The workspace may contain unrelated user changes. Never revert them.

## Architecture

This branch is an Electron-backend scaffold with one deployable API and explicit module composition.

Projects:

- `Api` — Kestrel hosting, machine access boundary, lifecycle, discovery, Files HTTP, and SignalR transport.
- `Application` — CQRS requests/handlers, validation, localization catalogs, module contracts, and application-owned ports.
- `Application.Infrastructure` — direct filesystem and provider-specific implementations of Application ports.
- `Domain` — infrastructure-free entities and abstractions.
- `Application.Migrations` — embedded forward-only schema migrations.
- `Api.Test`, `Application.Test`, and `Application.Migrations.Test` — isolated tests.

Current modules are `Files` and `Realtime`. Health and System are host concerns. Do not reintroduce users, accounts,
passwords, JWT/OIDC, roles, email, chat, notifications, or other business features into the base.

Application request registration is explicit through `AddApplicationRequest<TRequest,TResponse,THandler>()`. Keep
requests/contracts public for API and test boundaries and handlers internal. API code must call Application requests or
intentional ports; it must not use repositories, SqlKata, raw connections, or command runners.

## Process and network contract

- Loopback is the default and binds `127.0.0.1:0`.
- `TURBO_DECK_CONTROL_TOKEN` is mandatory and is intended to be random per process launch.
- `TURBO_DECK_DATA_DIR` selects the persistent application data directory.
- The process writes one JSON readiness record to stdout; ordinary logs go to stderr.
- `--ParentPid=<pid>` enables parent-process monitoring.
- Exit code `23` requests a supervisor restart.
- `/system/*` is loopback-only and uses `X-Turbo-Deck-Control-Token`.
- LAN API traffic uses `X-Turbo-Deck-Api-Key`; LAN mode requires HTTPS unless insecure HTTP is explicitly enabled.
- Discovery uses `_turbo-deck._tcp` and never publishes credentials.
- Browser origins use an explicit CORS allowlist. Never use a wildcard with credentials.

## Files

- File bytes live beneath the configured data directory; databases store only `StoredFile` metadata.
- Keep `IFormFile` in Api. Application commands receive streams and metadata.
- Filesystem implementations belong in Application.Infrastructure.
- Never trust client paths or expose physical paths. Use opaque storage names and root-constrained resolution.
- Stream uploads, enforce configured limits, calculate SHA-256, and clean incomplete artifacts.
- The base does not provide malware scanning and must not claim that extension/media-type validation is scanning.

## Realtime

- `/hubs/realtime` is generic transient messaging, not chat.
- Do not add history, presence persistence, outbox, or retry semantics without a feature requirement.
- Never expose arbitrary CLR type dispatch or assembly scanning. Server-targeted messages require an explicitly registered
  `IRealtimeRequestHandler`; CQRS adapters derive from `RealtimeCqrsRequestHandler<TRequest,TResponse>`.
- SignalR payloads carry file IDs, never file bytes.
- In-memory routing is the default. Redis backplane support is optional and disabled unless configured.

## Localization

- `CommandQ.Localization` provides the reusable mechanism; application-owned catalogs live under `Application/Localization`.
- Localization is registered in the Application DI composition root. Keep `ITextProvider` and `ValidationTextProvider` scoped.
- `Api:Localization` owns default, fallback, supported-culture, locale-path, and provider-order configuration.
- Request culture must be established before endpoints execute; Electron and LAN clients may select it with `Accept-Language`.
- Keep the base catalogs free of business-specific text and update every supported JSON and RESX culture together.

## Logging

- Serilog is the host logging provider and is configured through `Api/appsettings.json`.
- Preserve stdout exclusively for the single readiness JSON record. Console logs must remain on stderr.
- Rolling file logs belong beneath the configured application data directory, never beside a packaged executable.
- Reserve event-ID ranges by project/module and use source-generated `LoggerMessage` methods for structured events.
- Never log control tokens, LAN API keys, realtime payloads, file bytes, or physical storage paths.

## Persistence and migrations

- SQLite is the default database.
- Supported engines remain `SqLite`, `MsSql`, `PgSql`, and `MySql`.
- Every schema change needs equivalent `migration-X.sql` scripts for all four engines.
- Migration IDs are unique and contiguous per engine. Scripts are embedded, forward-only, and applied before readiness.
- The baseline owns only `MigrationRecord` and `StoredFile`.
- Do not add row-level security or account session context to this base.

## Coding conventions

- Use CRLF line endings and follow `.editorconfig`.
- Order each contiguous C# using block from shortest trimmed directive to longest; alphabetically break equal lengths.
- Prefer existing patterns over new abstractions and avoid unrelated refactors.
- Keep declarations on one line when at most 140 characters.
- Use `rg`/`rg --files` for searches and `apply_patch` for manual edits.
- Use source-generated logging with `LoggerMessage` for repeated or structured log events.
- Keep configuration paths centralized and bindable models in `Configuration` folders with `*Options` names.
- Use configuration for environment-specific and runtime-tunable values.
- Keep query handlers on `IReadOnlyRepository` and command handlers on `IWritableRepository`.

## Verification

After code changes:

1. Run `dotnet build .\turbo-deck-backend.sln`.
2. Run targeted tests, then `dotnet test .\turbo-deck-backend.sln` when practical.
3. For host changes, smoke launch with a temporary data directory and control token.
4. Verify unauthorized calls fail, migrations complete before readiness, and shutdown/restart semantics work.
5. For packaging changes, publish a self-contained runtime-specific single-file artifact.
