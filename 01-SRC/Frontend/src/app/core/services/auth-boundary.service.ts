import { Injectable, signal } from '@angular/core';

export interface AuthBoundaryState {
  readonly status: 'DEFERRED_OPEN_003';
  readonly configured: boolean;
  readonly isAuthenticated: boolean;
  readonly note: string;
}

/**
 * Integration boundary for authentication (OPEN-003).
 * Concrete login flows and token storage remain deferred until OPEN-003 approval.
 * Always fails closed for permission checks until an approved provider is wired.
 */
@Injectable({ providedIn: 'root' })
export class AuthBoundaryService {
  private readonly stateSignal = signal<AuthBoundaryState>({
    status: 'DEFERRED_OPEN_003',
    configured: false,
    isAuthenticated: false,
    note: 'Authentication & token architecture deferred per OPEN-003. Access checks fail closed.',
  });

  readonly state = this.stateSignal.asReadonly();

  hasPermission(_permissionCode: string): boolean {
    return false;
  }
}

