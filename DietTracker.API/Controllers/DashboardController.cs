using DietTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DietTracker.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _svc;

    public DashboardController(IDashboardService svc) => _svc = svc;

    /// <summary>
    /// GET /api/dashboard
    /// Returns today's status, key stats, chart data (7 + 30 day), and recent activity.
    /// Pass ?tz=Asia/Kolkata (IANA timezone) so the server can determine the user's local "today".
    /// Falls back to UTC when the timezone is absent or unrecognised.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? tz = null)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        // Resolve "today" in the user's local timezone
        TimeZoneInfo userTz;
        try { userTz = string.IsNullOrWhiteSpace(tz) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(tz); }
        catch { userTz = TimeZoneInfo.Utc; }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTz));

        var result = await _svc.GetDashboardAsync(userId.Value, today);
        return Ok(result);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
