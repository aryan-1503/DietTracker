using DietTracker.API.Data;
using DietTracker.API.DTOs;
using DietTracker.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public UserSettingsController(AppDbContext db) => _db = db;

    // GET /api/settings
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var settings = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        // Return defaults if no settings row exists yet
        return Ok(new UserSettingsDto
        {
            ReminderTime = settings?.ReminderTime ?? "21:00",
            TimeZoneId   = settings?.TimeZoneId   ?? "UTC",
        });
    }

    // PUT /api/settings
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserSettingsRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        // Validate timezone
        try { TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZoneId); }
        catch { return BadRequest(new { message = $"Unknown timezone: '{dto.TimeZoneId}'." }); }

        var settings = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings is null)
        {
            settings = new UserSettings { UserId = userId.Value };
            _db.UserSettings.Add(settings);
        }

        settings.ReminderTime = dto.ReminderTime;
        settings.TimeZoneId   = dto.TimeZoneId;
        settings.UpdatedAt    = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new UserSettingsDto
        {
            ReminderTime = settings.ReminderTime,
            TimeZoneId   = settings.TimeZoneId,
        });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
