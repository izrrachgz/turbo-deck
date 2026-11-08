# Application.Infrastructure

Host-neutral implementations of Application-owned I/O ports. The base contains only local file storage.

Files are streamed beneath the configured data directory using opaque names. Client paths are never trusted, temporary
writes stay on the target volume, SHA-256 is calculated while streaming, and incomplete files are cleaned up. HTTP
endpoints and application use cases do not belong here.

Infrastructure events use their own reserved event-ID range and source-generated logging. Logs may identify opaque
storage names and operation outcomes, but never expose physical storage paths or file bytes.
