using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DietTracker.API.Models;

[Table("DailyNotes")]
public class DailyNote
{
    [Key] public int Id { get; set; }

    [Required] public int UserId { get; set; }

    /// <summary>Date the note applies to (stored as DATE in DB).</summary>
    [Required] public DateOnly EntryDate { get; set; }

    /// <summary>Optional free-text note for the day; null when cleared.</summary>
    [MaxLength(1000)] public string? NoteText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;
}
