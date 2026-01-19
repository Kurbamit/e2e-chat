namespace TermChat.Server.Models;

public record TextMessage
{
    public string Type { get; init; } = "msg";
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
}
