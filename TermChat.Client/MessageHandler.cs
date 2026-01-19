using System.Text.Json;

namespace TermChat.Client;

public class MessageHandler
{
    private readonly Func<string>? _getPrompt;

    public MessageHandler(Func<string>? getPrompt = null)
    {
        _getPrompt = getPrompt;
    }

    public void HandleIncomingMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;

            switch (type)
            {
                case "hello_ok":
                    DisplayServerMessage("hello_ok");
                    break;

                case "error":
                    var errorMsg = root.TryGetProperty("message", out var m) ? m.GetString() : "unknown error";
                    DisplayError(errorMsg);
                    break;

                case "pong":
                    DisplayServerMessage("pong");
                    break;

                case "msg":
                    var msgFrom = root.TryGetProperty("from", out var mf) ? mf.GetString() : "?";
                    var msgText = root.TryGetProperty("text", out var mt) ? mt.GetString() : "";
                    DisplayTextMessage(msgFrom, msgText);
                    break;

                case "handshake1":
                case "handshake2":
                    var from = root.TryGetProperty("from", out var f) ? f.GetString() : "?";
                    DisplayHandshakeMessage(type, from, root);
                    break;

                default:
                    DisplayRawMessage(json);
                    break;
            }
        }
        catch (JsonException)
        {
            DisplayNonJsonMessage(json);
        }
    }

    private void DisplayServerMessage(string message)
    {
        Console.WriteLine($"[server] {message}");
    }

    private void DisplayError(string? message)
    {
        Console.WriteLine($"[server error] {message}");
    }

    private void DisplayTextMessage(string from, string? text)
    {
        Console.WriteLine($"\n[{from}] {text}");
        if (_getPrompt != null)
            Console.Write(_getPrompt());
    }

    private void DisplayHandshakeMessage(string type, string from, JsonElement root)
    {
        // For handshake messages, show more details
        var json = root.ToString();
        Console.WriteLine($"\n[{type} from {from}] {json}");
        if (_getPrompt != null)
            Console.Write(_getPrompt());
    }

    private void DisplayRawMessage(string json)
    {
        Console.WriteLine($"\n[recv] {json}");
        if (_getPrompt != null)
            Console.Write(_getPrompt());
    }

    private void DisplayNonJsonMessage(string json)
    {
        Console.WriteLine($"\n[recv non-json] {json}\n> ");
    }
}
