using SessionState.Core;

namespace SessionState.Internal;

/// <summary>
/// Default implementation of <see cref="ISessionStateKeyAccessor"/>.
/// Reads from the scoped <see cref="SessionStateContext"/> with fallback
/// to <see cref="SessionStateAsyncLocal"/> for cross-scope scenarios.
/// </summary>
internal sealed class SessionStateKeyAccessor : ISessionStateKeyAccessor
{
    private readonly SessionStateContext _context;

    public SessionStateKeyAccessor(SessionStateContext context)
    {
        _context = context;
    }

    public string? SessionKey =>
        _context.SessionKey ?? SessionStateAsyncLocal.SessionKey;

    public bool HasSessionKey =>
        _context.SessionKey is not null || SessionStateAsyncLocal.SessionKey is not null;

    public bool IsAuthenticated =>
        _context.IsAuthenticated || SessionStateAsyncLocal.IsAuthenticated;
}
