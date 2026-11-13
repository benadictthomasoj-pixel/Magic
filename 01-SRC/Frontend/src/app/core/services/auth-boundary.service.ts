import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../contracts/api.models';

export interface UserSummary {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly displayName: string;
  readonly roles: readonly string[];
  readonly permissions: readonly string[];
}

export interface CompanyContext {
  readonly id: string;
  readonly name: string;
  readonly code: string;
}

export interface BranchContext {
  readonly id: string;
  readonly name: string;
  readonly code: string;
}

export interface TerminalContext {
  readonly id: string;
  readonly code: string;
  readonly machineName: string;
}

export interface LoginResponseData {
  readonly accessToken: string;
  readonly refreshToken: string;
  readonly expiresAtUtc: string;
  readonly user: UserSummary;
  readonly company: CompanyContext;
  readonly branch?: BranchContext | null;
  readonly terminal?: TerminalContext | null;
}

export interface AuthState {
  readonly isAuthenticated: boolean;
  readonly accessToken: string | null;
  readonly refreshToken: string | null;
  readonly expiresAtUtc: string | null;
  readonly user: UserSummary | null;
  readonly company: CompanyContext | null;
  readonly branch: BranchContext | null;
  readonly terminal: TerminalContext | null;
}

const STORAGE_KEYS = {
  ACCESS_TOKEN: 'magic_access_token',
  REFRESH_TOKEN: 'magic_refresh_token',
  USER_DATA: 'magic_user_data',
  COMPANY_DATA: 'magic_company_data',
  BRANCH_DATA: 'magic_branch_data',
  TERMINAL_DATA: 'magic_terminal_data',
};

@Injectable({ providedIn: 'root' })
export class AuthBoundaryService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = environment.apiBaseUrl;

  private readonly stateSignal = signal<AuthState>(this.loadInitialState());

  readonly state = this.stateSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.state().isAuthenticated);
  readonly currentUser = computed(() => this.state().user);
  readonly currentUsername = computed(() => this.state().user?.username ?? null);
  readonly accessToken = computed(() => this.state().accessToken);
  readonly userRoles = computed(() => this.state().user?.roles ?? []);
  readonly userPermissions = computed(() => this.state().user?.permissions ?? []);
  readonly company = computed(() => this.state().company);
  readonly branch = computed(() => this.state().branch);
  readonly terminal = computed(() => this.state().terminal);

  async login(
    usernameOrEmail: string,
    password: string,
    branchId?: string,
    terminalId?: string
  ): Promise<{ success: boolean; error?: string; lockout?: boolean }> {
    const trimmedUser = usernameOrEmail?.trim();
    if (!trimmedUser || !password) {
      return { success: false, error: 'Please enter both username/email and password.' };
    }

    try {
      const response = await firstValueFrom(
        this.http.post<ApiResponse<LoginResponseData>>(`${this.baseUrl}/v1/auth/login`, {
          usernameOrEmail: trimmedUser,
          password,
          branchId,
          terminalId,
        })
      );

      if (response.success && response.data) {
        this.setAuthenticatedSession(response.data);
        return { success: true };
      }

      return { success: false, error: 'Authentication failed. Please verify credentials.' };
    } catch (err: any) {
      const status = err.status;
      if (status === 0) {
        return { success: false, error: 'Cannot connect to server. Please ensure the backend service is running.' };
      }

      const errorDetail = err.error?.detail || err.error?.title || 'Invalid credentials.';

      if (status === 423) {
        return { success: false, error: errorDetail, lockout: true };
      }

      return { success: false, error: errorDetail };
    }
  }

  async refreshSession(): Promise<boolean> {
    const currentRefresh = this.state().refreshToken;
    if (!currentRefresh) {
      this.clearSession();
      return false;
    }

    try {
      const response = await firstValueFrom(
        this.http.post<ApiResponse<LoginResponseData>>(`${this.baseUrl}/v1/auth/refresh`, {
          refreshToken: currentRefresh,
        })
      );

      if (response.success && response.data) {
        this.setAuthenticatedSession(response.data);
        return true;
      }

      this.clearSession();
      return false;
    } catch {
      this.clearSession();
      return false;
    }
  }

  async logout(): Promise<void> {
    const currentRefresh = this.state().refreshToken;
    if (currentRefresh) {
      try {
        await firstValueFrom(
          this.http.post<ApiResponse<string>>(`${this.baseUrl}/v1/auth/logout`, {
            refreshToken: currentRefresh,
          })
        );
      } catch {
        // Ignore network errors on logout
      }
    }

    this.clearSession();
    this.router.navigate(['/login']);
  }

  async changePassword(
    currentPassword: string,
    newPassword: string,
    confirmPassword: string
  ): Promise<{ success: boolean; error?: string }> {
    try {
      const response = await firstValueFrom(
        this.http.post<ApiResponse<string>>(`${this.baseUrl}/v1/auth/change-password`, {
          currentPassword,
          newPassword,
          confirmPassword,
        })
      );

      return { success: response.success };
    } catch (err: any) {
      const detail = err.error?.detail || err.error?.title || 'Password change failed.';
      return { success: false, error: detail };
    }
  }

  hasPermission(permissionCode: string): boolean {
    if (!this.isAuthenticated()) {
      return false;
    }

    const roles = this.userRoles();
    if (roles.includes('Administrator')) {
      return true;
    }

    return this.userPermissions().includes(permissionCode);
  }

  hasRole(roleName: string): boolean {
    return this.userRoles().includes(roleName);
  }

  private setAuthenticatedSession(data: LoginResponseData): void {
    const newState: AuthState = {
      isAuthenticated: true,
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      expiresAtUtc: data.expiresAtUtc,
      user: data.user,
      company: data.company,
      branch: data.branch ?? null,
      terminal: data.terminal ?? null,
    };

    this.stateSignal.set(newState);

    try {
      sessionStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, data.accessToken);
      sessionStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, data.refreshToken);
      sessionStorage.setItem(STORAGE_KEYS.USER_DATA, JSON.stringify(data.user));
      sessionStorage.setItem(STORAGE_KEYS.COMPANY_DATA, JSON.stringify(data.company));
      if (data.branch) {
        sessionStorage.setItem(STORAGE_KEYS.BRANCH_DATA, JSON.stringify(data.branch));
      }
      if (data.terminal) {
        sessionStorage.setItem(STORAGE_KEYS.TERMINAL_DATA, JSON.stringify(data.terminal));
      }
    } catch {
      // Storage unavailable in private browsing
    }
  }

  private clearSession(): void {
    this.stateSignal.set({
      isAuthenticated: false,
      accessToken: null,
      refreshToken: null,
      expiresAtUtc: null,
      user: null,
      company: null,
      branch: null,
      terminal: null,
    });

    try {
      sessionStorage.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
      sessionStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
      sessionStorage.removeItem(STORAGE_KEYS.USER_DATA);
      sessionStorage.removeItem(STORAGE_KEYS.COMPANY_DATA);
      sessionStorage.removeItem(STORAGE_KEYS.BRANCH_DATA);
      sessionStorage.removeItem(STORAGE_KEYS.TERMINAL_DATA);
    } catch {
      // Storage unavailable
    }
  }

  private loadInitialState(): AuthState {
    try {
      const accessToken = sessionStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
      const refreshToken = sessionStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN);
      const rawUser = sessionStorage.getItem(STORAGE_KEYS.USER_DATA);
      const rawCompany = sessionStorage.getItem(STORAGE_KEYS.COMPANY_DATA);
      const rawBranch = sessionStorage.getItem(STORAGE_KEYS.BRANCH_DATA);
      const rawTerminal = sessionStorage.getItem(STORAGE_KEYS.TERMINAL_DATA);

      if (accessToken && rawUser) {
        return {
          isAuthenticated: true,
          accessToken,
          refreshToken,
          expiresAtUtc: null,
          user: JSON.parse(rawUser),
          company: rawCompany ? JSON.parse(rawCompany) : null,
          branch: rawBranch ? JSON.parse(rawBranch) : null,
          terminal: rawTerminal ? JSON.parse(rawTerminal) : null,
        };
      }
    } catch {
      // Storage unavailable
    }

    return {
      isAuthenticated: false,
      accessToken: null,
      refreshToken: null,
      expiresAtUtc: null,
      user: null,
      company: null,
      branch: null,
      terminal: null,
    };
  }
}
