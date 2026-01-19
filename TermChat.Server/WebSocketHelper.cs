using System.Net.WebSockets;
using System.Text;

namespace TermChat.Server;

public static class WebSocketHelper
{
    public static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken ct)
    {
        try
        {
            var buffer = new byte[8 * 1024];
            using var ms = new MemoryStream();

            while (!ct.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                ms.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                    break;
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch (WebSocketException)
        {
            // Client disconnected abruptly without completing close handshake
            return null;
        }
    }

    public static Task SendTextAsync(WebSocket socket, string text, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
    }

    public static async Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string description)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseAsync(status, description, CancellationToken.None);
            }
            catch { /* ignore */ }
        }
    }
}
