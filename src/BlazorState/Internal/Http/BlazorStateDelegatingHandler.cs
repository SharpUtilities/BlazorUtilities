using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorState.Internal.Http;

/// <summary>
/// DelegatingHandler that ensures BlazorState context flows to the handler pipeline.
/// Automatically added to all HttpClients via IHttpMessageHandlerBuilderFilter.
/// </summary>
internal sealed class BlazorStateDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BlazorStateAsyncLocalAccessor _asyncLocal;

    public BlazorStateDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        BlazorStateAsyncLocalAccessor asyncLocal)
    {
        _httpContextAccessor = httpContextAccessor;
        _asyncLocal = asyncLocal;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // If AsyncLocal doesn't have a value but HttpContext does, populate it.
        // This handles edge cases where the handler runs before context was established
        // or in a fresh execution context.
        if (!_asyncLocal.HasValue)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                var keyAccessor = httpContext.RequestServices.GetService<IBlazorStateKeyAccessor>();
                if (keyAccessor?.HasSessionKey == true && keyAccessor.SessionKey is not null)
                {
                    _asyncLocal.Set(keyAccessor.SessionKey, keyAccessor.IsAuthenticated);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
