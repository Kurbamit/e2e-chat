using TermChat.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to DI container
builder.Services.AddSingleton<ClientRegistry>();
builder.Services.AddScoped<MessageRouter>();
builder.Services.AddScoped<ConnectionHandler>();

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapGet("/", () =>
    Results.Text("TermChat relay server is running. Connect via WebSocket at /ws", "text/plain"));

app.Map("/ws", async (HttpContext context, ConnectionHandler connectionHandler) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket requests only.");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await connectionHandler.HandleConnectionAsync(socket, context.RequestAborted);
});

app.Run();
