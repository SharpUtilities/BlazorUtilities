namespace BlazorState.Internal;

/// <summary>
/// Default implementation of <see cref="IBlazorStateKeyAccessor"/>.
/// Reads from the scoped <see cref="BlazorStateContext"/> with fallback
/// to <see cref="BlazorStateAsyncLocalAccessor"/> for cross-scope scenarios.
/// </summary>
internal sealed class BlazorStateKeyAccessor : IBlazorStateKeyAccessor
{
    private readonly BlazorStateContext _context;
    private readonly BlazorStateAsyncLocalAccessor _asyncLocal;

    public BlazorStateKeyAccessor(BlazorStateContext context, BlazorStateAsyncLocalAccessor asyncLocal)
    {
        _context = context;
        _asyncLocal = asyncLocal;
    }

    public string? SessionKey =>
        _context.SessionKey ?? _asyncLocal.SessionKey;

    public bool HasSessionKey =>
        _context.SessionKey is not null || _asyncLocal.HasValue;

    public bool IsAuthenticated =>
        _context.IsAuthenticated || _asyncLocal.IsAuthenticated;
}
