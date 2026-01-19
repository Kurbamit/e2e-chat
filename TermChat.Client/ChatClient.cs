using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TermChat.Client;

public class ChatClient : IAsyncDisposable
{
    private readonly ClientWebSocket _ws;
    private readonly string _username;
    private readonly CancellationTokenSource _cts;
    private Task? _receiverTask;

    public event Action<string>? OnMessageReceived;
    public WebSocketState State => _ws.State;

    public ChatClient(string username)
    {
        _username = username;
        _ws = new ClientWebSocket();
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        _cts = new CancellationTokenSource();
    }

    public async Task ConnectAsync(string wsUrl)
    {
        await _ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

        await SendJsonAsync(new
        {
            type = "hello",
            user = _username
        });

        StartReceiving();
    }

    public async Task SendMessageAsync(string to, string text)
    {
        await SendJsonAsync(new
        {
            type = "msg",
            to,
            text
        });
    }

    public async Task SendPingAsync()
    {
        await SendJsonAsync(new { type = "ping" });
    }

    public async Task DisconnectAsync()
    {
        _cts.Cancel();

        if (_ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
            }
            catch { /* ignore */ }
        }

        if (_receiverTask != null)
        {
            try { await _receiverTask; }
            catch { /* ignore */ }
        }
    }

    private void StartReceiving()
    {
        _receiverTask = Task.Run(async () =>
        {
            try
            {
                while (!_cts.IsCancellationRequested && _ws.State == WebSocketState.Open)
                {
                    var json = await ReceiveTextAsync(_cts.Token);
                    if (json is null) break;

                    OnMessageReceived?.Invoke(json);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"Receiver WebSocket error: {ex.Message}");
            }
        }, _cts.Token);
    }

    private async Task SendJsonAsync(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
    }

    private async Task<string?> ReceiveTextAsync(CancellationToken ct)
    {
        var buffer = new byte[8 * 1024];
        using var ms = new MemoryStream();

        while (!ct.IsCancellationRequested)
        {
            var result = await _ws.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            ms.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
                break;
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _ws.Dispose();
        _cts.Dispose();
    }
}
