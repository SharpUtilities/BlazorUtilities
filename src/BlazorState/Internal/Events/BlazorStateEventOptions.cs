namespace BlazorState.Internal.Events;

/// <summary>
/// Options for configuring BlazorState event handlers.
/// </summary>
internal sealed class BlazorStateEventOptions
{
    /// <summary>
    /// Called when any value is set.
    /// </summary>
    public Action<BlazorStateValueSetEventArgs>? OnValueSet { get; set; }

    /// <summary>
    /// Called when any value is cleared.
    /// </summary>
    public Action<BlazorStateValueClearedEventArgs>? OnValueCleared { get; set; }

    /// <summary>
    /// Called when any value is refreshed.
    /// </summary>
    public Action<BlazorStateValueRefreshedEventArgs>? OnValueRefreshed { get; set; }

    /// <summary>
    /// Called when a property changes within a stored object.
    /// </summary>
    public Action<BlazorStateValueChangedEventArgs>? OnValueChanged { get; set; }

    /// <summary>
    /// Called for backend-level operations.
    /// Essential for distributed scenarios where changes may come from other nodes.
    /// </summary>
    public Action<BlazorStateBackendEventArgs>? OnBackendOperation { get; set; }
}
