# Realtime messaging

`/hubs/realtime` is a generic SignalR transport, not a chat system. It retains no history, outbox, retry, or offline
delivery state. Delivery is transient and at-most-once.

Each connection supplies a validated `clientId`. Clients can send envelopes to another client ID, a joined group, all
other clients, or the server. Envelopes include message and correlation IDs, a kind, a stable name, JSON payload, and
optional expiration. The server stamps the trusted origin and creation time.

Files are uploaded through `/files`; realtime messages carry file IDs rather than file bytes.

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

SignalR uses in-memory routing by default. A Redis backplane can be enabled for a deliberately multi-host deployment;
multiple devices connected to one backend do not require Redis.
