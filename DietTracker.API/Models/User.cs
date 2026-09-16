using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DietTracker.API.Models;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    // ── Name fields ───────────────────────────────────────────────────────────
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Computed display name: "First [Middle] Last", falling back gracefully
    /// when parts are absent. Stored client-side as a convenience — never
    /// persisted to the DB (use the SQL computed column for DB-side queries).
    /// </summary>
    [NotMapped]
    public string FullName
    {
        get
        {
            var parts = new[] { FirstName, MiddleName, LastName }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var full = string.Join(" ", parts).Trim();
            return string.IsNullOrEmpty(full) ? Email : full;
        }
    }

    // ── Auth flags ────────────────────────────────────────────────────────────
    public bool IsActive { get; set; } = true;

    public bool IsEmailVerified { get; set; } = false;

    [MaxLength(64)]
    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    // ── Onboarding ────────────────────────────────────────────────────────────
    public bool OnboardingCompleted { get; set; } = false;

    public decimal? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? GoalWeightKg { get; set; }

    public int? TargetDurationWeeks { get; set; }

    // ── Timestamps ────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
