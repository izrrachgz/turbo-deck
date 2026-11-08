# Development guide

## Backend

Run the backend independently with:

```powershell
.\backend\scripts\run-api.ps1
```

The script prints a development control token and readiness URL. Electron development normally launches the backend
itself, using `TURBO_DECK_CONTROL_TOKEN`, `TURBO_DECK_DATA_DIR`, `--Port=0`, and `--ParentPid`.

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
2. Add the exact method/path pattern to `desktop/electron/main.js`.
3. Add a narrow typed method to the preload and Angular backend service.
4. Add backend and frontend tests appropriate to the behavior.

Generic SignalR remains available at `/hubs/realtime`. Add a main-process SignalR adapter when the first product feature
needs realtime UI events so the renderer credential boundary remains intact.
