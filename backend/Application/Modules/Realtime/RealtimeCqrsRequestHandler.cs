using Cqrs.Abstractions;
using System.Text.Json;

namespace Application.Modules.Realtime;

/// <summary>
/// Adapts one explicitly registered realtime message name to one closed CQRS request type.
/// </summary>
public abstract class RealtimeCqrsRequestHandler<TRequest, TResponse>(IRequestDispatcher dispatcher)
    : IRealtimeRequestHandler
    where TRequest : class, IRequest<TResponse>
{
    public abstract string Name { get; }

    public abstract RealtimeMessageKind Kind { get; }

    public async Task<JsonElement?> HandleAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        TRequest request = payload.Deserialize<TRequest>() ??
            throw new JsonException($"The payload for realtime request '{Name}' is invalid.");
        TResponse response = await dispatcher.Send(request, cancellationToken);
        return JsonSerializer.SerializeToElement(response);
    }
}
