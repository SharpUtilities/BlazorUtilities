using System.ComponentModel;
using BlazorState.Core;
using BlazorState.Internal.Events;

namespace BlazorState.Internal;

/// <summary>
/// Scoped cache holder for a specific session state type.
/// Automatically subscribes to backend events for cache invalidation.
/// </summary>
internal sealed class BlazorStateCache<T> : IDisposable
    where T : class, IBlazorStateType<T>
{
    private readonly BlazorStateEventDispatcher _eventDispatcher;
    private readonly EventHandler<BlazorStateBackendEventArgs> _backendOperationHandler;
    private readonly Guid _instanceId;

    private T? _value;
    private bool _isLoaded;
    private string? _sessionKey;
    private PropertyChangedEventHandler? _propertyChangedHandler;
    private bool _disposed;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    public BlazorStateCache(BlazorStateEventDispatcher eventDispatcher)
    {
        _eventDispatcher = eventDispatcher;
        _instanceId = eventDispatcher.InstanceId;

        _backendOperationHandler = OnBackendOperation;
        _eventDispatcher.BackendOperation += _backendOperationHandler;
    }

    /// <summary>
    /// The cached value, or null if not loaded or invalidated.
    /// </summary>
    public T? Value
    {
        get
        {
            lock (_lock)
            {
                return _value;
            }
        }
    }

    /// <summary>
    /// Whether the cache has been loaded from the backend.
    /// </summary>
    public bool IsLoaded
    {
        get
        {
            lock (_lock)
            {
                return _isLoaded;
            }
        }
    }

    /// <summary>
    /// The session key this cache is associated with, if known.
    /// </summary>
    public string? SessionKey
    {
        get
        {
            lock (_lock)
            {
                return _sessionKey;
            }
        }
    }

    /// <summary>
    /// Raised when the cache is invalidated by an external change.
    /// </summary>
    public event Action? Invalidated;

    /// <summary>
    /// Atomically gets the value and whether it exists.
    /// </summary>
    public (bool HasValue, T? Value) TryGetValue()
    {
        lock (_lock)
        {
            return (_value is not null, _value);
        }
    }

    /// <summary>
    /// Sets the cached value and marks as loaded.
    /// </summary>
    public void Set(string sessionKey, T? value)
    {
        lock (_lock)
        {
            UnsubscribeFromPropertyChangesInternal();
            _sessionKey = sessionKey;
            _value = value;
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Sets the cached value with property change tracking.
    /// </summary>
    public void Set(string sessionKey, T? value, PropertyChangedEventHandler? handler)
    {
        lock (_lock)
        {
            UnsubscribeFromPropertyChangesInternal();
            _sessionKey = sessionKey;
            _value = value;
            _isLoaded = true;

            if (value is not null && handler is not null)
            {
                _propertyChangedHandler = handler;
                value.PropertyChanged += _propertyChangedHandler;
            }
        }
    }

    /// <summary>
    /// Marks the cache as loaded with no value and no session key.
    /// Used when no session key exists yet.
    /// </summary>
    public void MarkLoadedEmpty()
    {
        lock (_lock)
        {
            UnsubscribeFromPropertyChangesInternal();
            _sessionKey = null;
            _value = null;
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Clears the cached value.
    /// </summary>
    public T? Clear()
    {
        lock (_lock)
        {
            var previous = _value;
            UnsubscribeFromPropertyChangesInternal();
            _value = null;
            _isLoaded = true;
            return previous;
        }
    }

    /// <summary>
    /// Invalidates the cache, forcing a reload on next access.
    /// </summary>
    public T? Invalidate()
    {
        T? previous;

        lock (_lock)
        {
            previous = _value;
            UnsubscribeFromPropertyChangesInternal();
            _value = null;
            _isLoaded = false;
        }

        return previous;
    }

    /// <summary>
    /// Gets the current value for persistence without modifying state.
    /// </summary>
    public T? GetForPersistence()
    {
        lock (_lock)
        {
            return _value;
        }
    }

    /// <summary>
    /// Gets both the session key and loaded state atomically.
    /// </summary>
    public (string? SessionKey, bool IsLoaded) GetState()
    {
        lock (_lock)
        {
            return (_sessionKey, _isLoaded);
        }
    }

    private void OnBackendOperation(object? sender, BlazorStateBackendEventArgs args)
    {
        if (_disposed)
        {
            return;
        }

        // Only skip if source instance is set AND matches this instance
        // For in-memory scenarios, source instance has no value so all caches get notified
        if (args.SourceInstance.Matches(_instanceId))
        {
            return;
        }

        if (args.StateType != typeof(T))
        {
            return;
        }

        if (args.Operation is not (BlazorStateBackendOperation.Set
            or BlazorStateBackendOperation.Remove
            or BlazorStateBackendOperation.Expired
            or BlazorStateBackendOperation.SessionCleared))
        {
            return;
        }

        string? currentKey;
        lock (_lock)
        {
            currentKey = _sessionKey;
        }

        if (currentKey is null || currentKey != args.SessionKey)
        {
            return;
        }

        Invalidate();
        Invalidated?.Invoke();
    }

    private void UnsubscribeFromPropertyChangesInternal()
    {
        if (_value is not null && _propertyChangedHandler is not null)
        {
            _value.PropertyChanged -= _propertyChangedHandler;
            _propertyChangedHandler = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _eventDispatcher.BackendOperation -= _backendOperationHandler;
        Invalidated = null;

        lock (_lock)
        {
            UnsubscribeFromPropertyChangesInternal();
        }
    }
}
