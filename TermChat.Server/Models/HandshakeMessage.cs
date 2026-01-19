namespace TermChat.Server.Models;

public record HandshakeMessage
{
    public string Type { get; init; } = "handshake1";
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
}
