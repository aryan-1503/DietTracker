import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserProfile } from '../../models/auth.models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="layout">
      <!-- Top bar -->
      <header class="topbar">
        <button class="icon-btn" (click)="goBack()" aria-label="Go back">
          <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24"
               fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" stroke-linejoin="round">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
        </button>

        <span class="topbar-title">My Profile</span>

        <div class="topbar-actions">
          <!-- Edit / Cancel toggle -->
          <button class="icon-btn" (click)="toggleEdit()" [attr.aria-label]="editing() ? 'Cancel' : 'Edit profile'">
            <!-- Pencil icon (view mode) -->
            <svg *ngIf="!editing()" xmlns="http://www.w3.org/2000/svg" width="20" height="20"
                 viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round">
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
            </svg>
            <!-- X icon (edit mode) -->
            <svg *ngIf="editing()" xmlns="http://www.w3.org/2000/svg" width="20" height="20"
                 viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"/>
              <line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>

          <!-- Sign out -->
          <button class="icon-btn icon-btn--danger" (click)="logout()" aria-label="Sign out">
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

      <!-- Loading -->
      <div class="loader-wrap" *ngIf="!profile()">
        <div class="loader"></div>
      </div>

      <main class="content" *ngIf="profile() as p">

        <!-- Avatar + name -->
        <div class="avatar-section">
          <div class="avatar">{{ initials() }}</div>
          <h1 class="display-name">{{ p.fullName }}</h1>
          <p class="display-email">{{ p.email }}</p>
          <span class="badge" [class.badge--verified]="p.isEmailVerified"
                               [class.badge--pending]="!p.isEmailVerified">
            {{ p.isEmailVerified ? '✓ Email verified' : '⏳ Email not verified' }}
          </span>
        </div>

        <!-- ── VIEW MODE ────────────────────────────────────────────────────── -->
        <ng-container *ngIf="!editing()">

          <div class="card">
            <h2 class="card-title">Personal info</h2>
            <div class="info-row">
              <span class="info-label">First name</span>
              <span class="info-value">{{ p.firstName || '—' }}</span>
            </div>
            <div class="info-row" *ngIf="p.middleName">
              <span class="info-label">Middle name</span>
              <span class="info-value">{{ p.middleName }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Last name</span>
              <span class="info-value">{{ p.lastName || '—' }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Email</span>
              <span class="info-value">{{ p.email }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Member since</span>
              <span class="info-value">{{ p.createdAt | date:'mediumDate' }}</span>
            </div>
          </div>

          <div class="card" *ngIf="p.heightCm || p.weightKg">
            <h2 class="card-title">Body metrics</h2>
            <div class="info-row" *ngIf="p.heightCm">
              <span class="info-label">Height</span>
              <span class="info-value">{{ p.heightCm }} cm</span>
            </div>
            <div class="info-row" *ngIf="p.weightKg">
              <span class="info-label">Current weight</span>
              <span class="info-value">{{ p.weightKg }} kg</span>
            </div>
          </div>

          <div class="card" *ngIf="p.goalWeightKg || p.targetDurationWeeks">
            <h2 class="card-title">My goal</h2>
            <div class="info-row" *ngIf="p.goalWeightKg">
              <span class="info-label">Goal weight</span>
              <span class="info-value">{{ p.goalWeightKg }} kg</span>
            </div>
            <div class="info-row" *ngIf="p.targetDurationWeeks">
              <span class="info-label">Target duration</span>
              <span class="info-value">{{ durationLabel(p.targetDurationWeeks) }}</span>
            </div>
            <div class="info-row" *ngIf="p.weightKg && p.goalWeightKg">
              <span class="info-label">To go</span>
              <span class="info-value"
                    [class.positive]="toGo(p) < 0"
                    [class.neutral]="toGo(p) === 0">
                {{ toGoLabel(p) }}
              </span>
            </div>
          </div>

          <div class="card card--nudge" *ngIf="!p.onboardingCompleted">
            <p>Complete your profile setup to track your progress.</p>
            <button class="btn-primary" (click)="goToOnboarding()">Finish setup</button>
          </div>

        </ng-container>

        <!-- ── EDIT MODE ────────────────────────────────────────────────────── -->
        <ng-container *ngIf="editing()">
          <form [formGroup]="form" (ngSubmit)="save()">

            <div *ngIf="saveError()" class="error-banner" role="alert">{{ saveError() }}</div>
            <div *ngIf="saveSuccess()" class="success-banner" role="status">Profile updated ✓</div>

            <div class="card">
              <h2 class="card-title">Personal info</h2>

              <div class="field">
                <label for="firstName">First name <span class="req">*</span></label>
                <input id="firstName" type="text" formControlName="firstName"
                       autocomplete="given-name"
                       [class.invalid]="isInvalid('firstName')" />
                <span *ngIf="isInvalid('firstName')" class="field-error">Required.</span>
              </div>

              <div class="field">
                <label for="middleName">Middle name <span class="opt">(optional)</span></label>
                <input id="middleName" type="text" formControlName="middleName"
                       autocomplete="additional-name" />
              </div>

              <div class="field">
                <label for="lastName">Last name <span class="req">*</span></label>
                <input id="lastName" type="text" formControlName="lastName"
                       autocomplete="family-name"
                       [class.invalid]="isInvalid('lastName')" />
                <span *ngIf="isInvalid('lastName')" class="field-error">Required.</span>
              </div>
            </div>

            <div class="card">
              <h2 class="card-title">Body metrics</h2>

              <div class="field">
                <label for="heightCm">Height</label>
                <div class="input-unit">
                  <input id="heightCm" type="number" inputmode="decimal"
                         formControlName="heightCm"
                         [class.invalid]="isInvalid('heightCm')"
                         placeholder="e.g. 170" min="50" max="300" />
                  <span class="unit">cm</span>
                </div>
                <span *ngIf="isInvalid('heightCm')" class="field-error">50–300 cm.</span>
              </div>

              <div class="field">
                <label for="weightKg">Current weight</label>
                <div class="input-unit">
                  <input id="weightKg" type="number" inputmode="decimal"
                         formControlName="weightKg"
                         [class.invalid]="isInvalid('weightKg')"
                         placeholder="e.g. 75" min="20" max="500" />
                  <span class="unit">kg</span>
                </div>
                <span *ngIf="isInvalid('weightKg')" class="field-error">20–500 kg.</span>
              </div>
            </div>

            <div class="card">
              <h2 class="card-title">My goal</h2>

              <div class="field">
                <label for="goalWeightKg">Goal weight</label>
                <div class="input-unit">
                  <input id="goalWeightKg" type="number" inputmode="decimal"
                         formControlName="goalWeightKg"
                         [class.invalid]="isInvalid('goalWeightKg')"
                         placeholder="e.g. 65" min="20" max="500" />
                  <span class="unit">kg</span>
                </div>
                <span *ngIf="isInvalid('goalWeightKg')" class="field-error">20–500 kg.</span>
              </div>

              <div class="field">
                <label for="targetDurationWeeks">Target duration</label>
                <div class="input-unit">
                  <input id="targetDurationWeeks" type="number" inputmode="numeric"
                         formControlName="targetDurationWeeks"
                         [class.invalid]="isInvalid('targetDurationWeeks')"
                         placeholder="e.g. 12" min="1" max="104" />
                  <span class="unit">weeks</span>
                </div>
                <span *ngIf="isInvalid('targetDurationWeeks')" class="field-error">1–104 weeks.</span>
              </div>
            </div>

            <div class="btn-group">
              <button type="button" class="btn-secondary" (click)="cancelEdit()">Cancel</button>
              <button type="submit" class="btn-primary" [disabled]="saving()">
                {{ saving() ? 'Saving…' : 'Save changes' }}
              </button>
            </div>

          </form>
        </ng-container>

      </main>
    </div>
  `,
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  readonly profile = signal<UserProfile | null>(null);
  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly saveError = signal('');
  readonly saveSuccess = signal(false);

  form = this.fb.nonNullable.group({
    firstName:           ['', Validators.required],
    middleName:          [''],
    lastName:            ['', Validators.required],
    heightCm:            [null as number | null, [Validators.min(50), Validators.max(300)]],
    weightKg:            [null as number | null, [Validators.min(20), Validators.max(500)]],
    goalWeightKg:        [null as number | null, [Validators.min(20), Validators.max(500)]],
    targetDurationWeeks: [null as number | null, [Validators.min(1),  Validators.max(104)]],
  });

  ngOnInit() {
    this.authService.getProfile().subscribe({
      next: (p) => this.profile.set(p),
    });
  }

  initials(): string {
    const p = this.profile();
    if (!p) return '?';
    const first = p.firstName?.[0] ?? '';
    const last  = p.lastName?.[0]  ?? '';
    return (first + last).toUpperCase() || p.email[0].toUpperCase();
  }

  toGo(p: UserProfile): number {
    return (p.weightKg ?? 0) - (p.goalWeightKg ?? 0);
  }

  toGoLabel(p: UserProfile): string {
    const diff = this.toGo(p);
    if (diff === 0) return 'At goal weight 🎉';
    return diff > 0
      ? `${diff.toFixed(1)} kg to lose`
      : `${Math.abs(diff).toFixed(1)} kg to gain`;
  }

  durationLabel(weeks?: number | null): string {
    if (!weeks) return '—';
    if (weeks < 8) return `${weeks} weeks`;
    const months = Math.round(weeks / 4.33);
    return `${months} month${months !== 1 ? 's' : ''}`;
  }

  isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  toggleEdit() {
    if (this.editing()) {
      this.cancelEdit();
    } else {
      this.startEdit();
    }
  }

  startEdit() {
    const p = this.profile();
    if (!p) return;
    this.form.setValue({
      firstName:           p.firstName           ?? '',
      middleName:          p.middleName           ?? '',
      lastName:            p.lastName             ?? '',
      heightCm:            p.heightCm             ?? null,
      weightKg:            p.weightKg             ?? null,
      goalWeightKg:        p.goalWeightKg         ?? null,
      targetDurationWeeks: p.targetDurationWeeks  ?? null,
    });
    this.saveError.set('');
    this.saveSuccess.set(false);
    this.editing.set(true);
  }

  cancelEdit() {
    this.editing.set(false);
    this.saveError.set('');
    this.saveSuccess.set(false);
    this.form.reset();
  }

  save() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.saving.set(true);
    this.saveError.set('');
    this.saveSuccess.set(false);

    const v = this.form.getRawValue();

    this.authService.updateProfile({
      firstName:           v.firstName.trim(),
      middleName:          v.middleName?.trim() || undefined,
      lastName:            v.lastName.trim(),
      heightCm:            v.heightCm            ?? null,
      weightKg:            v.weightKg            ?? null,
      goalWeightKg:        v.goalWeightKg        ?? null,
      targetDurationWeeks: v.targetDurationWeeks ?? null,
    }).subscribe({
      next: (updated) => {
        this.profile.set(updated);
        this.saving.set(false);
        this.saveSuccess.set(true);
        this.editing.set(false);
        // Clear success banner after 3s
        setTimeout(() => this.saveSuccess.set(false), 3000);
      },
      error: (err) => {
        this.saveError.set(err.error?.message ?? 'Failed to save. Please try again.');
        this.saving.set(false);
      },
    });
  }

  goBack()         { this.router.navigate(['/dashboard']); }
  goToOnboarding() { this.router.navigate(['/auth/onboarding']); }
  logout()         { this.authService.logout(); }
}
