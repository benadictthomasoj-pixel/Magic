import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  EnterpriseApiService,
  AdminUser,
  RoleWithPermissions,
  ApprovalWorkflow,
  ApprovalRequest,
  AuditLogEntry,
  SecurityMetrics,
} from '../../../core/services/enterprise-api.service';

@Component({
  selector: 'magic-enterprise-administration',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './enterprise-administration.component.html',
  styleUrl: './enterprise-administration.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnterpriseAdministrationComponent implements OnInit {
  private readonly api = inject(EnterpriseApiService);

  protected readonly activeTab = signal<'users' | 'roles' | 'approvals' | 'audit' | 'metrics'>('users');
  protected readonly users = signal<readonly AdminUser[]>([]);
  protected readonly roles = signal<readonly RoleWithPermissions[]>([]);
  protected readonly workflows = signal<readonly ApprovalWorkflow[]>([]);
  protected readonly approvalRequests = signal<readonly ApprovalRequest[]>([]);
  protected readonly auditLogs = signal<readonly AuditLogEntry[]>([]);
  protected readonly securityMetrics = signal<SecurityMetrics | null>(null);
  protected readonly loading = signal(false);
  protected readonly actionMessage = signal<string | null>(null);

  // Modals
  protected readonly showUserModal = signal(false);
  protected readonly showWorkflowModal = signal(false);
  protected readonly showActionModal = signal(false);

  // Selected request for action
  protected selectedRequestId: string | null = null;
  protected actionType: 'Approved' | 'Rejected' = 'Approved';
  protected actionReason = '';

  // New User Form State
  protected userForm = {
    username: '',
    email: '',
    displayName: '',
    password: '',
    roleCodes: ['operator'],
  };

  // New Workflow Form State
  protected workflowForm = {
    module: 'Procurement',
    transactionType: 'PurchaseOrder',
    minimumAmountRupees: 50000,
    requiredApprovalsCount: 1,
  };

  ngOnInit(): void {
    this.loadUsers();
    this.loadSecurityMetrics();
  }

  protected setTab(tab: 'users' | 'roles' | 'approvals' | 'audit' | 'metrics'): void {
    this.activeTab.set(tab);
    if (tab === 'users') this.loadUsers();
    if (tab === 'roles') this.loadRoles();
    if (tab === 'approvals') {
      this.loadWorkflows();
      this.loadApprovalRequests();
    }
    if (tab === 'audit') this.loadAuditLogs();
    if (tab === 'metrics') this.loadSecurityMetrics();
  }

  protected loadUsers(): void {
    this.loading.set(true);
    this.api.getUsers().subscribe({
      next: (res) => {
        this.users.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadRoles(): void {
    this.loading.set(true);
    this.api.getRolesWithPermissions().subscribe({
      next: (res) => {
        this.roles.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadWorkflows(): void {
    this.api.getApprovalWorkflows().subscribe({
      next: (res) => this.workflows.set(res),
      error: () => {},
    });
  }

  protected loadApprovalRequests(): void {
    this.loading.set(true);
    this.api.getApprovalRequests().subscribe({
      next: (res) => {
        this.approvalRequests.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadAuditLogs(): void {
    this.loading.set(true);
    this.api.getAuditLogs().subscribe({
      next: (res) => {
        this.auditLogs.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadSecurityMetrics(): void {
    this.api.getSecurityMetrics().subscribe({
      next: (res) => this.securityMetrics.set(res),
      error: () => {},
    });
  }

  protected createUser(): void {
    this.api.createUser(this.userForm).subscribe({
      next: () => {
        this.showUserModal.set(false);
        this.notify('User created successfully');
        this.loadUsers();
      },
      error: (err) => this.notify(`Failed to create user: ${err.message}`),
    });
  }

  protected toggleLockout(userId: string, lockout: boolean): void {
    this.api.toggleUserLockout(userId, lockout).subscribe({
      next: () => {
        this.notify(`User lockout status changed`);
        this.loadUsers();
      },
      error: (err) => this.notify(`Update failed: ${err.message}`),
    });
  }

  protected createWorkflow(): void {
    this.api
      .createApprovalWorkflow({
        module: this.workflowForm.module,
        transactionType: this.workflowForm.transactionType,
        minimumAmountPaise: Math.round(this.workflowForm.minimumAmountRupees * 100),
        requiredApprovalsCount: this.workflowForm.requiredApprovalsCount,
      })
      .subscribe({
        next: () => {
          this.showWorkflowModal.set(false);
          this.notify('Approval workflow rule created');
          this.loadWorkflows();
        },
        error: (err) => this.notify(`Workflow creation failed: ${err.message}`),
      });
  }

  protected openActionModal(requestId: string, action: 'Approved' | 'Rejected'): void {
    this.selectedRequestId = requestId;
    this.actionType = action;
    this.actionReason = '';
    this.showActionModal.set(true);
  }

  protected submitApprovalAction(): void {
    if (!this.selectedRequestId) return;
    this.api
      .processApprovalAction(this.selectedRequestId, {
        action: this.actionType,
        reason: this.actionReason,
      })
      .subscribe({
        next: () => {
          this.showActionModal.set(false);
          this.notify(`Request marked as ${this.actionType}`);
          this.loadApprovalRequests();
        },
        error: (err) => this.notify(`Action failed: ${err.message}`),
      });
  }

  protected formatPaise(paise?: number): string {
    if (paise === undefined || paise === null) return '0.00';
    return (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }

  private notify(msg: string): void {
    this.actionMessage.set(msg);
    setTimeout(() => this.actionMessage.set(null), 4000);
  }
}
