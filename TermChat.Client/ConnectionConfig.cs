namespace TermChat.Client;

public record ConnectionConfig(string WsUrl, string Username)
{
    public static ConnectionConfig? ParseFromArgs(string[] args)
    {
        if (args.Length < 2)
            return null;

        return new ConnectionConfig(args[0], args[1]);
    }
}
