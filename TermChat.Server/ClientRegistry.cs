using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace TermChat.Server;

public class ClientRegistry
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _clients.Count;

    public bool TryRegister(string username, WebSocket socket)
    {
        return _clients.TryAdd(username, socket);
    }

    public bool TryGetClient(string username, out WebSocket? socket)
    {
        return _clients.TryGetValue(username, out socket);
    }

    public void Unregister(string username, WebSocket socket)
    {
        // Remove only if this socket is still the registered one
        if (_clients.TryGetValue(username, out var existing) && ReferenceEquals(existing, socket))
        {
            _clients.TryRemove(username, out _);
        }
    }

    public bool IsUserOnline(string username)
    {
        return _clients.TryGetValue(username, out var socket) && socket.State == WebSocketState.Open;
    }
}
