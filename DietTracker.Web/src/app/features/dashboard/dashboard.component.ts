import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { DashboardService } from '../../services/dashboard.service';
import { WeightService } from '../../services/weight.service';
import { UserProfile } from '../../models/auth.models';
import { DashboardDto, ChartPointDto, DayStatus } from '../../models/dashboard.models';
import { WeightEntry } from '../../models/weight.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private auth = inject(AuthService);
  private dashSvc = inject(DashboardService);
  private weightSvc = inject(WeightService);
  private router = inject(Router);

  readonly profile = signal<UserProfile | null>(null);
  readonly dashboard = signal<DashboardDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly chartRange = signal<7 | 30>(7);
  readonly weightEntries = signal<WeightEntry[]>([]);

  readonly chartPoints = computed(() => {
    const d = this.dashboard();
    if (!d) return [];
    return this.chartRange() === 7 ? d.chart7Day.points : d.chart30Day.points;
  });

  ngOnInit(): void {
    this.auth.getProfile().subscribe({ next: p => this.profile.set(p) });
    this.dashSvc.get().subscribe({
      next: d => { this.dashboard.set(d); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
    this.loadWeightEntries();
  }

  loadWeightEntries(): void {
    this.weightSvc.getAll().subscribe({
      next: entries => this.weightEntries.set(entries),
      error: () => this.weightEntries.set([]),
    });
  }

  setChartRange(r: 7 | 30) { this.chartRange.set(r); }

  // ── SVG chart helpers (adherence) ─────────────────────────────────────────

  readonly CHART_W = 320;
  readonly CHART_H = 120;
  readonly PAD = { top: 8, right: 8, bottom: 24, left: 32 };

  private innerW() { return this.CHART_W - this.PAD.left - this.PAD.right; }
  private innerH() { return this.CHART_H - this.PAD.top - this.PAD.bottom; }

  chartDataPoints(points: ChartPointDto[]) {
    const data = points
      .map((p, i) => ({ ...p, i }))
      .filter(p => p.adherencePct !== null);
    if (data.length === 0) return [];
    const total = points.length;
    return data.map(p => ({
      x: this.PAD.left + (p.i / Math.max(total - 1, 1)) * this.innerW(),
      y: this.PAD.top + (1 - p.adherencePct! / 100) * this.innerH(),
      pct: p.adherencePct!,
      date: p.date,
      followed: p.followedCount,
      answered: p.answeredCount,
    }));
  }

  chartPath(points: ChartPointDto[]): string {
    return this.chartDataPoints(points)
      .map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`)
      .join(' ');
  }

  chartAreaPath(points: ChartPointDto[]): string {
    const pts = this.chartDataPoints(points);
    if (!pts.length) return '';
    const bottom = this.CHART_H - this.PAD.bottom;
    const line = pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
    return `${line} L${pts[pts.length - 1].x.toFixed(1)},${bottom} L${pts[0].x.toFixed(1)},${bottom} Z`;
  }

  chartXLabels(points: ChartPointDto[]): { x: number; label: string }[] {
    if (!points.length) return [];
    const total = points.length;
    const step = total <= 7 ? 1 : Math.ceil(total / 6);
    return points
      .filter((_, i) => i % step === 0)
      .map((p, _, arr) => {
        const idx = points.indexOf(p);
        const x = this.PAD.left + (idx / Math.max(total - 1, 1)) * this.innerW();
        const d = new Date(p.date + 'T00:00:00');
        return { x, label: `${d.getDate()}/${d.getMonth() + 1}` };
      });
  }

  hasChartData(points: ChartPointDto[]): boolean {
    return points.some(p => p.adherencePct !== null);
  }

  // ── Weight chart SVG helpers ──────────────────────────────────────────────

  readonly W_CHART_W = 320;
  readonly W_CHART_H = 130;
  readonly W_PAD = { top: 10, right: 12, bottom: 28, left: 40 };

  private wInnerW() { return this.W_CHART_W - this.W_PAD.left - this.W_PAD.right; }
  private wInnerH() { return this.W_CHART_H - this.W_PAD.top - this.W_PAD.bottom; }

  weightChartPoints(): { x: number; y: number; kg: number; label: string }[] {
    const entries = this.weightEntries();
    if (!entries.length) return [];

    const minKg = Math.min(...entries.map(e => e.weightKg));
    const maxKg = Math.max(...entries.map(e => e.weightKg));
    // Add a little padding to Y range so points aren't pinned to edges
    const kgRange = Math.max(maxKg - minKg, 5);
    const kgMin = minKg - kgRange * 0.1;
    const kgMax = maxKg + kgRange * 0.1;

    const total = entries.length;
    return entries.map((e, i) => ({
      x: this.W_PAD.left + (i / Math.max(total - 1, 1)) * this.wInnerW(),
      y: this.W_PAD.top + (1 - (e.weightKg - kgMin) / (kgMax - kgMin)) * this.wInnerH(),
      kg: e.weightKg,
      label: e.weekLabel,
    }));
  }

  weightChartPath(): string {
    return this.weightChartPoints()
      .map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`)
      .join(' ');
  }

  weightChartAreaPath(): string {
    const pts = this.weightChartPoints();
    if (!pts.length) return '';
    const bottom = this.W_CHART_H - this.W_PAD.bottom;
    const line = pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
    return `${line} L${pts[pts.length - 1].x.toFixed(1)},${bottom} L${pts[0].x.toFixed(1)},${bottom} Z`;
  }

  weightXLabels(): { x: number; label: string }[] {
    const entries = this.weightEntries();
    if (!entries.length) return [];
    const total = entries.length;
    const step = total <= 8 ? 1 : Math.ceil(total / 7);
    return entries
      .filter((_, i) => i % step === 0)
      .map((e, _, arr) => {
        const i = entries.indexOf(e);
        const x = this.W_PAD.left + (i / Math.max(total - 1, 1)) * this.wInnerW();
        return { x, label: e.weekLabel };
      });
  }

  weightYLabels(): { y: number; label: string }[] {
    const entries = this.weightEntries();
    if (!entries.length) return [];
    const minKg = Math.min(...entries.map(e => e.weightKg));
    const maxKg = Math.max(...entries.map(e => e.weightKg));
    const kgRange = Math.max(maxKg - minKg, 5);
    const kgMin = minKg - kgRange * 0.1;
    const kgMax = maxKg + kgRange * 0.1;

    const steps = 4;
    return Array.from({ length: steps + 1 }, (_, i) => {
      const frac = i / steps;
      const kg = kgMin + frac * (kgMax - kgMin);
      const y = this.W_PAD.top + (1 - frac) * this.wInnerH();
      return { y, label: kg.toFixed(1) };
    });
  }

  // ── Formatting ─────────────────────────────────────────────────────────────

  formatDate(iso: string): string {
    const d = new Date(iso + 'T00:00:00');
    return d.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
  }

  statusLabel(s: string): string {
    return s === 'Completed' ? 'Done' : s === 'Partial' ? 'Partial' : 'Missing';
  }

  statusClass(s: string): string {
    return s === 'Completed' ? 'pill--complete' : s === 'Partial' ? 'pill--partial' : 'pill--missing';
  }

  // ── Navigation ─────────────────────────────────────────────────────────────

  goToProfile() { this.router.navigate(['/profile']); }
  goToDietPlans() { this.router.navigate(['/diet-plans']); }
  goToDailyIntake() { this.router.navigate(['/daily-intake']); }
  goToSettings() { this.router.navigate(['/settings']); }
  goToReports() { this.router.navigate(['/reports']); }
  goToWeightLog() { this.router.navigate(['/weight-log']); }
  goToDate(date: string) { this.router.navigate(['/daily-intake'], { queryParams: { date } }); }
  logout() { this.auth.logout(); }
}
