import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ConnectivityMonitorService } from '../../../core/services/connectivity-monitor.service';
import { StatusBadgeComponent, StatusTone } from '../status-badge/status-badge.component';

@Component({
  selector: 'magic-connectivity-banner',
  standalone: true,
  imports: [StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="banner"
      [attr.data-state]="state()"
      [attr.role]="state() === 'unreachable' ? 'alert' : 'status'"
      [attr.aria-live]="state() === 'unreachable' ? 'assertive' : 'polite'"
    >
      <div class="summary">
        <magic-status-badge [label]="badgeLabel()" [tone]="badgeTone()" />
        <p class="message">{{ message() }}</p>
      </div>
      <button
        type="button"
        class="retry-button"
        [disabled]="monitor.isChecking()"
        (click)="monitor.refresh()"
      >
        {{ monitor.isChecking() ? 'Checking LAN...' : 'Verify Connection' }}
      </button>
    </div>
  `,
  styles: `
    .banner {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--magic-space-3);
      padding: var(--magic-space-3) var(--magic-space-4);
      border-radius: var(--magic-radius-md);
      border: 1px solid var(--magic-border-subtle);
      background-color: var(--magic-surface-elevated);
      box-shadow: var(--magic-shadow-sm);
    }

    .banner[data-state='unreachable'] {
      border-color: var(--magic-status-error-border);
    }

    .banner[data-state='degraded'] {
      border-color: var(--magic-status-warn-border);
    }

    .summary {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--magic-space-3);
      min-inline-size: 0;
    }

    .message {
      color: var(--magic-text-secondary);
      font-size: 0.875rem;
    }

    .retry-button {
      min-block-size: 2.75rem;
      min-inline-size: 2.75rem;
      padding-inline: var(--magic-space-4);
      border-radius: var(--magic-radius-sm);
      border: 1px solid var(--magic-border-strong);
      background-color: var(--magic-surface-subtle);
      color: var(--magic-text-primary);
      font-weight: 600;
      cursor: pointer;
    }

    .retry-button:disabled {
      opacity: 0.65;
      cursor: progress;
    }
  `,
})
export class ConnectivityBannerComponent {
  protected readonly monitor = inject(ConnectivityMonitorService);
  protected readonly state = this.monitor.connectivityState;

  protected readonly badgeLabel = computed(() => {
    switch (this.state()) {
      case 'connected':
        return 'Strict Online LAN Ready';
      case 'degraded':
        return 'Backend Degraded';
      case 'checking':
        return 'Checking LAN Host';
      case 'unreachable':
        return 'LAN Host Unreachable';
    }
  });

  protected readonly badgeTone = computed<StatusTone>(() => {
    switch (this.state()) {
      case 'connected':
        return 'ok';
      case 'degraded':
        return 'warn';
      case 'unreachable':
        return 'error';
      case 'checking':
        return 'neutral';
    }
  });

  protected readonly message = computed(() => {
    const err = this.monitor.lastError();
    if (err) {
      return err;
    }
    const status = this.monitor.systemStatus();
    if (!status) {
      return 'Probing MAGIC local backend and PostgreSQL database connectivity...';
    }
    return `Connected to ${status.serviceName} (${status.environment}) · Database: ${
      status.databaseConnected ? 'PostgreSQL Online' : 'Disconnected'
    } · Zero Offline Buffering (OPEN-001)`;
  });
}

