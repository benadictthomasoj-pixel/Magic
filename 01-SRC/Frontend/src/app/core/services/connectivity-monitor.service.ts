import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, finalize, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SystemContextBoundaryDto, SystemHealthStatusDto } from '../contracts/api.models';
import { ApiClientService } from './api-client.service';

export type ConnectivityState = 'checking' | 'connected' | 'unreachable' | 'degraded';

@Injectable({ providedIn: 'root' })
export class ConnectivityMonitorService {
  private readonly apiClient = inject(ApiClientService);
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);

  private readonly browserOnlineSignal = signal<boolean>(this.readNavigatorOnline());
  private readonly isCheckingSignal = signal<boolean>(false);
  private readonly systemStatusSignal = signal<SystemHealthStatusDto | null>(null);
  private readonly contextBoundarySignal = signal<SystemContextBoundaryDto | null>(null);
  private readonly lastErrorSignal = signal<string | null>(null);
  private readonly lastCheckedUtcSignal = signal<string | null>(null);

  private pollHandle: number | null = null;

  readonly browserOnline = this.browserOnlineSignal.asReadonly();
  readonly isChecking = this.isCheckingSignal.asReadonly();
  readonly systemStatus = this.systemStatusSignal.asReadonly();
  readonly contextBoundary = this.contextBoundarySignal.asReadonly();
  readonly lastError = this.lastErrorSignal.asReadonly();
  readonly lastCheckedUtc = this.lastCheckedUtcSignal.asReadonly();

  readonly connectivityState = computed<ConnectivityState>(() => {
    if (!this.browserOnlineSignal()) {
      return 'unreachable';
    }
    if (this.lastErrorSignal()) {
      return 'unreachable';
    }
    const status = this.systemStatusSignal();
    if (!status) {
      return this.isCheckingSignal() ? 'checking' : 'unreachable';
    }
    return status.databaseConnected && status.status === 'Healthy' ? 'connected' : 'degraded';
  });

  constructor() {
    const win = this.document.defaultView;
    if (win) {
      const onOnline = (): void => {
        this.browserOnlineSignal.set(true);
        this.refresh();
      };
      const onOffline = (): void => {
        this.browserOnlineSignal.set(false);
        this.lastErrorSignal.set(
          'Browser/terminal network offline. Strict Online LAN policy blocks offline transactions.'
        );
      };

      win.addEventListener('online', onOnline);
      win.addEventListener('offline', onOffline);

      this.destroyRef.onDestroy(() => {
        win.removeEventListener('online', onOnline);
        win.removeEventListener('offline', onOffline);
        this.stopPolling();
      });
    }
  }

  startPolling(): void {
    this.refresh();

    const win = this.document.defaultView;
    if (!win || this.pollHandle !== null) {
      return;
    }

    this.pollHandle = win.setInterval(() => {
      if (this.browserOnlineSignal()) {
        this.refresh();
      }
    }, environment.connectivityPollIntervalMs);
  }

  stopPolling(): void {
    const win = this.document.defaultView;
    if (win && this.pollHandle !== null) {
      win.clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
  }

  refresh(): void {
    if (this.isCheckingSignal()) {
      return;
    }

    this.isCheckingSignal.set(true);

    this.apiClient
      .getSystemStatus()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => {
          this.lastErrorSignal.set(
            'Central MAGIC backend is unreachable on LAN. Start Magic.Api to verify PostgreSQL readiness.'
          );
          this.systemStatusSignal.set(null);
          return of(null);
        }),
        finalize(() => {
          this.isCheckingSignal.set(false);
          this.lastCheckedUtcSignal.set(new Date().toISOString());
        })
      )
      .subscribe((response) => {
        if (response?.success && response.data) {
          this.systemStatusSignal.set(response.data);
          this.lastErrorSignal.set(null);
        }
      });

    this.apiClient
      .getContextBoundary()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of(null))
      )
      .subscribe((response) => {
        if (response?.success && response.data) {
          this.contextBoundarySignal.set(response.data);
        }
      });
  }

  private readNavigatorOnline(): boolean {
    const nav = this.document.defaultView?.navigator;
    return nav ? nav.onLine : true;
  }
}
