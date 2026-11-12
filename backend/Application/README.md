# Application

CQRS requests, handlers, localized validation, Deck, Files, Media, Playback, and Settings workflows, and generic
realtime contracts.

Requests and handlers are registered explicitly with `AddApplicationRequest<TRequest,TResponse,THandler>()`.
`RealtimeCqrsRequestHandler<TRequest,TResponse>` adapts an explicitly named realtime request to the dispatcher.
Application owns ports such as `IFileStorage`, `IMediaInspector`, `IPlaybackQueue`, and `IRealtimePublisher` and does
not implement filesystem, inspection, queue storage, or SignalR transport concerns.

`ConfigureApplicationServices` registers scoped `ITextProvider` and `ValidationTextProvider` services through
`CommandQ.Localization`. Application-owned English and Spanish JSON/RESX catalogs live under `Localization`; future
modules must update every supported catalog together. Deck, Files, Settings, Media, Playback, and Realtime use reserved
event-ID ranges and source-generated logging methods without recording payloads or file bytes.

Deck requests own the profile/page/cell limits and expose sparse cell configuration through CQRS. Settings requests own
product preferences such as culture, palette, timezone mode, queue capacity, and visualizer safe margin. Kestrel
network configuration remains a host concern under `Api`.

Media requests compose the Files module rather than handling physical paths. Cell-thumbnail uploads use the
Application-owned processor port and persist only the optimized PNG derivative. Playback requests resolve enabled cell
configuration and enqueue immutable media identifiers and playback parameters. Queue state is transient by design.
