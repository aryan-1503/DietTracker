// ── Today's Status ────────────────────────────────────────────────────────────

export interface TodayStatusDto {
  hasPrimaryPlan: boolean;
  dietPlanId: number;
  dietPlanName: string;
  totalSlots: number;
  completedSlots: number;
  remainingSlots: number;
  progressPercent: number;
}

// ── Key Statistics ────────────────────────────────────────────────────────────

export interface WeeklyCheckinDto {
  completedDays: number;
  applicableDays: number;
  percent: number;
}

export interface KeyStatsDto {
  weeklyCheckin: WeeklyCheckinDto;
  planFollowingPct: number;
  mealCoveragePct: number;
  missingDays: number;
}

// ── Chart ─────────────────────────────────────────────────────────────────────

export interface ChartPointDto {
  date: string;           // YYYY-MM-DD
  adherencePct: number | null;   // null = no data that day
  followedCount: number;
  answeredCount: number;
}

export interface AdherenceChartDto {
  points: ChartPointDto[];
}

// ── Recent Activity ───────────────────────────────────────────────────────────

export type DayStatus = 'Completed' | 'Partial' | 'Missing';

export interface RecentDayDto {
  date: string;            // YYYY-MM-DD
  completedMeals: number;
  totalMeals: number;
  adherencePct: number | null;
  status: string;
}

// ── Full Dashboard ────────────────────────────────────────────────────────────

export interface DashboardDto {
  todayStatus: TodayStatusDto;
  keyStats: KeyStatsDto;
  chart7Day: AdherenceChartDto;
  chart30Day: AdherenceChartDto;
  recentActivity: RecentDayDto[];
}
