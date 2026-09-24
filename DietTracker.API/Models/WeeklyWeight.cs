using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DietTracker.API.Models;

[Table("WeeklyWeights")]
public class WeeklyWeight
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    public int WeekNumber { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,1)")]
    public decimal WeightKg { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
