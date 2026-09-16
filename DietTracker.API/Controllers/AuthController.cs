using DietTracker.API.Data;
using DietTracker.API.DTOs;
using DietTracker.API.Models;
using DietTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public AuthController(AppDbContext db, ITokenService tokenService, IEmailService emailService)
    {
        _db = db;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    /// <summary>Register with email + password. Returns token; email verification required.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == dto.Email.ToLowerInvariant());

        if (emailExists)
            return Conflict(new { message = "Email is already registered." });

        var verificationToken = GenerateToken();

        var user = new User
        {
            Email = dto.Email.ToLowerInvariant().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Send verification email (fire-and-forget: registration never fails due to SMTP)
        _ = _emailService.SendVerificationEmailAsync(user.Email, verificationToken)
              .ContinueWith(t => { /* errors are logged inside the service */ },
                            TaskContinuationOptions.OnlyOnFaulted);

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return CreatedAtAction(nameof(GetProfile), null, new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            IsEmailVerified = false,
            OnboardingCompleted = false,
        });
    }

    /// <summary>Verify email using the token sent to the user's inbox.</summary>
    [Authorize]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        if (user.IsEmailVerified)
            return Ok(new { message = "Email already verified." });

        if (user.EmailVerificationToken != dto.Token ||
            user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "Invalid or expired verification token." });

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            IsEmailVerified = true,
            OnboardingCompleted = user.OnboardingCompleted,
        });
    }

    /// <summary>Submit onboarding details (name, height, weight, goal, duration).</summary>
    [Authorize]
    [HttpPost("onboarding")]
    public async Task<IActionResult> CompleteOnboarding([FromBody] OnboardingRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        if (!user.IsEmailVerified)
            return BadRequest(new { message = "Please verify your email first." });

        user.FirstName = dto.FirstName.Trim();
        user.MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim();
        user.LastName = dto.LastName.Trim();
        user.HeightCm = dto.HeightCm;
        user.WeightKg = dto.WeightKg;
        user.GoalWeightKg = dto.GoalWeightKg;
        user.TargetDurationWeeks = dto.TargetDurationWeeks;
        user.OnboardingCompleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var (token, expiresAt) = _tokenService.GenerateToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            IsEmailVerified = true,
            OnboardingCompleted = true,
        });
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLowerInvariant());

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Account is deactivated." });

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            IsEmailVerified = user.IsEmailVerified,
            OnboardingCompleted = user.OnboardingCompleted,
        });
    }

    /// <summary>Get the currently authenticated user's profile.</summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            FullName = user.FullName,
            IsEmailVerified = user.IsEmailVerified,
            OnboardingCompleted = user.OnboardingCompleted,
            HeightCm = user.HeightCm,
            WeightKg = user.WeightKg,
            GoalWeightKg = user.GoalWeightKg,
            TargetDurationWeeks = user.TargetDurationWeeks,
            CreatedAt = user.CreatedAt,
        });
    }

    /// <summary>Update the current user's editable profile fields.</summary>
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        user.FirstName = dto.FirstName.Trim();
        user.MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim();
        user.LastName = dto.LastName.Trim();
        user.HeightCm = dto.HeightCm;
        user.WeightKg = dto.WeightKg;
        user.GoalWeightKg = dto.GoalWeightKg;
        user.TargetDurationWeeks = dto.TargetDurationWeeks;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            LastName = user.LastName,
            FullName = user.FullName,
            IsEmailVerified = user.IsEmailVerified,
            OnboardingCompleted = user.OnboardingCompleted,
            HeightCm = user.HeightCm,
            WeightKg = user.WeightKg,
            GoalWeightKg = user.GoalWeightKg,
            TargetDurationWeeks = user.TargetDurationWeeks,
            CreatedAt = user.CreatedAt,
        });
    }

    // ── Dev helper: get verification token (remove before production) ─────────
    [Authorize]
    [HttpGet("dev/verification-token")]
    public async Task<IActionResult> GetVerificationToken()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        return Ok(new { token = user.EmailVerificationToken });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub");
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
               .Replace("+", "-").Replace("/", "_").Replace("=", "");
}
