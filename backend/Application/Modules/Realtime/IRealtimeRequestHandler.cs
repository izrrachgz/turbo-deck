using System.Text.Json;

namespace Application.Modules.Realtime;

public interface IRealtimeRequestHandler
{
    string Name { get; }

    RealtimeMessageKind Kind { get; }

    Task<JsonElement?> HandleAsync(JsonElement payload, CancellationToken cancellationToken);
}
