# Development guide

## Backend

Run the backend independently with:

```powershell
.\backend\scripts\run-api.ps1
```

The script prints a development control token and readiness URL. Electron development normally launches the backend
itself, using `TURBO_DECK_CONTROL_TOKEN`, `TURBO_DECK_DATA_DIR`, `--Port=0`, and `--ParentPid`.
Console logs use stderr, while rolling files are written beneath `TURBO_DECK_DATA_DIR\Logs`. The backend defaults to
`en-US`; send `Accept-Language: es-ES` to exercise the included Spanish resources.

## Desktop

Install locked dependencies and launch the compiled Angular UI in Electron:

```powershell
cd .\desktop
npm ci
npm run electron:dev
```

`npm start` runs the Angular development server for presentation-only work, but backend calls require Electron's preload
API. The production renderer uses `connect-src 'none'`; all privileged communication crosses IPC.

## Adding API operations

1. Add the backend command/query and controller endpoint in its owning module.
2. Add user-facing text to every supported JSON/RESX catalog and use the scoped localization providers.
3. Add source-generated structured events in the module's reserved event-ID range when operational logging is needed.
4. Add the exact method/path pattern to `desktop/electron/main.js`.
5. Add a narrow typed method to the preload and Angular backend service.
6. Add backend and frontend tests appropriate to the behavior.

Generic SignalR remains available at `/hubs/realtime`. Add a main-process SignalR adapter when the first product feature
needs realtime UI events so the renderer credential boundary remains intact.
