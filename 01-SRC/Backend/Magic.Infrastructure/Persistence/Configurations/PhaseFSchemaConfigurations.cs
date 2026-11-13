using Magic.Domain.Accounting.Entities;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magic.Infrastructure.Persistence.Configurations;

// ==========================================
// 1. EXPENSES SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategoryRecord>
{
    public void Configure(EntityTypeBuilder<ExpenseCategoryRecord> builder)
    {
        builder.ToTable("expense_categories", "expenses");
        builder.HasKey(x => x.Id).HasName("pk_expense_categories");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.DefaultGlAccountId).HasColumnName("default_gl_account_id").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("ix_expense_categories_company_code");
        builder.HasOne(x => x.DefaultGlAccount).WithMany().HasForeignKey(x => x.DefaultGlAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<ExpenseRecord>
{
    public void Configure(EntityTypeBuilder<ExpenseRecord> builder)
    {
        builder.ToTable("expenses", "expenses", t =>
        {
            t.HasCheckConstraint("chk_expense_amounts", "amount_paise >= 0 AND tax_amount_paise >= 0 AND total_amount_paise = amount_paise + tax_amount_paise");
        });
        builder.HasKey(x => x.Id).HasName("pk_expenses");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.ExpenseNumber).HasColumnName("expense_number").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ExpenseCategoryId).HasColumnName("expense_category_id").IsRequired();
        builder.Property(x => x.GlAccountId).HasColumnName("gl_account_id").IsRequired();
        builder.Property(x => x.VendorId).HasColumnName("vendor_id");
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(20).IsRequired();
        builder.Property(x => x.BankCashAccountId).HasColumnName("bank_cash_account_id");
        builder.Property(x => x.ExpenseDate).HasColumnName("expense_date").IsRequired();
        builder.Property(x => x.AmountPaise).HasColumnName("amount_paise").IsRequired();
        builder.Property(x => x.TaxAmountPaise).HasColumnName("tax_amount_paise").HasDefaultValue(0);
        builder.Property(x => x.TotalAmountPaise).HasColumnName("total_amount_paise").IsRequired();
        builder.Property(x => x.CostCenter).HasColumnName("cost_center").HasMaxLength(50);
        builder.Property(x => x.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100);
        builder.Property(x => x.Description).HasColumnName("description").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Submitted").IsRequired();
        builder.Property(x => x.ApprovedByUserId).HasColumnName("approved_by_user_id");
        builder.Property(x => x.ApprovedAtUtc).HasColumnName("approved_at_utc");
        builder.Property(x => x.RejectionReason).HasColumnName("rejection_reason");
        builder.Property(x => x.JournalVoucherId).HasColumnName("journal_voucher_id");
        builder.Property(x => x.ReversalVoucherId).HasColumnName("reversal_voucher_id");
        builder.Property(x => x.AttachmentPath).HasColumnName("attachment_path");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.ExpenseNumber }).IsUnique().HasDatabaseName("ix_expenses_company_number");
        builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.ExpenseDate }).HasDatabaseName("ix_expenses_company_branch_date");
        builder.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.GlAccount).WithMany().HasForeignKey(x => x.GlAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BankCashAccount).WithMany().HasForeignKey(x => x.BankCashAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.JournalVoucher).WithMany().HasForeignKey(x => x.JournalVoucherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReversalVoucher).WithMany().HasForeignKey(x => x.ReversalVoucherId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PettyCashAccountConfiguration : IEntityTypeConfiguration<PettyCashAccountRecord>
{
    public void Configure(EntityTypeBuilder<PettyCashAccountRecord> builder)
    {
        builder.ToTable("petty_cash_accounts", "expenses");
        builder.HasKey(x => x.Id).HasName("pk_petty_cash_accounts");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.AccountName).HasColumnName("account_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.GlAccountId).HasColumnName("gl_account_id").IsRequired();
        builder.Property(x => x.CustodianUserId).HasColumnName("custodian_user_id").IsRequired();
        builder.Property(x => x.FloatLimitPaise).HasColumnName("float_limit_paise").IsRequired();
        builder.Property(x => x.CurrentBalancePaise).HasColumnName("current_balance_paise").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.AccountName }).IsUnique().HasDatabaseName("ix_petty_cash_company_branch_name");
        builder.HasOne(x => x.GlAccount).WithMany().HasForeignKey(x => x.GlAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CustodianUser).WithMany().HasForeignKey(x => x.CustodianUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PettyCashTransactionConfiguration : IEntityTypeConfiguration<PettyCashTransactionRecord>
{
    public void Configure(EntityTypeBuilder<PettyCashTransactionRecord> builder)
    {
        builder.ToTable("petty_cash_transactions", "expenses");
        builder.HasKey(x => x.Id).HasName("pk_petty_cash_transactions");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.PettyCashAccountId).HasColumnName("petty_cash_account_id").IsRequired();
        builder.Property(x => x.TransactionType).HasColumnName("transaction_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.AmountPaise).HasColumnName("amount_paise").IsRequired();
        builder.Property(x => x.BalanceAfterPaise).HasColumnName("balance_after_paise").IsRequired();
        builder.Property(x => x.ExpenseId).HasColumnName("expense_id");
        builder.Property(x => x.JournalVoucherId).HasColumnName("journal_voucher_id");
        builder.Property(x => x.PerformedByUserId).HasColumnName("performed_by_user_id").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes");
        builder.Property(x => x.TransactionDateUtc).HasColumnName("transaction_date_utc").IsRequired();

        builder.HasOne(x => x.PettyCashAccount).WithMany().HasForeignKey(x => x.PettyCashAccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Expense).WithMany().HasForeignKey(x => x.ExpenseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.JournalVoucher).WithMany().HasForeignKey(x => x.JournalVoucherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PerformedByUser).WithMany().HasForeignKey(x => x.PerformedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 2. HR SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class DesignationConfiguration : IEntityTypeConfiguration<DesignationRecord>
{
    public void Configure(EntityTypeBuilder<DesignationRecord> builder)
    {
        builder.ToTable("designations", "hr");
        builder.HasKey(x => x.Id).HasName("pk_designations");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(100).IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("ix_designations_company_code");
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<EmployeeRecord>
{
    public void Configure(EntityTypeBuilder<EmployeeRecord> builder)
    {
        builder.ToTable("employees", "hr");
        builder.HasKey(x => x.Id).HasName("pk_employees");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.EmployeeCode).HasColumnName("employee_code").HasMaxLength(30).IsRequired();
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(60).IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(60).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Address).HasColumnName("address");
        builder.Property(x => x.JoiningDate).HasColumnName("joining_date").IsRequired();
        builder.Property(x => x.EmploymentType).HasColumnName("employment_type").HasMaxLength(30).HasDefaultValue("FullTime").IsRequired();
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
        builder.Property(x => x.DesignationId).HasColumnName("designation_id");
        builder.Property(x => x.ReportingManagerId).HasColumnName("reporting_manager_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Active").IsRequired();
        builder.Property(x => x.EmergencyContactName).HasColumnName("emergency_contact_name").HasMaxLength(100);
        builder.Property(x => x.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(30);
        builder.Property(x => x.BankAccountNumber).HasColumnName("bank_account_number").HasMaxLength(50);
        builder.Property(x => x.BankIfsc).HasColumnName("bank_ifsc").HasMaxLength(20);
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(100);
        builder.Property(x => x.PanNumber).HasColumnName("pan_number").HasMaxLength(20);
        builder.Property(x => x.LinkedUserId).HasColumnName("linked_user_id");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeCode }).IsUnique().HasDatabaseName("ix_employees_company_code");
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Designation).WithMany().HasForeignKey(x => x.DesignationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReportingManager).WithMany().HasForeignKey(x => x.ReportingManagerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LinkedUser).WithMany().HasForeignKey(x => x.LinkedUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AttendanceConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance", "hr");
        builder.HasKey(x => x.Id).HasName("pk_attendance");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.AttendanceDate).HasColumnName("attendance_date").IsRequired();
        builder.Property(x => x.CheckInTimeUtc).HasColumnName("check_in_time_utc");
        builder.Property(x => x.CheckOutTimeUtc).HasColumnName("check_out_time_utc");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Present").IsRequired();
        builder.Property(x => x.WorkDurationMinutes).HasColumnName("work_duration_minutes").HasDefaultValue(0);
        builder.Property(x => x.IsCorrected).HasColumnName("is_corrected").HasDefaultValue(false);
        builder.Property(x => x.CorrectionNotes).HasColumnName("correction_notes");
        builder.Property(x => x.CorrectedByUserId).HasColumnName("corrected_by_user_id");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate }).IsUnique().HasDatabaseName("ix_attendance_employee_date");
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.CorrectedByUser).WithMany().HasForeignKey(x => x.CorrectedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveTypeRecord>
{
    public void Configure(EntityTypeBuilder<LeaveTypeRecord> builder)
    {
        builder.ToTable("leave_types", "hr");
        builder.HasKey(x => x.Id).HasName("pk_leave_types");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(60).IsRequired();
        builder.Property(x => x.AnnualDays).HasColumnName("annual_days").IsRequired();
        builder.Property(x => x.IsPaid).HasColumnName("is_paid").HasDefaultValue(true);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("ix_leave_types_company_code");
    }
}

internal sealed class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalanceRecord>
{
    public void Configure(EntityTypeBuilder<LeaveBalanceRecord> builder)
    {
        builder.ToTable("leave_balances", "hr");
        builder.HasKey(x => x.Id).HasName("pk_leave_balances");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(x => x.LeaveTypeId).HasColumnName("leave_type_id").IsRequired();
        builder.Property(x => x.FiscalYearId).HasColumnName("fiscal_year_id").IsRequired();
        builder.Property(x => x.TotalDays).HasColumnName("total_days").HasPrecision(5, 1).IsRequired();
        builder.Property(x => x.ConsumedDays).HasColumnName("consumed_days").HasPrecision(5, 1).HasDefaultValue(0);
        builder.Property(x => x.PendingDays).HasColumnName("pending_days").HasPrecision(5, 1).HasDefaultValue(0);

        builder.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.FiscalYearId }).IsUnique().HasDatabaseName("ix_leave_balances_employee_type_year");
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LeaveApplicationConfiguration : IEntityTypeConfiguration<LeaveApplicationRecord>
{
    public void Configure(EntityTypeBuilder<LeaveApplicationRecord> builder)
    {
        builder.ToTable("leave_applications", "hr");
        builder.HasKey(x => x.Id).HasName("pk_leave_applications");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ApplicationNumber).HasColumnName("application_number").HasMaxLength(40).IsRequired();
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(x => x.LeaveTypeId).HasColumnName("leave_type_id").IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(x => x.TotalDays).HasColumnName("total_days").HasPrecision(5, 1).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Applied").IsRequired();
        builder.Property(x => x.DecisionByUserId).HasColumnName("decision_by_user_id");
        builder.Property(x => x.DecisionAtUtc).HasColumnName("decision_at_utc");
        builder.Property(x => x.RejectionReason).HasColumnName("rejection_reason");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => x.ApplicationNumber).IsUnique().HasDatabaseName("ix_leave_applications_number");
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DecisionByUser).WithMany().HasForeignKey(x => x.DecisionByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 3. PAYROLL SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class SalaryStructureConfiguration : IEntityTypeConfiguration<SalaryStructureRecord>
{
    public void Configure(EntityTypeBuilder<SalaryStructureRecord> builder)
    {
        builder.ToTable("salary_structures", "payroll");
        builder.HasKey(x => x.Id).HasName("pk_salary_structures");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
    }
}

internal sealed class SalaryStructureComponentConfiguration : IEntityTypeConfiguration<SalaryStructureComponentRecord>
{
    public void Configure(EntityTypeBuilder<SalaryStructureComponentRecord> builder)
    {
        builder.ToTable("salary_structure_components", "payroll");
        builder.HasKey(x => x.Id).HasName("pk_salary_structure_components");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SalaryStructureId).HasColumnName("salary_structure_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(60).IsRequired();
        builder.Property(x => x.ComponentType).HasColumnName("component_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CalculationType).HasColumnName("calculation_type").HasMaxLength(20).HasDefaultValue("Fixed").IsRequired();
        builder.Property(x => x.DefaultValue).HasColumnName("default_value").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.IsDeduction).HasColumnName("is_deduction").IsRequired();
        builder.Property(x => x.IsEmployerContribution).HasColumnName("is_employer_contribution").IsRequired();

        builder.HasOne(x => x.SalaryStructure).WithMany(s => s.Components).HasForeignKey(x => x.SalaryStructureId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class EmployeeSalaryAssignmentConfiguration : IEntityTypeConfiguration<EmployeeSalaryAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryAssignmentRecord> builder)
    {
        builder.ToTable("employee_salary_assignments", "payroll");
        builder.HasKey(x => x.Id).HasName("pk_employee_salary_assignments");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(x => x.SalaryStructureId).HasColumnName("salary_structure_id").IsRequired();
        builder.Property(x => x.MonthlyGrossPaise).HasColumnName("monthly_gross_paise").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SalaryStructure).WithMany().HasForeignKey(x => x.SalaryStructureId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriodRecord>
{
    public void Configure(EntityTypeBuilder<PayrollPeriodRecord> builder)
    {
        builder.ToTable("payroll_periods", "payroll", t =>
        {
            t.HasCheckConstraint("chk_payroll_net_calc", "total_net_paise = total_gross_paise - total_deductions_paise");
        });
        builder.HasKey(x => x.Id).HasName("pk_payroll_periods");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.FiscalYearId).HasColumnName("fiscal_year_id").IsRequired();
        builder.Property(x => x.PeriodName).HasColumnName("period_name").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Month).HasColumnName("month").IsRequired();
        builder.Property(x => x.Year).HasColumnName("year").IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Calculated").IsRequired();
        builder.Property(x => x.TotalGrossPaise).HasColumnName("total_gross_paise").HasDefaultValue(0);
        builder.Property(x => x.TotalDeductionsPaise).HasColumnName("total_deductions_paise").HasDefaultValue(0);
        builder.Property(x => x.TotalNetPaise).HasColumnName("total_net_paise").HasDefaultValue(0);
        builder.Property(x => x.TotalEmployerContributionsPaise).HasColumnName("total_employer_contributions_paise").HasDefaultValue(0);
        builder.Property(x => x.JournalVoucherId).HasColumnName("journal_voucher_id");
        builder.Property(x => x.DisbursementVoucherId).HasColumnName("disbursement_voucher_id");
        builder.Property(x => x.ProcessedByUserId).HasColumnName("processed_by_user_id").IsRequired();
        builder.Property(x => x.ApprovedByUserId).HasColumnName("approved_by_user_id");
        builder.Property(x => x.FinalizedAtUtc).HasColumnName("finalized_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Year, x.Month }).IsUnique().HasDatabaseName("ix_payroll_periods_company_branch_month_year");
        builder.HasOne(x => x.FiscalYear).WithMany().HasForeignKey(x => x.FiscalYearId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.JournalVoucher).WithMany().HasForeignKey(x => x.JournalVoucherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DisbursementVoucher).WithMany().HasForeignKey(x => x.DisbursementVoucherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProcessedByUser).WithMany().HasForeignKey(x => x.ProcessedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PayrollItemConfiguration : IEntityTypeConfiguration<PayrollItemRecord>
{
    public void Configure(EntityTypeBuilder<PayrollItemRecord> builder)
    {
        builder.ToTable("payroll_items", "payroll", t =>
        {
            t.HasCheckConstraint("chk_item_net_calc", "net_salary_paise = gross_salary_paise - total_deductions_paise");
        });
        builder.HasKey(x => x.Id).HasName("pk_payroll_items");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.PayrollPeriodId).HasColumnName("payroll_period_id").IsRequired();
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(x => x.PresentDays).HasColumnName("present_days").HasPrecision(5, 1).IsRequired();
        builder.Property(x => x.PaidLeaveDays).HasColumnName("paid_leave_days").HasPrecision(5, 1).IsRequired();
        builder.Property(x => x.UnpaidLeaveDays).HasColumnName("unpaid_leave_days").HasPrecision(5, 1).IsRequired();
        builder.Property(x => x.BasicPaise).HasColumnName("basic_paise").IsRequired();
        builder.Property(x => x.HraPaise).HasColumnName("hra_paise").IsRequired();
        builder.Property(x => x.AllowancesPaise).HasColumnName("allowances_paise").IsRequired();
        builder.Property(x => x.GrossSalaryPaise).HasColumnName("gross_salary_paise").IsRequired();
        builder.Property(x => x.PfDeductionPaise).HasColumnName("pf_deduction_paise").IsRequired();
        builder.Property(x => x.EsiDeductionPaise).HasColumnName("esi_deduction_paise").IsRequired();
        builder.Property(x => x.PtDeductionPaise).HasColumnName("pt_deduction_paise").IsRequired();
        builder.Property(x => x.TdsDeductionPaise).HasColumnName("tds_deduction_paise").IsRequired();
        builder.Property(x => x.TotalDeductionsPaise).HasColumnName("total_deductions_paise").IsRequired();
        builder.Property(x => x.NetSalaryPaise).HasColumnName("net_salary_paise").IsRequired();
        builder.Property(x => x.EmployerPfPaise).HasColumnName("employer_pf_paise").IsRequired();
        builder.Property(x => x.EmployerEsiPaise).HasColumnName("employer_esi_paise").IsRequired();
        builder.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
        builder.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(100);

        builder.HasIndex(x => new { x.PayrollPeriodId, x.EmployeeId }).IsUnique().HasDatabaseName("ix_payroll_items_period_employee");
        builder.HasOne(x => x.PayrollPeriod).WithMany(p => p.Items).HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 4. MARKETING SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class CustomerSegmentConfiguration : IEntityTypeConfiguration<CustomerSegmentRecord>
{
    public void Configure(EntityTypeBuilder<CustomerSegmentRecord> builder)
    {
        builder.ToTable("customer_segments", "marketing");
        builder.HasKey(x => x.Id).HasName("pk_customer_segments");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SegmentType).HasColumnName("segment_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.MinLifetimeSpendPaise).HasColumnName("min_lifetime_spend_paise").HasDefaultValue(0);
        builder.Property(x => x.MinOrdersCount).HasColumnName("min_orders_count").HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
    }
}

internal sealed class CampaignConfiguration : IEntityTypeConfiguration<CampaignRecord>
{
    public void Configure(EntityTypeBuilder<CampaignRecord> builder)
    {
        builder.ToTable("campaigns", "marketing");
        builder.HasKey(x => x.Id).HasName("pk_campaigns");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.TargetSegmentId).HasColumnName("target_segment_id");
        builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(x => x.BudgetPaise).HasColumnName("budget_paise").IsRequired();
        builder.Property(x => x.ActualSpendPaise).HasColumnName("actual_spend_paise").HasDefaultValue(0);
        builder.Property(x => x.AttributedRevenuePaise).HasColumnName("attributed_revenue_paise").HasDefaultValue(0);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Draft").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasOne(x => x.TargetSegment).WithMany().HasForeignKey(x => x.TargetSegmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PromotionCouponConfiguration : IEntityTypeConfiguration<PromotionCouponRecord>
{
    public void Configure(EntityTypeBuilder<PromotionCouponRecord> builder)
    {
        builder.ToTable("promotion_coupons", "marketing");
        builder.HasKey(x => x.Id).HasName("pk_promotion_coupons");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CampaignId).HasColumnName("campaign_id");
        builder.Property(x => x.DiscountType).HasColumnName("discount_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.DiscountValue).HasColumnName("discount_value").IsRequired();
        builder.Property(x => x.MinOrderAmountPaise).HasColumnName("min_order_amount_paise").HasDefaultValue(0);
        builder.Property(x => x.MaxDiscountAmountPaise).HasColumnName("max_discount_amount_paise").HasDefaultValue(0);
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from").IsRequired();
        builder.Property(x => x.ValidTo).HasColumnName("valid_to").IsRequired();
        builder.Property(x => x.UsageLimitTotal).HasColumnName("usage_limit_total").HasDefaultValue(1000);
        builder.Property(x => x.UsageLimitPerCustomer).HasColumnName("usage_limit_per_customer").HasDefaultValue(1);
        builder.Property(x => x.CurrentUsageCount).HasColumnName("current_usage_count").HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("ix_promotion_coupons_company_code");
        builder.HasOne(x => x.Campaign).WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemptionRecord>
{
    public void Configure(EntityTypeBuilder<CouponRedemptionRecord> builder)
    {
        builder.ToTable("coupon_redemptions", "marketing");
        builder.HasKey(x => x.Id).HasName("pk_coupon_redemptions");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CouponId).HasColumnName("coupon_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.SalesInvoiceId).HasColumnName("sales_invoice_id").IsRequired();
        builder.Property(x => x.DiscountAmountPaise).HasColumnName("discount_amount_paise").IsRequired();
        builder.Property(x => x.RedeemedAtUtc).HasColumnName("redeemed_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CouponId, x.SalesInvoiceId }).IsUnique().HasDatabaseName("ix_coupon_redemptions_coupon_invoice");
        builder.HasOne(x => x.Coupon).WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LoyaltyProgramConfiguration : IEntityTypeConfiguration<LoyaltyProgramRecord>
{
    public void Configure(EntityTypeBuilder<LoyaltyProgramRecord> builder)
    {
        builder.ToTable("loyalty_programs", "marketing");
        builder.HasKey(x => x.Id).HasName("pk_loyalty_programs");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PointsPerHundredSpent).HasColumnName("points_per_hundred_spent").HasDefaultValue(1);
        builder.Property(x => x.CurrencyValuePerPointPaise).HasColumnName("currency_value_per_point_paise").HasDefaultValue(100);
        builder.Property(x => x.MinPointsToRedeem).HasColumnName("min_points_to_redeem").HasDefaultValue(100);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
    }
}

internal sealed class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransactionRecord>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransactionRecord> builder)
    {
        builder.ToTable("loyalty_transactions", "marketing");
        builder.HasKey(x => x.Id).HasName("pk_loyalty_transactions");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.TransactionType).HasColumnName("transaction_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Points).HasColumnName("points").IsRequired();
        builder.Property(x => x.BalanceAfterPoints).HasColumnName("balance_after_points").IsRequired();
        builder.Property(x => x.SalesInvoiceId).HasColumnName("sales_invoice_id");
        builder.Property(x => x.Notes).HasColumnName("notes");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 5. ORDERS SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrderRecord>
{
    public void Configure(EntityTypeBuilder<SalesOrderRecord> builder)
    {
        builder.ToTable("sales_orders", "orders", t =>
        {
            t.HasCheckConstraint("chk_order_total", "total_amount_paise = subtotal_paise + tax_amount_paise");
        });
        builder.HasKey(x => x.Id).HasName("pk_sales_orders");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(x => x.OrderNumber).HasColumnName("order_number").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.OrderDate).HasColumnName("order_date").IsRequired();
        builder.Property(x => x.ExpectedDeliveryDate).HasColumnName("expected_delivery_date");
        builder.Property(x => x.SubtotalPaise).HasColumnName("subtotal_paise").IsRequired();
        builder.Property(x => x.TaxAmountPaise).HasColumnName("tax_amount_paise").IsRequired();
        builder.Property(x => x.TotalAmountPaise).HasColumnName("total_amount_paise").IsRequired();
        builder.Property(x => x.DeliveryAddress).HasColumnName("delivery_address").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(25).HasDefaultValue("Submitted").IsRequired();
        builder.Property(x => x.ConfirmedByUserId).HasColumnName("confirmed_by_user_id");
        builder.Property(x => x.ConfirmedAtUtc).HasColumnName("confirmed_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.OrderNumber }).IsUnique().HasDatabaseName("ix_sales_orders_company_number");
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ConfirmedByUser).WithMany().HasForeignKey(x => x.ConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLineRecord>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineRecord> builder)
    {
        builder.ToTable("sales_order_lines", "orders");
        builder.HasKey(x => x.Id).HasName("pk_sales_order_lines");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SalesOrderId).HasColumnName("sales_order_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.VariantId).HasColumnName("variant_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(12, 3).IsRequired();
        builder.Property(x => x.FulfilledQuantity).HasColumnName("fulfilled_quantity").HasPrecision(12, 3).HasDefaultValue(0);
        builder.Property(x => x.InvoicedQuantity).HasColumnName("invoiced_quantity").HasPrecision(12, 3).HasDefaultValue(0);
        builder.Property(x => x.UnitPricePaise).HasColumnName("unit_price_paise").IsRequired();
        builder.Property(x => x.DiscountAmountPaise).HasColumnName("discount_amount_paise").HasDefaultValue(0);
        builder.Property(x => x.TaxRatePercent).HasColumnName("tax_rate_percent").HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.TaxAmountPaise).HasColumnName("tax_amount_paise").IsRequired();
        builder.Property(x => x.LineTotalPaise).HasColumnName("line_total_paise").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pending").IsRequired();

        builder.HasOne(x => x.SalesOrder).WithMany(o => o.Lines).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrderFulfillmentConfiguration : IEntityTypeConfiguration<OrderFulfillmentRecord>
{
    public void Configure(EntityTypeBuilder<OrderFulfillmentRecord> builder)
    {
        builder.ToTable("order_fulfillments", "orders");
        builder.HasKey(x => x.Id).HasName("pk_order_fulfillments");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SalesOrderId).HasColumnName("sales_order_id").IsRequired();
        builder.Property(x => x.FulfillmentNumber).HasColumnName("fulfillment_number").HasMaxLength(40).IsRequired();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(x => x.CarrierName).HasColumnName("carrier_name").HasMaxLength(100);
        builder.Property(x => x.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(100);
        builder.Property(x => x.DispatchedAtUtc).HasColumnName("dispatched_at_utc");
        builder.Property(x => x.DeliveredAtUtc).HasColumnName("delivered_at_utc");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Preparing").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => x.FulfillmentNumber).IsUnique().HasDatabaseName("ix_order_fulfillments_number");
        builder.HasOne(x => x.SalesOrder).WithMany(o => o.Fulfillments).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrderFulfillmentLineConfiguration : IEntityTypeConfiguration<OrderFulfillmentLineRecord>
{
    public void Configure(EntityTypeBuilder<OrderFulfillmentLineRecord> builder)
    {
        builder.ToTable("order_fulfillment_lines", "orders");
        builder.HasKey(x => x.Id).HasName("pk_order_fulfillment_lines");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.OrderFulfillmentId).HasColumnName("order_fulfillment_id").IsRequired();
        builder.Property(x => x.SalesOrderLineId).HasColumnName("sales_order_line_id").IsRequired();
        builder.Property(x => x.QuantityDispatched).HasColumnName("quantity_dispatched").HasPrecision(12, 3).IsRequired();

        builder.HasOne(x => x.OrderFulfillment).WithMany(f => f.Lines).HasForeignKey(x => x.OrderFulfillmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SalesOrderLine).WithMany().HasForeignKey(x => x.SalesOrderLineId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BackorderConfiguration : IEntityTypeConfiguration<BackorderRecord>
{
    public void Configure(EntityTypeBuilder<BackorderRecord> builder)
    {
        builder.ToTable("backorders", "orders");
        builder.HasKey(x => x.Id).HasName("pk_backorders");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SalesOrderId).HasColumnName("sales_order_id").IsRequired();
        builder.Property(x => x.SalesOrderLineId).HasColumnName("sales_order_line_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.PendingQuantity).HasColumnName("pending_quantity").HasPrecision(12, 3).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
        builder.Property(x => x.ExpectedRestockDate).HasColumnName("expected_restock_date");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasOne(x => x.SalesOrder).WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SalesOrderLine).WithMany().HasForeignKey(x => x.SalesOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 6. SERVICES SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class ServiceCatalogConfiguration : IEntityTypeConfiguration<ServiceCatalogRecord>
{
    public void Configure(EntityTypeBuilder<ServiceCatalogRecord> builder)
    {
        builder.ToTable("service_catalog", "services");
        builder.HasKey(x => x.Id).HasName("pk_service_catalog");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(60).HasDefaultValue("Repair").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.StandardLaborChargePaise).HasColumnName("standard_labor_charge_paise").IsRequired();
        builder.Property(x => x.EstimatedDurationMinutes).HasColumnName("estimated_duration_minutes").HasDefaultValue(60);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("ix_service_catalog_company_code");
    }
}

internal sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequestRecord>
{
    public void Configure(EntityTypeBuilder<ServiceRequestRecord> builder)
    {
        builder.ToTable("service_requests", "services");
        builder.HasKey(x => x.Id).HasName("pk_service_requests");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.RequestNumber).HasColumnName("request_number").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.ServiceCatalogId).HasColumnName("service_catalog_id").IsRequired();
        builder.Property(x => x.ItemDescription).HasColumnName("item_description").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SerialNumber).HasColumnName("serial_number").HasMaxLength(100);
        builder.Property(x => x.ReportedIssue).HasColumnName("reported_issue").IsRequired();
        builder.Property(x => x.Priority).HasColumnName("priority").HasMaxLength(20).HasDefaultValue("Medium").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(25).HasDefaultValue("NewRequest").IsRequired();
        builder.Property(x => x.AssignedEmployeeId).HasColumnName("assigned_employee_id");
        builder.Property(x => x.ReceivedDate).HasColumnName("received_date").IsRequired();
        builder.Property(x => x.TargetCompletionDate).HasColumnName("target_completion_date");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(x => x.DeliveredAtUtc).HasColumnName("delivered_at_utc");
        builder.Property(x => x.TechnicianNotes).HasColumnName("technician_notes");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.RequestNumber }).IsUnique().HasDatabaseName("ix_service_requests_company_number");
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ServiceCatalog).WithMany().HasForeignKey(x => x.ServiceCatalogId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AssignedEmployee).WithMany().HasForeignKey(x => x.AssignedEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ServiceQuotationConfiguration : IEntityTypeConfiguration<ServiceQuotationRecord>
{
    public void Configure(EntityTypeBuilder<ServiceQuotationRecord> builder)
    {
        builder.ToTable("service_quotations", "services", t =>
        {
            t.HasCheckConstraint("chk_quote_total", "total_amount_paise = labor_total_paise + parts_total_paise - discount_paise + tax_amount_paise");
        });
        builder.HasKey(x => x.Id).HasName("pk_service_quotations");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
        builder.Property(x => x.QuotationNumber).HasColumnName("quotation_number").HasMaxLength(40).IsRequired();
        builder.Property(x => x.LaborTotalPaise).HasColumnName("labor_total_paise").IsRequired();
        builder.Property(x => x.PartsTotalPaise).HasColumnName("parts_total_paise").IsRequired();
        builder.Property(x => x.DiscountPaise).HasColumnName("discount_paise").HasDefaultValue(0);
        builder.Property(x => x.TaxAmountPaise).HasColumnName("tax_amount_paise").IsRequired();
        builder.Property(x => x.TotalAmountPaise).HasColumnName("total_amount_paise").IsRequired();
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Draft").IsRequired();
        builder.Property(x => x.ApprovedAtUtc).HasColumnName("approved_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => x.QuotationNumber).IsUnique().HasDatabaseName("ix_service_quotations_number");
        builder.HasOne(x => x.ServiceRequest).WithMany(r => r.Quotations).HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ServiceQuotationLineConfiguration : IEntityTypeConfiguration<ServiceQuotationLineRecord>
{
    public void Configure(EntityTypeBuilder<ServiceQuotationLineRecord> builder)
    {
        builder.ToTable("service_quotation_lines", "services");
        builder.HasKey(x => x.Id).HasName("pk_service_quotation_lines");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ServiceQuotationId).HasColumnName("service_quotation_id").IsRequired();
        builder.Property(x => x.LineType).HasColumnName("line_type").HasMaxLength(20).HasDefaultValue("Labor").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(12, 3).IsRequired();
        builder.Property(x => x.UnitRatePaise).HasColumnName("unit_rate_paise").IsRequired();
        builder.Property(x => x.LineTotalPaise).HasColumnName("line_total_paise").IsRequired();

        builder.HasOne(x => x.ServiceQuotation).WithMany(q => q.Lines).HasForeignKey(x => x.ServiceQuotationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ServicePartConsumptionConfiguration : IEntityTypeConfiguration<ServicePartConsumptionRecord>
{
    public void Configure(EntityTypeBuilder<ServicePartConsumptionRecord> builder)
    {
        builder.ToTable("service_part_consumptions", "services");
        builder.HasKey(x => x.Id).HasName("pk_service_part_consumptions");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(12, 3).IsRequired();
        builder.Property(x => x.UnitCostPaise).HasColumnName("unit_cost_paise").IsRequired();
        builder.Property(x => x.StockMovementId).HasColumnName("stock_movement_id");
        builder.Property(x => x.ConsumedAtUtc).HasColumnName("consumed_at_utc").IsRequired();

        builder.HasOne(x => x.ServiceRequest).WithMany(r => r.ConsumedParts).HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockMovement).WithMany().HasForeignKey(x => x.StockMovementId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ServiceInvoiceLinkConfiguration : IEntityTypeConfiguration<ServiceInvoiceLinkRecord>
{
    public void Configure(EntityTypeBuilder<ServiceInvoiceLinkRecord> builder)
    {
        builder.ToTable("service_invoice_links", "services");
        builder.HasKey(x => x.Id).HasName("pk_service_invoice_links");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
        builder.Property(x => x.SalesInvoiceId).HasColumnName("sales_invoice_id").IsRequired();
        builder.Property(x => x.InvoicedAtUtc).HasColumnName("invoiced_at_utc").IsRequired();

        builder.HasIndex(x => new { x.ServiceRequestId, x.SalesInvoiceId }).IsUnique().HasDatabaseName("ix_service_invoice_links_req_inv");
        builder.HasOne(x => x.ServiceRequest).WithMany().HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

