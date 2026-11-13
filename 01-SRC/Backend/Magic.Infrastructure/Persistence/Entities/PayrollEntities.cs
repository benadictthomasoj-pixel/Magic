using Magic.Domain.Accounting.Entities;

namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Salary structure defining component rules and allowances.
/// </summary>
public sealed class SalaryStructureRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<SalaryStructureComponentRecord> Components { get; set; } = new List<SalaryStructureComponentRecord>();
}

/// <summary>
/// Component of a salary structure (Basic, HRA, Allowance, PF, ESI, PT, TDS).
/// </summary>
public sealed class SalaryStructureComponentRecord
{
    public Guid Id { get; set; }
    public Guid SalaryStructureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ComponentType { get; set; } = "Basic"; // Basic, HRA, Allowance, Bonus, PF_Employee, ESI_Employee, PT, TDS, PF_Employer, ESI_Employer
    public string CalculationType { get; set; } = "Fixed"; // Fixed, PercentageOfBasic
    public decimal DefaultValue { get; set; }
    public bool IsDeduction { get; set; }
    public bool IsEmployerContribution { get; set; }

    public SalaryStructureRecord? SalaryStructure { get; set; }
}

/// <summary>
/// Links an employee to a specific salary structure and gross compensation package.
/// </summary>
public sealed class EmployeeSalaryAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid SalaryStructureId { get; set; }
    public long MonthlyGrossPaise { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public bool IsActive { get; set; } = true;

    public EmployeeRecord? Employee { get; set; }
    public SalaryStructureRecord? SalaryStructure { get; set; }
}

/// <summary>
/// Monthly payroll cycle with multi-stage approval, GL accrual, and bank disbursement.
/// </summary>
public sealed class PayrollPeriodRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid FiscalYearId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Calculated"; // Calculated, Approved, Finalized, Paid, Cancelled
    public long TotalGrossPaise { get; set; }
    public long TotalDeductionsPaise { get; set; }
    public long TotalNetPaise { get; set; }
    public long TotalEmployerContributionsPaise { get; set; }
    public Guid? JournalVoucherId { get; set; }
    public Guid? DisbursementVoucherId { get; set; }
    public Guid ProcessedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public FiscalYear? FiscalYear { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }
    public JournalVoucher? DisbursementVoucher { get; set; }
    public UserRecord? ProcessedByUser { get; set; }
    public UserRecord? ApprovedByUser { get; set; }
    public ICollection<PayrollItemRecord> Items { get; set; } = new List<PayrollItemRecord>();
}

/// <summary>
/// Individual employee payslip calculation within a payroll cycle.
/// </summary>
public sealed class PayrollItemRecord
{
    public Guid Id { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal PresentDays { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public long BasicPaise { get; set; }
    public long HraPaise { get; set; }
    public long AllowancesPaise { get; set; }
    public long GrossSalaryPaise { get; set; }
    public long PfDeductionPaise { get; set; }
    public long EsiDeductionPaise { get; set; }
    public long PtDeductionPaise { get; set; }
    public long TdsDeductionPaise { get; set; }
    public long TotalDeductionsPaise { get; set; }
    public long NetSalaryPaise { get; set; }
    public long EmployerPfPaise { get; set; }
    public long EmployerEsiPaise { get; set; }
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid
    public string? PaymentReference { get; set; }

    public PayrollPeriodRecord? PayrollPeriod { get; set; }
    public EmployeeRecord? Employee { get; set; }
}

