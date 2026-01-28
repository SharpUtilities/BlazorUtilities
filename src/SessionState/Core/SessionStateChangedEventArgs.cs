namespace SessionState.Core;

/// <summary>
/// Event arguments for SessionState changes.
/// </summary>
/// <typeparam name="T">The type of state.</typeparam>
public sealed class SessionStateChangedEventArgs<T> : EventArgs where T : class
{
    /// <summary>
    /// The type of change that occurred.
    /// </summary>
    public required SessionStateChangeType ChangeType { get; init; }

    /// <summary>
    /// The current value after the change. Null if cleared.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// The previous value before the change. Null if this was a new value.
    /// </summary>
    public T? PreviousValue { get; init; }

    /// <summary>
    /// The property name that changed. Only set when ChangeType is PropertyChanged.
    /// </summary>
    public string? PropertyName { get; init; }
}
