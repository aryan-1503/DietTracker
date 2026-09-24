using DietTracker.API.Data;
using DietTracker.API.DTOs;
using DietTracker.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Controllers;

[ApiController]
[Route("api/daily-entries")]
[Authorize]
public class DailyEntriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DailyEntriesController(AppDbContext db) => _db = db;

    // GET /api/daily-entries/today
    // Returns today's primary-plan meal slots merged with any existing entries.
    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        return await GetIntakeForDate(userId.Value, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    // GET /api/daily-entries?date=YYYY-MM-DD
    // Returns any date's primary-plan slots merged with entries.
    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] DateOnly date)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        return await GetIntakeForDate(userId.Value, date);
    }

    // POST /api/daily-entries  — create or update an entry (upsert by user+date+slot)
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertDailyEntryRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        // Validate: must have either FoodOptionId OR OtherText (not both, not neither) — but only when FollowedPlan=true
        if (dto.FollowedPlan)
        {
            var hasFood   = dto.FoodOptionId.HasValue;
            var hasOther  = !string.IsNullOrWhiteSpace(dto.OtherText);
            if (!hasFood && !hasOther)
                return BadRequest(new { message = "Either FoodOptionId or OtherText must be provided when FollowedPlan is true." });
            if (hasFood && hasOther)
                return BadRequest(new { message = "Provide either FoodOptionId or OtherText, not both." });
        }

        // Find primary diet plan
        var primaryPlan = await _db.DietPlans
            .Where(dp => dp.UserId == userId && dp.IsPrimary && dp.IsActive)
            .FirstOrDefaultAsync();

        if (primaryPlan is null)
            return BadRequest(new { message = "No primary diet plan found." });

        // Validate the meal slot belongs to the primary plan
        var slot = await _db.MealSlots
            .FirstOrDefaultAsync(ms => ms.Id == dto.MealSlotId && ms.DietPlanId == primaryPlan.Id);

        if (slot is null)
            return BadRequest(new { message = "MealSlot does not belong to the primary diet plan." });

        // Validate food option belongs to the slot (if provided)
        if (dto.FoodOptionId.HasValue)
        {
            var foodExists = await _db.FoodOptions
                .AnyAsync(fo => fo.Id == dto.FoodOptionId.Value && fo.MealSlotId == dto.MealSlotId);
            if (!foodExists)
                return BadRequest(new { message = "FoodOption does not belong to the specified meal slot." });
        }

        // Optional: add OtherText as a new food option to the slot
        if (dto.AddOtherToMealOptions && !string.IsNullOrWhiteSpace(dto.OtherText))
        {
            var nameToAdd = dto.OtherText.Trim();
            var alreadyExists = await _db.FoodOptions
                .AnyAsync(fo => fo.MealSlotId == dto.MealSlotId &&
                                fo.Name.ToLower() == nameToAdd.ToLower());
            if (!alreadyExists)
            {
                var maxOrder = await _db.FoodOptions
                    .Where(fo => fo.MealSlotId == dto.MealSlotId)
                    .Select(fo => (int?)fo.SortOrder)
                    .MaxAsync() ?? -1;

                _db.FoodOptions.Add(new FoodOption
                {
                    MealSlotId = dto.MealSlotId,
                    Name = nameToAdd,
                    SortOrder = maxOrder + 1,
                });
            }
        }

        // Upsert DailyEntry
        var existing = await _db.DailyEntries
            .FirstOrDefaultAsync(de => de.UserId == userId
                                    && de.EntryDate == dto.EntryDate
                                    && de.MealSlotId == dto.MealSlotId);

        if (existing is null)
        {
            existing = new DailyEntry
            {
                UserId     = userId.Value,
                EntryDate  = dto.EntryDate,
                DietPlanId = primaryPlan.Id,
                MealSlotId = dto.MealSlotId,
            };
            _db.DailyEntries.Add(existing);
        }

        existing.FollowedPlan = dto.FollowedPlan;
        existing.ActualTime   = dto.ActualTime;
        existing.FoodOptionId = dto.FollowedPlan ? dto.FoodOptionId : null;
        existing.OtherText    = dto.FollowedPlan ? dto.OtherText?.Trim() : null;
        existing.UpdatedAt    = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(MapEntryToDto(existing));
    }

    // DELETE /api/daily-entries/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var entry = await _db.DailyEntries
            .FirstOrDefaultAsync(de => de.Id == id && de.UserId == userId);

        if (entry is null) return NotFound(new { message = "Entry not found." });

        _db.DailyEntries.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET /api/daily-entries/note?date=YYYY-MM-DD
    [HttpGet("note")]
    public async Task<IActionResult> GetNote([FromQuery] DateOnly date)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var note = await _db.DailyNotes
            .FirstOrDefaultAsync(dn => dn.UserId == userId && dn.EntryDate == date);

        if (note is null) return NoContent();
        return Ok(MapNoteToDto(note));
    }

    // PUT /api/daily-entries/note  — upsert; empty NoteText deletes/clears
    [HttpPut("note")]
    public async Task<IActionResult> UpsertNote([FromBody] UpsertDailyNoteRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var existing = await _db.DailyNotes
            .FirstOrDefaultAsync(dn => dn.UserId == userId && dn.EntryDate == dto.EntryDate);

        var isEmpty = string.IsNullOrWhiteSpace(dto.NoteText);

        if (isEmpty)
        {
            if (existing != null)
            {
                _db.DailyNotes.Remove(existing);
                await _db.SaveChangesAsync();
            }
            return NoContent();
        }

        if (existing is null)
        {
            existing = new Models.DailyNote { UserId = userId.Value, EntryDate = dto.EntryDate };
            _db.DailyNotes.Add(existing);
        }

        existing.NoteText  = dto.NoteText!.Trim();
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapNoteToDto(existing));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<IActionResult> GetIntakeForDate(int userId, DateOnly date)
    {
        // Find primary plan
        var primaryPlan = await _db.DietPlans
            .Where(dp => dp.UserId == userId && dp.IsPrimary && dp.IsActive)
            .Include(dp => dp.MealSlots.OrderBy(ms => ms.SortOrder))
                .ThenInclude(ms => ms.FoodOptions.OrderBy(fo => fo.SortOrder))
            .FirstOrDefaultAsync();

        if (primaryPlan is null)
            return Ok(new { hasPrimaryPlan = false, data = (object?)null });

        // Load entries for the date
        var entries = await _db.DailyEntries
            .Where(de => de.UserId == userId
                      && de.EntryDate == date
                      && de.DietPlanId == primaryPlan.Id)
            .ToListAsync();

        var entryBySlot = entries.ToDictionary(e => e.MealSlotId);

        // Load daily note for the date
        var dailyNote = await _db.DailyNotes
            .FirstOrDefaultAsync(dn => dn.UserId == userId && dn.EntryDate == date);

        var result = new DailyIntakeDto
        {
            DietPlanId   = primaryPlan.Id,
            DietPlanName = primaryPlan.Name,
            Date         = date,
            Slots = primaryPlan.MealSlots.Select(ms => new SlotIntakeDto
            {
                MealSlotId   = ms.Id,
                MealCategory = ms.MealCategory,
                StartTime    = ms.StartTime,
                EndTime      = ms.EndTime,
                SortOrder    = ms.SortOrder,
                IsComplete   = entryBySlot.ContainsKey(ms.Id),
                FoodOptions  = ms.FoodOptions.Select(fo => new FoodOptionDto
                {
                    Id        = fo.Id,
                    Name      = fo.Name,
                    SortOrder = fo.SortOrder,
                }).ToList(),
                Entry = entryBySlot.TryGetValue(ms.Id, out var e) ? MapEntryToDto(e) : null,
            }).ToList(),
            DailyNote = dailyNote != null ? MapNoteToDto(dailyNote) : null,
        };

        return Ok(new { hasPrimaryPlan = true, data = result });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private static DailyEntryDto MapEntryToDto(DailyEntry e) => new()
    {
        Id           = e.Id,
        UserId       = e.UserId,
        EntryDate    = e.EntryDate,
        DietPlanId   = e.DietPlanId,
        MealSlotId   = e.MealSlotId,
        FollowedPlan = e.FollowedPlan,
        ActualTime   = e.ActualTime,
        FoodOptionId = e.FoodOptionId,
        OtherText    = e.OtherText,
        CreatedAt    = e.CreatedAt,
        UpdatedAt    = e.UpdatedAt,
    };

    private static DailyNoteDto MapNoteToDto(Models.DailyNote n) => new()
    {
        Id        = n.Id,
        UserId    = n.UserId,
        EntryDate = n.EntryDate,
        NoteText  = n.NoteText,
        CreatedAt = n.CreatedAt,
        UpdatedAt = n.UpdatedAt,
    };
}
