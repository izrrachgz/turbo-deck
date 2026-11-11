# Turbo Deck architecture

## Runtime topology

```text
Electron main process
|-- Angular renderer (sandboxed, context isolated)
|-- preload API (narrow IPC contract)
`-- TurboDeck.Backend.exe
    |-- ASP.NET Core API on a configurable loopback/LAN listener
    |-- CQRS application and domain projects
    |-- SQLite by default; SQL Server, PostgreSQL, and MySQL optional
    |-- English/Spanish localization and scoped text providers
    |-- structured Serilog events and rolling data-directory logs
    |-- Deck, Settings, Media, Playback, and Twitch-binding CQRS modules
    |-- local file storage plus media metadata inspection
    |-- optional Twitch EventSub WebSocket transport
    `-- generic SignalR hub
```

Electron owns backend process lifetime and the renderer trust boundary. It creates a random 256-bit control token,
passes it only through the backend environment, validates the readiness URL, and attaches the token to allow-listed
requests. The renderer can request operations through preload IPC but cannot read the URL or token.

Closing the main window hides it while the Electron tray continues supervising the backend. Quitting is explicit from
the tray or About page and always requests graceful backend shutdown. Update checks are manual. Unsigned local packages
carry a test-build marker and cannot enter download/apply states; signed releases use electron-updater metadata,
integrity verification, and platform signing before Apply is exposed.

The backend remains independently usable by other trusted hosts. Loopback mode is the default. LAN mode requires a
persistent API key and HTTPS unless insecure HTTP is deliberately enabled. System configuration and lifecycle routes
remain loopback-only.

## Source boundaries

- `backend/Api`: process hosting, HTTP, SignalR, discovery, credentials, lifecycle, culture selection, and Serilog setup.
- `backend/Application`: CQRS requests, localized validation, resource catalogs, product workflows, file workflows,
  queue contracts, and realtime contracts.
- `backend/Application.Infrastructure`: filesystem adapters, built-in media inspection, the transient playback queue,
  and infrastructure logging.
- `backend/Domain`: infrastructure-free entities and abstractions.
- `backend/Application.Migrations`: provider-specific embedded migrations.
- `desktop/electron`: backend supervisor and privileged IPC adapters.
- `desktop/src`: Angular presentation code only, organized as `core`, `layout`, `shared`, and lazy `features`.

## Current product slice

The persistent product model contains `Profile`, `DeckPage`, sparse `CellConfig`, `MediaAsset`, and singleton
`AppSettings` records. File bytes remain outside the database. The application enforces up to eight profiles, eight
pages per profile, four-by-eight grid coordinates, web-compatible media uploads, and bounded playback parameters. Cell
configuration persists image duration/permanence, image/video size and enter/exit transitions, plus audio speed and
pitch. The transient queue immediately replaces the current item when an `override` cell is triggered, supports operator
removal and reordering of pending items, promotes the next pending item when the current item is removed, and deliberately
does not persist history. Creating a profile atomically provisions all eight numbered pages. Cells may reference a
separate image-only media asset for their deck thumbnail without duplicating file bytes.

The backend also serves a separately built transparent Angular visualizer at the stable global route `/visualizer`.
Its dedicated public hub and `/api/v1/visualizer/*` surface require no credential,
while general APIs remain protected. Visualizer hub clients cannot invoke commands, join arbitrary groups, enumerate
clients, or call general APIs. The visualizer disposes media after every item and uses Tone.js for independent audio
speed and pitch control. Browser and OBS clients explicitly activate media playback once; queued items wait for
activation instead of being failed by the browser autoplay policy.

The separately built responsive live deck is hosted at `/deck`. Its public API exposes only profile/page/grid reads,
image thumbnails, shared queue state, and enabled-cell triggering. Each browser chooses its own visible profile and page,
while all deck clients and the visualizer observe the single transient queue owned by the backend. `/hubs/deck-explorer`
is receive-only and joins the same playback group; polling keeps queue state usable through temporary realtime failures.

The built-in inspector extracts image dimensions and PCM WAV duration/waveform data without native dependencies;
other accepted web media receives basic metadata. A later packaging slice may add an explicitly managed ffprobe
adapter for broader codec and duration coverage.

Twitch remains an optional host integration. Public clients authorize through Twitch's Device Code flow without an
embedded client secret. OAuth tokens are encrypted with ASP.NET Core Data Protection; Windows protects the key ring
with DPAPI. Disconnect attempts remote revocation and always hard-purges the local token artifact. Custom reward
bindings are normal CQRS/domain data, while EventSub messages are transient, bounded, deduplicated, and dispatch only
the explicitly bound cell command. The desktop binding UI mirrors the selected profile/page grid so reward mappings
remain spatially aligned with the same deck cells users operate elsewhere.

The Electron renderer uses hash routing because packaged content loads through `file://`. It never receives the backend
URL or control token; every profile, page, cell, media, playback, and settings route is explicitly allowlisted in the
main process. File selection and multipart media upload use a dedicated IPC operation rather than the JSON proxy.
Cell background images and editor media previews stream through a read-only custom Electron protocol that accepts only
numeric media IDs, forwards the control token internally, validates media types, and supports bounded range requests.
Opening the stable `/visualizer` and `/deck` routes uses dedicated IPC operations and does not expose the Electron control token.

Network administration uses dedicated IPC operations too. Pairing QR values are generated locally and contain the LAN
endpoint plus a rotated LAN API key, so they are credentials. Windows Firewall changes run only after a renderer
confirmation, through a fixed packaged script with validated port/action arguments, and still require Windows UAC.

New product features should add modules inside the existing backend layers and expose only narrow Electron IPC methods.
Do not expose a generic arbitrary URL proxy or pass the control token to the renderer.
