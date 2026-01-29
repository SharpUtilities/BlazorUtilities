namespace BlazorState.Internal.Events;

/// <summary>
/// Base class for BlazorState events.
/// </summary>
public abstract class BlazorStateEventArgs : EventArgs
{
    /// <summary>
    /// The session key associated with this event.
    /// </summary>
    public required string SessionKey { get; init; }

    /// <summary>
    /// The type of state that was affected.
    /// </summary>
    public required Type StateType { get; init; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
