import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserProfile } from '../../models/auth.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="layout">
      <header class="topbar">
        <span class="brand">🥗 DietTracker</span>

        <div class="topbar-actions">
          <!-- Profile icon -->
          <button class="icon-btn" (click)="goToProfile()" aria-label="Profile" title="Profile">
            <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24"
                 fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="8" r="4"/>
              <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7"/>
            </svg>
          </button>

          <!-- Sign out icon -->
          <button class="icon-btn icon-btn--danger" (click)="logout()" aria-label="Sign out" title="Sign out">
            <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24"
                 fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
              <polyline points="16 17 21 12 16 7"/>
              <line x1="21" y1="12" x2="9" y2="12"/>
            </svg>
          </button>
        </div>
      </header>

      <main class="content">
        <div *ngIf="profile()" class="welcome-card">
          <p class="greeting">👋 Welcome back,</p>
          <h2>{{ profile()?.fullName }}</h2>
        </div>

        <div class="placeholder-card">
          <p>🚧 Dashboard coming soon…</p>
        </div>

        <div class="nav-card" (click)="goToDietPlans()">
          <span class="nav-icon">🥗</span>
          <div class="nav-text">
            <span class="nav-title">My Diet Plans</span>
            <span class="nav-sub">Create and manage your custom diet plans</span>
          </div>
          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24"
               fill="none" stroke="#9ca3af" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="9 18 15 12 9 6"/>
          </svg>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .layout {
      min-height: 100dvh;
      display: flex;
      flex-direction: column;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      background: #f5f7fa;
    }

    .topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 1.25rem;
      height: 56px;
      background: #fff;
      border-bottom: 1px solid #e5e7eb;
      position: sticky;
      top: 0;
      z-index: 10;
    }

    .brand {
      font-size: 1.1rem;
      font-weight: 700;
      color: #4f46e5;
    }

    .topbar-actions {
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }

    .icon-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border: none;
      background: transparent;
      border-radius: 10px;
      color: #6b7280;
      cursor: pointer;
      touch-action: manipulation;
      transition: background 0.15s, color 0.15s;

      &:hover, &:focus-visible {
        background: #f3f4f6;
        color: #111827;
        outline: none;
      }

      &:active { background: #e5e7eb; }

      &--danger {
        color: #ef4444;
        &:hover, &:focus-visible { background: #fee2e2; color: #b91c1c; }
        &:active { background: #fecaca; }
      }
    }

    .content {
      flex: 1;
      padding: 1.25rem 1rem;
      max-width: 720px;
      width: 100%;
      margin: 0 auto;
    }

    .welcome-card {
      background: #fff;
      border-radius: 14px;
      padding: 1.25rem;
      margin-bottom: 1rem;
      box-shadow: 0 1px 6px rgba(0,0,0,0.06);

      .greeting { margin: 0; color: #6b7280; font-size: 0.9rem; }
      h2 { margin: 0.2rem 0 0; font-size: 1.2rem; color: #1a1a2e; }
    }

    .placeholder-card {
      background: #fff;
      border-radius: 14px;
      padding: 2rem 1.25rem;
      text-align: center;
      color: #6b7280;
      box-shadow: 0 1px 6px rgba(0,0,0,0.06);
      margin-bottom: 1rem;
    }

    .nav-card {
      display: flex;
      align-items: center;
      gap: 0.875rem;
      background: #fff;
      border-radius: 14px;
      padding: 1rem 1.25rem;
      margin-bottom: 0.75rem;
      box-shadow: 0 1px 6px rgba(0,0,0,0.06);
      cursor: pointer;
      touch-action: manipulation;
      transition: box-shadow 0.15s, transform 0.1s;

      &:active { transform: scale(0.99); }
      &:hover  { box-shadow: 0 3px 12px rgba(0,0,0,0.1); }
    }

    .nav-icon { font-size: 1.75rem; flex-shrink: 0; }

    .nav-text {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
    }

    .nav-title { font-size: 0.95rem; font-weight: 600; color: #1a1a2e; }
    .nav-sub   { font-size: 0.78rem; color: #6b7280; }
  `],
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  readonly profile = signal<UserProfile | null>(null);

  ngOnInit() {
    this.authService.getProfile().subscribe({
      next: (p) => this.profile.set(p),
    });
  }

  goToProfile()    { this.router.navigate(['/profile']); }
  goToDietPlans()  { this.router.navigate(['/diet-plans']); }
  logout()         { this.authService.logout(); }
}
