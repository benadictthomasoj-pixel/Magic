import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, UrlTree } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { AuthBoundaryService } from '../services/auth-boundary.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  it('redirects to /login when user is unauthenticated', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const result = TestBed.runInInjectionContext(() => {
      return authGuard({} as any, { url: '/accounting/dashboard' } as any);
    });

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toContain('/login');
  });

  it('allows access to workspace routes when user is authenticated', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const auth = TestBed.inject(AuthBoundaryService);
    // Simulate authenticated state
    (auth as any).stateSignal.set({
      isAuthenticated: true,
      accessToken: 'test-token',
      refreshToken: 'test-refresh',
      expiresAtUtc: null,
      user: {
        id: '11111111-1111-1111-1111-111111111111',
        username: 'admin',
        email: 'admin@magic.local',
        displayName: 'Administrator',
        roles: ['Administrator'],
        permissions: ['accounting:dashboard:view'],
      },
      company: null,
      branch: null,
      terminal: null,
    });

    const result = TestBed.runInInjectionContext(() => {
      return authGuard({} as any, { url: '/accounting/dashboard' } as any);
    });

    expect(result).toBe(true);
  });
});
