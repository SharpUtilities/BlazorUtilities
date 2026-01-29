namespace BlazorState.Internal.Events;

/// <summary>
/// Event arguments for backend-level operations.
/// </summary>
public sealed class BlazorStateBackendEventArgs : BlazorStateEventArgs
{
    /// <summary>
    /// The type of backend operation.
    /// </summary>
    public required BlazorStateBackendOperation Operation { get; init; }

    /// <summary>
    /// The source instance that triggered this operation.
    /// Used for distributed cache invalidation - caches can ignore events from themselves.
    /// <see cref="BlazorStateSourceInstance.None"/> for in-memory/single-server scenarios
    /// where all caches should be notified.
    /// </summary>
    public BlazorStateSourceInstance SourceInstance { get; init; }
}
