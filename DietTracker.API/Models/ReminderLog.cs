using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DietTracker.API.Models;

[Table("ReminderLogs")]
public class ReminderLog
{
    [Key] public int Id { get; set; }

    [Required] public int UserId { get; set; }

    /// <summary>The date for which this reminder was sent.</summary>
    [Required] public DateOnly ReminderDate { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;
}
