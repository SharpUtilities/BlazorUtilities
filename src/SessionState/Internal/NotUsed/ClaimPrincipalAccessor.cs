using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;

namespace SessionState.Internal.NotUsed;

file static class AuthenticationStateProviderExtensions
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_authenticationStateTask")]
    private static extern ref Task<AuthenticationState>? GetAuthenticationStateTaskRef(ServerAuthenticationStateProvider provider);

    /// <summary>
    /// Checks whether the authentication state has been set without throwing an exception.
    /// </summary>
    /// <param name="provider">The authentication state provider to check.</param>
    /// <returns>True if the authentication state task has been set; otherwise, false.</returns>
    internal static bool HasAuthenticationState(this AuthenticationStateProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (provider is ServerAuthenticationStateProvider serverProvider)
        {
            return GetAuthenticationStateTaskRef(serverProvider) is not null;
        }

        return false;
    }
}

internal interface IClaimPrincipalAccessor
{
    ValueTask<ClaimsPrincipal?> GetPrincipalAsync();
}

internal sealed class ClaimPrincipalAccessor : IClaimPrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public ClaimPrincipalAccessor(IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async ValueTask<ClaimsPrincipal?> GetPrincipalAsync()
    {
        if (_httpContextAccessor.HttpContext is not null)
        {
            return _httpContextAccessor.HttpContext.User;
        }

        if (!_authenticationStateProvider.HasAuthenticationState())
        {
            return null;
        }

        var result = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return result.User;
    }
}
