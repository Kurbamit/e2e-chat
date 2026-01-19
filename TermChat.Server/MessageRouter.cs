using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TermChat.Server;

public class MessageRouter
{
    private readonly ClientRegistry _clientRegistry;
    private readonly ILogger<MessageRouter> _logger;

    public MessageRouter(ClientRegistry clientRegistry, ILogger<MessageRouter> logger)
    {
        _clientRegistry = clientRegistry;
        _logger = logger;
    }

    public async Task<MessageRouteResult> RouteMessageAsync(string messageJson, string fromUsername, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(messageJson);
            var root = doc.RootElement;

            var msgType = root.TryGetProperty("type", out var mt) ? mt.GetString() : null;

            // Handle ping
            if (msgType == "ping")
            {
                return MessageRouteResult.Pong();
            }

            // Validate message type
            if (msgType != "msg" && msgType != "handshake1" && msgType != "handshake2")
            {
                return MessageRouteResult.Error("Unknown message type");
            }

            // Validate recipient
            if (!root.TryGetProperty("to", out var toProp))
            {
                return MessageRouteResult.Error("Missing 'to'");
            }

            var to = toProp.GetString();
            if (string.IsNullOrWhiteSpace(to))
            {
                return MessageRouteResult.Error("Invalid 'to'");
            }

            // Check if recipient is online
            if (!_clientRegistry.TryGetClient(to, out var targetSocket) ||
                targetSocket?.State != System.Net.WebSockets.WebSocketState.Open)
            {
                return MessageRouteResult.Error($"User '{JsonHelper.Escape(to)}' is not online");
            }

            // Forward message with authenticated sender
            var forwarded = AddOrReplaceFrom(messageJson, fromUsername);
            await WebSocketHelper.SendTextAsync(targetSocket, forwarded, ct);

            return MessageRouteResult.Success();
        }
        catch (JsonException)
        {
            return MessageRouteResult.Error("Invalid JSON");
        }
    }

    private static string AddOrReplaceFrom(string json, string fromUser)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();

            // Write/overwrite "from" first
            writer.WriteString("from", fromUser);

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.NameEquals("from")) continue; // overwritten
                prop.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}

public record MessageRouteResult
{
    public bool IsSuccess { get; init; }
    public bool IsPong { get; init; }
    public string? ErrorMessage { get; init; }

    public static MessageRouteResult Success() => new() { IsSuccess = true };
    public static MessageRouteResult Pong() => new() { IsSuccess = true, IsPong = true };
    public static MessageRouteResult Error(string message) => new() { IsSuccess = false, ErrorMessage = message };
}
