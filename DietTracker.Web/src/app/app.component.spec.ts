import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Router, NavigationEnd } from '@angular/router';
import { of, Subject } from 'rxjs';

import { AppComponent } from './app.component';
import { AuthService } from './services/auth.service';
import { WeightService } from './services/weight.service';
import { WeightPopupDeferralService } from './services/weight-popup-deferral.service';

// ── Minimal stubs ──────────────────────────────────────────────────────────

const mockPendingCheck = {
  hasPendingEntry: true,
  pendingWeekNumber: 1,
  pendingYear: 2025,
  pendingWeekLabel: 'Week 1',
};

function makeAuthStub(loggedIn = true) {
  return {
    isLoggedIn: jasmine.createSpy('isLoggedIn').and.returnValue(loggedIn),
    currentUser: jasmine.createSpy('currentUser').and.returnValue({ userId: 42 }),
  };
}

function makeWeightStub(hasPending = true) {
  return {
    getPendingCheck: jasmine
      .createSpy('getPendingCheck')
      .and.returnValue(of(hasPending ? mockPendingCheck : { hasPendingEntry: false })),
  };
}

// ── Tests ──────────────────────────────────────────────────────────────────

describe('AppComponent', () => {
  it('should create the app', async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AppComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});

// ── WeightPopupDeferralService unit tests ──────────────────────────────────

describe('WeightPopupDeferralService', () => {
  let svc: WeightPopupDeferralService;
  let authStub: ReturnType<typeof makeAuthStub>;

  beforeEach(() => {
    authStub = makeAuthStub();
    TestBed.configureTestingModule({
      providers: [
        WeightPopupDeferralService,
        { provide: AuthService, useValue: authStub },
      ],
    });
    svc = TestBed.inject(WeightPopupDeferralService);
    // Clear any existing deferral keys
    for (const k of Object.keys(localStorage)) {
      if (k.startsWith('weight_popup_deferred_until_')) localStorage.removeItem(k);
    }
  });

  afterEach(() => {
    for (const k of Object.keys(localStorage)) {
      if (k.startsWith('weight_popup_deferred_until_')) localStorage.removeItem(k);
    }
  });

  it('isDeferredForToday() returns false when no key is set', () => {
    expect(svc.isDeferredForToday()).toBeFalse();
  });

  it('isDeferredForToday() returns true immediately after deferUntilEndOfDay()', () => {
    svc.deferUntilEndOfDay();
    expect(svc.isDeferredForToday()).toBeTrue();
  });

  it('isDeferredForToday() returns false after clearDeferral()', () => {
    svc.deferUntilEndOfDay();
    svc.clearDeferral();
    expect(svc.isDeferredForToday()).toBeFalse();
  });

  it('isDeferredForToday() returns false when stored expiry is in the past', () => {
    const key = 'weight_popup_deferred_until_42';
    const past = new Date(Date.now() - 1000);
    localStorage.setItem(key, past.toISOString());
    expect(svc.isDeferredForToday()).toBeFalse();
  });

  it('deferUntilEndOfDay() stores expiry at 23:59:59 of today', () => {
    svc.deferUntilEndOfDay();
    const raw = localStorage.getItem('weight_popup_deferred_until_42');
    expect(raw).toBeTruthy();
    const expiry = new Date(raw!);
    const now = new Date();
    expect(expiry.getDate()).toEqual(now.getDate());
    expect(expiry.getHours()).toEqual(23);
    expect(expiry.getMinutes()).toEqual(59);
    expect(expiry.getSeconds()).toEqual(59);
  });

  it('isDeferredForToday() returns false and removes key for corrupt stored value', () => {
    localStorage.setItem('weight_popup_deferred_until_42', 'not-a-date');
    expect(svc.isDeferredForToday()).toBeFalse();
    expect(localStorage.getItem('weight_popup_deferred_until_42')).toBeNull();
  });

  it('uses user-scoped key so different users do not share deferral', () => {
    // User 42 defers
    svc.deferUntilEndOfDay();
    expect(localStorage.getItem('weight_popup_deferred_until_42')).toBeTruthy();

    // Simulate a different user (id 99)
    authStub.currentUser.and.returnValue({ userId: 99 });
    expect(svc.isDeferredForToday()).toBeFalse();
  });

  it('isDeferredForToday() returns false when no user is authenticated', () => {
    authStub.currentUser.and.returnValue(null);
    svc.deferUntilEndOfDay(); // should be a no-op
    expect(svc.isDeferredForToday()).toBeFalse();
  });
});
