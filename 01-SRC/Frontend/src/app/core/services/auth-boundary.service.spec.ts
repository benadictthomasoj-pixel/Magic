import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { AuthBoundaryService } from './auth-boundary.service';

describe('AuthBoundaryService', () => {
  it('starts unauthenticated and fails closed on permission checks', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const service = TestBed.inject(AuthBoundaryService);

    expect(service.isAuthenticated()).toBe(false);
    expect(service.accessToken()).toBeNull();
    expect(service.hasPermission('accounting:vouchers:create')).toBe(false);
    expect(service.hasRole('Administrator')).toBe(false);
  });
});
