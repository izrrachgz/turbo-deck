# Turbo Deck backend

A .NET 10 backend scaffold for Electron applications. It preserves the modular API, CQRS application, domain,
infrastructure, and embedded migration boundaries while providing only two reusable features: local file storage and
generic SignalR messaging.

The backend has no users, accounts, roles, JWTs, OIDC, email, chat, or application-specific business modules.

## Runtime model

- Loopback mode is the default. Kestrel binds to `127.0.0.1:0` and every request requires the per-launch control token.
- LAN mode binds configured interfaces, can advertise `_turbo-deck._tcp` through mDNS, and uses a persistent LAN API key.
- LAN mode requires HTTPS unless `AllowInsecureHttp` is explicitly enabled.
- The process writes one JSON readiness record to stdout; ordinary logs go to stderr.
- Serilog writes structured events to stderr, the debugger, and rolling files under the configured data directory.
- Exit code `23` requests a supervisor restart. The backend also stops when its configured parent PID exits.
- SQLite is the default database. SQL Server, PostgreSQL, and MySQL remain supported through configuration and
  provider-specific embedded migrations.

See [Electron integration](docs/electron-integration.md), [realtime messaging](docs/realtime.md), and
[database engines](docs/database-engines.md).

## Build and test

```powershell
dotnet restore .\turbo-deck-backend.sln
dotnet build .\turbo-deck-backend.sln
dotnet test .\turbo-deck-backend.sln
```

The local `CommandQ.*` packages come from `..\..\command-q-build-output`. Build the sibling `command-q` repository when
the pinned packages are unavailable.

## Run locally

```powershell
.\scripts\run-api.ps1
```

The script generates a control token, starts on an OS-assigned loopback port, prints the token for development use,
and relaunches the backend after an authenticated `POST /system/restart` request.

## Localization

Localization is configured under `Api:Localization`. The base supports `en-US` and `es-ES`, defaults and falls back to
`en-US`, and selects the request culture from the standard ASP.NET Core culture providers, including `Accept-Language`.
Application-owned RESX and JSON catalogs live under `Application/Localization`; they contain only generic API,
validation, field-name, and file-module text. `ITextProvider` and `ValidationTextProvider` are scoped and registered by
the Application DI composition root so future modules can add localized messages without changing the host boundary.

## Logging

Serilog is configured from `Api/appsettings.json` with console, debug, and rolling-file sinks. Console events are forced
to stderr so stdout remains reserved for the Electron supervisor readiness record. File logs are written beneath
`TURBO_DECK_DATA_DIR\Logs`, roll daily or at 10 MB, and retain 16 files. Host, Files, Realtime, infrastructure, and
migration events use reserved ranges and source-generated `LoggerMessage` methods; credentials, payloads, file bytes,
and physical paths are not logged.

## API surface

```text
GET    /health/live
GET    /health/ready
POST   /files
GET    /files
GET    /files/{id}
GET    /files/{id}/content
DELETE /files/{id}
GET    /system/network
PUT    /system/network
POST   /system/network/api-key/rotate
POST   /system/restart
POST   /system/shutdown
HUB    /hubs/realtime
```

`/system/*` endpoints are loopback-only and require `X-Turbo-Deck-Control-Token`. Ordinary REST calls accept that token
on loopback or `X-Turbo-Deck-Api-Key` for LAN clients.

## Projects

- `Api` — process hosting, local/LAN access boundary, Files HTTP endpoints, generic SignalR hub, discovery, and lifecycle.
- `Application` — explicit CQRS registration, validation, Files workflows, realtime contracts, and application ports.
- `Application.Infrastructure` — local filesystem implementation of the Files storage port.
- `Domain` — base entity abstractions and account-neutral `StoredFile` metadata.
- `Application.Migrations` — forward-only embedded baselines for SQLite, SQL Server, PostgreSQL, and MySQL.
- `Api.Test`, `Application.Test`, `Application.Migrations.Test` — isolated scaffold tests.
