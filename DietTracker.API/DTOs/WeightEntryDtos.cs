using System.ComponentModel.DataAnnotations;

namespace DietTracker.API.DTOs;

// ── Request ───────────────────────────────────────────────────────────────────

public class CreateWeightEntryDto
{
    [Required]
    [Range(20.0, 300.0, ErrorMessage = "Weight must be between 20 and 300 kg.")]
    public decimal WeightKg { get; set; }

    [Required]
    [Range(1, 53, ErrorMessage = "WeekNumber must be between 1 and 53.")]
    public int WeekNumber { get; set; }

    [Required]
    [Range(2000, 2200, ErrorMessage = "Year must be a valid year.")]
    public int Year { get; set; }
}

public class UpdateWeightEntryDto
{
    [Required]
    [Range(20.0, 300.0, ErrorMessage = "Weight must be between 20 and 300 kg.")]
    public decimal WeightKg { get; set; }
}

// ── Response ──────────────────────────────────────────────────────────────────

public class WeightEntryDto
{
    public int Id { get; set; }
    public decimal WeightKg { get; set; }
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public string WeekLabel { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

public class PendingWeightCheckDto
{
    public bool HasPendingEntry { get; set; }
    public int PendingWeekNumber { get; set; }
    public int PendingYear { get; set; }
    public string PendingWeekLabel { get; set; } = string.Empty;
}
