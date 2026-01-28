namespace SessionState.Core;

/// <summary>
/// Information about an expired session state entry.
/// </summary>
public sealed class ExpiredSessionStateInfo
{
    public required string SessionKey { get; init; }
    public required Type StateType { get; init; }
    public required object? Value { get; init; }
}
