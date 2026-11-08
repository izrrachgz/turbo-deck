# Turbo Deck architecture

## Runtime topology

```text
Electron main process
|-- Angular renderer (sandboxed, context isolated)
|-- preload API (narrow IPC contract)
`-- TurboDeck.Backend.exe
    |-- ASP.NET Core API on an OS-assigned loopback port
    |-- CQRS application and domain projects
    |-- SQLite by default; SQL Server, PostgreSQL, and MySQL optional
    |-- local file storage
    `-- generic SignalR hub
```

Electron owns backend process lifetime and the renderer trust boundary. It creates a random 256-bit control token,
passes it only through the backend environment, validates the readiness URL, and attaches the token to allow-listed
requests. The renderer can request operations through preload IPC but cannot read the URL or token.

The backend remains independently usable by other trusted hosts. Loopback mode is the default. LAN mode requires a
persistent API key and HTTPS unless insecure HTTP is deliberately enabled. System configuration and lifecycle routes
remain loopback-only.

## Source boundaries

- `backend/Api`: process hosting, HTTP, SignalR, discovery, credentials, and lifecycle.
- `backend/Application`: CQRS requests, validation, file workflows, and realtime contracts.
- `backend/Application.Infrastructure`: filesystem adapters.
- `backend/Domain`: infrastructure-free entities and abstractions.
- `backend/Application.Migrations`: provider-specific embedded migrations.
- `desktop/electron`: backend supervisor and privileged IPC adapters.
- `desktop/src`: Angular presentation code only.

New product features should add modules inside the existing backend layers and expose only narrow Electron IPC methods.
Do not expose a generic arbitrary URL proxy or pass the control token to the renderer.
