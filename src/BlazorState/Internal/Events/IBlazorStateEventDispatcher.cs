namespace BlazorState.Internal.Events;

/// <summary>
/// Dispatches BlazorState events to registered handlers.
/// Subscribe to these events to react to state changes across your application.
/// </summary>
internal interface IBlazorStateEventDispatcher
{
    /// <summary>
    /// Raised when a value is set.
    /// </summary>
    event EventHandler<BlazorStateValueSetEventArgs>? ValueSet;

    /// <summary>
    /// Raised when a value is cleared.
    /// </summary>
    event EventHandler<BlazorStateValueClearedEventArgs>? ValueCleared;

    /// <summary>
    /// Raised when a value is refreshed.
    /// </summary>
    event EventHandler<BlazorStateValueRefreshedEventArgs>? ValueRefreshed;

    /// <summary>
    /// Raised when a property changes within a stored object.
    /// </summary>
    event EventHandler<BlazorStateValueChangedEventArgs>? ValueChanged;

    /// <summary>
    /// Raised for backend-level operations.
    /// Essential for cache invalidation and distributed scenarios.
    /// </summary>
    event EventHandler<BlazorStateBackendEventArgs>? BackendOperation;
}
