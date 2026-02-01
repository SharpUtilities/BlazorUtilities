namespace BlazorState.Core;

/// <summary>
/// Represents an acquired lock. Dispose or call Release() to release the lock.
/// </summary>
public sealed class  BlazorStateLock : IDisposable
{
    private readonly IBlazorStateLockReleaser? _releaser;
    private readonly string? _name;

    internal BlazorStateLock(string name, IBlazorStateLockReleaser releaser)
    {
        _name = name;
        _releaser = releaser;
        IsAcquired = true;
    }

    /// <summary>
    /// Whether the lock was successfully acquired.
    /// </summary>
    public bool IsAcquired { get; }

    /// <summary>
    /// The name of the lock.
    /// </summary>
    public string? Name => _name;

    /// <summary>
    /// Releases the lock. Safe to call multiple times.
    /// </summary>
    public void Release()
    {
        if (IsAcquired && _name is not null)
        {
            _releaser?.ReleaseLock(_name);
        }
    }

    /// <summary>
    /// Releases the lock.
    /// </summary>
    public void Dispose() => Release();
}
