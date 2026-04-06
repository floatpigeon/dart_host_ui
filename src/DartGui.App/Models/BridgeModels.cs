using System.Text.Json.Serialization;

namespace DartGui.App.Models;

public sealed class BridgeStateSnapshot
{
    [JsonPropertyName("manager")]
    public BridgeManagerState Manager { get; set; } = new();

    [JsonPropertyName("feedback")]
    public BridgeFeedbackState Feedback { get; set; } = new();
}

public sealed class BridgeManagerState
{
    [JsonPropertyName("lifecycle_state")]
    public string LifecycleState { get; set; } = string.Empty;

    [JsonPropertyName("current_task")]
    public string CurrentTask { get; set; } = string.Empty;

    [JsonPropertyName("current_action")]
    public string CurrentAction { get; set; } = string.Empty;

    [JsonPropertyName("fire_count")]
    public int FireCount { get; set; }

    [JsonPropertyName("queue")]
    public List<BridgeQueueItem> Queue { get; set; } = [];

    [JsonPropertyName("last_error")]
    public BridgeFaultInfo? LastError { get; set; }
}

public sealed class BridgeQueueItem
{
    [JsonPropertyName("task_name")]
    public string TaskName { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public sealed class BridgeFeedbackState
{
    [JsonPropertyName("belt")]
    public BridgeBeltFeedback Belt { get; set; } = new();

    [JsonPropertyName("lift")]
    public BridgeLiftFeedback Lift { get; set; } = new();
}

public sealed class BridgeBeltFeedback
{
    [JsonPropertyName("left_velocity")]
    public double LeftVelocity { get; set; }

    [JsonPropertyName("right_velocity")]
    public double RightVelocity { get; set; }

    [JsonPropertyName("left_torque")]
    public double LeftTorque { get; set; }

    [JsonPropertyName("right_torque")]
    public double RightTorque { get; set; }
}

public sealed class BridgeLiftFeedback
{
    [JsonPropertyName("left_velocity")]
    public double LeftVelocity { get; set; }

    [JsonPropertyName("right_velocity")]
    public double RightVelocity { get; set; }
}

public sealed class BridgeFaultInfo
{
    [JsonPropertyName("task_name")]
    public string TaskName { get; set; } = string.Empty;

    [JsonPropertyName("action_name")]
    public string ActionName { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("timestamp_ms")]
    public long TimestampMs { get; set; }
}

public sealed class BridgeCommandAckMessage
{
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public readonly record struct BridgeCommandAck(bool Accepted, string Reason);
