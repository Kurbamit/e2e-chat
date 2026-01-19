namespace TermChat.Client;

public class ConsoleUI
{
    private string? _currentRecipient;

    public void ShowWelcome()
    {
        Console.WriteLine("Connected. Type /to <user> to set recipient. /quit to exit.");
    }

    public void ShowConnectionStatus(string wsUrl, string username)
    {
        Console.WriteLine($"Connecting to {wsUrl} as {username} ...");
    }

    public void ShowUsage()
    {
        Console.WriteLine("Usage: dotnet run -- <wsUrl> <username>");
        Console.WriteLine(@"Example: dotnet run -- ws://localhost:5000/ws alice");
    }

    public string GetPrompt()
    {
        return string.IsNullOrWhiteSpace(_currentRecipient)
            ? "> "
            : $"[@{_currentRecipient}]> ";
    }

    public string? GetUserInput()
    {
        Console.Write(GetPrompt());
        return Console.ReadLine()?.Trim();
    }

    public async Task<bool> ProcessCommandAsync(string input, ChatClient client)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // Quit command
        if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Set recipient
        if (input.StartsWith("/to ", StringComparison.OrdinalIgnoreCase))
        {
            _currentRecipient = input.Substring(4).Trim();
            Console.WriteLine($"Active recipient: {_currentRecipient}");
            return false;
        }

        // Direct message
        if (input.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessDirectMessageAsync(input, client);
            return false;
        }

        // Ping
        if (input.Equals("/ping", StringComparison.OrdinalIgnoreCase))
        {
            await client.SendPingAsync();
            return false;
        }

        // Regular message to current recipient
        if (string.IsNullOrWhiteSpace(_currentRecipient))
        {
            Console.WriteLine("No active recipient. Use /to <user> or /msg <user> <text>.");
            return false;
        }

        await client.SendMessageAsync(_currentRecipient, input);
        return false;
    }

    private async Task ProcessDirectMessageAsync(string input, ChatClient client)
    {
        var rest = input.Substring(5);
        var firstSpace = rest.IndexOf(' ');

        if (firstSpace <= 0)
        {
            Console.WriteLine("Usage: /msg <user> <text>");
            return;
        }

        var to = rest.Substring(0, firstSpace).Trim();
        var text = rest.Substring(firstSpace + 1);

        await client.SendMessageAsync(to, text);
    }
}
