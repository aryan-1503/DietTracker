import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ReportService } from '../../services/report.service';
import { MonthlyReportMetaDto } from '../../models/daily-intake.models';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss',
})
export class ReportsComponent implements OnInit {
  private readonly svc    = inject(ReportService);
  private readonly router = inject(Router);

  // Default to current month
  private readonly now = new Date();
  readonly selectedYear  = signal(this.now.getFullYear());
  readonly selectedMonth = signal(this.now.getMonth() + 1); // 1-based

  readonly loading     = signal(false);
  readonly downloading = signal(false);
  readonly error       = signal<string | null>(null);
  readonly meta        = signal<MonthlyReportMetaDto | null>(null);

  /** Last 13 months (current + 12 prior), newest first. */
  readonly monthOptions = computed(() => {
    const opts: { year: number; month: number; label: string; value: string }[] = [];
    const base = new Date(this.now.getFullYear(), this.now.getMonth(), 1);
    for (let i = 0; i < 13; i++) {
      const d = new Date(base.getFullYear(), base.getMonth() - i, 1);
      const y = d.getFullYear();
      const m = d.getMonth() + 1;
      opts.push({
        year:  y,
        month: m,
        label: d.toLocaleString('default', { month: 'long', year: 'numeric' }),
        value: `${y}-${String(m).padStart(2, '0')}`,
      });
    }
    return opts;
  });

  readonly selectedValue = computed(() =>
    `${this.selectedYear()}-${String(this.selectedMonth()).padStart(2, '0')}`);

  ngOnInit(): void {
    this.loadMeta();
  }

  onMonthChange(value: string): void {
    const [y, m] = value.split('-').map(Number);
    this.selectedYear.set(y);
    this.selectedMonth.set(m);
    this.loadMeta();
  }

  private loadMeta(): void {
    this.loading.set(true);
    this.error.set(null);
    this.meta.set(null);
    this.svc.getMeta(this.selectedYear(), this.selectedMonth()).subscribe({
      next:  m => { this.meta.set(m); this.loading.set(false); },
      error: () => { this.error.set('Failed to load report info. Please try again.'); this.loading.set(false); },
    });
  }

  downloadExcel(): void {
    this.downloading.set(true);
    this.error.set(null);
    this.svc.downloadExcel(this.selectedYear(), this.selectedMonth()).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const a   = document.createElement('a');
        a.href     = url;
        a.download = `DietTracker_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        this.downloading.set(false);
      },
      error: () => {
        this.error.set('Failed to download report. Please try again.');
        this.downloading.set(false);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
