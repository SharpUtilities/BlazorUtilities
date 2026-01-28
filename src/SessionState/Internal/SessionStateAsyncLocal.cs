namespace SessionState.Internal;

/// <summary>
/// Provides ambient access to the session key across async flows.
/// Works across DI scopes including DelegatingHandlers.
/// </summary>
internal static class SessionStateAsyncLocal
{
    private static readonly AsyncLocal<string?> _sessionKey = new();
    private static readonly AsyncLocal<bool> _isAuthenticated = new();

    public static string? SessionKey
    {
        get => _sessionKey.Value;
        internal set => _sessionKey.Value = value;
    }

    public static bool IsAuthenticated
    {
        get => _isAuthenticated.Value;
        internal set => _isAuthenticated.Value = value;
    }

    public static void Clear()
    {
        _sessionKey.Value = null;
        _isAuthenticated.Value = false;
    }
}
