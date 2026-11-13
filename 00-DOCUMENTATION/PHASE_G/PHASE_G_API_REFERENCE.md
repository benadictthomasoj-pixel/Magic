# MAGIC ERP — PHASE G: REST API REFERENCE
## Modules 17–21 Endpoint Documentation

All endpoints require JWT Bearer authorization (`Authorization: Bearer <token>`). Responses adhere to the standard `ApiResponse<T>` envelope.

---

### 1. Module 17: Reporting & Business Intelligence (`/api/v1/reports`)

- `GET /api/v1/reports/dashboard?branchId={guid}`
  - Returns `ExecutiveDashboardDto` with unified cross-module KPIs and provisional accounting status.
- `GET /api/v1/reports/sales?startDate={date}&endDate={date}&branchId={guid}&groupByType={Daily|Customer|Category}`
  - Returns `ReportResultDto` with aggregated sales metrics.
- `GET /api/v1/reports/purchases?startDate={date}&endDate={date}&supplierId={guid}`
  - Returns `ReportResultDto` with inbound procurement and supplier metrics.
- `GET /api/v1/reports/inventory?warehouseId={guid}&lowStockOnly={bool}`
  - Returns `ReportResultDto` with physical stock balances and reorder alerts.
- `GET /api/v1/reports/financial?startDate={date}&endDate={date}`
  - Returns `ReportResultDto` with General Ledger trial balances and AR/AP totals.
- `GET /api/v1/reports/hr-payroll?month={int}&year={int}`
  - Returns `ReportResultDto` with payroll register and salary disbursements.
- `GET /api/v1/reports/operations?startDate={date}&endDate={date}`
  - Returns `ReportResultDto` with service ticket counts and POS shifts.
- `POST /api/v1/reports/export`
  - Body: `ReportExportRequest` (`reportType`, `format`, `startDate`, `endDate`).
  - Returns: Binary file download stream (`text/csv` or `application/pdf`).

---

### 2. Module 18: Multi-Branch & POS Terminals (`/api/v1/branches`)

- `GET /api/v1/branches/company/profile` -> `CompanyProfileDto`
- `PUT /api/v1/branches/company/profile` -> `UpdateCompanyProfileRequest` -> `CompanyProfileDto`
- `GET /api/v1/branches` -> `IReadOnlyList<BranchDto>`
- `POST /api/v1/branches` -> `CreateBranchRequest` -> `BranchDto`
- `PUT /api/v1/branches/{id}` -> `UpdateBranchRequest` -> `BranchDto`
- `GET /api/v1/branches/terminals?branchId={guid}` -> `IReadOnlyList<TerminalDetailsDto>`
- `POST /api/v1/branches/terminals` -> `RegisterTerminalRequest` -> `TerminalDetailsDto`
- `PUT /api/v1/branches/terminals/{id}/status` -> `UpdateTerminalStatusRequest` -> `TerminalDetailsDto`
- `GET /api/v1/branches/transfers` -> `IReadOnlyList<InterBranchTransferRequestDto>`
- `POST /api/v1/branches/transfers` -> `CreateInterBranchTransferRequest` -> `InterBranchTransferRequestDto`
- `POST /api/v1/branches/transfers/{id}/approve` -> `InterBranchTransferRequestDto`
- `POST /api/v1/branches/transfers/{id}/dispatch` -> `InterBranchTransferRequestDto`
- `POST /api/v1/branches/transfers/{id}/receive` -> `InterBranchTransferRequestDto`

---

### 3. Module 19: Enterprise Administration (`/api/v1/administration`)

- `GET /api/v1/administration/users` -> `IReadOnlyList<AdminUserDto>`
- `POST /api/v1/administration/users` -> `CreateAdminUserRequest` -> `AdminUserDto`
- `PUT /api/v1/administration/users/{id}` -> `UpdateAdminUserRequest` -> `AdminUserDto`
- `GET /api/v1/administration/roles` -> `IReadOnlyList<RoleWithPermissionsDto>`
- `PUT /api/v1/administration/roles/{id}/permissions` -> `UpdateRolePermissionsRequest` -> `RoleWithPermissionsDto`
- `GET /api/v1/administration/workflows` -> `IReadOnlyList<ApprovalWorkflowDto>`
- `POST /api/v1/administration/workflows` -> `CreateApprovalWorkflowRequest` -> `ApprovalWorkflowDto`
- `GET /api/v1/administration/approvals?status={string}` -> `IReadOnlyList<ApprovalRequestDto>`
- `POST /api/v1/administration/approvals/{id}/action` -> `SubmitApprovalActionRequest` -> `ApprovalRequestDto`
- `GET /api/v1/administration/audit-logs?startDate={date}&endDate={date}&eventType={string}` -> `IReadOnlyList<AuditLogEntryDto>`
- `GET /api/v1/administration/security/metrics` -> `SecurityMetricsDto`

---

### 4. Module 20: Automation & AI Assistant (`/api/v1/automation`)

- `GET /api/v1/automation/rules` -> `IReadOnlyList<AutomationRuleDto>`
- `POST /api/v1/automation/rules` -> `CreateAutomationRuleRequest` -> `AutomationRuleDto`
- `POST /api/v1/automation/rules/{id}/trigger` -> `AutomationExecutionDto`
- `GET /api/v1/automation/notifications?unreadOnly={bool}` -> `IReadOnlyList<NotificationDto>`
- `POST /api/v1/automation/notifications/{id}/read` -> `string`
- `POST /api/v1/automation/assistant/query` -> `AiAssistantQueryRequest` -> `AiAssistantResponseDto`

---

### 5. Module 21: System Management (`/api/v1/system-management`)

- `GET /api/v1/system-management/health` -> `SystemHealthDashboardDto`
- `GET /api/v1/system-management/backups` -> `IReadOnlyList<BackupRecordDto>`
- `POST /api/v1/system-management/backups` -> `CreateBackupRequest` -> `BackupRecordDto`
- `POST /api/v1/system-management/restore/verify` -> `RestoreDatabaseRequest` -> `RestoreResultDto`
- `GET /api/v1/system-management/migrations` -> `DatabaseMigrationSummaryDto`
- `GET /api/v1/system-management/jobs` -> `IReadOnlyList<BackgroundJobStatusDto>`
- `GET /api/v1/system-management/configurations?category={string}` -> `IReadOnlyList<SystemConfigurationItemDto>`
- `PUT /api/v1/system-management/configurations` -> `UpdateSystemConfigurationRequest` -> `SystemConfigurationItemDto`
