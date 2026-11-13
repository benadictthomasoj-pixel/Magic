using Magic.Domain.Accounting.Entities;

namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Expense category mapped to General Ledger expense accounts.
/// </summary>
public sealed class ExpenseCategoryRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid DefaultGlAccountId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public Account? DefaultGlAccount { get; set; }
}

/// <summary>
/// Operational expense record with multi-tier approval and double-entry GL linkage.
/// </summary>
public sealed class ExpenseRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string ExpenseNumber { get; set; } = string.Empty;
    public Guid ExpenseCategoryId { get; set; }
    public Guid GlAccountId { get; set; }
    public Guid? VendorId { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank, PettyCash, Payable
    public Guid? BankCashAccountId { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public long AmountPaise { get; set; }
    public long TaxAmountPaise { get; set; }
    public long TotalAmountPaise { get; set; }
    public string? CostCenter { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Submitted"; // Draft, Submitted, Approved, Rejected, Paid, Cancelled
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? JournalVoucherId { get; set; }
    public Guid? ReversalVoucherId { get; set; }
    public string? AttachmentPath { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }

    public ExpenseCategoryRecord? ExpenseCategory { get; set; }
    public Account? GlAccount { get; set; }
    public SupplierRecord? Vendor { get; set; }
    public BankCashAccountRecord? BankCashAccount { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }
    public JournalVoucher? ReversalVoucher { get; set; }
}

/// <summary>
/// Petty cash account float assigned to a custodian at a branch.
/// </summary>
public sealed class PettyCashAccountRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public Guid GlAccountId { get; set; }
    public Guid CustodianUserId { get; set; }
    public long FloatLimitPaise { get; set; }
    public long CurrentBalancePaise { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public Account? GlAccount { get; set; }
    public UserRecord? CustodianUser { get; set; }
}

/// <summary>
/// Audit trail for petty cash float allocations, expenses, replenishments, shortages, and excesses.
/// </summary>
public sealed class PettyCashTransactionRecord
{
    public Guid Id { get; set; }
    public Guid PettyCashAccountId { get; set; }
    public string TransactionType { get; set; } = "Allocation"; // Allocation, Expense, Replenishment, Shortage, Excess
    public long AmountPaise { get; set; }
    public long BalanceAfterPaise { get; set; }
    public Guid? ExpenseId { get; set; }
    public Guid? JournalVoucherId { get; set; }
    public Guid PerformedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset TransactionDateUtc { get; set; }

    public PettyCashAccountRecord? PettyCashAccount { get; set; }
    public ExpenseRecord? Expense { get; set; }
    public JournalVoucher? JournalVoucher { get; set; }
    public UserRecord? PerformedByUser { get; set; }
}

