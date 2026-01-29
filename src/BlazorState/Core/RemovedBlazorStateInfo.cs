namespace BlazorState.Core;

/// <summary>
/// Information about a removed session state entry.
/// </summary>
public sealed class RemovedBlazorStateInfo
{
    public required string SessionKey { get; init; }
    public required Type StateType { get; init; }
    public required object? Value { get; init; }
}
