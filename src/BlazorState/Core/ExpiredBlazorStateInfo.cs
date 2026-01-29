namespace BlazorState.Core;

/// <summary>
/// Information about an expired session state entry.
/// </summary>
public sealed class ExpiredBlazorStateInfo
{
    public required string SessionKey { get; init; }
    public required Type StateType { get; init; }
    public required object? Value { get; init; }
}
