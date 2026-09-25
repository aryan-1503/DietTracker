import { Injectable, inject } from '@angular/core';
import { AuthService } from './auth.service';

/**
 * Manages the "Enter Later" deferral state for the weekly weight popup.
 *
 * Suppression is stored in localStorage keyed per user so that one user's
 * deferral never affects another user's session on a shared device.
 *
 * Key format: `weight_popup_deferred_until_<userId>`
 * Value: ISO-8601 string of the end-of-day expiry (23:59:59 local time)
 */
@Injectable({ providedIn: 'root' })
export class WeightPopupDeferralService {
  private readonly auth = inject(AuthService);

  /** Returns the localStorage key scoped to the current user's ID. */
  private getStorageKey(): string | null {
    const user = this.auth.currentUser();
    if (!user) return null;
    return `weight_popup_deferred_until_${user.userId}`;
  }

  /**
   * Returns true when the popup is currently suppressed for the logged-in user
   * (i.e. they clicked "Enter Later" today and the day has not yet rolled over).
   */
  isDeferredForToday(): boolean {
    const key = this.getStorageKey();
    if (!key) return false;

    const raw = localStorage.getItem(key);
    if (!raw) return false;

    const expiry = new Date(raw);
    if (isNaN(expiry.getTime())) {
      // Corrupt value — treat as expired
      localStorage.removeItem(key);
      return false;
    }

    return new Date() < expiry;
  }

  /**
   * Records a deferral that expires at 23:59:59 of the current local calendar
   * day.  Calling this immediately after the user clicks "Enter Later" is
   * sufficient to suppress the popup for the rest of that day.
   */
  deferUntilEndOfDay(): void {
    const key = this.getStorageKey();
    if (!key) return;

    const endOfDay = new Date();
    endOfDay.setHours(23, 59, 59, 999);
    localStorage.setItem(key, endOfDay.toISOString());
  }

  /**
   * Clears the deferral state, e.g. after the user successfully submits their
   * weight so the normal weekly-check logic resumes on the next navigation.
   */
  clearDeferral(): void {
    const key = this.getStorageKey();
    if (!key) return;
    localStorage.removeItem(key);
  }
}
