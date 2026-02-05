using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Http;

namespace SessionAndState.Internal.Http;

/// <summary>
/// Automatically adds SessionAndState context propagation to ALL HttpClients
/// created via IHttpClientFactory.
/// </summary>
internal sealed class SessionStateHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SessionAndStateAsyncLocalAccessor _asyncLocal;

    public SessionStateHttpMessageHandlerBuilderFilter(
        IHttpContextAccessor httpContextAccessor,
        SessionAndStateAsyncLocalAccessor asyncLocal)
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
            builder.AdditionalHandlers.Insert(0, new SessionStateDelegatingHandler(_httpContextAccessor, _asyncLocal));
        };
    }
}
