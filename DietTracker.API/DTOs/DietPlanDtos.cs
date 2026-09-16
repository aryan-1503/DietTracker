using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DietTracker.API.DTOs;

// ── Response DTOs ─────────────────────────────────────────────────────────────

public class FoodOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class MealSlotDto
{
    public int Id { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string MealCategory { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<FoodOptionDto> FoodOptions { get; set; } = new();
}

public class DietPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MealSlotDto> MealSlots { get; set; } = new();
}

public class DietPlanSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsPrimary { get; set; }
    public int MealSlotCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class FoodOptionRequestDto
{
    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;
}

/// <summary>Time string validator attribute for HH:mm format.</summary>
public sealed class HhMmTimeAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string s) return false;
        return Regex.IsMatch(s, @"^([01]\d|2[0-3]):[0-5]\d$");
    }

    public override string FormatErrorMessage(string name) =>
        $"{name} must be in HH:mm format (e.g. 07:30).";
}

public class MealSlotRequestDto
{
    [Required]
    [HhMmTime(ErrorMessage = "StartTime must be in HH:mm format.")]
    public string StartTime { get; set; } = string.Empty;

    [Required]
    [HhMmTime(ErrorMessage = "EndTime must be in HH:mm format.")]
    public string EndTime { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string MealCategory { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;

    [Required]
    [MinLength(1, ErrorMessage = "At least one food option is required.")]
    public List<FoodOptionRequestDto> FoodOptions { get; set; } = new();
}

public class CreateDietPlanRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = false;

    [Required]
    [MinLength(1, ErrorMessage = "At least one meal slot is required.")]
    public List<MealSlotRequestDto> MealSlots { get; set; } = new();
}

public class UpdateDietPlanRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = false;

    [Required]
    [MinLength(1, ErrorMessage = "At least one meal slot is required.")]
    public List<MealSlotRequestDto> MealSlots { get; set; } = new();
}

/// <summary>Used to reorder meal slots (PUT /diet-plans/{id}/slots/reorder).</summary>
public class ReorderSlotsRequestDto
{
    /// <summary>Ordered list of meal slot IDs defining the new sort order.</summary>
    [Required]
    public List<int> SlotIds { get; set; } = new();
}
