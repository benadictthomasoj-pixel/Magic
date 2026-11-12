import { TestBed } from '@angular/core/testing';
import { AuthBoundaryService } from './auth-boundary.service';

describe('AuthBoundaryService', () => {
  it('stays deferred under OPEN-003 and fails closed on permissions', () => {
    const service = TestBed.inject(AuthBoundaryService);

    expect(service.state().status).toBe('DEFERRED_OPEN_003');
    expect(service.state().configured).toBe(false);
    expect(service.hasPermission('system.admin')).toBe(false);
  });
});

