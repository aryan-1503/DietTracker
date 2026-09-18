using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DietTracker.API.Models;

[Table("DietPlans")]
public class DietPlan
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsPrimary { get; set; } = false;

    /// <summary>Optional date when the user started following this plan.</summary>
    public DateOnly? StartDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<MealSlot> MealSlots { get; set; } = new List<MealSlot>();
}
