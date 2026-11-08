# Application

CQRS requests, handlers, localized validation, Files workflows, and generic realtime contracts.

Requests and handlers are registered explicitly with `AddApplicationRequest<TRequest,TResponse,THandler>()`.
`RealtimeCqrsRequestHandler<TRequest,TResponse>` adapts an explicitly named realtime request to the dispatcher.
Application owns ports such as `IFileStorage` and `IRealtimePublisher` and does not implement filesystem or SignalR
transport concerns.

`ConfigureApplicationServices` registers scoped `ITextProvider` and `ValidationTextProvider` services through
`CommandQ.Localization`. Application-owned English and Spanish JSON/RESX catalogs live under `Localization`; future
modules must update every supported catalog together. Files and Realtime use reserved event-ID ranges and
source-generated logging methods without recording payloads or file bytes.
