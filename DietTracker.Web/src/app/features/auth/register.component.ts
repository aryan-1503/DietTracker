import { Component, signal, inject } from '@angular/core';
import {
  FormBuilder,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
  const pw = group.get('password')?.value;
  const cpw = group.get('confirmPassword')?.value;
  return pw && cpw && pw !== cpw ? { passwordMismatch: true } : null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <div class="auth-logo"><img src="/logo.png" alt="EatTrack Logo" /></div>
        <h1>Create account</h1>
        <p class="subtitle">Start your diet journey today</p>

        <div *ngIf="errorMessage()" class="error-banner" role="alert">
          {{ errorMessage() }}
        </div>

        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <div class="field">
            <label for="email">Email</label>
            <input id="email" type="email" formControlName="email"
                   autocomplete="email" [class.invalid]="isInvalid('email')"
                   placeholder="you@example.com" />
            <span *ngIf="isInvalid('email')" class="field-error">Valid email required.</span>
          </div>

          <div class="field">
            <label for="password">Password</label>
            <input id="password" type="password" formControlName="password"
                   autocomplete="new-password" [class.invalid]="isInvalid('password')"
                   placeholder="At least 8 characters" />
            <span *ngIf="isInvalid('password')" class="field-error">Minimum 8 characters.</span>
          </div>

          <div class="field">
            <label for="confirmPassword">Confirm password</label>
            <input id="confirmPassword" type="password" formControlName="confirmPassword"
                   autocomplete="new-password"
                   [class.invalid]="isInvalid('confirmPassword') || (form.hasError('passwordMismatch') && form.get('confirmPassword')?.touched)"
                   placeholder="Repeat password" />
            <span *ngIf="form.hasError('passwordMismatch') && form.get('confirmPassword')?.touched"
                  class="field-error">Passwords do not match.</span>
          </div>

          <button type="submit" class="btn-primary" [disabled]="loading()">
            {{ loading() ? 'Creating account…' : 'Create account' }}
          </button>
        </form>

        <p class="switch-link">
          Already have an account? <a routerLink="/auth/login">Sign in</a>
        </p>
      </div>
    </div>
  `,
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  form = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordMatchValidator }
  );

  isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  onSubmit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.register(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/auth/verify-email']),
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Registration failed. Please try again.');
        this.loading.set(false);
      },
    });
  }
}
