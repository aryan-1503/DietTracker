using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DietTracker.API.Models;

[Table("UserSettings")]
public class UserSettings
{
    [Key] public int Id { get; set; }

    [Required] public int UserId { get; set; }

    /// <summary>HH:mm local time for the daily reminder. Default 21:00.</summary>
    [Required][MaxLength(5)] public string ReminderTime { get; set; } = "21:00";

    /// <summary>IANA or Windows timezone ID for the user's local time zone.</summary>
    [Required][MaxLength(100)] public string TimeZoneId { get; set; } = "UTC";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;
}
