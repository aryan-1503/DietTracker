import { Routes } from '@angular/router';
import { authGuard, guestGuard, onboardingGuard, verifyEmailGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  {
    path: 'auth',
    children: [
      {
        path: 'login',
        canActivate: [guestGuard],
        loadComponent: () =>
          import('./features/auth/login.component').then(m => m.LoginComponent),
      },
      {
        path: 'register',
        canActivate: [guestGuard],
        loadComponent: () =>
          import('./features/auth/register.component').then(m => m.RegisterComponent),
      },
      {
        path: 'verify-email',
        canActivate: [verifyEmailGuard],
        loadComponent: () =>
          import('./features/auth/verify-email.component').then(m => m.VerifyEmailComponent),
      },
      {
        path: 'onboarding',
        canActivate: [onboardingGuard],
        loadComponent: () =>
          import('./features/auth/onboarding.component').then(m => m.OnboardingComponent),
      },
      { path: '', redirectTo: 'login', pathMatch: 'full' },
    ],
  },

  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
  },

  {
    path: 'diet-plans',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/diet-plans/diet-plan-list.component').then(m => m.DietPlanListComponent),
      },
      {
        path: 'new',
        loadComponent: () =>
          import('./features/diet-plans/diet-plan-form.component').then(m => m.DietPlanFormComponent),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./features/diet-plans/diet-plan-detail.component').then(m => m.DietPlanDetailComponent),
      },
      {
        path: ':id/edit',
        loadComponent: () =>
          import('./features/diet-plans/diet-plan-form.component').then(m => m.DietPlanFormComponent),
      },
    ],
  },

  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/profile/profile.component').then(m => m.ProfileComponent),
  },

  {
    path: 'daily-intake',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/daily-intake/daily-intake.component').then(m => m.DailyIntakeComponent),
  },

  {
    path: 'settings',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/settings/user-settings.component').then(m => m.UserSettingsComponent),
  },

  { path: '**', redirectTo: 'dashboard' },
];
