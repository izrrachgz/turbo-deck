# Electron integration

Electron should own the backend process and the renderer trust boundary.

## Startup

1. Electron obtains its single-instance lock and generates a random 256-bit control token.
2. It creates the application data directory beneath `app.getPath('userData')`.
3. It starts the backend with a minimized environment containing:
   - `TURBO_DECK_CONTROL_TOKEN`
   - `TURBO_DECK_DATA_DIR`
   - `ASPNETCORE_ENVIRONMENT`
4. It passes `--ParentPid=<electron-pid>`; a fresh configuration already selects port `0`, while saved fixed ports are honored.
5. It reads stdout until the backend writes its single readiness JSON record.

Example readiness record:

```json
{"event":"ready","url":"http://127.0.0.1:54321","addresses":["http://127.0.0.1:54321"],"pid":1234}
```

All console logs are written to stderr, and rolling files are written beneath `TURBO_DECK_DATA_DIR\Logs`. Electron may
consume stderr for diagnostics but must treat stdout exclusively as the readiness protocol. It must reject readiness
URLs that are not loopback in loopback mode.

## Renderer boundary

The Angular renderer should never receive the backend URL or control token. Electron main owns HTTP and SignalR
connections and exposes narrow, allow-listed IPC operations through a context-isolated preload. Recommended Electron
settings are `contextIsolation: true`, `sandbox: true`, `nodeIntegration: false`, denied permissions, and blocked
unapproved navigation or webviews.

The visualizer is intentionally different: it runs as a same-origin browser client served by the backend, not inside
the privileged Electron renderer. A dedicated IPC call opens the fixed local visualizer URL in the default browser.
The visualizer route has no user or token; its public API and dedicated receive-only hub cannot authorize general API
or interactive realtime operations. Never expose the control token to the browser.

The live deck follows the same hosted-browser model at the stable `/deck` route. A second dedicated IPC call opens it
without exposing a credential. Phones and tablets use the configured LAN listener directly; their narrow public API can
selectively read deck state and trigger enabled cells, while editing and management remain behind normal credentials.

Electron main may attach `Accept-Language` to proxied requests using an allow-listed application locale. The backend
defaults and falls back to `en-US` and also includes `es-ES`; unsupported or missing translations fall back to English.

## Shutdown and restart

- Graceful shutdown: `POST /system/shutdown` with `X-Turbo-Deck-Control-Token`.
- Configuration restart: `POST /system/restart` with the same token.
- Exit code `23` means Electron should start the backend again using the persisted settings.
- Any other non-zero exit is a failure and should not create an uncontrolled restart loop.
- The backend independently monitors `ParentPid` and exits if Electron disappears.

## LAN mode

Network configuration is stored atomically in `backendsettings.json` beneath the data directory. Generate a LAN API
key before enabling LAN mode. The plaintext key is returned only by the rotation call and is never stored, logged, or
advertised. Discovery publishes `_turbo-deck._tcp` with instance, protocol, hub, port, and TLS metadata only.

Binding, port, TLS, discovery, and CORS changes return `restartRequired: true`. System configuration, key rotation,
restart, and shutdown remain loopback-only even while ordinary API and SignalR traffic is exposed to the LAN.

The dedicated LAN-pairing IPC rotates the API key and returns concrete active LAN listeners; Angular generates the QR
locally. The QR payload contains both endpoint and credential and must never be logged or copied automatically. The
Windows Firewall IPC accepts only an integer port and `add`/`remove`, launches the fixed packaged helper, and depends on
an explicit renderer confirmation plus UAC. Electron never runs the helper at startup.
