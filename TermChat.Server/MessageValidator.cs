using System.Text.Json;

namespace TermChat.Server;

public static class MessageValidator
{
    public static HelloValidationResult ValidateHello(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "hello")
            {
                return HelloValidationResult.Error("First message must be hello with user");
            }

            if (!root.TryGetProperty("user", out var userProp))
            {
                return HelloValidationResult.Error("First message must be hello with user");
            }

            var username = userProp.GetString();
            if (string.IsNullOrWhiteSpace(username))
            {
                return HelloValidationResult.Error("Invalid username");
            }

            return HelloValidationResult.Success(username);
        }
        catch (JsonException)
        {
            return HelloValidationResult.Error("Invalid JSON in hello message");
        }
    }
}

public record HelloValidationResult
{
    public bool IsValid { get; init; }
    public string? Username { get; init; }
    public string? ErrorMessage { get; init; }

    public static HelloValidationResult Success(string username) =>
        new() { IsValid = true, Username = username };

    public static HelloValidationResult Error(string message) =>
        new() { IsValid = false, ErrorMessage = message };
}
