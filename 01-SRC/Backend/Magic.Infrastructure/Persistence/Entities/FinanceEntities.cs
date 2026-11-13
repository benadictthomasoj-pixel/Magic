using Magic.Domain.Accounting.Entities;
using Magic.Infrastructure.Persistence.Entities;

namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Bank and Cash Accounts master entity mapped directly to General Ledger control accounts.
/// </summary>
public sealed class BankCashAccountRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Bank"; // Bank, Cash
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string MaskedAccountNumber { get; set; } = string.Empty;
    public string? IfscCode { get; set; }
    public Guid GlAccountId { get; set; }
    public long OpeningBalancePaise { get; set; }
    public long CurrentBalancePaise { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }

    public Account? GlAccount { get; set; }
}

/// <summary>
/// Customer payment receipt entity with support for advance balances and non-destructive reversal.
/// </summary>
public sealed class CustomerReceiptRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public DateOnly ReceiptDate { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer"; // Cash, BankTransfer, UPI, Cheque
    public string? ReferenceNumber { get; set; }
    public Guid BankCashAccountId { get; set; }
    public long TotalAmountPaise { get; set; }
    public long AllocatedAmountPaise { get; set; }
    public long UnallocatedAmountPaise { get; set; }
    public string Status { get; set; } = "SUBMITTED"; // SUBMITTED, REVERSED
    public Guid VoucherId { get; set; }
    public string Narration { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }

    public CustomerRecord? Customer { get; set; }
    public BankCashAccountRecord? BankCashAccount { get; set; }
    public JournalVoucher? Voucher { get; set; }
    public List<CustomerReceiptAllocationRecord> Allocations { get; set; } = [];
}

/// <summary>
/// Allocation record linking a customer receipt to an open AR subledger invoice item.
/// </summary>
public sealed class CustomerReceiptAllocationRecord
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public Guid ArItemId { get; set; }
    public long AllocatedAmountPaise { get; set; }
    public DateOnly AllocationDate { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CustomerReceiptRecord? Receipt { get; set; }
    public ArSubledgerItemRecord? ArItem { get; set; }
}

/// <summary>
/// Supplier commercial purchase bill entity recognizing legal purchase liabilities.
/// </summary>
public sealed class SupplierInvoiceRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SupplierId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty; // Vendor bill reference
    public string SystemInvoiceNumber { get; set; } = string.Empty; // PINV-202611-0001
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid? PoId { get; set; }
    public Guid? GrnId { get; set; }
    public long SubTotalPaise { get; set; }
    public long TaxAmountPaise { get; set; }
    public long TotalAmountPaise { get; set; }
    public long SettledAmountPaise { get; set; }
    public long OutstandingAmountPaise { get; set; }
    public string Status { get; set; } = "APPROVED"; // APPROVED, PAID, PARTIALLY_PAID, CANCELLED
    public Guid VoucherId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }

    public SupplierRecord? Supplier { get; set; }
    public JournalVoucher? Voucher { get; set; }
    public List<SupplierInvoiceLineRecord> Lines { get; set; } = [];
}

/// <summary>
/// Line items belonging to a commercial supplier invoice bill.
/// </summary>
public sealed class SupplierInvoiceLineRecord
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public long UnitPricePaise { get; set; }
    public decimal TaxRatePercentage { get; set; }
    public long TaxAmountPaise { get; set; }
    public long TotalAmountPaise { get; set; }

    public SupplierInvoiceRecord? Invoice { get; set; }
    public ProductRecord? Product { get; set; }
}

/// <summary>
/// Supplier payment disbursement record with multi-bill settlement and advance balance tracking.
/// </summary>
public sealed class SupplierPaymentRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer"; // Cash, BankTransfer, UPI, Cheque
    public string? ReferenceNumber { get; set; }
    public Guid BankCashAccountId { get; set; }
    public long TotalAmountPaise { get; set; }
    public long AllocatedAmountPaise { get; set; }
    public long UnallocatedAmountPaise { get; set; }
    public string Status { get; set; } = "SUBMITTED"; // SUBMITTED, REVERSED
    public Guid VoucherId { get; set; }
    public string Narration { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }

    public SupplierRecord? Supplier { get; set; }
    public BankCashAccountRecord? BankCashAccount { get; set; }
    public JournalVoucher? Voucher { get; set; }
    public List<SupplierPaymentAllocationRecord> Allocations { get; set; } = [];
}

/// <summary>
/// Allocation record linking a supplier payment to an open AP subledger bill item.
/// </summary>
public sealed class SupplierPaymentAllocationRecord
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ApItemId { get; set; }
    public long AllocatedAmountPaise { get; set; }
    public DateOnly AllocationDate { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public SupplierPaymentRecord? Payment { get; set; }
    public ApSubledgerItemRecord? ApItem { get; set; }
}

/// <summary>
/// Internal cash/bank treasury contra movements, bank charges, and interest receipts.
/// </summary>
public sealed class TreasuryTransactionRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string TransactionType { get; set; } = "CashDeposit"; // CashDeposit, CashWithdrawal, BankTransfer, CashTransfer, BankCharges, BankInterest
    public DateOnly TransactionDate { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid? DestinationAccountId { get; set; }
    public long AmountPaise { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Narration { get; set; } = string.Empty;
    public string Status { get; set; } = "POSTED"; // POSTED, REVERSED
    public Guid VoucherId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }

    public BankCashAccountRecord? SourceAccount { get; set; }
    public BankCashAccountRecord? DestinationAccount { get; set; }
    public JournalVoucher? Voucher { get; set; }
}

/// <summary>
/// Bank statement header imported for reconciliation.
/// </summary>
public sealed class BankStatementRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateOnly StatementStartDate { get; set; }
    public DateOnly StatementEndDate { get; set; }
    public long OpeningBalancePaise { get; set; }
    public long ClosingBalancePaise { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string Status { get; set; } = "OPEN"; // OPEN, RECONCILED
    public DateTimeOffset ImportedAtUtc { get; set; }
    public Guid ImportedByUserId { get; set; }

    public BankCashAccountRecord? BankAccount { get; set; }
    public List<BankStatementLineRecord> Lines { get; set; } = [];
}

/// <summary>
/// Individual line within an imported bank statement.
/// </summary>
public sealed class BankStatementLineRecord
{
    public Guid Id { get; set; }
    public Guid StatementId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public DateOnly? ValueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public long WithdrawalDebitPaise { get; set; }
    public long DepositCreditPaise { get; set; }
    public long BalancePaise { get; set; }
    public bool IsReconciled { get; set; }
    public Guid? MatchedVoucherLineId { get; set; }
    public DateTimeOffset? MatchedAtUtc { get; set; }
    public string? MatchType { get; set; } // MANUAL, AUTO_EXACT, AUTO_SUGGESTED

    public BankStatementRecord? Statement { get; set; }
    public JournalLine? MatchedVoucherLine { get; set; }
}
