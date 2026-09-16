import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Requires: logged in + email verified + onboarding done → /dashboard */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return router.createUrlTree(['/auth/login']);
  if (!auth.isEmailVerified()) return router.createUrlTree(['/auth/verify-email']);
  if (!auth.onboardingCompleted()) return router.createUrlTree(['/auth/onboarding']);

  return true;
};

/** Requires: logged in + email verified → /auth/onboarding (not yet finished setup) */
export const onboardingGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return router.createUrlTree(['/auth/login']);
  if (!auth.isEmailVerified()) return router.createUrlTree(['/auth/verify-email']);
  if (auth.onboardingCompleted()) return router.createUrlTree(['/dashboard']);

  return true;
};

/** Requires: logged in → /auth/verify-email */
export const verifyEmailGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return router.createUrlTree(['/auth/login']);
  if (auth.isEmailVerified()) {
    if (!auth.onboardingCompleted()) return router.createUrlTree(['/auth/onboarding']);
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};

/** Requires: not logged in. Logged-in users go to their next pending step. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return true;
  if (!auth.isEmailVerified()) return router.createUrlTree(['/auth/verify-email']);
  if (!auth.onboardingCompleted()) return router.createUrlTree(['/auth/onboarding']);
  return router.createUrlTree(['/dashboard']);
};
