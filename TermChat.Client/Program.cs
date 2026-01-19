using TermChat.Client;

// Parse command line arguments
var config = ConnectionConfig.ParseFromArgs(args);
if (config is null)
{
    var ui = new ConsoleUI();
    ui.ShowUsage();
    return;
}

// Initialize components
var consoleUI = new ConsoleUI();
var messageHandler = new MessageHandler(consoleUI.GetPrompt);
await using var client = new ChatClient(config.Username);

// Set up message handler
client.OnMessageReceived += messageHandler.HandleIncomingMessage;

// Connect to server
consoleUI.ShowConnectionStatus(config.WsUrl, config.Username);
await client.ConnectAsync(config.WsUrl);
consoleUI.ShowWelcome();

// Main input loop
while (client.State == System.Net.WebSockets.WebSocketState.Open)
{
    var input = consoleUI.GetUserInput();
    if (input is null) break;

    var shouldQuit = await consoleUI.ProcessCommandAsync(input, client);

    if (shouldQuit)
        break;
}

// Cleanup
await client.DisconnectAsync();
