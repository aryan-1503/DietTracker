using DietTracker.API.Data;
using DietTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Services;

/// <summary>
/// Background service that fires once per minute, checks each user's configured
/// reminder time in their local timezone, and sends a reminder email when:
///  - The current local time matches their ReminderTime (HH:mm)
///  - Today's required meal entries are not all completed
///  - A reminder has not already been sent for today
/// </summary>
public class DailyReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyReminderService> _logger;

    public DailyReminderService(IServiceScopeFactory scopeFactory, ILogger<DailyReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyReminderService started.");

        // Align to the next whole minute before entering the loop
        var now = DateTime.UtcNow;
        var delay = TimeSpan.FromSeconds(60 - now.Second);
        await Task.Delay(delay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in DailyReminderService tick.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Load all active, onboarded users with their settings
        var users = await db.Users
            .Where(u => u.IsActive && u.OnboardingCompleted)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                Settings = db.UserSettings.FirstOrDefault(s => s.UserId == u.Id)
            })
            .ToListAsync(ct);

        foreach (var user in users)
        {
            try
            {
                var reminderTime = user.Settings?.ReminderTime ?? "21:00";
                var tzId = user.Settings?.TimeZoneId ?? "UTC";

                // Resolve timezone — support both IANA ("Asia/Kolkata") and Windows ("India Standard Time") IDs.
                // On Windows, FindSystemTimeZoneById only accepts Windows IDs, so we try an IANA→Windows
                // conversion first when the direct lookup fails.
                TimeZoneInfo tz;
                try
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
                }
                catch
                {
                    // Try converting IANA → Windows TZ ID (no-op on Linux where IANA is native)
                    if (TimeZoneInfo.TryConvertIanaIdToWindowsId(tzId, out var windowsTzId))
                    {
                        try { tz = TimeZoneInfo.FindSystemTimeZoneById(windowsTzId); }
                        catch
                        {
                            _logger.LogWarning("Could not load Windows TZ '{WindowsTzId}' (from IANA '{IanaTzId}') for user {UserId}, falling back to UTC.", windowsTzId, tzId, user.Id);
                            tz = TimeZoneInfo.Utc;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Unknown timezone '{TzId}' for user {UserId}, falling back to UTC.", tzId, user.Id);
                        tz = TimeZoneInfo.Utc;
                    }
                }

                var userLocalNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                var currentHhMm = userLocalNow.ToString("HH:mm");

                // Only fire at the configured minute
                // if (currentHhMm != reminderTime)
                //     continue;

                var today = DateOnly.FromDateTime(userLocalNow);

                // Already sent today?
                //var alreadySent = await db.ReminderLogs
                //    .AnyAsync(rl => rl.UserId == user.Id && rl.ReminderDate == today, ct);
                //if (alreadySent)
                //    continue;

                // Find primary diet plan
                var primaryPlan = await db.DietPlans
                    .Where(dp => dp.UserId == user.Id && dp.IsPrimary && dp.IsActive)
                    .Include(dp => dp.MealSlots)
                    .FirstOrDefaultAsync(ct);

                if (primaryPlan is null || !primaryPlan.MealSlots.Any())
                    continue;

                var totalSlots = primaryPlan.MealSlots.Count;

                // Count how many slots have been logged today
                var completedSlots = await db.DailyEntries
                    .CountAsync(de => de.UserId == user.Id
                                   && de.EntryDate == today
                                   && de.DietPlanId == primaryPlan.Id, ct);

                // All done — no reminder needed
                if (completedSlots >= totalSlots)
                    continue;

                // Send reminder
                var displayName = user.FirstName ?? user.Email;
                await emailService.SendReminderEmailAsync(
                    user.Email, displayName, today, completedSlots, totalSlots, ct);

                // Record the send to prevent duplicates
                db.ReminderLogs.Add(new ReminderLog
                {
                    UserId = user.Id,
                    ReminderDate = today,
                    SentAt = DateTime.UtcNow,
                });
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to process reminder for user {UserId}.", user.Id);
            }
        }
    }
}
