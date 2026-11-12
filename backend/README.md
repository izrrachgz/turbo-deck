# Turbo Deck backend

A .NET 10 backend for the Turbo Deck Electron application. It preserves the modular API, CQRS application, domain,
infrastructure, and embedded migration boundaries while adding Deck, Settings, Media, Playback, and Twitch product modules to
the reusable Files and Realtime foundation.

The backend has no users, accounts, roles, JWTs, OIDC, email, chat, or notifications.

## Runtime model

- Loopback mode is the default. Kestrel binds to `127.0.0.1:52024`; general API requests require the per-launch control token.
- LAN mode binds configured interfaces, can advertise `_turbo-deck._tcp` through mDNS, and uses a persistent LAN API key.
- LAN mode requires HTTPS unless `AllowInsecureHttp` is explicitly enabled.
- The process writes one JSON readiness record to stdout; ordinary logs go to stderr.
- Serilog writes structured events to stderr, the debugger, and rolling files under the configured data directory.
- Exit code `23` requests a supervisor restart. The backend also stops when its configured parent PID exits.
- SQLite is the default database. SQL Server, PostgreSQL, and MySQL remain supported through configuration and
  provider-specific embedded migrations.
- Separately built Angular visualizer and live-deck clients are served at fixed public routes with dedicated receive-only playback hubs.
- Twitch is optional; Device Code OAuth, encrypted token storage, EventSub, and reward-to-cell bindings do not gate local features.

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

The script generates a control token, starts on the fixed loopback port `52024`, prints the token for development use,
and relaunches the backend after an authenticated `POST /system/restart` request.

## Localization

Localization is configured under `Api:Localization`. The base supports `en-US` and `es-ES`, defaults and falls back to
`en-US`, and selects the request culture from the standard ASP.NET Core culture providers, including `Accept-Language`.
Application-owned RESX and JSON catalogs live under `Application/Localization`; they contain only generic API,
validation, field-name, deck, settings, media, and file-module text. `ITextProvider` and `ValidationTextProvider` are
scoped and registered by the Application DI composition root so future modules can add localized messages without
changing the host boundary.

## Logging

Serilog is configured from `Api/appsettings.json` with console, debug, and rolling-file sinks. Console events are forced
to stderr so stdout remains reserved for the Electron supervisor readiness record. File logs are written beneath
`TURBO_DECK_DATA_DIR\Logs`, roll daily or at 10 MB, and retain 16 files. Host, Deck, Files, Settings, Media, Playback,
Realtime, infrastructure, and migration events use reserved ranges and source-generated `LoggerMessage` methods;
credentials, payloads, file bytes, and physical paths are not logged.

## API surface

```text
GET    /health/live
GET    /health/ready
POST   /files
GET    /files
GET    /files/{id}
GET    /files/{id}/content
DELETE /files/{id}
GET    /api/v1/profiles
POST   /api/v1/profiles
GET    /api/v1/profiles/{id}/pages
POST   /api/v1/profiles/{id}/pages
GET    /api/v1/pages/{id}/grid
PUT    /api/v1/pages/{id}/cells/{row}/{column}
POST   /api/v1/media-assets
POST   /api/v1/media-assets/thumbnails
GET    /api/v1/media-assets
GET    /api/v1/media-assets/{id}
GET    /api/v1/media-assets/{id}/content
GET    /api/v1/playback/queue
POST   /api/v1/playback/queue
DELETE /api/v1/playback/queue/{itemId}
POST   /api/v1/playback/queue/{itemId}/move
POST   /api/v1/playback/complete
POST   /api/v1/playback/clear
GET    /api/v1/visualizer/state
GET    /api/v1/visualizer/media/{id}
POST   /api/v1/visualizer/complete
GET    /api/v1/deck-explorer/profiles
GET    /api/v1/deck-explorer/profiles/{profileId}/pages
GET    /api/v1/deck-explorer/pages/{pageId}/grid
GET    /api/v1/deck-explorer/queue
GET    /api/v1/deck-explorer/thumbnails/{mediaAssetId}
POST   /api/v1/deck-explorer/cells/{cellConfigId}/trigger
GET    /api/v1/settings
PUT    /api/v1/settings
GET    /api/v1/twitch/status
POST   /api/v1/twitch/device-authorization
POST   /api/v1/twitch/device-authorization/poll
POST   /api/v1/twitch/disconnect
GET    /api/v1/twitch/rewards
GET    /api/v1/twitch/bindings
PUT    /api/v1/twitch/bindings/{rewardId}
DELETE /api/v1/twitch/bindings/{rewardId}
GET    /system/network
PUT    /system/network
POST   /system/network/api-key/rotate
POST   /system/restart
POST   /system/shutdown
HUB    /hubs/realtime
HUB    /hubs/visualizer
HUB    /hubs/deck-explorer
```

`/system/*` plus Twitch authorization/poll/disconnect endpoints are loopback-only and require `X-Turbo-Deck-Control-Token`. Ordinary REST calls accept that token
on loopback or `X-Turbo-Deck-Api-Key` for LAN clients.

The fixed visualizer and live-deck pages, their narrow APIs, and their dedicated hubs are public. Both hub connection
types are placed in the playback group and cannot invoke interactive hub methods. The deck explorer can read deck and
queue state and trigger enabled cells, but cannot edit profiles, pages, cells, media, or settings. General APIs and
`/hubs/realtime` remain credential-protected.

## Projects

- `Api` — process hosting, local/LAN access boundary, product HTTP endpoints, generic SignalR hub, discovery, and lifecycle.
- `Application` — explicit CQRS registration, validation, product workflows, realtime contracts, and application ports.
- `Application.Infrastructure` — local filesystem storage, built-in media inspection, and transient queue implementation.
- `Domain` — base entity abstractions, product entities, and account-neutral `StoredFile` metadata.
- `Application.Migrations` — forward-only embedded baselines for SQLite, SQL Server, PostgreSQL, and MySQL.
- `Api.Test`, `Application.Test`, `Application.Migrations.Test` — isolated scaffold tests.
