using DietTracker.API.Data;
using DietTracker.API.DTOs;
using DietTracker.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Controllers;

[ApiController]
[Route("api/diet-plans")]
[Authorize]
public class DietPlansController : ControllerBase
{
    private readonly AppDbContext _db;

    public DietPlansController(AppDbContext db) => _db = db;

    // ── GET /api/diet-plans ───────────────────────────────────────────────────
    /// <summary>List all diet plans for the current user (summary only).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var plans = await _db.DietPlans
            .Where(dp => dp.UserId == userId)
            .OrderByDescending(dp => dp.UpdatedAt)
            .Select(dp => new DietPlanSummaryDto
            {
                Id = dp.Id,
                Name = dp.Name,
                IsActive = dp.IsActive,
                IsPrimary = dp.IsPrimary,
                MealSlotCount = dp.MealSlots.Count,
                CreatedAt = dp.CreatedAt,
                UpdatedAt = dp.UpdatedAt,
            })
            .ToListAsync();

        return Ok(plans);
    }

    // ── GET /api/diet-plans/{id} ──────────────────────────────────────────────
    /// <summary>Get a single diet plan with all meal slots and food options.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var plan = await _db.DietPlans
            .Where(dp => dp.Id == id && dp.UserId == userId)
            .Include(dp => dp.MealSlots.OrderBy(ms => ms.SortOrder))
                .ThenInclude(ms => ms.FoodOptions.OrderBy(fo => fo.SortOrder))
            .FirstOrDefaultAsync();

        if (plan is null) return NotFound(new { message = "Diet plan not found." });

        return Ok(MapToDto(plan));
    }

    // ── POST /api/diet-plans ──────────────────────────────────────────────────
    /// <summary>Create a new diet plan with meal slots and food options.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDietPlanRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var validationError = ValidateMealSlots(dto.MealSlots);
        if (validationError is not null) return BadRequest(new { message = validationError });

        var plan = new DietPlan
        {
            UserId = userId.Value,
            Name = dto.Name.Trim(),
            IsPrimary = dto.IsPrimary,
            MealSlots = dto.MealSlots.Select((s, i) => new MealSlot
            {
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                MealCategory = s.MealCategory.Trim(),
                SortOrder = i,
                FoodOptions = s.FoodOptions.Select((f, j) => new FoodOption
                {
                    Name = f.Name.Trim(),
                    SortOrder = j,
                }).ToList(),
            }).ToList(),
        };

        _db.DietPlans.Add(plan);
        await _db.SaveChangesAsync();

        // If this plan is marked primary, clear all other plans for this user
        if (plan.IsPrimary)
        {
            await _db.DietPlans
                .Where(dp => dp.UserId == userId && dp.IsPrimary && dp.Id != plan.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(dp => dp.IsPrimary, false));
        }

        // Reload with navigation properties
        await _db.Entry(plan)
            .Collection(p => p.MealSlots)
            .Query()
            .Include(ms => ms.FoodOptions)
            .LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, MapToDto(plan));
    }

    // ── PUT /api/diet-plans/{id} ──────────────────────────────────────────────
    /// <summary>Replace a diet plan's name, meal slots, and food options entirely.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDietPlanRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var plan = await _db.DietPlans
            .Where(dp => dp.Id == id && dp.UserId == userId)
            .Include(dp => dp.MealSlots)
                .ThenInclude(ms => ms.FoodOptions)
            .FirstOrDefaultAsync();

        if (plan is null) return NotFound(new { message = "Diet plan not found." });

        var validationError = ValidateMealSlots(dto.MealSlots);
        if (validationError is not null) return BadRequest(new { message = validationError });

        // Replace in-place: remove old children, add new
        plan.Name = dto.Name.Trim();
        plan.IsPrimary = dto.IsPrimary;
        plan.UpdatedAt = DateTime.UtcNow;

        _db.MealSlots.RemoveRange(plan.MealSlots);

        plan.MealSlots = dto.MealSlots.Select((s, i) => new MealSlot
        {
            DietPlanId = plan.Id,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            MealCategory = s.MealCategory.Trim(),
            SortOrder = i,
            FoodOptions = s.FoodOptions.Select((f, j) => new FoodOption
            {
                Name = f.Name.Trim(),
                SortOrder = j,
            }).ToList(),
        }).ToList();

        await _db.SaveChangesAsync();

        // If marked primary, clear others for this user
        if (plan.IsPrimary)
        {
            await _db.DietPlans
                .Where(dp => dp.UserId == userId && dp.IsPrimary && dp.Id != plan.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(dp => dp.IsPrimary, false));
        }

        return Ok(MapToDto(plan));
    }

    // ── PUT /api/diet-plans/{id}/slots/reorder ────────────────────────────────
    /// <summary>Reorder meal slots by submitting an ordered list of slot IDs.</summary>
    [HttpPut("{id:int}/slots/reorder")]
    public async Task<IActionResult> ReorderSlots(int id, [FromBody] ReorderSlotsRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var plan = await _db.DietPlans
            .Where(dp => dp.Id == id && dp.UserId == userId)
            .Include(dp => dp.MealSlots)
            .FirstOrDefaultAsync();

        if (plan is null) return NotFound(new { message = "Diet plan not found." });

        var slotIds = plan.MealSlots.Select(ms => ms.Id).ToHashSet();

        if (dto.SlotIds.Count != slotIds.Count || !dto.SlotIds.All(slotIds.Contains))
            return BadRequest(new { message = "SlotIds must contain exactly the plan's current slot IDs." });

        foreach (var (slotId, order) in dto.SlotIds.Select((s, i) => (s, i)))
        {
            var slot = plan.MealSlots.First(ms => ms.Id == slotId);
            slot.SortOrder = order;
            slot.UpdatedAt = DateTime.UtcNow;
        }

        plan.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Reordered successfully." });
    }

    // ── PUT /api/diet-plans/{id}/set-primary ──────────────────────────────────
    /// <summary>Mark a plan as primary; clears IsPrimary on all other user plans atomically.</summary>
    [HttpPut("{id:int}/set-primary")]
    public async Task<IActionResult> SetPrimary(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var plan = await _db.DietPlans
            .FirstOrDefaultAsync(dp => dp.Id == id && dp.UserId == userId);

        if (plan is null) return NotFound(new { message = "Diet plan not found." });

        // Clear all other primaries for this user, then set this one
        await _db.DietPlans
            .Where(dp => dp.UserId == userId && dp.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(dp => dp.IsPrimary, false));

        plan.IsPrimary = true;
        plan.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Primary plan updated." });
    }

    // ── DELETE /api/diet-plans/{id} ───────────────────────────────────────────
    /// <summary>Delete a diet plan (cascades to meal slots and food options).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var plan = await _db.DietPlans
            .FirstOrDefaultAsync(dp => dp.Id == id && dp.UserId == userId);

        if (plan is null) return NotFound(new { message = "Diet plan not found." });

        _db.DietPlans.Remove(plan);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private static string? ValidateMealSlots(List<MealSlotRequestDto> slots)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];

            // Check StartTime < EndTime
            if (string.Compare(s.StartTime, s.EndTime, StringComparison.Ordinal) >= 0)
                return $"Meal slot {i + 1}: StartTime ({s.StartTime}) must be before EndTime ({s.EndTime}).";

            // No empty food options
            if (s.FoodOptions.Any(f => string.IsNullOrWhiteSpace(f.Name)))
                return $"Meal slot {i + 1}: Food option names cannot be empty.";

            // No duplicate food option names within a slot
            var dupes = s.FoodOptions
                .GroupBy(f => f.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (dupes.Count > 0)
                return $"Meal slot {i + 1}: Duplicate food options: {string.Join(", ", dupes)}.";
        }

        // No duplicate meal categories within the plan
        var dupeCats = slots
            .GroupBy(s => s.MealCategory.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (dupeCats.Count > 0)
            return $"Duplicate meal categories: {string.Join(", ", dupeCats)}.";

        return null;
    }

    private static DietPlanDto MapToDto(DietPlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        IsActive = plan.IsActive,
        IsPrimary = plan.IsPrimary,
        CreatedAt = plan.CreatedAt,
        UpdatedAt = plan.UpdatedAt,
        MealSlots = plan.MealSlots
            .OrderBy(ms => ms.SortOrder)
            .Select(ms => new MealSlotDto
            {
                Id = ms.Id,
                StartTime = ms.StartTime,
                EndTime = ms.EndTime,
                MealCategory = ms.MealCategory,
                SortOrder = ms.SortOrder,
                FoodOptions = ms.FoodOptions
                    .OrderBy(fo => fo.SortOrder)
                    .Select(fo => new FoodOptionDto
                    {
                        Id = fo.Id,
                        Name = fo.Name,
                        SortOrder = fo.SortOrder,
                    }).ToList(),
            }).ToList(),
    };
}
