# Security

Report vulnerabilities privately to the repository owner rather than opening a public issue.

The default backend binds only to loopback and requires a random per-launch control token. LAN mode is opt-in, requires a
persistent API key, and requires HTTPS unless insecure HTTP is explicitly enabled for development. Management endpoints
remain loopback-only.

Do not commit control tokens, LAN API keys, database credentials, certificate passwords, private keys, or generated
`backendsettings.json` files. Electron renderers must not receive the backend URL or control token; the Electron main
process should proxy allow-listed HTTP and SignalR operations through a context-isolated preload.

The fixed visualizer page, `/api/v1/visualizer/*`, and `/hubs/visualizer` are intentionally public so browsers retain
media-playback permission for one stable origin. Visualizer clients are receive-only on the playback SignalR group.
They can read display state, stream media by ID, and complete the active queue item, but cannot access general APIs or
interactive hub methods. Enabling LAN mode exposes this narrow visualizer surface to the configured local network.

The fixed `/deck` page, `/api/v1/deck-explorer/*`, and `/hubs/deck-explorer` are also intentionally public. The explorer
can list profiles/pages, read configured grids and queue state, fetch image thumbnails, and trigger enabled cells. It
cannot edit configuration or invoke interactive hub operations. In LAN mode, anyone who can reach the listener can use
this surface to trigger configured media, so expose the listener only to a trusted local network and prefer HTTPS.

Twitch uses the public-client Device Code flow and never stores a client secret. OAuth access and refresh tokens are
encrypted with a dedicated Data Protection purpose; Windows DPAPI protects the persisted key ring. Disconnect attempts
Twitch revocation and always deletes the local OAuth artifact, even when Twitch is offline. Never log OAuth responses,
EventSub payloads, redemption user data, or tokens.
Twitch authorization, polling, and disconnect endpoints are loopback-control operations and do not accept the LAN API key.

LAN pairing QR codes contain a live API key and must be treated as credentials. Firewall changes are never automatic:
they require an explicit UI confirmation, validated fixed arguments, and the Windows elevation prompt, and affect only
Turbo Deck's named Private-profile TCP rule for the selected port.

Uploaded files are untrusted content. The base validates size, extension, and declared media type, stores opaque names,
prevents path traversal, and never extracts or executes uploads. It does not provide malware scanning.

Structured logs must not contain control tokens, LAN API keys, realtime payloads, file bytes,
database credentials, certificate passwords, or physical file paths. Stdout is reserved for the readiness record;
operational logs belong on stderr and in the rolling files beneath the configured data directory.

Release signing credentials and GitHub publishing tokens belong only in the release environment. Unsigned local test
packages are marked fail-closed and cannot use the Update Wizard to download or apply packages. Never publish an
installer without its matching updater metadata, block map, and checksum from the same build.
