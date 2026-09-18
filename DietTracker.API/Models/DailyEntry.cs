using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DietTracker.API.Models;

[Table("DailyEntries")]
public class DailyEntry
{
    [Key] public int Id { get; set; }

    [Required] public int UserId { get; set; }

    /// <summary>Date the entry is for (stored as DATE in DB).</summary>
    [Required] public DateOnly EntryDate { get; set; }

    /// <summary>The primary diet plan that was active on this date (never updated retroactively).</summary>
    [Required] public int DietPlanId { get; set; }

    [Required] public int MealSlotId { get; set; }

    public bool FollowedPlan { get; set; } = false;

    /// <summary>HH:mm — actual time of consumption; null until recorded.</summary>
    [MaxLength(5)] public string? ActualTime { get; set; }

    /// <summary>Null when OtherText is used instead.</summary>
    public int? FoodOptionId { get; set; }

    /// <summary>Free-text when "Other" is selected; max 500 chars.</summary>
    [MaxLength(500)] public string? OtherText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(UserId))]       public User        User       { get; set; } = null!;
    [ForeignKey(nameof(DietPlanId))]   public DietPlan    DietPlan   { get; set; } = null!;
    [ForeignKey(nameof(MealSlotId))]   public MealSlot    MealSlot   { get; set; } = null!;
    [ForeignKey(nameof(FoodOptionId))] public FoodOption? FoodOption { get; set; }
}
