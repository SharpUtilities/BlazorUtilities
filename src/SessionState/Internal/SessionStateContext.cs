
namespace SessionState.Internal;

/// <summary>
/// Holds the session state context for the current request scope.
/// This is the single source of truth for the session key within a request.
/// </summary>
internal sealed class SessionStateContext
{
    private string? _sessionKey;
    private bool _isAuthenticated;
    private bool _isEstablished;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    /// <summary>
    /// The session key for this request, or null if not established.
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
    /// Whether the session belongs to an authenticated user.
    /// </summary>
    public bool IsAuthenticated
    {
        get
        {
            lock (_lock)
            {
                return _isAuthenticated;
            }
        }
    }

    /// <summary>
    /// Whether a session has been established for this request.
    /// True even if SessionKey is null (when RequireAuthentication is true and user is anonymous).
    /// </summary>
    public bool IsEstablished
    {
        get
        {
            lock (_lock)
            {
                return _isEstablished;
            }
        }
    }

    /// <summary>
    /// Sets the session key for this request.
    /// </summary>
    internal void Set(string key, bool isAuthenticated)
    {
        lock (_lock)
        {
            _sessionKey = key;
            _isAuthenticated = isAuthenticated;
            _isEstablished = true;
        }

        // Flow to async local for cross-scope access (e.g., DelegatingHandlers)
        SessionStateAsyncLocal.SessionKey = key;
        SessionStateAsyncLocal.IsAuthenticated = isAuthenticated;
    }

    /// <summary>
    /// Marks the session as established but without a key.
    /// Used when RequireAuthentication is true and user is anonymous.
    /// </summary>
    internal void SetNoSession()
    {
        lock (_lock)
        {
            _sessionKey = null;
            _isAuthenticated = false;
            _isEstablished = true;
        }

        // Clear async local as well
        SessionStateAsyncLocal.Clear();
    }

    /// <summary>
    /// Clears the session context.
    /// </summary>
    internal void Clear()
    {
        lock (_lock)
        {
            _sessionKey = null;
            _isAuthenticated = false;
            _isEstablished = false;
        }

        // Clear async local as well
        SessionStateAsyncLocal.Clear();
    }

    /// <summary>
    /// Gets the current state atomically.
    /// </summary>
    internal (string? SessionKey, bool IsAuthenticated, bool IsEstablished) GetState()
    {
        lock (_lock)
        {
            return (_sessionKey, _isAuthenticated, _isEstablished);
        }
    }
}
