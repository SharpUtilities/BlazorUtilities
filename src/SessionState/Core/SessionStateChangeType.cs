namespace SessionState.Core;

/// <summary>
/// Types of changes that can occur to SessionState.
/// </summary>
public enum SessionStateChangeType
{
    /// <summary>
    /// A new value was set or an existing value was replaced.
    /// </summary>
    Set,

    /// <summary>
    /// The value was cleared/removed.
    /// </summary>
    Cleared,

    /// <summary>
    /// A property within the value changed (via INotifyPropertyChanged).
    /// </summary>
    PropertyChanged,

    /// <summary>
    /// The cache was invalidated due to an external update (e.g., another tab/session).
    /// The value will be reloaded on next access.
    /// </summary>
    Invalidated
}
