namespace SessionState.Core.Events;

/// <summary>
/// Event arguments for backend-level operations.
/// </summary>
public sealed class SessionStateBackendEventArgs : SessionStateEventArgs
{
    /// <summary>
    /// The type of backend operation.
    /// </summary>
    public required SessionStateBackendOperation Operation { get; init; }

    /// <summary>
    /// The source instance that triggered this operation.
    /// Used for distributed cache invalidation - caches can ignore events from themselves.
    /// <see cref="SessionStateSourceInstance.None"/> for in-memory/single-server scenarios
    /// where all caches should be notified.
    /// </summary>
    public SessionStateSourceInstance SourceInstance { get; init; }
}
