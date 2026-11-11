# Realtime messaging

`/hubs/realtime` is a generic SignalR transport, not a chat system. It retains no history, outbox, retry, or offline
delivery state. Delivery is transient and at-most-once.

Each connection supplies a validated `clientId`. Clients can send envelopes to another client ID, a joined group, all
other clients, or the server. Envelopes include message and correlation IDs, a kind, a stable name, JSON payload, and
optional expiration. The server stamps the trusted origin and creation time.

Files are uploaded through `/files`; realtime messages carry file IDs rather than file bytes.

Playback events target the `playback` group. Public connections through `/hubs/visualizer` and `/hubs/deck-explorer`
are joined to this group by the server and cannot send messages, join or leave groups, dispatch server requests, or
list connected clients. Any number of visualizer and live-deck clients observe the same backend-owned transient queue.
Trusted control-token and LAN-key clients retain the generic `/hubs/realtime` transport behavior.

## Server commands and queries

Only `Command` and `Query` messages can target the server. A name must have one explicitly registered
`IRealtimeRequestHandler`. Unknown names receive a structured failure response. There is no assembly scanning, CLR type
name input, reflection invocation, or generic access to the CQRS dispatcher.

Future modules can derive from `RealtimeCqrsRequestHandler<TRequest,TResponse>` and explicitly register the closed
handler with `AddRealtimeRequestHandler<THandler>()`.

## Credentials

Loopback supervisors can send the control token as a header or SignalR `access_token`. LAN clients use the persistent
LAN API key. Browser SignalR clients necessarily use a query token for WebSockets or server-sent events, so normal LAN
operation requires HTTPS and query strings must not be logged. Browser origins must also appear in the explicit CORS
allowlist.

The fixed visualizer page, `/api/v1/visualizer/*`, and `/hubs/visualizer` require no credential. This narrow public
surface exists so browser playback permission can remain associated with one stable origin. It does not authorize
general API requests or interactive realtime operations.

The fixed `/deck` page, `/api/v1/deck-explorer/*`, and `/hubs/deck-explorer` also require no credential. The explorer
surface is limited to deck reads, image thumbnails, queue reads, and enabled-cell triggers. Its browser selection state
is local to each device; queue state and playback routing remain centralized in the backend.

SignalR uses in-memory routing by default. A Redis backplane can be enabled for a deliberately multi-host deployment;
multiple devices connected to one backend do not require Redis.
