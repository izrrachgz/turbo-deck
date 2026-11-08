using System.Collections.Concurrent;

namespace Api.Modules.Realtime;

public interface IRealtimeConnectionRegistry
{
    void Add(string clientId, string connectionId);

    void Remove(string clientId, string connectionId);

    IReadOnlyList<string> GetConnectedClients();
}

internal sealed class RealtimeConnectionRegistry : IRealtimeConnectionRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _connections =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(string clientId, string connectionId)
    {
        _connections.GetOrAdd(clientId, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))[connectionId] = 0;
    }

    public void Remove(string clientId, string connectionId)
    {
        if (!_connections.TryGetValue(clientId, out ConcurrentDictionary<string, byte>? connections))
            return;

        connections.TryRemove(connectionId, out _);
        if (connections.IsEmpty)
            _connections.TryRemove(clientId, out _);
    }

    public IReadOnlyList<string> GetConnectedClients() => _connections.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
}
