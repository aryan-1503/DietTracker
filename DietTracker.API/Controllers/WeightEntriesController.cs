using System.Globalization;
using DietTracker.API.Data;
using DietTracker.API.DTOs;
using DietTracker.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Controllers;

[ApiController]
[Route("api/weight-entries")]
[Authorize]
public class WeightEntriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public WeightEntriesController(AppDbContext db) => _db = db;

    /// <summary>
    /// GET /api/weight-entries
    /// Returns all weight entries for the authenticated user, sorted ascending by year/week.
    /// Week labels (Week 1, Week 2, …) are computed relative to the user's primary diet plan StartDate.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var startDate = await GetPlanStartDateAsync(userId.Value);

        var entries = await _db.WeeklyWeights
            .Where(w => w.UserId == userId.Value)
            .OrderBy(w => w.Year)
            .ThenBy(w => w.WeekNumber)
            .ToListAsync();

        var dtos = entries.Select(e => new WeightEntryDto
        {
            Id = e.Id,
            WeightKg = e.WeightKg,
            WeekNumber = e.WeekNumber,
            Year = e.Year,
            WeekLabel = ComputeWeekLabel(e.Year, e.WeekNumber, startDate),
            RecordedAt = e.RecordedAt,
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// GET /api/weight-entries/pending-check
    /// Returns the OLDEST missing ISO week's weight for the authenticated user.
    /// "Oldest" = earliest week from plan StartDate up to last completed week with no entry.
    /// Uses ?tz for the user's local timezone.
    /// </summary>
    [HttpGet("pending-check")]
    public async Task<IActionResult> PendingCheck([FromQuery] string? tz = null)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        // Resolve user's local "today"
        TimeZoneInfo userTz;
        try { userTz = string.IsNullOrWhiteSpace(tz) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(tz); }
        catch { userTz = TimeZoneInfo.Utc; }

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTz);

        // Check if the user has an active diet plan (EC-1: no plan → no popup)
        var startDate = await GetPlanStartDateAsync(userId.Value);
        if (startDate is null)
        {
            return Ok(new PendingWeightCheckDto { HasPendingEntry = false });
        }

        // Determine the previous ISO week (the last completed week before "this" week)
        var prevWeekDate = localNow.AddDays(-7);
        int prevYear = ISOWeek.GetYear(DateOnly.FromDateTime(prevWeekDate));
        int prevWeek = ISOWeek.GetWeekOfYear(DateOnly.FromDateTime(prevWeekDate));

        // Plan start week bounds
        int startYear = ISOWeek.GetYear(startDate.Value);
        int startWeek = ISOWeek.GetWeekOfYear(startDate.Value);

        // If the previous week is before the plan start, nothing to prompt for
        bool prevIsBeforePlanStart = (prevYear < startYear) ||
                                     (prevYear == startYear && prevWeek < startWeek);
        if (prevIsBeforePlanStart)
        {
            return Ok(new PendingWeightCheckDto { HasPendingEntry = false });
        }

        // Fetch all existing entry (year, week) pairs for this user
        var existingKeys = await _db.WeeklyWeights
            .Where(w => w.UserId == userId.Value)
            .Select(w => new { w.Year, w.WeekNumber })
            .ToListAsync();

        var existingSet = existingKeys.Select(k => (k.Year, k.WeekNumber)).ToHashSet();

        // Walk from plan start week → previous week, find the OLDEST missing week
        var startMonday = ISOWeek.ToDateTime(startYear, startWeek, DayOfWeek.Monday);
        var prevMonday = ISOWeek.ToDateTime(prevYear, prevWeek, DayOfWeek.Monday);

        DateTime? oldestMissingMonday = null;
        var cursor = startMonday;
        while (cursor <= prevMonday)
        {
            var curDate = DateOnly.FromDateTime(cursor);
            int curYear = ISOWeek.GetYear(curDate);
            int curWeek = ISOWeek.GetWeekOfYear(curDate);

            if (!existingSet.Contains((curYear, curWeek)))
            {
                oldestMissingMonday = cursor;
                break; // found the oldest missing
            }

            cursor = cursor.AddDays(7);
        }

        if (oldestMissingMonday is null)
        {
            return Ok(new PendingWeightCheckDto { HasPendingEntry = false });
        }

        var missingDate = DateOnly.FromDateTime(oldestMissingMonday.Value);
        int missingYear = ISOWeek.GetYear(missingDate);
        int missingWeek = ISOWeek.GetWeekOfYear(missingDate);
        var weekLabel = ComputeWeekLabel(missingYear, missingWeek, startDate);

        return Ok(new PendingWeightCheckDto
        {
            HasPendingEntry = true,
            PendingWeekNumber = missingWeek,
            PendingYear = missingYear,
            PendingWeekLabel = weekLabel,
        });
    }

    /// <summary>
    /// POST /api/weight-entries
    /// Saves a weight entry. Returns 409 Conflict if an entry for the same ISO week already exists.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWeightEntryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        // Validate weight decimal places (max 1)
        if (decimal.Round(dto.WeightKg, 1) != dto.WeightKg)
            return BadRequest(new { message = "Weight must have at most 1 decimal place." });

        // Check for duplicate entry (FR-13 / AC-6)
        var duplicate = await _db.WeeklyWeights
            .AnyAsync(w => w.UserId == userId.Value && w.Year == dto.Year && w.WeekNumber == dto.WeekNumber);

        if (duplicate)
            return Conflict(new { message = $"A weight entry for Week {dto.WeekNumber} of {dto.Year} already exists." });

        var entry = new WeeklyWeight
        {
            UserId = userId.Value,
            Year = dto.Year,
            WeekNumber = dto.WeekNumber,
            WeightKg = dto.WeightKg,
            RecordedAt = DateTime.UtcNow,
        };

        _db.WeeklyWeights.Add(entry);
        await _db.SaveChangesAsync();

        var startDate = await GetPlanStartDateAsync(userId.Value);
        var responseDto = new WeightEntryDto
        {
            Id = entry.Id,
            WeightKg = entry.WeightKg,
            WeekNumber = entry.WeekNumber,
            Year = entry.Year,
            WeekLabel = ComputeWeekLabel(entry.Year, entry.WeekNumber, startDate),
            RecordedAt = entry.RecordedAt,
        };

        return CreatedAtAction(nameof(GetAll), null, responseDto);
    }

    /// <summary>
    /// PUT /api/weight-entries/{id}
    /// Updates the weight value of an existing entry. Only the owning user may update.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWeightEntryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        if (decimal.Round(dto.WeightKg, 1) != dto.WeightKg)
            return BadRequest(new { message = "Weight must have at most 1 decimal place." });

        var entry = await _db.WeeklyWeights
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId.Value);

        if (entry is null) return NotFound();

        entry.WeightKg = dto.WeightKg;
        await _db.SaveChangesAsync();

        var startDate = await GetPlanStartDateAsync(userId.Value);
        return Ok(new WeightEntryDto
        {
            Id = entry.Id,
            WeightKg = entry.WeightKg,
            WeekNumber = entry.WeekNumber,
            Year = entry.Year,
            WeekLabel = ComputeWeekLabel(entry.Year, entry.WeekNumber, startDate),
            RecordedAt = entry.RecordedAt,
        });
    }

    /// <summary>
    /// DELETE /api/weight-entries/{id}
    /// Deletes a weight entry. Only the owning user may delete.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var entry = await _db.WeeklyWeights
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId.Value);

        if (entry is null) return NotFound();

        _db.WeeklyWeights.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<DateOnly?> GetPlanStartDateAsync(int userId)
    {
        return await _db.DietPlans
            .Where(dp => dp.UserId == userId && dp.IsPrimary && dp.IsActive)
            .OrderByDescending(dp => dp.CreatedAt)
            .Select(dp => dp.StartDate)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Computes a "Week N" label relative to the plan start date.
    /// Week 1 = the ISO week containing the plan's StartDate.
    /// </summary>
    private static string ComputeWeekLabel(int entryYear, int entryWeekNumber, DateOnly? startDate)
    {
        if (startDate is null)
            return $"Week {entryWeekNumber}";

        int startYear = ISOWeek.GetYear(startDate.Value);
        int startWeek = ISOWeek.GetWeekOfYear(startDate.Value);

        var startMonday = ISOWeek.ToDateTime(startYear, startWeek, DayOfWeek.Monday);
        var entryMonday = ISOWeek.ToDateTime(entryYear, entryWeekNumber, DayOfWeek.Monday);

        int weeksSinceStart = (int)Math.Round((entryMonday - startMonday).TotalDays / 7.0);
        int weekLabel = weeksSinceStart + 1; // 1-indexed

        return weekLabel > 0 ? $"Week {weekLabel}" : $"Week {entryWeekNumber}";
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
