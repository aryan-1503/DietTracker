import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { DashboardService } from '../../services/dashboard.service';
import { UserProfile } from '../../models/auth.models';
import { DashboardDto, ChartPointDto, DayStatus } from '../../models/dashboard.models';

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
  private router = inject(Router);

  readonly profile = signal<UserProfile | null>(null);
  readonly dashboard = signal<DashboardDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly chartRange = signal<7 | 30>(7);

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
  }

  setChartRange(r: 7 | 30) { this.chartRange.set(r); }

  // ── SVG chart helpers ──────────────────────────────────────────────────────

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
  goToDate(date: string) { this.router.navigate(['/daily-intake'], { queryParams: { date } }); }
  logout() { this.auth.logout(); }
}
