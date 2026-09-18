namespace DietTracker.API.DTOs;

// ── Today's Status ────────────────────────────────────────────────────────────

public class TodayStatusDto
{
    public bool   HasPrimaryPlan   { get; set; }
    public int    DietPlanId       { get; set; }
    public string DietPlanName     { get; set; } = string.Empty;
    public int    TotalSlots       { get; set; }
    public int    CompletedSlots   { get; set; }
    public int    RemainingSlots   { get; set; }
    public int    ProgressPercent  { get; set; }
}

// ── Key Statistics ────────────────────────────────────────────────────────────

public class WeeklyCheckinDto
{
    /// <summary>Fully-completed days in the current Mon–Sun week (past + today only).</summary>
    public int CompletedDays  { get; set; }
    /// <summary>Elapsed days in the current week (Mon through today).</summary>
    public int ApplicableDays { get; set; }
    public int Percent        { get; set; }
}

public class KeyStatsDto
{
    public WeeklyCheckinDto WeeklyCheckin    { get; set; } = new();
    /// <summary>% of answered meal entries where FollowedPlan = true.</summary>
    public int              PlanFollowingPct { get; set; }
    /// <summary>% of required meal slots (past days) that have an entry.</summary>
    public int              MealCoveragePct  { get; set; }
    /// <summary>Count of past days with at least one unanswered required slot.</summary>
    public int              MissingDays      { get; set; }
}

// ── Adherence Chart ───────────────────────────────────────────────────────────

public class ChartPointDto
{
    public string Date          { get; set; } = string.Empty;  // YYYY-MM-DD
    /// <summary>Null when no answered meals that day (do not plot as 0).</summary>
    public int?   AdherencePct  { get; set; }
    public int    FollowedCount { get; set; }
    public int    AnsweredCount { get; set; }
}

public class AdherenceChartDto
{
    public List<ChartPointDto> Points { get; set; } = new();
}

// ── Recent Activity ───────────────────────────────────────────────────────────

public enum DayStatus { Completed, Partial, Missing }

public class RecentDayDto
{
    public string    Date           { get; set; } = string.Empty;  // YYYY-MM-DD
    public int       CompletedMeals { get; set; }
    public int       TotalMeals     { get; set; }
    /// <summary>Null when no answered meals (Missing day).</summary>
    public int?      AdherencePct   { get; set; }
    public string Status         { get; set; }
}

// ── Full Dashboard Response ───────────────────────────────────────────────────

public class DashboardDto
{
    public TodayStatusDto   TodayStatus    { get; set; } = new();
    public KeyStatsDto      KeyStats       { get; set; } = new();
    public AdherenceChartDto Chart7Day     { get; set; } = new();
    public AdherenceChartDto Chart30Day    { get; set; } = new();
    public List<RecentDayDto> RecentActivity { get; set; } = new();
}
