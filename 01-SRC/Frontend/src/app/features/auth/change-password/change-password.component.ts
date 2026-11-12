import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthBoundaryService } from '../../../core/services/auth-boundary.service';

@Component({
  selector: 'magic-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="pwd-wrapper">
      <div class="pwd-card">
        <h2 class="pwd-title">Change Password</h2>
        <p class="pwd-subtitle">Update your password to keep your enterprise account secure.</p>

        @if (successMessage()) {
          <div class="alert alert-success" role="alert">
            <span>{{ successMessage() }}</span>
          </div>
        }

        @if (errorMessage()) {
          <div class="alert alert-error" role="alert">
            <span>{{ errorMessage() }}</span>
          </div>
        }

        <form (ngSubmit)="onSubmit()" novalidate>
          <div class="form-group">
            <label for="current-pwd">Current Password</label>
            <input
              id="current-pwd"
              type="password"
              class="form-control"
              [(ngModel)]="currentPassword"
              name="currentPassword"
              required
              [disabled]="isLoading()"
            />
          </div>

          <div class="form-group">
            <label for="new-pwd">New Password (Min 8 characters)</label>
            <input
              id="new-pwd"
              type="password"
              class="form-control"
              [(ngModel)]="newPassword"
              name="newPassword"
              required
              [disabled]="isLoading()"
            />
          </div>

          <div class="form-group">
            <label for="confirm-pwd">Confirm New Password</label>
            <input
              id="confirm-pwd"
              type="password"
              class="form-control"
              [(ngModel)]="confirmPassword"
              name="confirmPassword"
              required
              [disabled]="isLoading()"
            />
          </div>

          <div class="btn-row">
            <button type="submit" class="btn btn-primary" [disabled]="isLoading()">
              @if (isLoading()) {
                <span>Updating...</span>
              } @else {
                <span>Update Password</span>
              }
            </button>
            <button type="button" class="btn btn-secondary" (click)="cancel()" [disabled]="isLoading()">
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .pwd-wrapper {
      min-height: 80vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
    }
    .pwd-card {
      max-width: 440px;
      width: 100%;
      background: var(--surface-primary, #ffffff);
      border: 1px solid var(--border-subtle, #e2e8f0);
      border-radius: 12px;
      padding: 2.25rem;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
    }
    .pwd-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--text-primary, #0f172a);
      margin: 0 0 0.35rem;
    }
    .pwd-subtitle {
      font-size: 0.8125rem;
      color: var(--text-secondary, #64748b);
      margin: 0 0 1.5rem;
    }
    .alert {
      padding: 0.75rem 1rem;
      border-radius: 6px;
      font-size: 0.8125rem;
      font-weight: 500;
      margin-bottom: 1.25rem;
    }
    .alert-success {
      background: #ecfdf5;
      border: 1px solid #a7f3d0;
      color: #065f46;
    }
    .alert-error {
      background: #fef2f2;
      border: 1px solid #fecaca;
      color: #b91c1c;
    }
    .form-group {
      margin-bottom: 1.15rem;
      label {
        display: block;
        font-size: 0.8125rem;
        font-weight: 600;
        color: #334155;
        margin-bottom: 0.35rem;
      }
    }
    .form-control {
      width: 100%;
      height: 2.5rem;
      padding: 0 0.75rem;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      font-size: 0.875rem;
      &:focus {
        outline: none;
        border-color: #e7aa1b;
        box-shadow: 0 0 0 3px rgba(231, 170, 27, 0.15);
      }
    }
    .btn-row {
      display: flex;
      gap: 0.75rem;
      margin-top: 1.5rem;
    }
    .btn {
      padding: 0.65rem 1.25rem;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
    }
    .btn-primary {
      background: #0f172a;
      color: #ffffff;
      border: none;
      flex: 1;
      &:hover:not(:disabled) { background: #1e293b; }
      &:disabled { opacity: 0.7; }
    }
    .btn-secondary {
      background: #ffffff;
      color: #334155;
      border: 1px solid #cbd5e1;
      &:hover:not(:disabled) { background: #f8fafc; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordComponent {
  private readonly auth = inject(AuthBoundaryService);
  private readonly router = inject(Router);

  protected currentPassword = '';
  protected newPassword = '';
  protected confirmPassword = '';
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);

  protected async onSubmit(): Promise<void> {
    const cur = this.currentPassword;
    const np = this.newPassword;
    const cp = this.confirmPassword;

    if (!cur || !np || !cp) {
      this.errorMessage.set('All fields are required.');
      return;
    }

    if (np.length < 8) {
      this.errorMessage.set('New password must be at least 8 characters.');
      return;
    }

    if (np !== cp) {
      this.errorMessage.set('New passwords do not match.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const result = await this.auth.changePassword(cur, np, cp);
    this.isLoading.set(false);

    if (result.success) {
      this.successMessage.set('Password changed successfully! Please sign in again.');
      setTimeout(() => {
        this.auth.logout();
      }, 1500);
    } else {
      this.errorMessage.set(result.error || 'Failed to change password.');
    }
  }

  protected cancel(): void {
    this.router.navigate(['/accounting/dashboard']);
  }
}

