# Application.Infrastructure

Host-neutral implementations of Application-owned I/O ports. The base contains local file storage, media inspection,
cell-thumbnail rendering, and transient playback queue implementations.

Files are streamed beneath the configured data directory using opaque names. Client paths are never trusted, temporary
writes stay on the target volume, SHA-256 is calculated while streaming, and incomplete files are cleaned up. HTTP
endpoints and application use cases do not belong here.

Cell background uploads are decoded with bounded source and pixel limits, center-cropped to a 320 x 320 canvas, and
encoded as compressed PNG files without retaining the full-resolution source. Generic media uploads remain unchanged.

Infrastructure events use their own reserved event-ID range and source-generated logging. Logs may identify opaque
storage names and operation outcomes, but never expose physical storage paths or file bytes.
