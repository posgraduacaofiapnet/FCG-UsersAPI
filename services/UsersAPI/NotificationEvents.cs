using System.Text.Json;

namespace UsersAPI;

public static class NotificationEventTypes
{
    public const string UserCreated = "UserCreated";
}

public sealed record UserCreatedNotification(
    Guid UserId,
    string Name,
    string Email,
    DateTimeOffset CreatedAt);

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset? NextAttemptAt { get; set; }
    public int Attempts { get; set; }

    public static OutboxMessage Create<T>(string eventType, T payload, JsonSerializerOptions options) => new()
    {
        EventType = eventType,
        Payload = JsonSerializer.Serialize(payload, options)
    };
}
