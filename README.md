# Turbo Deck

[![Quality](https://github.com/izrrachgz/turbo-deck/actions/workflows/quality.yml/badge.svg)](https://github.com/izrrachgz/turbo-deck/actions/workflows/quality.yml)

Turbo Deck is a local-first Electron application with an Angular 22 renderer and a self-contained .NET 11 backend. It
starts from the `electron-backend` branch of `command-q-back` and follows the secure process topology used by Turbo
Reactor without carrying over any Reactor-specific features.

## Repository layout

```text
backend/    Modular API, CQRS application, localization, structured logging, files, SignalR, and four-engine migrations
desktop/    Angular renderer plus the Electron main/preload trust boundary
scripts/    Build, backend publish, and Windows packaging entry points
docs/       Architecture and development guidance
```

SQLite and loopback-only hosting are the defaults. The backend also retains SQL Server, PostgreSQL, MySQL, configurable
LAN hosting, mDNS discovery, generic file storage, and generic SignalR messaging.
The backend includes English/Spanish localization and Serilog output to stderr, debug, and rolling files under the
application data directory.

The implemented product slices include a Command Q-aligned routed shell, grouped navigation, English/Spanish UI
catalogs, theme palettes, persisted preferences with a searchable IANA timezone selector, an API-backed profile/page
4x8 deck grid, media upload and metadata,
automatic numeric pages, per-cell background thumbnails, media-specific playback controls and previews, persisted
image/video sizing and transitions, permanent image display, a transient priority queue with immediate overrides,
and a transparent OBS/browser visualizer with a public receive-only SignalR hub and Tone.js audio playback. A separate
responsive `/deck` client lets phones and tablets select a profile/page and trigger the same backend-owned queue from
multiple devices. The connectivity slice adds optional Twitch Device Code authorization and EventSub
reward bindings, editable LAN hosting, offline QR pairing payloads, and an explicitly confirmed Windows Firewall helper.
When HTTPS is enabled, Turbo Deck creates a reusable local certificate authority plus a renewable server certificate
covering localhost, the machine name, and concrete local-network addresses. It trusts the authority for the current
Windows user and stores the authority and server certificate files under the backend application-data directory.
With local-network discovery enabled, the browser clients are also available through the advertised
`https://turbo-deck.local:<port>` hostname.
The final desktop slice adds system-tray background operation, About/MIT, direct GitHub issue access, and a
manual Check → Download → Verify → Apply update workflow that is enabled only in signed release builds.

## Prerequisites

- .NET SDK 11.0.100 or a compatible later feature band
- Node.js 24.15.0 (the package also accepts the compatible Node 22 and 26 lines)
- The local `CommandQ.*` NuGet packages expected by `backend/NuGet.Config`

## Development

```powershell
dotnet restore .\backend\turbo-deck-backend.sln
dotnet build .\backend\turbo-deck-backend.sln
dotnet test .\backend\turbo-deck-backend.sln

cd .\desktop
npm ci
npm run electron:dev
```

Electron generates a per-launch control token, starts the backend with its persisted listener configuration, waits for its JSON
readiness record, and proxies allow-listed operations through context-isolated IPC. The Angular renderer never receives
the backend URL or credential.

## Packaging

```powershell
.\scripts\package.ps1
```

This builds the Angular application, publishes `TurboDeck.Backend.exe` as a self-contained single file, and creates an
NSIS installer plus SHA-256 checksum under `artifacts/installer`. See [docs/releasing.md](docs/releasing.md) for release
and cross-platform packaging requirements.

See [ARCHITECTURE.md](ARCHITECTURE.md) and [docs/development.md](docs/development.md) for the current boundaries and
extension points.

## Windows release status

Turbo Deck's public Windows installers are currently distributed without a code-signing certificate. Windows may show
a SmartScreen warning because the publisher cannot be verified. Verify the adjacent SHA-256 checksum before running an
installer downloaded from a GitHub release.

See the [privacy policy](PRIVACY.md), [security policy](SECURITY.md), and [release guide](docs/releasing.md).
