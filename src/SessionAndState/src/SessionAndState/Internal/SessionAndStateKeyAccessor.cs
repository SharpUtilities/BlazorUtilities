namespace SessionAndState.Internal;

/// <summary>
/// Default implementation of <see cref="ISessionAndStateKeyAccessor"/>.
/// Reads from the scoped <see cref="SessionStateContext"/> with fallback
/// to <see cref="SessionAndStateAsyncLocalAccessor"/> for cross-scope scenarios.
/// </summary>
internal sealed class SessionAndStateKeyAccessor : ISessionAndStateKeyAccessor
{
    private readonly SessionStateContext _context;
    private readonly SessionAndStateAsyncLocalAccessor _asyncLocal;

    public SessionAndStateKeyAccessor(SessionStateContext context, SessionAndStateAsyncLocalAccessor asyncLocal)
    {
        _context = context;
        _asyncLocal = asyncLocal;
    }

    public string? SessionKey =>
        _context.SessionKey ?? _asyncLocal.SessionKey;

    public bool HasSessionKey =>
        _context.SessionKey is not null || _asyncLocal.HasValue;

    public bool IsUsingAuthenticatedSessionKey =>
        _context.IsAuthenticated || _asyncLocal.IsAuthenticated;
}
