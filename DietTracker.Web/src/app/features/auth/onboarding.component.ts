import { Component, signal, computed, inject } from '@angular/core';
import {
  FormBuilder,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
} from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

const STEPS = ['Name', 'Height', 'Current Weight', 'Goal Weight', 'Duration'] as const;

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="onboarding-container">
      <div class="onboarding-card">

        <!-- Progress bar -->
        <div class="progress-bar" role="progressbar"
             [attr.aria-valuenow]="step() + 1" [attr.aria-valuemax]="totalSteps">
          <div class="progress-track">
            <div class="progress-fill" [style.width.%]="progressPct()"></div>
          </div>
          <span class="progress-label">Step {{ step() + 1 }} of {{ totalSteps }}</span>
        </div>

        <!-- ── Step 0: Name ─────────────────────────────────────────────────── -->
        <ng-container *ngIf="step() === 0">
          <div class="step-icon">👤</div>
          <h1>What's your name?</h1>
          <p class="subtitle">We'll use this to personalise your experience.</p>

          <div *ngIf="errorMessage()" class="error-banner" role="alert">{{ errorMessage() }}</div>

          <div class="field">
            <label for="firstName">First name <span class="required">*</span></label>
            <input id="firstName" type="text" inputmode="text"
                   [formControl]="firstNameCtrl"
                   [class.invalid]="firstNameCtrl.invalid && firstNameCtrl.touched"
                   autocomplete="given-name" placeholder="e.g. Aryan" />
            <span *ngIf="firstNameCtrl.invalid && firstNameCtrl.touched" class="field-error">
              First name is required.
            </span>
          </div>

          <div class="field">
            <label for="middleName">Middle name <span class="optional">(optional)</span></label>
            <input id="middleName" type="text" inputmode="text"
                   [formControl]="middleNameCtrl"
                   autocomplete="additional-name" placeholder="e.g. Kumar" />
          </div>

          <div class="field">
            <label for="lastName">Last name <span class="required">*</span></label>
            <input id="lastName" type="text" inputmode="text"
                   [formControl]="lastNameCtrl"
                   [class.invalid]="lastNameCtrl.invalid && lastNameCtrl.touched"
                   autocomplete="family-name" placeholder="e.g. Sharma" />
            <span *ngIf="lastNameCtrl.invalid && lastNameCtrl.touched" class="field-error">
              Last name is required.
            </span>
          </div>

          <button class="btn-primary" (click)="nextName()"
                  [disabled]="firstNameCtrl.invalid || lastNameCtrl.invalid">
            Continue
          </button>
        </ng-container>

        <!-- ── Step 1: Height ───────────────────────────────────────────────── -->
        <ng-container *ngIf="step() === 1">
          <div class="step-icon">📏</div>
          <h1>How tall are you?</h1>
          <p class="subtitle">We use this to calculate your daily calorie needs.</p>

          <div *ngIf="errorMessage()" class="error-banner" role="alert">{{ errorMessage() }}</div>

          <div class="field">
            <label for="heightCm">Height</label>
            <div class="input-with-unit">
              <input id="heightCm" type="number" inputmode="decimal"
                     [formControl]="heightCtrl"
                     [class.invalid]="heightCtrl.invalid && heightCtrl.touched"
                     placeholder="e.g. 170" min="50" max="300" />
              <span class="unit">cm</span>
            </div>
            <span *ngIf="heightCtrl.invalid && heightCtrl.touched" class="field-error">
              Enter a height between 50 and 300 cm.
            </span>
          </div>

          <div class="btn-row">
            <button class="btn-back" (click)="back()">Back</button>
            <button class="btn-primary flex-1" (click)="next(heightCtrl)" [disabled]="heightCtrl.invalid">
              Continue
            </button>
          </div>
        </ng-container>

        <!-- ── Step 2: Current weight ────────────────────────────────────────── -->
        <ng-container *ngIf="step() === 2">
          <div class="step-icon">⚖️</div>
          <h1>What's your current weight?</h1>
          <p class="subtitle">We'll track your progress from here.</p>

          <div *ngIf="errorMessage()" class="error-banner" role="alert">{{ errorMessage() }}</div>

          <div class="field">
            <label for="weightKg">Current weight</label>
            <div class="input-with-unit">
              <input id="weightKg" type="number" inputmode="decimal"
                     [formControl]="weightCtrl"
                     [class.invalid]="weightCtrl.invalid && weightCtrl.touched"
                     placeholder="e.g. 75" min="20" max="500" />
              <span class="unit">kg</span>
            </div>
            <span *ngIf="weightCtrl.invalid && weightCtrl.touched" class="field-error">
              Enter a weight between 20 and 500 kg.
            </span>
          </div>

          <div class="btn-row">
            <button class="btn-back" (click)="back()">Back</button>
            <button class="btn-primary flex-1" (click)="next(weightCtrl)" [disabled]="weightCtrl.invalid">
              Continue
            </button>
          </div>
        </ng-container>

        <!-- ── Step 3: Goal weight ───────────────────────────────────────────── -->
        <ng-container *ngIf="step() === 3">
          <div class="step-icon">🎯</div>
          <h1>What's your goal weight?</h1>
          <p class="subtitle">Set a realistic target to work towards.</p>

          <div *ngIf="errorMessage()" class="error-banner" role="alert">{{ errorMessage() }}</div>

          <div class="field">
            <label for="goalWeightKg">Goal weight</label>
            <div class="input-with-unit">
              <input id="goalWeightKg" type="number" inputmode="decimal"
                     [formControl]="goalWeightCtrl"
                     [class.invalid]="goalWeightCtrl.invalid && goalWeightCtrl.touched"
                     placeholder="e.g. 65" min="20" max="500" />
              <span class="unit">kg</span>
            </div>
            <span *ngIf="goalWeightCtrl.invalid && goalWeightCtrl.touched" class="field-error">
              Enter a weight between 20 and 500 kg.
            </span>
          </div>

          <div class="btn-row">
            <button class="btn-back" (click)="back()">Back</button>
            <button class="btn-primary flex-1" (click)="next(goalWeightCtrl)" [disabled]="goalWeightCtrl.invalid">
              Continue
            </button>
          </div>
        </ng-container>

        <!-- ── Step 4: Duration ──────────────────────────────────────────────── -->
        <ng-container *ngIf="step() === 4">
          <div class="step-icon">📅</div>
          <h1>How long to reach your goal?</h1>
          <p class="subtitle">Choose a realistic timeframe. You can always adjust later.</p>

          <div *ngIf="errorMessage()" class="error-banner" role="alert">{{ errorMessage() }}</div>

          <div class="duration-options">
            <button *ngFor="let opt of durationOptions"
                    class="duration-btn"
                    [class.selected]="durationCtrl.value === opt.weeks"
                    (click)="durationCtrl.setValue(opt.weeks)"
                    type="button">
              <span class="dur-label">{{ opt.label }}</span>
              <span class="dur-sub">{{ opt.sub }}</span>
            </button>
          </div>

          <div class="btn-row">
            <button class="btn-back" (click)="back()">Back</button>
            <button class="btn-primary flex-1" (click)="submit()"
                    [disabled]="durationCtrl.invalid || loading()">
              {{ loading() ? 'Saving…' : 'Finish setup' }}
            </button>
          </div>
        </ng-container>

      </div>
    </div>
  `,
  styleUrl: './onboarding.component.scss',
})
export class OnboardingComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  readonly step = signal(0);
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly totalSteps = STEPS.length;
  readonly progressPct = computed(() => ((this.step() + 1) / this.totalSteps) * 100);

  // Name controls
  firstNameCtrl  = this.fb.nonNullable.control('', Validators.required);
  middleNameCtrl = this.fb.nonNullable.control('');
  lastNameCtrl   = this.fb.nonNullable.control('', Validators.required);

  // Health controls
  heightCtrl = this.fb.nonNullable.control<number | null>(null, [
    Validators.required, Validators.min(50), Validators.max(300),
  ]);
  weightCtrl = this.fb.nonNullable.control<number | null>(null, [
    Validators.required, Validators.min(20), Validators.max(500),
  ]);
  goalWeightCtrl = this.fb.nonNullable.control<number | null>(null, [
    Validators.required, Validators.min(20), Validators.max(500),
  ]);
  durationCtrl = this.fb.nonNullable.control<number | null>(null, [Validators.required]);

  durationOptions = [
    { weeks: 4,  label: '1 month',   sub: '~0.5 kg/week' },
    { weeks: 8,  label: '2 months',  sub: '~0.5–1 kg/week' },
    { weeks: 12, label: '3 months',  sub: '~0.75–1 kg/week' },
    { weeks: 24, label: '6 months',  sub: 'Steady pace' },
    { weeks: 52, label: '12 months', sub: 'Long-term change' },
  ];

  /** Advance from name step — validates both required name fields. */
  nextName() {
    this.firstNameCtrl.markAsTouched();
    this.lastNameCtrl.markAsTouched();
    this.errorMessage.set('');
    if (this.firstNameCtrl.invalid || this.lastNameCtrl.invalid) return;
    this.step.update(s => s + 1);
  }

  /** Advance from a single-control step. */
  next(ctrl: AbstractControl) {
    ctrl.markAsTouched();
    this.errorMessage.set('');
    if (ctrl.invalid) return;
    this.step.update(s => s + 1);
  }

  back() {
    this.errorMessage.set('');
    this.step.update(s => Math.max(0, s - 1));
  }

  submit() {
    this.durationCtrl.markAsTouched();
    if (this.durationCtrl.invalid) return;

    this.loading.set(true);
    this.errorMessage.set('');

    const middleName = this.middleNameCtrl.value.trim() || undefined;

    this.authService.completeOnboarding({
      firstName: this.firstNameCtrl.value.trim(),
      middleName,
      lastName: this.lastNameCtrl.value.trim(),
      heightCm: this.heightCtrl.value!,
      weightKg: this.weightCtrl.value!,
      goalWeightKg: this.goalWeightCtrl.value!,
      targetDurationWeeks: this.durationCtrl.value!,
    }).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Something went wrong. Please try again.');
        this.loading.set(false);
      },
    });
  }
}
