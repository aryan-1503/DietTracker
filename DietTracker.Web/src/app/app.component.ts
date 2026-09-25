import { Component, OnInit, signal, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from './services/auth.service';
import { WeightService } from './services/weight.service';
import { WeightPopupDeferralService } from './services/weight-popup-deferral.service';
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
  private deferral = inject(WeightPopupDeferralService);

  readonly showWeightModal = signal(false);
  readonly pendingCheck = signal<PendingWeightCheck | null>(null);

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

    // If modal is already open, do nothing — avoid stacking calls
    if (this.showWeightModal()) return;

    // Respect the "Enter Later" deferral: skip until end of current calendar day
    if (this.deferral.isDeferredForToday()) return;

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
    // Clear any lingering deferral so normal weekly-check logic resumes
    this.deferral.clearDeferral();
    // Trigger dashboard refresh by navigating to current route
    const currentUrl = this.router.url;
    if (currentUrl.includes('/dashboard')) {
      this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
        this.router.navigate(['/dashboard']);
      });
    }
  }

  onWeightDismissed(): void {
    this.showWeightModal.set(false);
    this.pendingCheck.set(null);
    // Persist the deferral until 23:59:59 of the current local day.
    // Subsequent navigations and page reloads will read this value and skip
    // the popup for the rest of the day.
    this.deferral.deferUntilEndOfDay();
  }
}
