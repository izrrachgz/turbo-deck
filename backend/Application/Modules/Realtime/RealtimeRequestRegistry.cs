using System.Collections.Frozen;
using System.Text.Json;

namespace Application.Modules.Realtime;

public sealed class RealtimeRequestRegistry
{
    private readonly FrozenDictionary<(RealtimeMessageKind Kind, string Name), IRealtimeRequestHandler> _handlers;

    public RealtimeRequestRegistry(IEnumerable<IRealtimeRequestHandler> handlers)
    {
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
            throw new KeyNotFoundException($"Realtime request '{kind}:{name}' is not registered.");

        return await handler.HandleAsync(payload, cancellationToken);
    }
}
