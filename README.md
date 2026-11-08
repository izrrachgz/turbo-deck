# Turbo Deck

Turbo Deck is an Electron desktop application scaffold with an Angular 22 renderer and a self-contained .NET 10
backend. It starts from the `electron-backend` branch of `command-q-back` and follows the secure process topology used
by Turbo Reactor without carrying over any Reactor-specific features.

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

## Prerequisites

- .NET SDK 10.0.403 or a compatible later feature band
- Node.js 24.15.0 (the package also accepts the compatible Node 22 and 26 lines)
- The local `CommandQ.*` NuGet packages expected by `backend/NuGet.Config`

## Development

```powershell
dotnet restore .\TurboDeck.slnx
dotnet build .\TurboDeck.slnx
dotnet test .\TurboDeck.slnx

cd .\desktop
npm ci
npm run electron:dev
```

Electron generates a per-launch control token, starts the backend on an OS-assigned loopback port, waits for its JSON
readiness record, and proxies allow-listed operations through context-isolated IPC. The Angular renderer never receives
the backend URL or credential.

## Packaging

```powershell
.\scripts\package.ps1
```

This builds the Angular application, publishes `TurboDeck.Backend.exe` as a self-contained single file, and creates an
NSIS installer under `artifacts/installer`.

See [ARCHITECTURE.md](ARCHITECTURE.md) and [docs/development.md](docs/development.md) for the initial boundaries and
extension points.
