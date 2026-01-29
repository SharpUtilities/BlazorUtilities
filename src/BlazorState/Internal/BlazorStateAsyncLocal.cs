using System.Diagnostics;

namespace BlazorState.Internal;

/// <summary>
/// Provides ambient access to the session context across async flows.
/// Works across DI scopes including DelegatingHandlers.
/// </summary>
/// <remarks>
/// Uses the same holder pattern as <see cref="Microsoft.AspNetCore.Http.HttpContextAccessor"/>
/// to ensure clearing works correctly across all execution contexts.
/// </remarks>
[DebuggerDisplay("SessionKey = {SessionKey}, IsAuthenticated = {IsAuthenticated}")]
internal sealed class BlazorStateAsyncLocalAccessor
{
    private static readonly AsyncLocal<BlazorStateHolder> _current = new();

    public string? SessionKey => _current.Value?.SessionKey;

    public bool IsAuthenticated => _current.Value?.IsAuthenticated ?? false;

    public bool HasValue => _current.Value?.SessionKey is not null;

    public void Set(string sessionKey, bool isAuthenticated)
    {
        ArgumentNullException.ThrowIfNull(sessionKey);

        var holder = _current.Value;
        if (holder is not null)
        {
            // Clear current values trapped in the AsyncLocal, as it's done.
            holder.SessionKey = null;
            holder.IsAuthenticated = false;
        }

        // Use an object indirection to hold the values in the AsyncLocal,
        // so they can be cleared in all ExecutionContexts when cleared.
        _current.Value = new BlazorStateHolder
        {
            SessionKey = sessionKey,
            IsAuthenticated = isAuthenticated
        };
    }

    public void Clear()
    {
        var holder = _current.Value;
        if (holder is not null)
        {
            holder.SessionKey = null;
            holder.IsAuthenticated = false;
        }
    }

    private sealed class BlazorStateHolder
    {
        public string? SessionKey;
        public bool IsAuthenticated;
    }
}
