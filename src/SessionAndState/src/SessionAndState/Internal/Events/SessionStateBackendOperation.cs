namespace SessionAndState.Internal.Events;

/// <summary>
/// Types of backend operations.
/// </summary>
public enum SessionStateBackendOperation
{
    Set,
    Remove,
    Refresh,
    Expired,
    SessionCleared
}
