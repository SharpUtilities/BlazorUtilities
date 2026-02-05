using System.Collections.Concurrent;
using SessionAndState.Core;

namespace SessionAndState.Internal;

/// <summary>
/// Scoped implementation of <see cref="ISessionStateLockManager"/>.
/// Uses a ConcurrentDictionary to track held locks within the session scope.
/// </summary>
internal sealed class SessionStateLockManager : ISessionStateLockManager, ISessionStateLockReleaser, IDisposable
{
    private readonly ConcurrentDictionary<string, byte> _locks = new();
    private bool _disposed;

    public bool TryAcquireLock(string name, out SessionStateLock lockHandle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_disposed)
        {
            lockHandle = SessionStateLock.Empty;
            return false;
        }

        if (_locks.TryAdd(name, 0))
        {
            lockHandle = new SessionStateLock(name, this);
            return true;
        }

        lockHandle = SessionStateLock.Empty;
        return false;
    }

    public bool TryAcquireLock(string name, TimeSpan timeout, out SessionStateLock lockHandle)
    {
        // Simple implementation: just try once, ignore timeout
        // Can be upgraded to proper waiting implementation later
        return TryAcquireLock(name, out lockHandle);
    }

    public ValueTask<SessionStateLock> AcquireLockAsync(string name, CancellationToken cancellationToken = default)
    {
        // Simple implementation: try once, throw if unavailable
        // Can be upgraded to proper async waiting implementation later
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SessionStateLockManager));
        }

        if (_locks.TryAdd(name, 0))
        {
            return ValueTask.FromResult(new SessionStateLock(name, this));
        }

        throw new InvalidOperationException($"Lock '{name}' is already held.");
    }

    public ValueTask<SessionStateLock> TryAcquireLockAsync(string name, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        // Simple implementation: just try once, ignore timeout
        // Can be upgraded to proper async waiting implementation later
        if (TryAcquireLock(name, out var handle))
        {
            return ValueTask.FromResult(handle);
        }

        return ValueTask.FromResult(SessionStateLock.Empty);
    }

    public bool IsLocked(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _locks.ContainsKey(name);
    }

    void ISessionStateLockReleaser.ReleaseLock(string name)
    {
        _locks.TryRemove(name, out _);
    }

    public void Dispose()
    {
        _disposed = true;
        _locks.Clear();
    }
}
