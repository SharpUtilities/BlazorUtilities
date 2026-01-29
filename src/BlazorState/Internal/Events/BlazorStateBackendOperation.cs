namespace BlazorState.Internal.Events;

/// <summary>
/// Types of backend operations.
/// </summary>
public enum BlazorStateBackendOperation
{
    Set,
    Remove,
    Refresh,
    Expired,
    SessionCleared
}
