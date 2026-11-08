# Api

The .NET 10 executable host. It owns Kestrel loopback/LAN binding, machine credentials, CORS/TLS policy, mDNS
advertisement, lifecycle control, request-culture selection, structured logging, Files HTTP transport, and the generic
SignalR hub.

Run through `..\scripts\run-api.ps1`. The process requires `TURBO_DECK_CONTROL_TOKEN`, applies migrations before
readiness, writes one JSON readiness record to stdout, and writes Serilog events to stderr and
`TURBO_DECK_DATA_DIR\Logs`. API clients select a supported culture with `Accept-Language`.

Controllers remain transport-only and dispatch application requests. Host implementation types are internal unless a
public framework-discovered type requires an intentional interface.
