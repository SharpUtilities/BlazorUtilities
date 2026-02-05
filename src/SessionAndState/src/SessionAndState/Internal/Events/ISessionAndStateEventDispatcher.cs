namespace SessionAndState.Internal.Events;

/// <summary>
/// Dispatches SessionAndState events to registered handlers.
/// Subscribe to these events to react to state changes across your application.
/// </summary>
internal interface ISessionAndStateEventDispatcher
{
    /// <summary>
    /// Raised when a value is set.
    /// </summary>
    event EventHandler<SessionStateValueSetEventArgs>? ValueSet;

    /// <summary>
    /// Raised when a value is cleared.
    /// </summary>
    event EventHandler<SessionStateValueClearedEventArgs>? ValueCleared;

    /// <summary>
    /// Raised when a value is refreshed.
    /// </summary>
    event EventHandler<SessionStateValueRefreshedEventArgs>? ValueRefreshed;

    /// <summary>
    /// Raised when a property changes within a stored object.
    /// </summary>
    event EventHandler<SessionStateValueChangedEventArgs>? ValueChanged;

    /// <summary>
    /// Raised for backend-level operations.
    /// Essential for cache invalidation and distributed scenarios.
    /// </summary>
    event EventHandler<SessionStateBackendEventArgs>? BackendOperation;
}
