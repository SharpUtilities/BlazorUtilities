namespace SessionAndState.Core;

/// <summary>
/// Information about a removed session state entry.
/// </summary>
public sealed class RemovedSessionStateInfo
{
    public required string SessionKey { get; init; }
    public required Type StateType { get; init; }
    public required object? Value { get; init; }
}
