# Electron integration

Electron should own the backend process and the renderer trust boundary.

## Startup

1. Electron obtains its single-instance lock and generates a random 256-bit control token.
2. It creates the application data directory beneath `app.getPath('userData')`.
3. It starts the backend with a minimized environment containing:
   - `TURBO_DECK_CONTROL_TOKEN`
   - `TURBO_DECK_DATA_DIR`
   - `ASPNETCORE_ENVIRONMENT`
4. It passes `--Port=0 --ParentPid=<electron-pid>`.
5. It reads stdout until the backend writes its single readiness JSON record.

Example readiness record:

```json
{"event":"ready","url":"http://127.0.0.1:54321","addresses":["http://127.0.0.1:54321"],"pid":1234}
```

All backend logs are written to stderr. Electron must reject readiness URLs that are not loopback in loopback mode.

## Renderer boundary

The Angular renderer should never receive the backend URL or control token. Electron main owns HTTP and SignalR
connections and exposes narrow, allow-listed IPC operations through a context-isolated preload. Recommended Electron
settings are `contextIsolation: true`, `sandbox: true`, `nodeIntegration: false`, denied permissions, and blocked
unapproved navigation or webviews.

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
