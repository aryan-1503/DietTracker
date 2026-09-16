using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DietTracker.API.Models;

[Table("MealSlots")]
public class MealSlot
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DietPlanId { get; set; }

    /// <summary>HH:mm format, e.g. "07:30"</summary>
    [Required]
    [MaxLength(5)]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>HH:mm format, e.g. "08:00"</summary>
    [Required]
    [MaxLength(5)]
    public string EndTime { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string MealCategory { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    [ForeignKey(nameof(DietPlanId))]
    public DietPlan DietPlan { get; set; } = null!;

    public ICollection<FoodOption> FoodOptions { get; set; } = new List<FoodOption>();
}
