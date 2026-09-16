using System.ComponentModel.DataAnnotations;

namespace DietTracker.API.DTOs;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class VerifyEmailRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class OnboardingRequestDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Range(50, 300, ErrorMessage = "Height must be between 50 and 300 cm.")]
    public decimal HeightCm { get; set; }

    [Required]
    [Range(20, 500, ErrorMessage = "Weight must be between 20 and 500 kg.")]
    public decimal WeightKg { get; set; }

    [Required]
    [Range(20, 500, ErrorMessage = "Goal weight must be between 20 and 500 kg.")]
    public decimal GoalWeightKg { get; set; }

    [Required]
    [Range(1, 104, ErrorMessage = "Target duration must be between 1 and 104 weeks.")]
    public int TargetDurationWeeks { get; set; }
}

public class UpdateProfileRequestDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Range(50, 300, ErrorMessage = "Height must be between 50 and 300 cm.")]
    public decimal? HeightCm { get; set; }

    [Range(20, 500, ErrorMessage = "Weight must be between 20 and 500 kg.")]
    public decimal? WeightKg { get; set; }

    [Range(20, 500, ErrorMessage = "Goal weight must be between 20 and 500 kg.")]
    public decimal? GoalWeightKg { get; set; }

    [Range(1, 104, ErrorMessage = "Target duration must be between 1 and 104 weeks.")]
    public int? TargetDurationWeeks { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool OnboardingCompleted { get; set; }
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool OnboardingCompleted { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? GoalWeightKg { get; set; }
    public int? TargetDurationWeeks { get; set; }
    public DateTime CreatedAt { get; set; }
}
