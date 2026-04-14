using System.Text.Json.Serialization;

namespace DartHost.App.Transport;

public enum DartConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Faulted
}

public sealed record DartClientOptions(
    string Host = "127.0.0.1",
    int Port = 37601,
    string ClientName = "dart-host-app",
    string ClientVersion = "0.3.0",
    IReadOnlyList<string>? Capabilities = null)
{
    public IReadOnlyList<string> EffectiveCapabilities =>
        Capabilities is { Count: > 0 } ? Capabilities : ["command", "state_subscribe"];
}

public sealed record HelloAckPayload(
    [property: JsonPropertyName("server_name")] string ServerName,
    [property: JsonPropertyName("server_version")] string ServerVersion,
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("heartbeat_interval_ms")] int HeartbeatIntervalMs,
    [property: JsonPropertyName("state_push_interval_ms")] int StatePushIntervalMs,
    [property: JsonPropertyName("supported_commands")] IReadOnlyList<string> SupportedCommands);

public sealed record CommandAckPayload(
    [property: JsonPropertyName("accepted")] bool Accepted,
    [property: JsonPropertyName("command_name")] string CommandName);

public sealed record QueueItemDto(
    [property: JsonPropertyName("task_name")] string TaskName,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("status")] string Status);

public sealed record LastErrorDto(
    [property: JsonPropertyName("task_name")] string TaskName,
    [property: JsonPropertyName("action_name")] string ActionName,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("timestamp_ms")] long TimestampMs);

public sealed record ManagerStatePayload(
    [property: JsonPropertyName("lifecycle_state")] string LifecycleState,
    [property: JsonPropertyName("current_task")] string CurrentTask,
    [property: JsonPropertyName("current_action")] string CurrentAction,
    [property: JsonPropertyName("fire_count")] uint FireCount,
    [property: JsonPropertyName("queue")] IReadOnlyList<QueueItemDto> Queue,
    [property: JsonPropertyName("last_error")] LastErrorDto? LastError);

public sealed record HeartbeatPayload(
    [property: JsonPropertyName("session_id")] string SessionId);

public sealed record ErrorPayload(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message);

public sealed class DartProtocolException : Exception
{
    public DartProtocolException(string code, string message)
        : base($"Protocol error: {code}: {message}")
    {
        Code = code;
    }

    public string Code { get; }
}
