import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthBoundaryService } from '../../core/services/auth-boundary.service';
import { ConnectivityMonitorService } from '../../core/services/connectivity-monitor.service';
import { DesktopBridgeService } from '../../core/services/desktop-bridge.service';
import { WorkspaceContextService } from '../../core/services/workspace-context.service';
import { ConnectivityBannerComponent } from '../../shared/components/connectivity-banner/connectivity-banner.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-platform-status-page',
  standalone: true,
  imports: [ConnectivityBannerComponent, EmptyStateComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <h1>Platform Technical Foundation Status</h1>
        <p class="subtitle">
          Phase 1 environment readiness, PostgreSQL database verification, and approved architecture boundaries.
        </p>
      </header>

      <magic-connectivity-banner />

      <section class="grid" aria-label="Architecture Guardrails">
        <article class="card">
          <div class="card-head">
            <h2>Network &amp; Persistence (OPEN-001)</h2>
            <magic-status-badge label="Strict Online LAN" tone="ok" />
          </div>
          <p>
            Single authoritative PostgreSQL 18 cluster. Local SQLite buffering and offline transaction queues are strictly prohibited in V1.
          </p>
        </article>

        <article class="card">
          <div class="card-head">
            <h2>UI Foundation (OPEN-002)</h2>
            <magic-status-badge label="Pure Angular CDK" tone="ok" />
          </div>
          <p>
            Custom layered SCSS and CSS design tokens built with &#64;angular/cdk primitives; zero external component kit lock-in.
          </p>
        </article>

        <article class="card">
          <div class="card-head">
            <h2>Security Boundary (OPEN-003)</h2>
            <magic-status-badge [label]="auth.state().status" tone="warn" />
          </div>
          <p>{{ auth.state().note }}</p>
        </article>

        <article class="card">
          <div class="card-head">
            <h2>Client Host Environment</h2>
            <magic-status-badge
              [label]="desktop.hostMode() === 'webview2' ? 'WPF WebView2' : 'Web Browser'"
              tone="neutral"
            />
          </div>
          <p>
            Active context headers: Branch
            <code>{{ workspace.context().branchId ?? 'Unbound' }}</code>,
            Terminal
            <code>{{ workspace.context().terminalId ?? 'Unbound' }}</code>.
          </p>
        </article>
      </section>

      @if (monitor.systemStatus(); as status) {
        <section class="card" aria-label="Backend Component Health">
          <h2>Live Backend Components ({{ status.serviceName }} v{{ status.version }})</h2>
          <ul class="component-list">
            @for (component of status.components; track component.name) {
              <li class="component-row">
                <div>
                  <strong>{{ component.name }}</strong>
                  @if (component.details) {
                    <p class="detail">{{ component.details }}</p>
                  }
                </div>
                <magic-status-badge
                  [label]="component.status"
                  [tone]="
                    component.status === 'Healthy'
                      ? 'ok'
                      : component.status === 'Unhealthy'
                        ? 'error'
                        : 'neutral'
                  "
                />
              </li>
            }
          </ul>
        </section>
      } @else {
        <magic-empty-state
          heading="Waiting for Live Backend Snapshot"
          description="Start Magic.Api on the LAN server (default http://127.0.0.1:5000) and select Verify Connection to inspect live PostgreSQL and outbox telemetry."
        />
      }
    </div>
  `,
  styles: `
    .page {
      display: grid;
      gap: var(--magic-space-5);
    }

    .page-header h1 {
      font-size: 1.5rem;
      line-height: 1.25;
      color: var(--magic-text-primary);
    }

    .subtitle {
      margin-block-start: var(--magic-space-1);
      color: var(--magic-text-secondary);
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr));
      gap: var(--magic-space-4);
    }

    .card {
      padding: var(--magic-space-4);
      border-radius: var(--magic-radius-md);
      border: 1px solid var(--magic-border-subtle);
      background-color: var(--magic-surface-elevated);
      box-shadow: var(--magic-shadow-sm);
      display: grid;
      gap: var(--magic-space-3);
      align-content: start;
    }

    .card-head {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--magic-space-2);
    }

    h2 {
      font-size: 1.0625rem;
      color: var(--magic-text-primary);
    }

    p {
      color: var(--magic-text-secondary);
    }

    .component-list {
      display: grid;
      gap: var(--magic-space-2);
    }

    .component-row {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--magic-space-3);
      padding-block: var(--magic-space-2);
      border-block-end: 1px solid var(--magic-border-subtle);
    }

    .component-row:last-child {
      border-block-end: none;
    }

    .detail {
      font-size: 0.875rem;
      color: var(--magic-text-muted);
    }
  `,
})
export class PlatformStatusPage {
  protected readonly monitor = inject(ConnectivityMonitorService);
  protected readonly auth = inject(AuthBoundaryService);
  protected readonly desktop = inject(DesktopBridgeService);
  protected readonly workspace = inject(WorkspaceContextService);
}
