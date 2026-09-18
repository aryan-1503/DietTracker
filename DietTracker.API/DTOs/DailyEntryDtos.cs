using System.ComponentModel.DataAnnotations;

namespace DietTracker.API.DTOs;

// ── Daily Entry Response DTOs ──────────────────────────────────────────────────

public class DailyEntryDto
{
    public int      Id           { get; set; }
    public int      UserId       { get; set; }
    public DateOnly EntryDate    { get; set; }
    public int      DietPlanId   { get; set; }
    public int      MealSlotId   { get; set; }
    public bool     FollowedPlan { get; set; }
    public string?  ActualTime   { get; set; }
    public int?     FoodOptionId { get; set; }
    public string?  OtherText    { get; set; }
    public DateTime CreatedAt    { get; set; }
    public DateTime UpdatedAt    { get; set; }
}

/// <summary>Full snapshot of today's primary plan with completion state per slot.</summary>
public class DailyIntakeDto
{
    public int    DietPlanId   { get; set; }
    public string DietPlanName { get; set; } = string.Empty;
    public DateOnly Date       { get; set; }
    public List<SlotIntakeDto> Slots { get; set; } = new();
}

public class SlotIntakeDto
{
    public int    MealSlotId    { get; set; }
    public string MealCategory  { get; set; } = string.Empty;
    public string StartTime     { get; set; } = string.Empty;
    public string EndTime       { get; set; } = string.Empty;
    public int    SortOrder     { get; set; }
    public bool   IsComplete    { get; set; }   // true when a DailyEntry row exists

    /// <summary>Food options configured for this slot (not including "Other").</summary>
    public List<FoodOptionDto> FoodOptions { get; set; } = new();

    /// <summary>Null when no entry recorded yet.</summary>
    public DailyEntryDto? Entry { get; set; }
}

// ── Daily Entry Request DTOs ───────────────────────────────────────────────────

public class UpsertDailyEntryRequestDto
{
    [Required]
    public DateOnly EntryDate { get; set; }

    [Required]
    public int MealSlotId { get; set; }

    public bool FollowedPlan { get; set; } = false;

    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$",
        ErrorMessage = "ActualTime must be in HH:mm format.")]
    public string? ActualTime { get; set; }

    /// <summary>Null when Other is selected.</summary>
    public int? FoodOptionId { get; set; }

    [MaxLength(500)]
    public string? OtherText { get; set; }

    /// <summary>When true and OtherText is set, adds OtherText as a new FoodOption for this slot.</summary>
    public bool AddOtherToMealOptions { get; set; } = false;
}

// ── User Settings DTOs ─────────────────────────────────────────────────────────

public class UserSettingsDto
{
    public string ReminderTime { get; set; } = "21:00";
    public string TimeZoneId   { get; set; } = "UTC";
}

public class UpdateUserSettingsRequestDto
{
    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$",
        ErrorMessage = "ReminderTime must be in HH:mm format.")]
    public string ReminderTime { get; set; } = "21:00";

    [Required]
    [MaxLength(100)]
    public string TimeZoneId { get; set; } = "UTC";
}
