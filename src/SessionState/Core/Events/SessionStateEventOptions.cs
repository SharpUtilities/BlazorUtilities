namespace SessionState.Core.Events;

/// <summary>
/// Options for configuring SessionState event handlers.
/// </summary>
public sealed class SessionStateEventOptions
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

    /// <summary>
    /// Whether to track property changes on stored objects.
    /// Default: true
    /// </summary>
    public bool TrackPropertyChanges { get; set; } = true;

    /// <summary>
    /// Whether to automatically persist changes when a property changes.
    /// Default: true
    /// </summary>
    public bool AutoPersistOnPropertyChange { get; set; } = true;
}
