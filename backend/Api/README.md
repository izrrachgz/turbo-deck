# Api

The .NET 10 executable host. It owns Kestrel loopback/LAN binding, machine credentials, CORS/TLS policy, mDNS
advertisement, lifecycle control, Files HTTP transport, and the generic SignalR hub.

Run through `..\scripts\run-api.ps1`. The process requires `TURBO_DECK_CONTROL_TOKEN`, applies migrations before
readiness, writes one JSON readiness record to stdout, and writes logs to stderr.

Controllers remain transport-only and dispatch application requests. Host implementation types are internal unless a
public framework-discovered type requires an intentional interface.
