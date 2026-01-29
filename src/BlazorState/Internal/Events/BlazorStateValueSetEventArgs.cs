namespace BlazorState.Internal.Events;

/// <summary>
/// Event arguments for when a value is set.
/// </summary>
public sealed class BlazorStateValueSetEventArgs : BlazorStateEventArgs
{
    /// <summary>
    /// The value that was set.
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// Whether this was a new value or an update to an existing one.
    /// </summary>
    public required bool IsUpdate { get; init; }
}
