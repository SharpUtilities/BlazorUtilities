namespace SessionAndState.Internal.Events;

/// <summary>
/// Event arguments for when a value is cleared/removed.
/// </summary>
public sealed class SessionStateValueClearedEventArgs : SessionStateEventArgs
{
    /// <summary>
    /// The value that was cleared, if available.
    /// </summary>
    public object? PreviousValue { get; init; }
}
