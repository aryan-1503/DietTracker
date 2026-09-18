using DietTracker.API.Data;
using DietTracker.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Services;

/// <summary>
/// All dashboard calculations are server-side.
/// Single DB round-trip: loads primary plan + entries for that plan once,
/// then computes everything in memory.
///
/// StartDate floor: when a plan has a StartDate set, NO data before that date
/// is shown or counted anywhere on the dashboard.
/// </summary>
public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(int userId, DateOnly today, CancellationToken ct = default);
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardDto> GetDashboardAsync(int userId, DateOnly today, CancellationToken ct = default)
    {
        // ── 1. Load primary plan ──────────────────────────────────────────────
        var primaryPlan = await _db.DietPlans
            .Where(dp => dp.UserId == userId && dp.IsPrimary && dp.IsActive)
            .Include(dp => dp.MealSlots)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        // ── 2. Determine the hard floor date ──────────────────────────────────
        // If the plan has a StartDate, nothing before it is ever shown.
        var floor = primaryPlan?.StartDate ?? DateOnly.MinValue;

        // ── 3. Load entries scoped to [floor, today] ──────────────────────────
        var allEntries = await _db.DailyEntries
            .Where(de => de.UserId == userId
                      && de.EntryDate <= today
                      && (primaryPlan == null || de.DietPlanId == primaryPlan.Id)
                      && de.EntryDate >= floor)
            .AsNoTracking()
            .ToListAsync(ct);

        return new DashboardDto
        {
            TodayStatus    = BuildTodayStatus(primaryPlan, allEntries, today),
            KeyStats       = BuildKeyStats(primaryPlan, allEntries, today, floor),
            Chart7Day      = BuildChart(primaryPlan, allEntries, today, 7,  floor),
            Chart30Day     = BuildChart(primaryPlan, allEntries, today, 30, floor),
            RecentActivity = BuildRecentActivity(primaryPlan, allEntries, today, 7, floor),
        };
    }

    // ── Today's Status ────────────────────────────────────────────────────────

    internal static TodayStatusDto BuildTodayStatus(
        Models.DietPlan? plan,
        IEnumerable<Models.DailyEntry> allEntries,
        DateOnly today)
    {
        if (plan is null)
            return new TodayStatusDto { HasPrimaryPlan = false };

        var totalSlots     = plan.MealSlots.Count;
        var completedSlots = allEntries.Count(e => e.EntryDate == today);
        var progressPct    = totalSlots == 0 ? 0
            : (int)Math.Round(completedSlots * 100.0 / totalSlots);

        return new TodayStatusDto
        {
            HasPrimaryPlan  = true,
            DietPlanId      = plan.Id,
            DietPlanName    = plan.Name,
            TotalSlots      = totalSlots,
            CompletedSlots  = Math.Min(completedSlots, totalSlots),
            RemainingSlots  = Math.Max(totalSlots - completedSlots, 0),
            ProgressPercent = Math.Min(progressPct, 100),
        };
    }

    // ── Key Stats ─────────────────────────────────────────────────────────────

    internal static KeyStatsDto BuildKeyStats(
        Models.DietPlan? plan,
        IEnumerable<Models.DailyEntry> allEntries,
        DateOnly today,
        DateOnly floor)
    {
        var stats = new KeyStatsDto();
        if (plan is null) return stats;

        var totalSlots = plan.MealSlots.Count;
        if (totalSlots == 0) return stats;

        var entries = allEntries.ToList(); // already filtered to [floor, today] in the query

        // A. Weekly check-in (Mon–today of current week, never before floor)
        stats.WeeklyCheckin = BuildWeeklyCheckin(plan, entries, today, floor);

        // B. Plan following — % of answered entries where FollowedPlan = true
        var followed = entries.Count(e => e.FollowedPlan);
        stats.PlanFollowingPct = entries.Count == 0
            ? 0
            : (int)Math.Round(followed * 100.0 / entries.Count);

        // C. Meal coverage — answered / required over applicable days
        var applicableDays = GetApplicableDayCount(plan, entries, today, floor);
        var totalRequired  = applicableDays * totalSlots;
        stats.MealCoveragePct = totalRequired == 0
            ? 0
            : (int)Math.Round(entries.Count * 100.0 / totalRequired);

        // D. Missing days
        stats.MissingDays = CountMissingDays(plan, entries, today, floor);

        return stats;
    }

    internal static WeeklyCheckinDto BuildWeeklyCheckin(
        Models.DietPlan plan,
        List<Models.DailyEntry> entries,
        DateOnly today,
        DateOnly floor)
    {
        var totalSlots = plan.MealSlots.Count;
        if (totalSlots == 0) return new WeeklyCheckinDto();

        // Monday of current week
        var dow           = (int)today.DayOfWeek; // 0=Sun..6=Sat
        var daysFromMon   = dow == 0 ? 6 : dow - 1;
        var monday        = today.AddDays(-daysFromMon);

        // Applicable: Mon–today, but never before floor
        var effectiveStart = monday < floor ? floor : monday;
        if (effectiveStart > today) return new WeeklyCheckinDto();

        var applicableDays = today.DayNumber - effectiveStart.DayNumber + 1;

        int completedDays = 0;
        for (int d = 0; d < applicableDays; d++)
        {
            var day       = effectiveStart.AddDays(d);
            var dayCount  = entries.Count(e => e.EntryDate == day);
            if (dayCount >= totalSlots) completedDays++;
        }

        return new WeeklyCheckinDto
        {
            CompletedDays  = completedDays,
            ApplicableDays = applicableDays,
            Percent        = (int)Math.Round(completedDays * 100.0 / applicableDays),
        };
    }

    // ── Chart ─────────────────────────────────────────────────────────────────

    internal static AdherenceChartDto BuildChart(
        Models.DietPlan? plan,
        IEnumerable<Models.DailyEntry> allEntries,
        DateOnly today,
        int days,
        DateOnly floor)
    {
        var points = new List<ChartPointDto>();
        if (plan is null) return new AdherenceChartDto { Points = points };

        var entries     = allEntries.ToList();
        var windowStart = today.AddDays(-(days - 1));
        // Respect the floor: don't show days before the plan started
        var start       = windowStart < floor ? floor : windowStart;

        for (var day = start; day <= today; day = day.AddDays(1))
        {
            var dayEntries = entries.Where(e => e.EntryDate == day).ToList();
            var answered   = dayEntries.Count;
            var followed   = dayEntries.Count(e => e.FollowedPlan);

            points.Add(new ChartPointDto
            {
                Date          = day.ToString("yyyy-MM-dd"),
                AdherencePct  = answered == 0 ? null : (int)Math.Round(followed * 100.0 / answered),
                FollowedCount = followed,
                AnsweredCount = answered,
            });
        }

        return new AdherenceChartDto { Points = points };
    }

    // ── Recent Activity ───────────────────────────────────────────────────────

    internal static List<RecentDayDto> BuildRecentActivity(
        Models.DietPlan? plan,
        IEnumerable<Models.DailyEntry> allEntries,
        DateOnly today,
        int daysBack,
        DateOnly floor)
    {
        var result = new List<RecentDayDto>();
        if (plan is null) return result;

        var totalSlots  = plan.MealSlots.Count;
        var entries     = allEntries.ToList();
        var windowStart = today.AddDays(-(daysBack - 1));
        // Respect the floor: never show days before the plan started
        var start       = windowStart < floor ? floor : windowStart;

        for (var day = start; day <= today; day = day.AddDays(1))
        {
            var dayEntries = entries.Where(e => e.EntryDate == day).ToList();
            var completed  = dayEntries.Count;
            var followed   = dayEntries.Count(e => e.FollowedPlan);

            DayStatus status = completed == 0         ? DayStatus.Missing
                             : completed >= totalSlots ? DayStatus.Completed
                             :                          DayStatus.Partial;

            result.Add(new RecentDayDto
            {
                Date           = day.ToString("yyyy-MM-dd"),
                CompletedMeals = completed,
                TotalMeals     = totalSlots,
                AdherencePct   = completed == 0 ? null : (int)Math.Round(followed * 100.0 / completed),
                Status         = status.ToString(),
            });
        }

        // Newest first
        result.Reverse();
        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Number of calendar days from the effective start (plan.StartDate or first
    /// entry date, whichever is later) through today — used as the coverage denominator.
    /// </summary>
    private static int GetApplicableDayCount(
        Models.DietPlan plan,
        List<Models.DailyEntry> entries,
        DateOnly today,
        DateOnly floor)
    {
        // floor already accounts for StartDate (it was applied to the DB query).
        // Use the later of floor and the earliest actual entry.
        var start = floor > DateOnly.MinValue ? floor
                  : entries.Any()             ? entries.Min(e => e.EntryDate)
                  : today;

        if (start > today) return 0;
        return today.DayNumber - start.DayNumber + 1;
    }

    private static int CountMissingDays(
        Models.DietPlan plan,
        List<Models.DailyEntry> entries,
        DateOnly today,
        DateOnly floor)
    {
        var totalSlots = plan.MealSlots.Count;
        if (totalSlots == 0) return 0;

        // Start from floor (or first entry if no floor)
        var start = floor > DateOnly.MinValue ? floor
                  : entries.Any()             ? entries.Min(e => e.EntryDate)
                  : today;

        int missing = 0;
        for (var d = start; d <= today; d = d.AddDays(1))
        {
            var dayCount = entries.Count(e => e.EntryDate == d);
            if (dayCount < totalSlots) missing++;
        }

        return missing;
    }
}
