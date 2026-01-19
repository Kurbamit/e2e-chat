namespace TermChat.Server.Models;

public record ErrorMessage
{
    public string Type => "error";
    public string Message { get; init; } = string.Empty;
}

public record PongMessage
{
    public string Type => "pong";
}
