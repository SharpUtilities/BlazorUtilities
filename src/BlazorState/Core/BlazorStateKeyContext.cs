using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BlazorState.Core;

public sealed class BlazorStateKeyContext
{
    public required ClaimsPrincipal User { get; init; }

    public required HttpContext HttpContext { get; init; }

    public required IServiceProvider Services { get; init; }
}
