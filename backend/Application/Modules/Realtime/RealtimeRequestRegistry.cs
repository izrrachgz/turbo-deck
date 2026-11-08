using System.Text.Json;
using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using Application.Modules.Realtime.Logging;

namespace Application.Modules.Realtime;

public sealed class RealtimeRequestRegistry
{
    private readonly FrozenDictionary<(RealtimeMessageKind Kind, string Name), IRealtimeRequestHandler> _handlers;

    private readonly ILogger<RealtimeRequestRegistry> _logger;

    public RealtimeRequestRegistry(IEnumerable<IRealtimeRequestHandler> handlers, ILogger<RealtimeRequestRegistry> logger)
    {
        _logger = logger;
        _handlers = handlers.ToFrozenDictionary(
            handler => (handler.Kind, handler.Name),
            handler => handler);
    }

    public async Task<JsonElement?> DispatchAsync(
        RealtimeMessageKind kind,
        string name,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue((kind, name), out IRealtimeRequestHandler? handler))
        {
            _logger.RealtimeRequestNotRegistered(kind, name);
            throw new KeyNotFoundException($"Realtime request '{kind}:{name}' is not registered.");
        }

        _logger.RealtimeRequestDispatching(kind, name);
        return await handler.HandleAsync(payload, cancellationToken);
    }
}
