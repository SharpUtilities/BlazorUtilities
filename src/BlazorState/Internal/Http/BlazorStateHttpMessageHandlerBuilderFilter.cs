using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Http;

namespace BlazorState.Internal.Http;

/// <summary>
/// Automatically adds BlazorState context propagation to ALL HttpClients
/// created via IHttpClientFactory.
/// </summary>
internal sealed class BlazorStateHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BlazorStateAsyncLocalAccessor _asyncLocal;

    public BlazorStateHttpMessageHandlerBuilderFilter(
        IHttpContextAccessor httpContextAccessor,
        BlazorStateAsyncLocalAccessor asyncLocal)
    {
        _httpContextAccessor = httpContextAccessor;
        _asyncLocal = asyncLocal;
    }

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            // Call the next filter first
            next(builder);

            // Insert our handler at the START of the pipeline so it runs before all other handlers
            builder.AdditionalHandlers.Insert(0, new BlazorStateDelegatingHandler(_httpContextAccessor, _asyncLocal));
        };
    }
}
