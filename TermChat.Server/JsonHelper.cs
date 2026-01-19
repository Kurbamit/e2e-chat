namespace TermChat.Server;

public static class JsonHelper
{
    public static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
