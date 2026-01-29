using System.Diagnostics.CodeAnalysis;

namespace BlazorState.Core;

/// <summary>
/// Cross-transport state storage for Blazor Server applications.
/// Provides a unified state container accessible from HTTP, SignalR, and SSE contexts.
/// </summary>
/// <typeparam name="T">
/// The type of state to store. Must implement <see cref="IBlazorStateType{T}"/>.
/// </typeparam>
public abstract class BlazorState<T> where T : class, IBlazorStateType<T>
{
    /// <summary>
    /// Internal constructor to prevent external inheritance.
    /// </summary>
    protected internal BlazorState()
    {
    }

    /// <summary>
    /// The current state value. Null if not initialised.
    /// Setting this value will persist it and raise events only if the value has changed.
    /// </summary>
    /// <remarks>
    /// For thread-safe check-and-get operations, use <see cref="TryGetValue"/> instead.
    /// </remarks>
    public abstract T? Value { get; set; }

    /// <summary>
    /// Whether state has been initialised.
    /// </summary>
    /// <remarks>
    /// Note: The value may change between checking <see cref="HasValue"/> and accessing <see cref="Value"/>.
    /// For thread-safe check-and-get operations, use <see cref="TryGetValue"/> instead.
    /// </remarks>
    public abstract bool HasValue { get; }

    /// <summary>
    /// Atomically checks if a value exists and retrieves it.
    /// </summary>
    /// <param name="value">The value if it exists, otherwise null.</param>
    /// <returns>True if a value exists, false otherwise.</returns>
    public abstract bool TryGetValue([NotNullWhen(true)] out T? value);

    /// <summary>
    /// Raised when the state changes (set, cleared, property changed, or invalidated by external update).
    /// Subscribe to this in components to react to changes.
    /// </summary>
    public abstract event EventHandler<BlazorStateChangedEventArgs<T>>? Changed;

    /// <summary>
    /// Initialise or replace the state value asynchronously.
    /// Only persists and raises events if the value has changed.
    /// </summary>
    /// <param name="value">The value to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the value was changed, false if it was equal to the existing value.</returns>
    public abstract ValueTask<bool> SetAsync(T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear the state entirely.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public abstract ValueTask ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Extend expiry without modification.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public abstract ValueTask RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the local cache, forcing the next access to reload from the backend.
    /// Called automatically when another instance updates the same session/type.
    /// </summary>
    internal abstract void InvalidateCache();

    /// <summary>
    /// Implicit conversion to the underlying value type.
    /// Returns null if no value is set.
    /// </summary>
    public static implicit operator T?(BlazorState<T> state) => state.Value;
}
