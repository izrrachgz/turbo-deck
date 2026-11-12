# Api

The .NET 10 executable host. It owns Kestrel loopback/LAN binding, machine credentials, CORS/TLS policy, mDNS
advertisement, lifecycle control, request-culture selection, structured logging, Files HTTP transport, and the generic
SignalR hub.

Run through `..\scripts\run-api.ps1`. The process requires `TURBO_DECK_CONTROL_TOKEN`, applies migrations before
readiness, writes one JSON readiness record to stdout, and writes Serilog events to stderr and
`TURBO_DECK_DATA_DIR\Logs`. API clients select a supported culture with `Accept-Language`.

Controllers remain transport-only and dispatch application requests. Host implementation types are internal unless a
public framework-discovered type requires an intentional interface.

Managed HTTPS assets live under `TURBO_DECK_DATA_DIR\Certificates`. The host reuses its local authority, renews the
server certificate when required, and installs the public authority in the Windows current-user Root store. HTTPS mode
also exposes an ephemeral HTTP loopback endpoint exclusively for the supervising Electron process; announced LAN
browser endpoints continue to use the configured HTTPS port.
