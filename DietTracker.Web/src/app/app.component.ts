import { Component, OnInit, signal, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from './services/auth.service';
import { WeightService } from './services/weight.service';
import { WeeklyWeightModalComponent } from './components/weekly-weight-modal/weekly-weight-modal.component';
import { PendingWeightCheck } from './models/weight.models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, WeeklyWeightModalComponent],
  template: `
    <router-outlet />
    @if (showWeightModal() && pendingCheck()) {
      <app-weekly-weight-modal
        [pendingCheck]="pendingCheck()!"
        (saved)="onWeightSaved()"
        (dismissed)="onWeightDismissed()"
      />
    }
  `,
})
export class AppComponent implements OnInit {
  private auth = inject(AuthService);
  private weightSvc = inject(WeightService);
  private router = inject(Router);

  readonly showWeightModal = signal(false);
  readonly pendingCheck = signal<PendingWeightCheck | null>(null);

  /** Track if we already checked this session tab (so we only dismiss once per tab). */
  private checkedThisSession = false;

  ngOnInit(): void {
    // Trigger check on every navigation (covers app load, login redirect, route changes)
    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => {
        this.maybeTriggerWeightCheck();
      });
  }

  private maybeTriggerWeightCheck(): void {
    // Only check when authenticated
    if (!this.auth.isLoggedIn()) return;
    // If already dismissed this session and modal is hidden, skip
    if (this.checkedThisSession && !this.showWeightModal()) return;

    this.weightSvc.getPendingCheck().subscribe({
      next: (check) => {
        if (check.hasPendingEntry) {
          this.pendingCheck.set(check);
          this.showWeightModal.set(true);
        }
      },
      error: () => {
        // Silently ignore errors — weight popup is non-blocking
      },
    });
  }

  onWeightSaved(): void {
    this.showWeightModal.set(false);
    this.pendingCheck.set(null);
    this.checkedThisSession = true;
    // Trigger dashboard refresh by navigating to current route
    const currentUrl = this.router.url;
    if (currentUrl.includes('/dashboard')) {
      this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
        this.router.navigate(['/dashboard']);
      });
    }
  }

  onWeightDismissed(): void {
    // Dismiss for this tab session only — no local storage flag
    this.showWeightModal.set(false);
    // Do NOT set checkedThisSession = true, so next navigation re-check fires
    // But we do reset so the next navigation re-opens the modal
    this.checkedThisSession = false;
  }
}
