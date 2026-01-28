using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SessionState.Core;

public sealed class SessionStateKeyContext
{
    public required ClaimsPrincipal User { get; init; }

    public required HttpContext HttpContext { get; init; }

    public required IServiceProvider Services { get; init; }
}
