import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthBoundaryService } from '../../../core/services/auth-boundary.service';

@Component({
  selector: 'magic-access-denied',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="denied-container">
      <div class="denied-card">
        <div class="icon-circle">
          <svg class="lock-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
            <path d="M7 11V7a5 5 0 0 1 10 0v4" />
          </svg>
        </div>
        <h1 class="denied-title">Access Denied (403 Forbidden)</h1>
        <p class="denied-desc">
          You do not have the required permissions to access this module or execute this operation.
        </p>

        @if (requiredPermission()) {
          <div class="permission-pill">
            <span class="pill-label">Required Claim:</span>
            <code>{{ requiredPermission() }}</code>
          </div>
        }

        <div class="user-meta">
          <span>Signed in as: <strong>{{ auth.currentUsername() || 'Anonymous' }}</strong></span>
          <span>Assigned Roles: <strong>{{ auth.userRoles().join(', ') || 'None' }}</strong></span>
        </div>

        <div class="actions-row">
          <a routerLink="/accounting/dashboard" class="btn btn-primary">
            Return to Dashboard
          </a>
          <button type="button" class="btn btn-secondary" (click)="signOut()">
            Sign in as Different User
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .denied-container {
      min-height: 80vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
    }
    .denied-card {
      max-width: 500px;
      width: 100%;
      background: var(--surface-primary, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: 12px;
      padding: 2.5rem;
      text-align: center;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
    }
    .icon-circle {
      width: 64px;
      height: 64px;
      border-radius: 50%;
      background: #fef2f2;
      border: 1px solid #fecaca;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 1.25rem;
      color: #dc2626;
    }
    .lock-icon {
      width: 32px;
      height: 32px;
    }
    .denied-title {
      font-size: 1.35rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0 0 0.5rem;
    }
    .denied-desc {
      font-size: 0.875rem;
      color: var(--text-secondary, #64748b);
      margin: 0 0 1.25rem;
      line-height: 1.5;
    }
    .permission-pill {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      background: #f1f5f9;
      padding: 0.4rem 0.75rem;
      border-radius: 6px;
      font-size: 0.8125rem;
      margin-bottom: 1.25rem;
      code {
        font-family: monospace;
        font-weight: 700;
        color: #b91c1c;
      }
    }
    .user-meta {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      font-size: 0.8125rem;
      color: #64748b;
      margin-bottom: 1.75rem;
      padding-top: 1rem;
      border-top: 1px solid var(--border-subtle, #e2e8f0);
    }
    .actions-row {
      display: flex;
      gap: 0.75rem;
      justify-content: center;
    }
    .btn {
      padding: 0.65rem 1.25rem;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      text-decoration: none;
      display: inline-flex;
      align-items: center;
    }
    .btn-primary {
      background: #0f172a;
      color: #ffffff;
      border: 1px solid #0f172a;
      &:hover { background: #1e293b; }
    }
    .btn-secondary {
      background: #ffffff;
      color: #334155;
      border: 1px solid #cbd5e1;
      &:hover { background: #f8fafc; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessDeniedComponent implements OnInit {
  protected readonly auth = inject(AuthBoundaryService);
  private readonly route = inject(ActivatedRoute);

  protected readonly requiredPermission = signal<string | null>(null);

  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      if (params['permission']) {
        this.requiredPermission.set(params['permission']);
      }
    });
  }

  protected signOut(): void {
    this.auth.logout();
  }
}

