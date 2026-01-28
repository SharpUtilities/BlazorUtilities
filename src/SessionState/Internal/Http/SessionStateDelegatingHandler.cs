using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SessionState.Core;

namespace SessionState.Internal.Http;

/// <summary>
/// DelegatingHandler that propagates SessionState context to the handler pipeline.
/// Automatically added to all HttpClients via IHttpMessageHandlerBuilderFilter.
/// </summary>
internal sealed class SessionStateDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionStateDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            var keyAccessor = httpContext.RequestServices
                .GetService<ISessionStateKeyAccessor>();

            if (keyAccessor?.HasSessionKey == true)
            {
                SessionStateAsyncLocal.SessionKey = keyAccessor.SessionKey;
                SessionStateAsyncLocal.IsAuthenticated = keyAccessor.IsAuthenticated;
            }
        }

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            SessionStateAsyncLocal.Clear();
        }
    }
}
