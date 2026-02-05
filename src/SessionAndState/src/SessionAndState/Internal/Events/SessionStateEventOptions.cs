namespace SessionAndState.Internal.Events;

/// <summary>
/// Options for configuring SessionAndState event handlers.
/// </summary>
internal sealed class SessionStateEventOptions
{
    /// <summary>
    /// Called when any value is set.
    /// </summary>
    public Action<SessionStateValueSetEventArgs>? OnValueSet { get; set; }

    /// <summary>
    /// Called when any value is cleared.
    /// </summary>
    public Action<SessionStateValueClearedEventArgs>? OnValueCleared { get; set; }

    /// <summary>
    /// Called when any value is refreshed.
    /// </summary>
    public Action<SessionStateValueRefreshedEventArgs>? OnValueRefreshed { get; set; }

    /// <summary>
    /// Called when a property changes within a stored object.
    /// </summary>
    public Action<SessionStateValueChangedEventArgs>? OnValueChanged { get; set; }

    /// <summary>
    /// Called for backend-level operations.
    /// Essential for distributed scenarios where changes may come from other nodes.
    /// </summary>
    public Action<SessionStateBackendEventArgs>? OnBackendOperation { get; set; }
}
