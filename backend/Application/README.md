# Application

CQRS requests, handlers, validation, Files workflows, and generic realtime contracts.

Requests and handlers are registered explicitly with `AddApplicationRequest<TRequest,TResponse,THandler>()`.
`RealtimeCqrsRequestHandler<TRequest,TResponse>` adapts an explicitly named realtime request to the dispatcher.
Application owns ports such as `IFileStorage` and `IRealtimePublisher` and does not implement filesystem or SignalR
transport concerns.
