using DietTracker.API.Data;
using DietTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IReportService _reportService;

    public ReportsController(AppDbContext db, IReportService reportService)
    {
        _db           = db;
        _reportService = reportService;
    }

    // GET /api/reports/monthly/meta?year=&month=
    [HttpGet("monthly/meta")]
    public async Task<IActionResult> GetMeta(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        if (!IsValidYearMonth(year, month))
            return BadRequest(new { message = "Invalid year or month." });

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var meta = await _reportService.GetMonthlyMetaAsync(userId.Value, year, month, ct);
        return Ok(meta);
    }

    // GET /api/reports/monthly/download?year=&month=
    [HttpGet("monthly/download")]
    public async Task<IActionResult> Download(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        if (!IsValidYearMonth(year, month))
            return BadRequest(new { message = "Invalid year or month." });

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        // Build display name from user profile
        var user = await _db.Users.FindAsync(new object[] { userId.Value }, ct);
        var displayName = user is not null
            ? string.Join(" ", new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)))
            : "User";

        try
        {
            var bytes = await _reportService.GenerateMonthlyExcelAsync(
                userId.Value, year, month, displayName, ct);

            var fileName = $"DietTracker_{year}_{month:D2}.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Failed to generate report. Please try again." });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsValidYearMonth(int year, int month)
        => year >= 2020 && year <= 2100 && month >= 1 && month <= 12;

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
