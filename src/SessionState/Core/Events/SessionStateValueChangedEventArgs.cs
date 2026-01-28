namespace SessionState.Core.Events;

/// <summary>
/// Event arguments for when a property changes within a stored object.
/// </summary>
public sealed class SessionStateValueChangedEventArgs : SessionStateEventArgs
{
    /// <summary>
    /// The property name that changed.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// The current value of the state object.
    /// </summary>
    public required object Value { get; init; }
}
