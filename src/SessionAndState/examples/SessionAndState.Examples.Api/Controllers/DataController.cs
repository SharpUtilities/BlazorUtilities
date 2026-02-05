using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SessionAndState.Examples.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataController : ControllerBase
{
    private readonly ILogger<DataController> _logger;

    public DataController(ILogger<DataController> logger)
    {
        _logger = logger;
    }

    [HttpGet("user-info")]
    public IActionResult GetUserInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.FindFirstValue(ClaimTypes.Name);

        _logger.LogInformation("User info requested - UserId: {UserId}, UserName: {UserName}", userId, userName);

        return Ok(new
        {
            UserId = userId,
            UserName = userName,
            Message = "✓ Authenticated API call successful!",
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("protected-data")]
    public IActionResult GetProtectedData()
    {
        var userName = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new
        {
            Message = $"Hello {userName}! Here is your protected data.",
            Data = new
            {
                Items = new[] { "Item 1", "Item 2", "Item 3" },
                GeneratedAt = DateTime.UtcNow
            }
        });
    }

    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}
