import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AuthResponse,
  LoginRequest,
  OnboardingRequest,
  RegisterRequest,
  UpdateProfileRequest,
  UserProfile,
  VerifyEmailRequest,
} from '../models/auth.models';

const TOKEN_KEY = 'diet_tracker_token';
const USER_KEY = 'diet_tracker_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly _currentUser = signal<AuthResponse | null>(this.loadUser());

  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => this._currentUser() !== null);
  readonly isEmailVerified = computed(() => this._currentUser()?.isEmailVerified ?? false);
  readonly onboardingCompleted = computed(() => this._currentUser()?.onboardingCompleted ?? false);

  register(data: RegisterRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/register`, data)
      .pipe(
        tap(res => this.persistSession(res)),
        catchError(err => throwError(() => err))
      );
  }

  login(data: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/login`, data)
      .pipe(
        tap(res => this.persistSession(res)),
        catchError(err => throwError(() => err))
      );
  }

  verifyEmail(data: VerifyEmailRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/verify-email`, data)
      .pipe(
        tap(res => this.persistSession(res)),
        catchError(err => throwError(() => err))
      );
  }

  completeOnboarding(data: OnboardingRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/onboarding`, data)
      .pipe(
        tap(res => this.persistSession(res)),
        catchError(err => throwError(() => err))
      );
  }

  getProfile() {
    return this.http.get<UserProfile>(`${environment.apiUrl}/auth/profile`);
  }

  updateProfile(data: UpdateProfileRequest) {
    return this.http.put<UserProfile>(`${environment.apiUrl}/auth/profile`, data);
  }

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private persistSession(res: AuthResponse) {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(res));
    this._currentUser.set(res);
  }

  private loadUser(): AuthResponse | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      if (!raw) return null;
      const user = JSON.parse(raw) as AuthResponse;
      if (new Date(user.expiresAt) <= new Date()) {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        return null;
      }
      return user;
    } catch {
      return null;
    }
  }
}
