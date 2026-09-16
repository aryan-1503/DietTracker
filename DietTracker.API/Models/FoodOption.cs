using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DietTracker.API.Models;

[Table("FoodOptions")]
public class FoodOption
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MealSlotId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    [ForeignKey(nameof(MealSlotId))]
    public MealSlot MealSlot { get; set; } = null!;
}
