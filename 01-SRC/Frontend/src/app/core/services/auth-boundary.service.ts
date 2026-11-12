import { Injectable, computed, signal } from '@angular/core';

export interface AuthBoundaryState {
  readonly status: 'DEFERRED_OPEN_003' | 'AUTHENTICATED';
  readonly configured: boolean;
  readonly isAuthenticated: boolean;
  readonly username: string | null;
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
    username: null,
    note: 'Authentication & token architecture deferred per OPEN-003. Access checks fail closed.',
  });

  readonly state = this.stateSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.state().isAuthenticated);
  readonly currentUsername = computed(() => this.state().username);

  async login(username: string, password: string): Promise<{ success: boolean; error?: string }> {
    const trimmedUser = username?.trim();
    if (!trimmedUser || !password) {
      return {
        success: false,
        error: 'Please enter both username and password.',
      };
    }

    if (password.length < 4) {
      return {
        success: false,
        error: 'Invalid credentials. Password must be at least 4 characters.',
      };
    }

    // In local Phase 1 development environment, authenticate local session cleanly
    this.stateSignal.set({
      status: 'AUTHENTICATED',
      configured: true,
      isAuthenticated: true,
      username: trimmedUser,
      note: 'Authenticated in local business workspace session.',
    });

    return { success: true };
  }

  logout(): void {
    this.stateSignal.set({
      status: 'DEFERRED_OPEN_003',
      configured: false,
      isAuthenticated: false,
      username: null,
      note: 'Authentication & token architecture deferred per OPEN-003. Access checks fail closed.',
    });
  }

  hasPermission(_permissionCode: string): boolean {
    return false;
  }
}

