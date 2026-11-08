# Security

Report vulnerabilities privately to the repository owner rather than opening a public issue.

The default backend binds only to loopback and requires a random per-launch control token. LAN mode is opt-in, requires a
persistent API key, and requires HTTPS unless insecure HTTP is explicitly enabled for development. Management endpoints
remain loopback-only.

Do not commit control tokens, LAN API keys, database credentials, certificate passwords, private keys, or generated
`backendsettings.json` files. Electron renderers must not receive the backend URL or control token; the Electron main
process should proxy allow-listed HTTP and SignalR operations through a context-isolated preload.

Uploaded files are untrusted content. The base validates size, extension, and declared media type, stores opaque names,
prevents path traversal, and never extracts or executes uploads. It does not provide malware scanning.

Structured logs must not contain control tokens, LAN API keys, realtime payloads, file bytes, database credentials,
certificate passwords, or physical file paths. Stdout is reserved for the readiness record; operational logs belong on
stderr and in the rolling files beneath the configured data directory.
