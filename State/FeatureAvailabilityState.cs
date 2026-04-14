namespace DartHost.App.State;

public sealed record FeatureAvailabilityState(
    bool TaskControlAvailable,
    bool StepControlAvailable,
    bool ManualControlAvailable,
    bool DeviceRawStateAvailable,
    bool VisualFeedbackAvailable)
{
    public static FeatureAvailabilityState Default =>
        new(
            TaskControlAvailable: false,
            StepControlAvailable: false,
            ManualControlAvailable: false,
            DeviceRawStateAvailable: false,
            VisualFeedbackAvailable: false);
}
