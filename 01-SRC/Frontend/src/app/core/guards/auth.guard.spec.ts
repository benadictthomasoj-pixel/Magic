import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  it('allows access to workspace routes directly', () => {
    TestBed.runInInjectionContext(() => {
      const result = authGuard({} as any, {} as any);
      expect(result).toBe(true);
    });
  });
});
