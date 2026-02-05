using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SessionAndState.Core;

namespace SessionAndState.Examples.Services;

public sealed class ApiDelegatingHandler : DelegatingHandler
{
    private readonly SessionState<TokenState> _tokenState;

    public ApiDelegatingHandler(SessionState<TokenState> tokenState)
    {
        _tokenState = tokenState;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_tokenState.TryGetValue(out var tokenState) && !string.IsNullOrWhiteSpace(tokenState.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenState.Token);
        }
        else
        {
            throw new Exception("No token");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Generates JWT tokens for authenticated users to call external APIs.
/// </summary>
public interface IJwtTokenService
{
    string? GenerateToken();
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JwtTokenService> _logger;

    // In production, these should come from configuration
    private const string SecretKey = "ThisIsASecretKeyForDemoOnlyDoNotUseInProduction1234!";
    private const string Issuer = "SessionAndState.Examples";
    private const string Audience = "SessionAndState.Examples.Api";

    public JwtTokenService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<JwtTokenService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? GenerateToken()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("Cannot generate JWT - user not authenticated");
            return null;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = user.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Cannot generate JWT - no user ID claim");
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName ?? userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogDebug("Generated JWT for user {UserId}", userId);

        return tokenString;
    }
}
