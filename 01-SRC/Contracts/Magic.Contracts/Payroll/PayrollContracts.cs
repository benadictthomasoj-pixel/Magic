namespace Magic.Contracts.Payroll;

public sealed record SalaryStructureComponentDto(
    Guid Id,
    Guid SalaryStructureId,
    string Name,
    string ComponentType,
    string CalculationType,
    decimal DefaultValue,
    bool IsDeduction,
    bool IsEmployerContribution);

public sealed record SalaryStructureDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<SalaryStructureComponentDto> Components);

public sealed record CreateSalaryStructureComponentRequest(
    string Name,
    string ComponentType,
    string CalculationType,
    decimal DefaultValue,
    bool IsDeduction,
    bool IsEmployerContribution);

public sealed record CreateSalaryStructureRequest(
    string Name,
    string? Description,
    IReadOnlyList<CreateSalaryStructureComponentRequest> Components);

public sealed record EmployeeSalaryAssignmentDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid SalaryStructureId,
    string SalaryStructureName,
    long MonthlyGrossPaise,
    DateOnly EffectiveFrom,
    bool IsActive);

public sealed record AssignEmployeeSalaryRequest(
    Guid EmployeeId,
    Guid SalaryStructureId,
    long MonthlyGrossPaise,
    DateOnly EffectiveFrom);

public sealed record PayrollItemDto(
    Guid Id,
    Guid PayrollPeriodId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? DesignationTitle,
    string? DepartmentName,
    decimal PresentDays,
    decimal PaidLeaveDays,
    decimal UnpaidLeaveDays,
    long BasicPaise,
    long HraPaise,
    long AllowancesPaise,
    long GrossSalaryPaise,
    long PfDeductionPaise,
    long EsiDeductionPaise,
    long PtDeductionPaise,
    long TdsDeductionPaise,
    long TotalDeductionsPaise,
    long NetSalaryPaise,
    long EmployerPfPaise,
    long EmployerEsiPaise,
    string PaymentStatus,
    string? PaymentReference);

public sealed record PayrollPeriodDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    Guid FiscalYearId,
    string PeriodName,
    int Month,
    int Year,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    long TotalGrossPaise,
    long TotalDeductionsPaise,
    long TotalNetPaise,
    long TotalEmployerContributionsPaise,
    Guid? JournalVoucherId,
    Guid? DisbursementVoucherId,
    Guid ProcessedByUserId,
    Guid? ApprovedByUserId,
    DateTimeOffset? FinalizedAtUtc,
    int TotalEmployeesCount,
    IReadOnlyList<PayrollItemDto>? Items);

public sealed record CalculatePayrollRequest(
    int Month,
    int Year,
    Guid? FiscalYearId);

public sealed record DisbursePayrollRequest(
    Guid BankAccountId,
    DateOnly DisbursementDate,
    string PaymentReference);

public sealed record PayrollSummaryDto(
    int TotalPeriodsCount,
    long LastMonthNetPayoutPaise,
    int LastMonthEmployeeCount,
    int PendingApprovalPeriodsCount);

