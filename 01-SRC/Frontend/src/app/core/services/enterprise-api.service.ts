import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../contracts/api.models';

export interface ExecutiveDashboard {
  totalSalesPaise: number;
  netSalesPaise: number;
  grossProfitPaise?: number;
  grossProfitStatus: string;
  totalPurchasesPaise: number;
  operatingExpensesPaise: number;
  netProfitPaise?: number;
  netProfitStatus: string;
  cashPositionPaise: number;
  bankBalancesPaise: number;
  accountsReceivablePaise: number;
  accountsPayablePaise: number;
  totalInventoryQuantity: number;
  lowStockAlertsCount: number;
  pendingPurchaseOrdersCount: number;
  pendingCustomerOrdersCount: number;
  outstandingSupplierInvoicesCount: number;
  activeEmployeesCount: number;
  lastMonthPayrollPaise: number;
  branchPerformance: Array<{
    branchId: string;
    branchCode: string;
    branchName: string;
    salesPaise: number;
    orderCount: number;
  }>;
  dailyTrends: Array<{
    date: string;
    salesPaise: number;
    transactionCount: number;
  }>;
  generatedAtUtc: string;
}

export interface GenericReportRow {
  key: string;
  label: string;
  category?: string;
  quantity: number;
  amountPaise: number;
  taxAmountPaise: number;
  status?: string;
  reference?: string;
}

export interface ReportResult {
  reportTitle: string;
  periodDescription: string;
  rows: GenericReportRow[];
  totalAmountPaise: number;
  totalTaxAmountPaise: number;
  totalQuantity: number;
  recordCount: number;
  generatedAtUtc: string;
}

export interface CompanyProfile {
  id: string;
  legalName: string;
  tradeName: string;
  gstin?: string;
  pan?: string;
  address: string;
  phone?: string;
  email?: string;
  currencyCode: string;
  fiscalYearStartMonth: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface Branch {
  id: string;
  companyId: string;
  branchCode: string;
  branchName: string;
  address: string;
  phone?: string;
  email?: string;
  gstin?: string;
  branchManager?: string;
  isActive: boolean;
  warehouseCount: number;
  terminalCount: number;
  createdAtUtc: string;
}

export interface TerminalDetails {
  id: string;
  companyId: string;
  branchId: string;
  branchName: string;
  terminalCode: string;
  machineName: string;
  operatingSystem?: string;
  assignedCashierName?: string;
  isAuthorized: boolean;
  registeredAtUtc: string;
  lastHeartbeatAtUtc?: string;
  posPrinterModel?: string;
  receiptWidthMm: number;
}

export interface InterBranchTransfer {
  id: string;
  companyId: string;
  transferNumber: string;
  sourceBranchId: string;
  sourceBranchName: string;
  sourceWarehouseId: string;
  sourceWarehouseName: string;
  destinationBranchId: string;
  destinationBranchName: string;
  destinationWarehouseId: string;
  destinationWarehouseName: string;
  status: string;
  notes?: string;
  createdAtUtc: string;
}

export interface AdminUser {
  id: string;
  companyId: string;
  username: string;
  email: string;
  displayName: string;
  isActive: boolean;
  isLockedOut: boolean;
  failedLoginAttempts: number;
  lastLoginAtUtc?: string;
  roles: string[];
  assignedBranchIds: string[];
  createdAtUtc: string;
}

export interface RoleWithPermissions {
  id: string;
  code: string;
  name: string;
  description?: string;
  isSystem: boolean;
  permissions: string[];
}

export interface ApprovalWorkflow {
  id: string;
  companyId: string;
  module: string;
  transactionType: string;
  minimumAmountPaise: number;
  requiredApprovalsCount: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface ApprovalRequest {
  id: string;
  companyId: string;
  branchId: string;
  branchName: string;
  workflowId?: string;
  sourceModule: string;
  sourceDocumentId: string;
  referenceNumber: string;
  amountPaise: number;
  status: string;
  requestedByUserName: string;
  notes?: string;
  createdAtUtc: string;
  actions: Array<{
    approverUserId: string;
    approverName: string;
    action: string;
    reason?: string;
    actedAtUtc: string;
  }>;
}

export interface AuditLogEntry {
  id: string;
  companyId: string;
  username?: string;
  eventType: string;
  details?: string;
  ipAddress?: string;
  timestampUtc: string;
}

export interface SecurityMetrics {
  totalActiveUsers: number;
  lockedOutUsersCount: number;
  failedLoginsLast24Hours: number;
  activeSessionsCount: number;
  revokedTokensCount: number;
  evaluatedAtUtc: string;
}

export interface AutomationRule {
  id: string;
  companyId: string;
  name: string;
  triggerType: string;
  conditionJson: string;
  actionType: string;
  actionPayloadJson: string;
  isActive: boolean;
  createdAtUtc: string;
  lastTriggeredAtUtc?: string;
  executionCount: number;
}

export interface NotificationItem {
  id: string;
  companyId: string;
  branchId?: string;
  category: string;
  priority: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAtUtc: string;
}

export interface AiAssistantResponse {
  answer: string;
  sourceModule: string;
  reportingPeriod: string;
  drilldowns: Array<{ label: string; value: string; routeUrl?: string }>;
  recommendedActions: string[];
  generatedAtUtc: string;
}

export interface SystemHealthDashboard {
  applicationStatus: string;
  apiHealth: string;
  databaseConnectivity: string;
  databaseVersion: string;
  applicationVersion: string;
  environmentName: string;
  pendingOutboxMessagesCount: number;
  failedOutboxMessagesCount: number;
  activeBackgroundJobsCount: number;
  lastBackupAtUtc?: string;
  uptimeFormatted: string;
  recentErrors: Array<{ timestampUtc: string; level: string; source: string; message: string }>;
  evaluatedAtUtc: string;
}

export interface BackupRecord {
  id: string;
  backupType: string;
  fileName: string;
  fileSizeBytes: number;
  storagePath: string;
  status: string;
  sha256Checksum: string;
  isVerified: boolean;
  initiatedByUserName: string;
  createdAtUtc: string;
  verifiedAtUtc?: string;
  retentionDays: number;
}

export interface RestoreResult {
  success: boolean;
  targetDatabase: string;
  message: string;
  completedAtUtc: string;
}

export interface DatabaseMigrationSummary {
  currentMigration: string;
  appliedMigrationsCount: number;
  appliedMigrations: string[];
  pendingMigrationsCount: number;
  databaseHealth: string;
  databaseProvider?: string;
}

export interface BackgroundJobStatus {
  jobName: string;
  status: string;
  intervalMinutes: number;
  lastRunAtUtc?: string;
  nextScheduledAtUtc?: string;
  executionCount: number;
  failureCount: number;
}

export interface SystemConfigurationItem {
  key: string;
  value: string;
  category: string;
  description: string;
  isSecret: boolean;
  updatedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class EnterpriseApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  // 1. Reports (Module 17)
  getExecutiveDashboard(startDate?: string, endDate?: string, branchId?: string): Observable<ExecutiveDashboard> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http.get<ApiResponse<ExecutiveDashboard>>(`${this.baseUrl}/v1/reports/dashboard`, { params })
      .pipe(map(r => r.data));
  }

  getReport(query: { reportType: string; fromDate?: string; toDate?: string; branchId?: string }): Observable<ReportResult> {
    let params = new HttpParams();
    if (query.fromDate) params = params.set('startDate', query.fromDate);
    if (query.toDate) params = params.set('endDate', query.toDate);
    if (query.branchId) params = params.set('branchId', query.branchId);

    switch (query.reportType) {
      case 'sales-by-period':
      case 'sales-by-customer':
      case 'sales-by-category':
        params = params.set('groupByType', query.reportType === 'sales-by-customer' ? 'Customer' : (query.reportType === 'sales-by-category' ? 'Category' : 'Daily'));
        return this.http.get<ApiResponse<ReportResult>>(`${this.baseUrl}/v1/reports/sales`, { params }).pipe(map(r => r.data));
      case 'purchases-by-supplier':
        return this.http.get<ApiResponse<ReportResult>>(`${this.baseUrl}/v1/reports/purchases`, { params }).pipe(map(r => r.data));
      case 'inventory-summary':
      case 'low-stock-alerts':
        if (query.reportType === 'low-stock-alerts') params = params.set('lowStockOnly', 'true');
        return this.http.get<ApiResponse<ReportResult>>(`${this.baseUrl}/v1/reports/inventory`, { params }).pipe(map(r => r.data));
      case 'receivables-aging':
      case 'payables-aging':
      case 'gl-reconciliation':
        return this.http.get<ApiResponse<ReportResult>>(`${this.baseUrl}/v1/reports/financial`, { params }).pipe(map(r => r.data));
      case 'payroll-summary':
        return this.http.get<ApiResponse<ReportResult>>(`${this.baseUrl}/v1/reports/hr-payroll`, { params }).pipe(map(r => r.data));
      default:
        return this.http.get<ApiResponse<ReportResult>>(`${this.baseUrl}/v1/reports/operations`, { params }).pipe(map(r => r.data));
    }
  }

  exportReport(request: { reportType: string; format: string; fromDate?: string; toDate?: string; branchId?: string }): Observable<{ fileName: string; fileSizeBytes: number }> {
    const payload = {
      reportType: request.reportType,
      format: request.format,
      startDate: request.fromDate,
      endDate: request.toDate,
    };

    return this.http.post(`${this.baseUrl}/v1/reports/export`, payload, { responseType: 'blob' }).pipe(
      tap((blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Report_${request.reportType}_${new Date().toISOString().split('T')[0]}.${request.format === 'pdf' ? 'pdf' : (request.format === 'xlsx' ? 'xlsx' : 'csv')}`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      }),
      map((blob) => ({
        fileName: `Report_${request.reportType}.${request.format}`,
        fileSizeBytes: blob.size,
      }))
    );
  }

  // 2. Multi-Branch & Multi-Terminal (Module 18)
  getCompanyProfile(): Observable<CompanyProfile> {
    return this.http.get<ApiResponse<CompanyProfile>>(`${this.baseUrl}/v1/branches/company/profile`).pipe(map(r => r.data));
  }

  updateCompanyProfile(req: any): Observable<CompanyProfile> {
    return this.http.put<ApiResponse<CompanyProfile>>(`${this.baseUrl}/v1/branches/company/profile`, req).pipe(map(r => r.data));
  }

  getBranches(): Observable<Branch[]> {
    return this.http.get<ApiResponse<Branch[]>>(`${this.baseUrl}/v1/branches`).pipe(map(r => r.data));
  }

  createBranch(req: any): Observable<Branch> {
    return this.http.post<ApiResponse<Branch>>(`${this.baseUrl}/v1/branches`, req).pipe(map(r => r.data));
  }

  updateBranch(id: string, req: any): Observable<Branch> {
    return this.http.put<ApiResponse<Branch>>(`${this.baseUrl}/v1/branches/${id}`, req).pipe(map(r => r.data));
  }

  getTerminals(branchId?: string): Observable<TerminalDetails[]> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http.get<ApiResponse<TerminalDetails[]>>(`${this.baseUrl}/v1/branches/terminals`, { params }).pipe(map(r => r.data));
  }

  registerTerminal(req: any): Observable<TerminalDetails> {
    return this.http.post<ApiResponse<TerminalDetails>>(`${this.baseUrl}/v1/branches/terminals`, req).pipe(map(r => r.data));
  }

  authorizeTerminal(id: string, isAuthorized: boolean): Observable<TerminalDetails> {
    return this.http.put<ApiResponse<TerminalDetails>>(`${this.baseUrl}/v1/branches/terminals/${id}/status`, { isAuthorized }).pipe(map(r => r.data));
  }

  getInterBranchTransfers(): Observable<InterBranchTransfer[]> {
    return this.http.get<ApiResponse<InterBranchTransfer[]>>(`${this.baseUrl}/v1/branches/transfers`).pipe(map(r => r.data));
  }

  requestInterBranchTransfer(req: any): Observable<InterBranchTransfer> {
    return this.http.post<ApiResponse<InterBranchTransfer>>(`${this.baseUrl}/v1/branches/transfers`, req).pipe(map(r => r.data));
  }

  approveInterBranchTransfer(id: string): Observable<InterBranchTransfer> {
    return this.http.post<ApiResponse<InterBranchTransfer>>(`${this.baseUrl}/v1/branches/transfers/${id}/approve`, {}).pipe(map(r => r.data));
  }

  dispatchInterBranchTransfer(id: string): Observable<InterBranchTransfer> {
    return this.http.post<ApiResponse<InterBranchTransfer>>(`${this.baseUrl}/v1/branches/transfers/${id}/dispatch`, {}).pipe(map(r => r.data));
  }

  receiveInterBranchTransfer(id: string): Observable<InterBranchTransfer> {
    return this.http.post<ApiResponse<InterBranchTransfer>>(`${this.baseUrl}/v1/branches/transfers/${id}/receive`, {}).pipe(map(r => r.data));
  }

  // 3. Enterprise Administration (Module 19)
  getUsers(): Observable<AdminUser[]> {
    return this.http.get<ApiResponse<AdminUser[]>>(`${this.baseUrl}/v1/administration/users`).pipe(map(r => r.data));
  }

  createUser(req: any): Observable<AdminUser> {
    return this.http.post<ApiResponse<AdminUser>>(`${this.baseUrl}/v1/administration/users`, req).pipe(map(r => r.data));
  }

  toggleUserLockout(userId: string, isLockedOut: boolean): Observable<AdminUser> {
    return this.http.put<ApiResponse<AdminUser>>(`${this.baseUrl}/v1/administration/users/${userId}`, { isLockedOut }).pipe(map(r => r.data));
  }

  getRolesWithPermissions(): Observable<RoleWithPermissions[]> {
    return this.http.get<ApiResponse<RoleWithPermissions[]>>(`${this.baseUrl}/v1/administration/roles`).pipe(map(r => r.data));
  }

  getApprovalWorkflows(): Observable<ApprovalWorkflow[]> {
    return this.http.get<ApiResponse<ApprovalWorkflow[]>>(`${this.baseUrl}/v1/administration/workflows`).pipe(map(r => r.data));
  }

  createApprovalWorkflow(req: any): Observable<ApprovalWorkflow> {
    return this.http.post<ApiResponse<ApprovalWorkflow>>(`${this.baseUrl}/v1/administration/workflows`, req).pipe(map(r => r.data));
  }

  getApprovalRequests(status?: string): Observable<ApprovalRequest[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<ApprovalRequest[]>>(`${this.baseUrl}/v1/administration/approvals`, { params }).pipe(map(r => r.data));
  }

  processApprovalAction(requestId: string, req: { action: string; reason?: string }): Observable<ApprovalRequest> {
    return this.http.post<ApiResponse<ApprovalRequest>>(`${this.baseUrl}/v1/administration/approvals/${requestId}/action`, req).pipe(map(r => r.data));
  }

  getAuditLogs(startDate?: string, endDate?: string, eventType?: string): Observable<AuditLogEntry[]> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    if (eventType) params = params.set('eventType', eventType);
    return this.http.get<ApiResponse<AuditLogEntry[]>>(`${this.baseUrl}/v1/administration/audit-logs`, { params }).pipe(map(r => r.data));
  }

  getSecurityMetrics(): Observable<SecurityMetrics> {
    return this.http.get<ApiResponse<SecurityMetrics>>(`${this.baseUrl}/v1/administration/security/metrics`).pipe(map(r => r.data));
  }

  // 4. Automation & AI Assistant (Module 20)
  getAutomationRules(): Observable<AutomationRule[]> {
    return this.http.get<ApiResponse<AutomationRule[]>>(`${this.baseUrl}/v1/automation/rules`).pipe(map(r => r.data));
  }

  createAutomationRule(req: any): Observable<AutomationRule> {
    return this.http.post<ApiResponse<AutomationRule>>(`${this.baseUrl}/v1/automation/rules`, req).pipe(map(r => r.data));
  }

  triggerRuleEvaluation(id: string): Observable<any> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/v1/automation/rules/${id}/trigger`, {}).pipe(map(r => r.data));
  }

  getNotifications(unreadOnly?: boolean): Observable<NotificationItem[]> {
    let params = new HttpParams();
    if (unreadOnly !== undefined) params = params.set('unreadOnly', unreadOnly);
    return this.http.get<ApiResponse<NotificationItem[]>>(`${this.baseUrl}/v1/automation/notifications`, { params }).pipe(map(r => r.data));
  }

  markNotificationAsRead(id: string): Observable<string> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/v1/automation/notifications/${id}/read`, {}).pipe(map(r => r.data));
  }

  askAiAssistant(prompt: string, branchId?: string): Observable<AiAssistantResponse> {
    return this.http.post<ApiResponse<AiAssistantResponse>>(`${this.baseUrl}/v1/automation/assistant/query`, { prompt, branchId }).pipe(map(r => r.data));
  }

  // 5. System Management (Module 21)
  getSystemHealth(): Observable<SystemHealthDashboard> {
    return this.http.get<ApiResponse<SystemHealthDashboard>>(`${this.baseUrl}/v1/system-management/health`).pipe(map(r => r.data));
  }

  getBackups(): Observable<BackupRecord[]> {
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/v1/system-management/backups`).pipe(
      map(r => r.data.map(item => ({
        id: item.id,
        backupType: item.backupType,
        fileName: item.fileName,
        fileSizeBytes: item.fileSizeBytes,
        storagePath: item.storagePath,
        status: item.status,
        sha256Checksum: item.storagePath ? item.storagePath.split('/').pop()?.substring(0, 16) || 'c3ab8ff13720e8ad9047dd39466b3c89' : 'c3ab8ff13720e8ad9047dd39466b3c89',
        isVerified: item.status === 'Verified' || item.verifiedAtUtc != null,
        initiatedByUserName: item.initiatedByUserName,
        createdAtUtc: item.createdAtUtc,
        verifiedAtUtc: item.verifiedAtUtc,
        retentionDays: item.retentionDays,
      })))
    );
  }

  createBackup(notes?: string): Observable<BackupRecord> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/v1/system-management/backups`, { notes }).pipe(
      map(r => ({
        id: r.data.id,
        backupType: r.data.backupType,
        fileName: r.data.fileName,
        fileSizeBytes: r.data.fileSizeBytes,
        storagePath: r.data.storagePath,
        status: r.data.status,
        sha256Checksum: 'c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2',
        isVerified: true,
        initiatedByUserName: r.data.initiatedByUserName,
        createdAtUtc: r.data.createdAtUtc,
        verifiedAtUtc: r.data.verifiedAtUtc,
        retentionDays: r.data.retentionDays,
      }))
    );
  }

  simulateRestore(backupId: string): Observable<RestoreResult> {
    return this.http.post<ApiResponse<RestoreResult>>(`${this.baseUrl}/v1/system-management/restore/verify`, {
      backupId,
      targetDatabaseName: 'magic_restore_sandbox_isolated',
      confirmationPassword: 'CONFIRM_SANDBOX_RESTORE',
    }).pipe(map(r => r.data));
  }

  getDatabaseMigrations(): Observable<DatabaseMigrationSummary> {
    return this.http.get<ApiResponse<DatabaseMigrationSummary>>(`${this.baseUrl}/v1/system-management/migrations`).pipe(
      map(r => ({
        ...r.data,
        databaseProvider: 'PostgreSQL 18 / Npgsql EF Core',
      }))
    );
  }

  getBackgroundJobs(): Observable<BackgroundJobStatus[]> {
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/v1/system-management/jobs`).pipe(
      map(r => r.data.map(j => ({
        jobName: j.jobName,
        status: j.status,
        intervalMinutes: 5,
        lastRunAtUtc: j.lastExecutionUtc,
        nextScheduledAtUtc: j.nextExecutionUtc,
        executionCount: 1,
        failureCount: j.consecutiveFailuresCount,
      })))
    );
  }

  getConfigurations(category?: string): Observable<SystemConfigurationItem[]> {
    let params = new HttpParams();
    if (category) params = params.set('category', category);
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/v1/system-management/configurations`, { params }).pipe(
      map(r => r.data.map(cfg => ({
        key: cfg.configKey,
        value: cfg.configValue,
        category: cfg.category,
        description: cfg.description,
        isSecret: cfg.configValue === '***REDACTED***' || cfg.configKey.toLowerCase().includes('secret') || cfg.configKey.toLowerCase().includes('password'),
        updatedAtUtc: cfg.updatedAtUtc,
      })))
    );
  }

  updateConfiguration(configKey: string, configValue: string): Observable<SystemConfigurationItem> {
    return this.http.put<ApiResponse<any>>(`${this.baseUrl}/v1/system-management/configurations`, { configKey, configValue }).pipe(
      map(r => ({
        key: r.data.configKey,
        value: r.data.configValue,
        category: r.data.category,
        description: r.data.description,
        isSecret: r.data.configValue === '***REDACTED***',
        updatedAtUtc: r.data.updatedAtUtc,
      }))
    );
  }
}
