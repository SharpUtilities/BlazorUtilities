using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using BlazorState.Core;

namespace BlazorState.Internal;

/// <summary>
/// Scoped implementation of <see cref="IBlazorStateLockManager"/>.
/// Uses a ConcurrentDictionary to track held locks within the session scope.
/// </summary>
internal sealed class BlazorStateLockManager : IBlazorStateLockManager, IBlazorStateLockReleaser, IDisposable
{
    private readonly ConcurrentDictionary<string, byte> _locks = new();
    private bool _disposed;

    public bool TryAcquireLock(string name, out BlazorStateLock? lockHandle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_disposed)
        {
            lockHandle = null;
            return false;
        }

        if (_locks.TryAdd(name, 0))
        {
            lockHandle = new BlazorStateLock(name, this);
            return true;
        }

        lockHandle = null;
        return false;
    }

    public bool TryAcquireLock(string name, TimeSpan timeout, [NotNullWhen(true)] out BlazorStateLock? lockHandle)
    {
        // Simple implementation: just try once, ignore timeout
        // Can be upgraded to proper waiting implementation later
        return TryAcquireLock(name, out lockHandle);
    }

    public ValueTask<BlazorStateLock> AcquireLockAsync(string name, CancellationToken cancellationToken = default)
    {
        // Simple implementation: try once, throw if unavailable
        // Can be upgraded to proper async waiting implementation later
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BlazorStateLockManager));
        }

        if (_locks.TryAdd(name, 0))
        {
            return ValueTask.FromResult(new BlazorStateLock(name, this));
        }

        throw new InvalidOperationException($"Lock '{name}' is already held.");
    }

    public ValueTask<BlazorStateLock?> TryAcquireLockAsync(string name, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        // Simple implementation: just try once, ignore timeout
        // Can be upgraded to proper async waiting implementation later
        if (TryAcquireLock(name, out var handle))
        {
            return ValueTask.FromResult(handle);
        }

        return ValueTask.FromResult<BlazorStateLock?>(null);
    }

    public bool IsLocked(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _locks.ContainsKey(name);
    }

    void IBlazorStateLockReleaser.ReleaseLock(string name)
    {
        _locks.TryRemove(name, out _);
    }

    public void Dispose()
    {
        _disposed = true;
        _locks.Clear();
    }
}
