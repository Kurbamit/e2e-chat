using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run -- <wsUrl> <username>");
    Console.WriteLine(@"Example: dotnet run -- ws://localhost:5000/ws alice");
    return;
}

var wsUrl = args[0];
var username = args[1];

using var ws = new ClientWebSocket();
Console.WriteLine($"Connecting to {wsUrl} as {username}...");
await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

// Send hello
await SendJsonAsync(ws, new { type = "hello", user = username });
Console.WriteLine("Sent hello. You can now send messages as JSON.");
Console.WriteLine(@"Try: /to bob Hello there");
Console.WriteLine(@"Type /quit to exit.");
Console.WriteLine();

var cts = new CancellationTokenSource();

// Background receive loop
var recvTask = Task.Run(async () =>
{
    try
    {
        while (!cts.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var text = await ReceiveTextAsync(ws, cts.Token);
            if (text == null) break;

            Console.WriteLine();
            Console.WriteLine("<< " + text);
            Console.Write("> ");
        }
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
        Console.WriteLine($"Receive error: {ex.Message}");
    }
});

// Input loop
while (ws.State == WebSocketState.Open)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line == null) break;

    if (line.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        break;

    if (line.StartsWith("/to ", StringComparison.OrdinalIgnoreCase))
    {
        // Format: /to bob message text...
        var rest = line.Substring(4).Trim();
        var firstSpace = rest.IndexOf(' ');
        if (firstSpace <= 0)
        {
            Console.WriteLine("Use: /to <user> <message>");
            continue;
        }

        var to = rest.Substring(0, firstSpace);
        var body = rest.Substring(firstSpace + 1);

        await SendJsonAsync(ws, new
        {
            type = "msg",
            to,
            body,
            ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });

        continue;
    }

    Console.WriteLine("Commands:");
    Console.WriteLine("  /to <user> <message>");
    Console.WriteLine("  /quit");
}

cts.Cancel();
try { await recvTask; } catch { /* ignore */ }

if (ws.State == WebSocketState.Open)
{
    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
}

static async Task SendJsonAsync(ClientWebSocket ws, object payload)
{
    var json = JsonSerializer.Serialize(payload);
    var bytes = Encoding.UTF8.GetBytes(json);
    await ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
}

static async Task<string?> ReceiveTextAsync(ClientWebSocket ws, CancellationToken ct)
{
    var buffer = new byte[8 * 1024];
    using var ms = new MemoryStream();

    while (!ct.IsCancellationRequested)
    {
        var result = await ws.ReceiveAsync(buffer, ct);

        if (result.MessageType == WebSocketMessageType.Close)
            return null;

        ms.Write(buffer, 0, result.Count);

        if (result.EndOfMessage)
            break;
    }

    return Encoding.UTF8.GetString(ms.ToArray());
}
