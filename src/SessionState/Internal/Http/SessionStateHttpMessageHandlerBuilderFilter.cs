using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Http;

namespace SessionState.Internal.Http;

/// <summary>
/// Automatically adds SessionState context propagation to ALL HttpClients
/// created via IHttpClientFactory.
/// </summary>
internal sealed class SessionStateHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionStateHttpMessageHandlerBuilderFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            // Call the next filter first
            next(builder);

            // Insert our handler at the START of the pipeline so it runs before all other handlers
            builder.AdditionalHandlers.Insert(0, new SessionStateDelegatingHandler(_httpContextAccessor));
        };
    }
}
