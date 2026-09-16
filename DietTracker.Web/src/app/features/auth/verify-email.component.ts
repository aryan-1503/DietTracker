import { Component, OnInit, signal, inject } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <div class="auth-logo">📧</div>
        <h1>Check your email</h1>
        <p class="subtitle">
          We sent a verification token to
          <strong>{{ userEmail() }}</strong>.
          Copy it from the email and paste it below.
        </p>

        <div *ngIf="errorMessage()" class="error-banner" role="alert">
          {{ errorMessage() }}
        </div>

        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <div class="field">
            <label for="token">Verification token</label>
            <input
              id="token"
              type="text"
              formControlName="token"
              class="code-input"
              [class.invalid]="isInvalid('token')"
              autocomplete="off"
              autocorrect="off"
              autocapitalize="off"
              spellcheck="false"
              placeholder="Paste token here"
            />
            <span *ngIf="isInvalid('token')" class="field-error">Token is required.</span>
          </div>

          <button type="submit" class="btn-primary" [disabled]="loading()">
            {{ loading() ? 'Verifying…' : 'Verify email' }}
          </button>
        </form>

        <!-- Dev-only hint block: shows the raw token so you can test without SMTP -->
        <div class="dev-hint" *ngIf="devToken()">
          <p>🛠 Dev — verification token (remove in production):</p>
          <code (click)="copyToken()">{{ devToken() }}</code>
          <span class="copied-badge" *ngIf="copied()">✓ Copied</span>
        </div>
      </div>
    </div>
  `,
  styleUrl: './verify-email.component.scss',
})
export class VerifyEmailComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly devToken = signal('');
  readonly copied = signal(false);
  readonly userEmail = signal('');

  form = this.fb.nonNullable.group({
    token: ['', Validators.required],
  });

  ngOnInit() {
    const user = this.authService.currentUser();
    if (!user) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.userEmail.set(user.email);

    // If the user tapped the email link, the token arrives as ?token=...
    // Pre-fill and auto-submit so they land on onboarding instantly.
    const queryToken = this.route.snapshot.queryParamMap.get('token');
    if (queryToken) {
      this.form.setValue({ token: queryToken });
      this.onSubmit();
      return;
    }

    // Dev only: fetch token so you can test without an email server
    this.http
      .get<{ token: string }>(`${environment.apiUrl}/auth/dev/verification-token`)
      .subscribe({
        next: (res) => this.devToken.set(res.token ?? ''),
        error: () => { /* not available in production */ },
      });
  }

  isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  copyToken() {
    navigator.clipboard.writeText(this.devToken()).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  onSubmit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.verifyEmail({ token: this.form.getRawValue().token }).subscribe({
      next: () => this.router.navigate(['/auth/onboarding']),
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Verification failed. Please try again.');
        this.loading.set(false);
      },
    });
  }
}
