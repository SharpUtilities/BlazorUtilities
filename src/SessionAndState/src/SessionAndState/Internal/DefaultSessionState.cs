
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionAndState.Core;
using SessionAndState.Core.Options;
using SessionAndState.Internal.Events;

namespace SessionAndState.Internal;

/// <summary>
/// Default implementation of <see cref="SessionState{T}"/>.
/// Thread-safe using SemaphoreSlim for async operations.
/// </summary>
internal sealed partial class DefaultSessionState<T> : SessionState<T>, IDisposable
    where T : class, ISessionStateType<T>
{
    private readonly SemaphoreSlim _asyncLock = new(1, 1);

    private readonly SessionStateCache<T> _cache;
    private readonly ISessionAndStateBackend _backend;
    private readonly ISessionAndStateKeyAccessor _keyAccessor;
    private readonly ILogger<DefaultSessionState<T>> _logger;
    private readonly SessionStateEventDispatcher _eventDispatcher;
    private readonly SessionAndStateOptions _options;

    private bool _disposed;

    private EventHandler<SessionStateChangedEventArgs<T>>? _changed;

    public DefaultSessionState(
        SessionStateCache<T> cache,
        ISessionAndStateBackend backend,
        ISessionAndStateKeyAccessor keyAccessor,
        ILogger<DefaultSessionState<T>> logger,
        SessionAndStatePipelineMarker pipelineMarker,
        SessionStateEventDispatcher eventDispatcher,
        IOptions<SessionAndStateOptions> options)
    {
        _cache = cache;
        _backend = backend;
        _keyAccessor = keyAccessor;
        _logger = logger;
        _eventDispatcher = eventDispatcher;
        _options = options.Value;

        pipelineMarker.EnsureConfigured();

        _cache.Invalidated += OnCacheInvalidated;
    }

    public override event EventHandler<SessionStateChangedEventArgs<T>>? Changed
    {
        add => _changed += value;
        remove => _changed -= value;
    }

    public override T? Value
    {
        get
        {
            EnsureLoadedSync();
            return _cache.Value;
        }
        set
        {
            if (value is null)
            {
                RunSync(ClearAsync(CancellationToken.None));
            }
            else
            {
                RunSync(SetAsync(value, CancellationToken.None));
            }
        }
    }

    public override bool HasValue
    {
        get
        {
            EnsureLoadedSync();
            return _cache.Value is not null;
        }
    }

    public override bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        EnsureLoadedSync();
        var (hasValue, sessionStateType) = _cache.TryGetValue();
        value = sessionStateType;
        return hasValue;
    }

    public override async ValueTask<bool> SetAsync(T value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var key = GetRequiredSessionKey();

        SessionStateChangedEventArgs<T>? args;
        bool result;

        await _asyncLock.WaitAsync(cancellationToken);
        try
        {
            await LoadCoreAsync(key, cancellationToken);

            var currentValue = _cache.Value;
            if (currentValue is not null && currentValue.Equals(value))
            {
                LogSkippedSetDueToEquality(typeof(T).Name);
                return false;
            }

            var isUpdate = currentValue is not null;

            await _backend.SetAsync(key, value, cancellationToken);

            if (_options.TrackPropertyChanges)
            {
                _cache.Set(key, value, CreatePropertyChangedHandler(key));
            }
            else
            {
                _cache.Set(key, value);
            }

            LogStateSet(typeof(T).Name, FormatKey(key));

            _eventDispatcher.RaiseValueSet(key, typeof(T), value, isUpdate);
            _eventDispatcher.RaiseBackendOperation(key, typeof(T), SessionStateBackendOperation.Set);

            args = new SessionStateChangedEventArgs<T>
            {
                ChangeType = SessionStateChangeType.Set,
                Value = value,
                PreviousValue = currentValue
            };

            result = true;
        }
        finally
        {
            _asyncLock.Release();
        }

        RaiseChanged(args);

        return result;
    }

    public override async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var key = _keyAccessor.SessionKey;
        if (key is null)
        {
            return; // No session, nothing to clear
        }

        // SharpUtilities

        SessionStateChangedEventArgs<T>? args = null;

        await _asyncLock.WaitAsync(cancellationToken);
        try
        {
            await LoadCoreAsync(key, cancellationToken);

            if (_cache.Value is null)
            {
                return;
            }

            var previousValue = _cache.Clear();

            await _backend.RemoveAsync<T>(key, cancellationToken);

            LogStateCleared(typeof(T).Name, FormatKey(key));

            _eventDispatcher.RaiseValueCleared(key, typeof(T), previousValue);
            _eventDispatcher.RaiseBackendOperation(key, typeof(T), SessionStateBackendOperation.Remove);

            args = new SessionStateChangedEventArgs<T>
            {
                ChangeType = SessionStateChangeType.Cleared,
                Value = null,
                PreviousValue = previousValue
            };
        }
        finally
        {
            _asyncLock.Release();
        }

        if (args is not null)
        {
            RaiseChanged(args);
        }
    }

    public override async ValueTask RefreshAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var key = _keyAccessor.SessionKey;
        if (key is null)
        {
            return;
        }

        await _backend.RefreshAsync<T>(key, cancellationToken);

        _eventDispatcher.RaiseValueRefreshed(key, typeof(T), null);
        _eventDispatcher.RaiseBackendOperation(key, typeof(T), SessionStateBackendOperation.Refresh);
    }

    internal override void InvalidateCache()
    {
        var previousValue = _cache.Invalidate();

        LogCacheInvalidated(typeof(T).Name);

        RaiseChanged(new SessionStateChangedEventArgs<T>
        {
            ChangeType = SessionStateChangeType.Invalidated,
            Value = null,
            PreviousValue = previousValue
        });
    }

    private void OnCacheInvalidated()
    {
        LogCacheInvalidated(typeof(T).Name);

        RaiseChanged(new SessionStateChangedEventArgs<T>
        {
            ChangeType = SessionStateChangeType.Invalidated,
            Value = null,
            PreviousValue = null
        });
    }

    private string GetRequiredSessionKey()
    {
        var key = _keyAccessor.SessionKey;
        if (key is null)
        {
            throw new InvalidOperationException(
                "No session key available. " +
                "Ensure UseSessionAndState() middleware has run and that you configured " +
                "WithAnonymousCookieSession(...) and/or WithAuthCookieClaimSessionKey(...).");
        }

        return key;
    }

    private void EnsureLoadedSync()
    {
        if (_cache.IsLoaded)
        {
            return;
        }

        var key = _keyAccessor.SessionKey;
        if (key is null)
        {
            _cache.MarkLoadedEmpty();
            return;
        }

        _asyncLock.Wait();
        try
        {
            if (_cache.IsLoaded)
            {
                return;
            }

            RunSync(LoadCoreAsync(key, CancellationToken.None));
        }
        finally
        {
            _asyncLock.Release();
        }
    }

    /// <summary>
    /// Loads from backend. Caller MUST hold the lock.
    /// </summary>
    private async ValueTask LoadCoreAsync(string key, CancellationToken cancellationToken)
    {
        if (_cache.IsLoaded)
        {
            return;
        }

        var value = await _backend.GetAsync<T>(key, cancellationToken);

        if (_options.TrackPropertyChanges && value is not null)
        {
            _cache.Set(key, value, CreatePropertyChangedHandler(key));
        }
        else
        {
            _cache.Set(key, value);
        }
    }

    private PropertyChangedEventHandler CreatePropertyChangedHandler(string sessionKey)
    {
        return (sender, args) =>
        {
            if (_disposed)
            {
                return;
            }

            if (string.IsNullOrEmpty(args.PropertyName))
            {
                return;
            }

            var value = _cache.Value;
            if (value is null)
            {
                return;
            }

            _eventDispatcher.RaiseValueChanged(sessionKey, typeof(T), value, args.PropertyName);

            RaiseChanged(new SessionStateChangedEventArgs<T>
            {
                ChangeType = SessionStateChangeType.PropertyChanged,
                Value = value,
                PropertyName = args.PropertyName
            });

            if (_options.AutoPersistOnPropertyChange)
            {
                _ = PersistCurrentValueAsync(sessionKey);
            }
        };
    }

    private async Task PersistCurrentValueAsync(string sessionKey)
    {
        try
        {
            var valueToSave = _cache.GetForPersistence();
            if (valueToSave is null)
            {
                return;
            }

            await _backend.SetAsync(sessionKey, valueToSave, CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogAutoPersistError(typeof(T).Name, ex);
        }
    }

    private void RaiseChanged(SessionStateChangedEventArgs<T> args)
    {
        var handler = _changed;
        if (handler is null)
        {
            return;
        }

        foreach (var invocation in handler.GetInvocationList().Cast<EventHandler<SessionStateChangedEventArgs<T>>>())
        {
            try
            {
                invocation(this, args);
            }
            catch (Exception ex)
            {
                LogEventHandlerError(typeof(T).Name, ex);
            }
        }
    }

    private static void RunSync(ValueTask task)
    {
        if (task.IsCompleted)
        {
            task.GetAwaiter().GetResult();
            return;
        }

        task.AsTask().GetAwaiter().GetResult();
    }

    private static TResult RunSync<TResult>(ValueTask<TResult> task)
    {
        if (task.IsCompleted)
        {
            return task.GetAwaiter().GetResult();
        }

        return task.AsTask().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cache.Invalidated -= OnCacheInvalidated;
        _asyncLock.Dispose();
    }

    private static string FormatKey(string key) =>
        key.Length > 8 ? key[..8] + "..." : key;

    [LoggerMessage(LogLevel.Debug, "SessionAndState<{TypeName}> set for key {Key}")]
    partial void LogStateSet(string typeName, string key);

    [LoggerMessage(LogLevel.Debug, "SessionAndState<{TypeName}> cleared for key {Key}")]
    partial void LogStateCleared(string typeName, string key);

    [LoggerMessage(LogLevel.Debug, "SessionAndState<{TypeName}> set skipped - values are equal")]
    partial void LogSkippedSetDueToEquality(string typeName);

    [LoggerMessage(LogLevel.Debug, "SessionAndState<{TypeName}> cache invalidated by external change")]
    partial void LogCacheInvalidated(string typeName);

    [LoggerMessage(LogLevel.Error, "Error auto-persisting SessionAndState<{TypeName}>")]
    partial void LogAutoPersistError(string typeName, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error in SessionAndState<{TypeName}> Changed event handler")]
    partial void LogEventHandlerError(string typeName, Exception ex);
}
