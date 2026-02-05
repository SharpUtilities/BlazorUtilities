using System.Security.Claims;
using SessionAndState.Core;

namespace SessionAndState.Examples.Services;

/// <summary>
/// Demo key generator that shows different sharing behaviors:
/// - Bob: Shared across all sessions (uses user ID as key)
/// - Alice, Charlie, etc.: Per-session (uses unique GUID)
/// - Anonymous: Per-session (uses unique GUID)
/// </summary>
public class DemoKeyGenerator : ISessionAndStateKeyGenerator
{
    public ValueTask<string> GenerateNonAuthenticatedKeyAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        // Anonymous users always get a unique session key
        return ValueTask.FromResult(Guid.NewGuid().ToString());
    }

    public ValueTask<string> GenerateAuthenticatedKeyAsync(
        SessionStateKeyContext context,
        string? nonAuthenticatedKey,
        CancellationToken cancellationToken = default)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.Equals(userId, "bob", StringComparison.OrdinalIgnoreCase))
        {
            // Bob shares state across all his sessions
            return ValueTask.FromResult($"shared:user:{userId}");
        }

        // Everyone else gets a unique session key
        // Optionally, we could reuse the anonymous key if one existed
        return ValueTask.FromResult(nonAuthenticatedKey ?? Guid.NewGuid().ToString());
    }
}