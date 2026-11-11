# Development guide

## Backend

Run the backend independently with:

```powershell
.\backend\scripts\run-api.ps1
```

The script prints a development control token and readiness URL. Electron development normally launches the backend
itself, using `TURBO_DECK_CONTROL_TOKEN`, `TURBO_DECK_DATA_DIR`, and `--ParentPid`. Kestrel uses the persisted network
configuration; a fresh installation defaults to the stable loopback origin `http://127.0.0.1:52024`.
Console logs use stderr, while rolling files are written beneath `TURBO_DECK_DATA_DIR\Logs`. The backend defaults to
`en-US`; send `Accept-Language: es-ES` to exercise the included Spanish resources.

## Desktop

Install locked dependencies and launch the compiled Angular UI in Electron:

```powershell
cd .\desktop
npm ci
npm run test:ci
npm run build:all
npm run electron:dev
```

`npm start` runs the Angular development server for presentation-only work, but backend calls require Electron's preload
API. The production renderer uses `connect-src 'none'`; all privileged communication crosses IPC.

The packaged renderer uses hash routing because it is loaded from `file://`. Feature pages remain lazy and follow the
`core`, `layout`, `shared`, and `features/{area}/{feature}` boundaries used by `command-q-front`.

## Adding API operations

1. Add the backend command/query and controller endpoint in its owning module.
2. Add user-facing text to every supported JSON/RESX catalog and use the scoped localization providers.
3. Add source-generated structured events in the module's reserved event-ID range when operational logging is needed.
4. Add the exact method/path pattern to `desktop/electron/main.js`.
5. Add a narrow typed method to the preload and Angular backend service.
6. Add backend and frontend tests appropriate to the behavior.

Generic SignalR remains available at `/hubs/realtime`; the public receive-only visualizer and live deck use
`/hubs/visualizer` and `/hubs/deck-explorer`.
Playback publishes `playback.requested`,
`playback.queue.changed`, and `playback.stop.requested` to the playback group without file bytes or physical paths.
Visualizer and live-deck clients are added to that group by the server and are receive-only at the hub boundary.

Media selection is a dedicated Electron IPC operation. The main process applies an extension filter, performs the
multipart upload with the private control token, and returns only the typed media result to Angular. Do not replace it
with a renderer-visible backend URL or generic fetch bridge.

`npm run build:all` produces the Electron UI plus separate visualizer and live-deck bundles. Development Electron uses
the dedicated Angular `electron` configuration and `desktop/dist-dev/turbo-deck-ui`, keeping its lazy route chunks
isolated from production verification, tests, and dev-server builds under `desktop/dist/turbo-deck-ui`. Build the browser clients before the backend so `Api.csproj` can copy them under
`wwwroot/visualizer` and `wwwroot/deck`; the packaging script enforces that order. Network & Firewall opens either
stable page through trusted Electron IPC.
The route has no user or token and the stable origin lets the browser retain playback permission. The visualizer
deliberately requires one user gesture before unmuted media playback; click **Enable audio and video** once. For an OBS
Browser Source, use **Interact** to press that button after the source loads. Playback requests remain queued until
activation succeeds.

The Twitch page needs a Twitch public application client ID. It uses Device Code authorization with only
`channel:read:redemptions`; no client secret belongs in configuration. Local deck, media, queue, visualizer, and network
features continue to work when Twitch is unconfigured or offline.

LAN pairing rotates the LAN API key and generates the QR entirely inside Angular. Firewall add/remove requests are
user-triggered only; do not invoke `electron/helpers/configure-firewall.ps1` during automated development or tests.

Closing the desktop window hides it to the system tray. Use the tray Quit action or About → Quit Turbo Deck when you
need the backend process to stop. The development Update Wizard is intentionally disabled. Local packaged artifacts are
marked unsigned test builds; only release packages built with required platform signing enable update download/apply.
