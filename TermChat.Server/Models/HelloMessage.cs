namespace TermChat.Server.Models;

public record HelloMessage
{
    public string Type => "hello";
    public string User { get; init; } = string.Empty;
}

public record HelloOkMessage
{
    public string Type => "hello_ok";
    public string User { get; init; } = string.Empty;
}
