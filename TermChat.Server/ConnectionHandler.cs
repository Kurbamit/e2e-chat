using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace TermChat.Server;

public class ConnectionHandler
{
    private readonly ClientRegistry _clientRegistry;
    private readonly MessageRouter _messageRouter;
    private readonly ILogger<ConnectionHandler> _logger;

    public ConnectionHandler(
        ClientRegistry clientRegistry,
        MessageRouter messageRouter,
        ILogger<ConnectionHandler> logger)
    {
        _clientRegistry = clientRegistry;
        _messageRouter = messageRouter;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(WebSocket socket, CancellationToken ct)
    {
        string? username = null;

        try
        {
            // Authenticate client with hello message
            var authResult = await AuthenticateClientAsync(socket, ct);
            if (!authResult.IsAuthenticated)
                return;

            username = authResult.Username!;
            _logger.LogInformation("[+] {Username} connected. Online: {Count}", username, _clientRegistry.Count);

            // Main message loop
            await ProcessMessagesAsync(socket, username, ct);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                _clientRegistry.Unregister(username, socket);
                _logger.LogInformation("[-] {Username} disconnected. Online: {Count}", username, _clientRegistry.Count);
            }

            await WebSocketHelper.CloseAsync(socket, WebSocketCloseStatus.NormalClosure, "Bye");
        }
    }

    private async Task<AuthenticationResult> AuthenticateClientAsync(WebSocket socket, CancellationToken ct)
    {
        // Receive hello message
        var helloJson = await WebSocketHelper.ReceiveTextAsync(socket, ct);
        if (helloJson is null)
            return AuthenticationResult.Failed();

        // Validate hello message
        var validation = MessageValidator.ValidateHello(helloJson);
        if (!validation.IsValid)
        {
            await SendErrorAndCloseAsync(socket, validation.ErrorMessage!,
                WebSocketCloseStatus.PolicyViolation, "Missing hello", ct);
            return AuthenticationResult.Failed();
        }

        var username = validation.Username!;

        // Check if username is already taken
        if (!_clientRegistry.TryRegister(username, socket))
        {
            await SendErrorAndCloseAsync(socket, "Username already connected",
                WebSocketCloseStatus.PolicyViolation, "User taken", ct);
            return AuthenticationResult.Failed();
        }

        // Send hello acknowledgment
        await WebSocketHelper.SendTextAsync(socket,
            $$"""{"type":"hello_ok","user":"{{JsonHelper.Escape(username)}}"}""", ct);

        return AuthenticationResult.Success(username);
    }

    private async Task ProcessMessagesAsync(WebSocket socket, string username, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var messageJson = await WebSocketHelper.ReceiveTextAsync(socket, ct);
                if (messageJson is null)
                    break;

                var routeResult = await _messageRouter.RouteMessageAsync(messageJson, username, ct);

                if (routeResult.IsPong)
                {
                    await WebSocketHelper.SendTextAsync(socket, """{"type":"pong"}""", ct);
                }
                else if (!routeResult.IsSuccess)
                {
                    await WebSocketHelper.SendTextAsync(socket,
                        $$"""{"type":"error","message":"{{routeResult.ErrorMessage}}"}""", ct);
                }
            }
        }
        catch (WebSocketException)
        {
            // Client disconnected abruptly - this is normal, will be handled in finally block
        }
    }

    private async Task SendErrorAndCloseAsync(WebSocket socket, string errorMessage,
        WebSocketCloseStatus closeStatus, string closeDescription, CancellationToken ct)
    {
        await WebSocketHelper.SendTextAsync(socket,
            $$"""{"type":"error","message":"{{errorMessage}}"}""", ct);
        await socket.CloseAsync(closeStatus, closeDescription, ct);
    }
}

public record AuthenticationResult
{
    public bool IsAuthenticated { get; init; }
    public string? Username { get; init; }

    public static AuthenticationResult Success(string username) =>
        new() { IsAuthenticated = true, Username = username };

    public static AuthenticationResult Failed() =>
        new() { IsAuthenticated = false };
}
