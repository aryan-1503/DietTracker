using DietTracker.API.DTOs;
using DietTracker.API.Models;
using DietTracker.API.Services;
using Xunit;

namespace DietTracker.Tests;

public class DashboardServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly DateOnly NoFloor = DateOnly.MinValue;

    private static DietPlan MakePlan(int slotCount = 3, DateOnly? startDate = null)
    {
        var plan = new DietPlan { Id = 1, UserId = 1, Name = "Test Plan", IsPrimary = true, IsActive = true, StartDate = startDate };
        for (int i = 0; i < slotCount; i++)
            plan.MealSlots.Add(new MealSlot { Id = i + 1, DietPlanId = 1, MealCategory = $"Meal{i}", StartTime = "08:00", EndTime = "09:00" });
        return plan;
    }

    private static List<DailyEntry> Entries(int planId, DateOnly date, int count, bool followed = false) =>
        Enumerable.Range(1, count)
            .Select(i => new DailyEntry { Id = i, UserId = 1, DietPlanId = planId, MealSlotId = i, EntryDate = date, FollowedPlan = followed })
            .ToList();

    // ── Today's Status ────────────────────────────────────────────────────────

    [Fact]
    public void TodayStatus_NoPrimaryPlan_ReturnsFalse()
    {
        var result = DashboardService.BuildTodayStatus(null, [], DateOnly.FromDateTime(DateTime.Today));
        Assert.False(result.HasPrimaryPlan);
    }

    [Fact]
    public void TodayStatus_NoEntries_ZeroCompleted()
    {
        var today = new DateOnly(2026, 9, 17);
        var result = DashboardService.BuildTodayStatus(MakePlan(3), [], today);
        Assert.Equal(0, result.CompletedSlots);
        Assert.Equal(3, result.RemainingSlots);
        Assert.Equal(0, result.ProgressPercent);
    }

    [Fact]
    public void TodayStatus_AllCompleted_100Percent()
    {
        var today = new DateOnly(2026, 9, 17);
        var plan  = MakePlan(3);
        var result = DashboardService.BuildTodayStatus(plan, Entries(plan.Id, today, 3), today);
        Assert.Equal(100, result.ProgressPercent);
        Assert.Equal(0, result.RemainingSlots);
    }

    [Fact]
    public void TodayStatus_FutureEntriesNotCounted()
    {
        var today = new DateOnly(2026, 9, 17);
        var plan  = MakePlan(3);
        var result = DashboardService.BuildTodayStatus(plan, Entries(plan.Id, today.AddDays(1), 3), today);
        Assert.Equal(0, result.CompletedSlots);
    }

    // ── Weekly Check-in ───────────────────────────────────────────────────────

    [Fact]
    public void WeeklyCheckin_MondayItself_OneDayApplicable()
    {
        var monday = new DateOnly(2026, 9, 14);
        var plan   = MakePlan(2);
        var result = DashboardService.BuildWeeklyCheckin(plan, Entries(plan.Id, monday, 2), monday, NoFloor);
        Assert.Equal(1, result.ApplicableDays);
        Assert.Equal(1, result.CompletedDays);
        Assert.Equal(100, result.Percent);
    }

    [Fact]
    public void WeeklyCheckin_Wednesday_ThreeDaysApplicable()
    {
        var wed  = new DateOnly(2026, 9, 17);
        var plan = MakePlan(2);
        var entries = new List<DailyEntry>();
        entries.AddRange(Entries(plan.Id, wed.AddDays(-2), 2)); // Mon complete
        entries.AddRange(Entries(plan.Id, wed.AddDays(-1), 1)); // Tue partial
        entries.AddRange(Entries(plan.Id, wed, 2));              // Wed complete
        var result = DashboardService.BuildWeeklyCheckin(plan, entries, wed, NoFloor);
        Assert.Equal(3, result.ApplicableDays);
        Assert.Equal(2, result.CompletedDays);
        Assert.Equal(67, result.Percent);
    }

    [Fact]
    public void WeeklyCheckin_FutureDaysNotCounted()
    {
        var monday = new DateOnly(2026, 9, 14);
        var plan   = MakePlan(2);
        // Entries for Tue–Sun (future from Monday's perspective)
        var entries = Enumerable.Range(1, 6)
            .SelectMany(i => Entries(plan.Id, monday.AddDays(i), 2)).ToList();
        var result = DashboardService.BuildWeeklyCheckin(plan, entries, monday, NoFloor);
        Assert.Equal(1, result.ApplicableDays);
        Assert.Equal(0, result.CompletedDays);
    }

    [Fact]
    public void WeeklyCheckin_FloorMidWeek_ReducesApplicableDays()
    {
        // Today = Wednesday 2026-09-17, floor = Wednesday (plan started today)
        var wed   = new DateOnly(2026, 9, 17);
        var plan  = MakePlan(2, startDate: wed);
        var entries = Entries(plan.Id, wed, 2);
        var result = DashboardService.BuildWeeklyCheckin(plan, entries, wed, wed);
        // Only Wednesday is applicable (Mon/Tue are before floor)
        Assert.Equal(1, result.ApplicableDays);
        Assert.Equal(1, result.CompletedDays);
        Assert.Equal(100, result.Percent);
    }

    // ── StartDate floor tests ─────────────────────────────────────────────────

    [Fact]
    public void RecentActivity_ExcludesDaysBeforeStartDate()
    {
        var today = new DateOnly(2026, 9, 17);
        var floor = new DateOnly(2026, 9, 15); // plan started 2 days ago
        var plan  = MakePlan(2, startDate: floor);

        var entries = new List<DailyEntry>();
        // Entries on Sep 13 and 14 (before floor) — must not appear
        entries.AddRange(Entries(plan.Id, new DateOnly(2026, 9, 13), 2));
        entries.AddRange(Entries(plan.Id, new DateOnly(2026, 9, 14), 2));
        // Entries on Sep 15, 16, 17 (on/after floor)
        entries.AddRange(Entries(plan.Id, floor, 2));
        entries.AddRange(Entries(plan.Id, floor.AddDays(1), 2));
        entries.AddRange(Entries(plan.Id, today, 2));

        var result = DashboardService.BuildRecentActivity(plan, entries, today, 7, floor);
        Assert.All(result, r => Assert.True(DateOnly.Parse(r.Date) >= floor,
            $"Day {r.Date} is before the floor {floor}"));
        Assert.Equal(3, result.Count); // Sep 15, 16, 17 only
    }

    [Fact]
    public void Chart_ExcludesDaysBeforeStartDate()
    {
        var today = new DateOnly(2026, 9, 17);
        var floor = new DateOnly(2026, 9, 15);
        var plan  = MakePlan(2, startDate: floor);

        var chart = DashboardService.BuildChart(plan, [], today, 7, floor);
        Assert.All(chart.Points, p => Assert.True(DateOnly.Parse(p.Date) >= floor,
            $"Point {p.Date} is before the floor {floor}"));
        Assert.Equal(3, chart.Points.Count); // Sep 15, 16, 17
    }

    [Fact]
    public void KeyStats_MissingDays_StartFromFloor()
    {
        var today = new DateOnly(2026, 9, 17);
        var floor = new DateOnly(2026, 9, 15); // 3 days: Sep 15, 16, 17
        var plan  = MakePlan(2, startDate: floor);

        // Only Sep 16 is complete
        var entries = Entries(plan.Id, floor.AddDays(1), 2);
        var stats   = DashboardService.BuildKeyStats(plan, entries, today, floor);

        // Sep 15 and 17 are missing → 2 missing days
        Assert.Equal(2, stats.MissingDays);
    }

    [Fact]
    public void KeyStats_NoStartDate_FallsBackToFirstEntry()
    {
        var today   = new DateOnly(2026, 9, 17);
        var plan    = MakePlan(2, startDate: null); // no start date
        var entries = Entries(plan.Id, today, 2, followed: true);
        var stats   = DashboardService.BuildKeyStats(plan, entries, today, NoFloor);
        Assert.Equal(100, stats.PlanFollowingPct);
        Assert.Equal(100, stats.MealCoveragePct);
    }

    // ── Chart ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Chart_NoEntries_AllNullAdherence()
    {
        var today = new DateOnly(2026, 9, 17);
        var chart = DashboardService.BuildChart(MakePlan(), [], today, 7, NoFloor);
        Assert.All(chart.Points, p => Assert.Null(p.AdherencePct));
    }

    [Fact]
    public void Chart_NoPlan_EmptyPoints()
    {
        var today = new DateOnly(2026, 9, 17);
        Assert.Empty(DashboardService.BuildChart(null, [], today, 7, NoFloor).Points);
    }

    [Fact]
    public void Chart_AllFollowed_100Percent()
    {
        var today = new DateOnly(2026, 9, 17);
        var plan  = MakePlan(2);
        var chart = DashboardService.BuildChart(plan, Entries(plan.Id, today, 2, followed: true), today, 7, NoFloor);
        Assert.Equal(100, chart.Points.Last().AdherencePct);
    }

    [Fact]
    public void Chart_FutureDatesNeverIncluded()
    {
        var today = new DateOnly(2026, 9, 17);
        var chart = DashboardService.BuildChart(MakePlan(), [], today, 7, NoFloor);
        Assert.All(chart.Points, p => Assert.True(DateOnly.Parse(p.Date) <= today));
    }

    [Fact]
    public void Chart_30Days_Has30Points_WithNoFloor()
    {
        var today = new DateOnly(2026, 9, 17);
        Assert.Equal(30, DashboardService.BuildChart(MakePlan(), [], today, 30, NoFloor).Points.Count);
    }

    // ── Recent Activity ───────────────────────────────────────────────────────

    [Fact]
    public void RecentActivity_NoPlan_EmptyList()
    {
        Assert.Empty(DashboardService.BuildRecentActivity(null, [], new DateOnly(2026, 9, 17), 7, NoFloor));
    }

    [Fact]
    public void RecentActivity_AllMissing_StatusMissing()
    {
        var today = new DateOnly(2026, 9, 17);
        var result = DashboardService.BuildRecentActivity(MakePlan(3), [], today, 7, NoFloor);
        Assert.All(result, r => Assert.Equal(DayStatus.Missing, r.Status));
    }

    [Fact]
    public void RecentActivity_AllComplete_StatusCompleted()
    {
        var today   = new DateOnly(2026, 9, 17);
        var plan    = MakePlan(3);
        var entries = Entries(plan.Id, today, 3);
        var result  = DashboardService.BuildRecentActivity(plan, entries, today, 7, NoFloor);
        Assert.Equal(DayStatus.Completed, result.First().Status);
    }

    [Fact]
    public void RecentActivity_Partial_StatusPartial()
    {
        var today  = new DateOnly(2026, 9, 17);
        var plan   = MakePlan(3);
        var result = DashboardService.BuildRecentActivity(plan, Entries(plan.Id, today, 1), today, 7, NoFloor);
        Assert.Equal(DayStatus.Partial, result.First().Status);
    }

    [Fact]
    public void RecentActivity_NewestFirst()
    {
        var today  = new DateOnly(2026, 9, 17);
        var result = DashboardService.BuildRecentActivity(MakePlan(2), [], today, 7, NoFloor);
        Assert.Equal(today.ToString("yyyy-MM-dd"), result[0].Date);
    }

    // ── Zero Denominator Guards ───────────────────────────────────────────────

    [Fact]
    public void BuildKeyStats_NoPlan_NoException()
    {
        var result = DashboardService.BuildKeyStats(null, [], new DateOnly(2026, 9, 17), NoFloor);
        Assert.Equal(0, result.PlanFollowingPct);
        Assert.Equal(0, result.MealCoveragePct);
        Assert.Equal(0, result.MissingDays);
    }

    [Fact]
    public void BuildKeyStats_ZeroSlotPlan_NoException()
    {
        var result = DashboardService.BuildKeyStats(MakePlan(0), [], new DateOnly(2026, 9, 17), NoFloor);
        Assert.Equal(0, result.PlanFollowingPct);
        Assert.Equal(0, result.MealCoveragePct);
    }

    [Fact]
    public void PlanFollowingPct_AllFollowed_100()
    {
        var today   = new DateOnly(2026, 9, 17);
        var plan    = MakePlan(3);
        var result  = DashboardService.BuildKeyStats(plan, Entries(plan.Id, today, 3, followed: true), today, NoFloor);
        Assert.Equal(100, result.PlanFollowingPct);
    }

    [Fact]
    public void MissingDays_AllDaysComplete_Zero()
    {
        var today   = new DateOnly(2026, 9, 17);
        var plan    = MakePlan(2);
        var entries = Enumerable.Range(0, 3)
            .SelectMany(d => Entries(plan.Id, today.AddDays(-d), 2)).ToList();
        Assert.Equal(0, DashboardService.BuildKeyStats(plan, entries, today, NoFloor).MissingDays);
    }

    [Fact]
    public void MissingDays_OneDayPartial_CountsAsMissing()
    {
        var today  = new DateOnly(2026, 9, 17);
        var plan   = MakePlan(3);
        Assert.Equal(1, DashboardService.BuildKeyStats(plan, Entries(plan.Id, today, 1), today, NoFloor).MissingDays);
    }
}
