namespace Magic.Contracts.Finance;

// ==========================================
// 1. Receivables & Aging DTOs
// ==========================================

public sealed record CustomerReceivableDto
{
    public required Guid Id { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required string DocumentNumber { get; init; }
    public Guid? InvoiceId { get; init; }
    public Guid VoucherId { get; init; }
    public required DateOnly ItemDate { get; init; }
    public required DateOnly DueDate { get; init; }
    public required long OriginalAmountPaise { get; init; }
    public required long SettledAmountPaise { get; init; }
    public required long OutstandingAmountPaise { get; init; }
    public required string Status { get; init; }
    public required int OverdueDays { get; init; }
}

public sealed record ReceivablesSummaryDto
{
    public required long TotalReceivablePaise { get; init; }
    public required long TotalOverduePaise { get; init; }
    public required int CustomerCount { get; init; }
    public required int OpenInvoiceCount { get; init; }
}

public sealed record CustomerAgingRowDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required long TotalOutstandingPaise { get; init; }
    public required long CurrentPaise { get; init; }
    public required long Overdue1To30Paise { get; init; }
    public required long Overdue31To60Paise { get; init; }
    public required long Overdue61To90Paise { get; init; }
    public required long OverdueAbove90Paise { get; init; }
}

public sealed record CustomerAgingReportDto
{
    public required DateOnly ReportDate { get; init; }
    public required long TotalOutstandingPaise { get; init; }
    public required long TotalCurrentPaise { get; init; }
    public required long Total1To30Paise { get; init; }
    public required long Total31To60Paise { get; init; }
    public required long Total61To90Paise { get; init; }
    public required long TotalAbove90Paise { get; init; }
    public required IReadOnlyList<CustomerAgingRowDto> Customers { get; init; }
}

public sealed record CustomerStatementLineDto
{
    public required DateOnly Date { get; init; }
    public required string DocumentType { get; init; } // Invoice, Receipt, Return, Adjustment
    public required string DocumentNumber { get; init; }
    public string? ReferenceNumber { get; init; }
    public required long DebitPaise { get; init; }
    public required long CreditPaise { get; init; }
    public required long RunningBalancePaise { get; init; }
    public string? Narration { get; init; }
}

public sealed record CustomerStatementDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required DateOnly FromDate { get; init; }
    public required DateOnly ToDate { get; init; }
    public required long OpeningBalancePaise { get; init; }
    public required long TotalDebitPaise { get; init; }
    public required long TotalCreditPaise { get; init; }
    public required long ClosingBalancePaise { get; init; }
    public required IReadOnlyList<CustomerStatementLineDto> Lines { get; init; }
}

// ==========================================
// 2. Customer Receipts & Collections DTOs
// ==========================================

public sealed record CustomerReceiptAllocationRequest
{
    public required Guid ArItemId { get; init; }
    public required long AllocatedAmountPaise { get; init; }
}

public sealed record CreateCustomerReceiptRequest
{
    public required Guid CustomerId { get; init; }
    public required DateOnly ReceiptDate { get; init; }
    public required string PaymentMethod { get; init; } // Cash, BankTransfer, UPI, Cheque
    public string? ReferenceNumber { get; init; }
    public required Guid BankCashAccountId { get; init; }
    public required long TotalAmountPaise { get; init; }
    public string? Narration { get; init; }
    public IReadOnlyList<CustomerReceiptAllocationRequest>? Allocations { get; init; }
}

public sealed record CustomerReceiptAllocationDto
{
    public required Guid Id { get; init; }
    public required Guid ArItemId { get; init; }
    public required string DocumentNumber { get; init; }
    public required long AllocatedAmountPaise { get; init; }
    public required DateOnly AllocationDate { get; init; }
}

public sealed record CustomerReceiptDto
{
    public required Guid Id { get; init; }
    public required string ReceiptNumber { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required DateOnly ReceiptDate { get; init; }
    public required string PaymentMethod { get; init; }
    public string? ReferenceNumber { get; init; }
    public required Guid BankCashAccountId { get; init; }
    public required string BankCashAccountName { get; init; }
    public required long TotalAmountPaise { get; init; }
    public required long AllocatedAmountPaise { get; init; }
    public required long UnallocatedAmountPaise { get; init; }
    public required string Status { get; init; }
    public required Guid VoucherId { get; init; }
    public required string VoucherNumber { get; init; }
    public required string Narration { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required IReadOnlyList<CustomerReceiptAllocationDto> Allocations { get; init; }
}

public sealed record CancelReceiptRequest
{
    public required string Reason { get; init; }
}

// ==========================================
// 3. Payables & Supplier Bills DTOs
// ==========================================

public sealed record SupplierPayableDto
{
    public required Guid Id { get; init; }
    public required Guid SupplierId { get; init; }
    public required string SupplierName { get; init; }
    public required string BillNumber { get; init; }
    public required string SystemInvoiceNumber { get; init; }
    public Guid? BillId { get; init; }
    public Guid VoucherId { get; init; }
    public required DateOnly ItemDate { get; init; }
    public required DateOnly DueDate { get; init; }
    public required long OriginalAmountPaise { get; init; }
    public required long SettledAmountPaise { get; init; }
    public required long OutstandingAmountPaise { get; init; }
    public required string Status { get; init; }
    public required int OverdueDays { get; init; }
}

public sealed record PayablesSummaryDto
{
    public required long TotalPayablePaise { get; init; }
    public required long TotalOverduePaise { get; init; }
    public required int SupplierCount { get; init; }
    public required int OpenBillCount { get; init; }
}

public sealed record SupplierAgingRowDto
{
    public required Guid SupplierId { get; init; }
    public required string SupplierName { get; init; }
    public required long TotalOutstandingPaise { get; init; }
    public required long CurrentPaise { get; init; }
    public required long Overdue1To30Paise { get; init; }
    public required long Overdue31To60Paise { get; init; }
    public required long Overdue61To90Paise { get; init; }
    public required long OverdueAbove90Paise { get; init; }
}

public sealed record SupplierAgingReportDto
{
    public required DateOnly ReportDate { get; init; }
    public required long TotalOutstandingPaise { get; init; }
    public required long TotalCurrentPaise { get; init; }
    public required long Total1To30Paise { get; init; }
    public required long Total31To60Paise { get; init; }
    public required long Total61To90Paise { get; init; }
    public required long TotalAbove90Paise { get; init; }
    public required IReadOnlyList<SupplierAgingRowDto> Suppliers { get; init; }
}

public sealed record SupplierStatementLineDto
{
    public required DateOnly Date { get; init; }
    public required string DocumentType { get; init; }
    public required string DocumentNumber { get; init; }
    public string? ReferenceNumber { get; init; }
    public required long DebitPaise { get; init; }
    public required long CreditPaise { get; init; }
    public required long RunningBalancePaise { get; init; }
    public string? Narration { get; init; }
}

public sealed record SupplierStatementDto
{
    public required Guid SupplierId { get; init; }
    public required string SupplierName { get; init; }
    public required DateOnly FromDate { get; init; }
    public required DateOnly ToDate { get; init; }
    public required long OpeningBalancePaise { get; init; }
    public required long TotalDebitPaise { get; init; }
    public required long TotalCreditPaise { get; init; }
    public required long ClosingBalancePaise { get; init; }
    public required IReadOnlyList<SupplierStatementLineDto> Lines { get; init; }
}

public sealed record CreateSupplierBillLineRequest
{
    public required string Description { get; init; }
    public required decimal Quantity { get; init; }
    public required long UnitPricePaise { get; init; }
    public decimal TaxRatePercentage { get; init; }
}

public sealed record CreateSupplierBillRequest
{
    public required Guid SupplierId { get; init; }
    public required string InvoiceNumber { get; init; } // Vendor's bill reference
    public required DateOnly InvoiceDate { get; init; }
    public required DateOnly DueDate { get; init; }
    public Guid? PoId { get; init; }
    public Guid? GrnId { get; init; }
    public string? Notes { get; init; }
    public required IReadOnlyList<CreateSupplierBillLineRequest> Lines { get; init; }
}

public sealed record SupplierBillLineDto
{
    public required Guid Id { get; init; }
    public required string Description { get; init; }
    public required decimal Quantity { get; init; }
    public required long UnitPricePaise { get; init; }
    public required decimal TaxRatePercentage { get; init; }
    public required long TaxAmountPaise { get; init; }
    public required long TotalAmountPaise { get; init; }
}

public sealed record SupplierBillDto
{
    public required Guid Id { get; init; }
    public required string SystemInvoiceNumber { get; init; }
    public required string InvoiceNumber { get; init; }
    public required Guid SupplierId { get; init; }
    public required string SupplierName { get; init; }
    public required DateOnly InvoiceDate { get; init; }
    public required DateOnly DueDate { get; init; }
    public Guid? PoId { get; init; }
    public Guid? GrnId { get; init; }
    public required long SubTotalPaise { get; init; }
    public required long TaxAmountPaise { get; init; }
    public required long TotalAmountPaise { get; init; }
    public required long SettledAmountPaise { get; init; }
    public required long OutstandingAmountPaise { get; init; }
    public required string Status { get; init; }
    public required Guid VoucherId { get; init; }
    public required string VoucherNumber { get; init; }
    public string? Notes { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required IReadOnlyList<SupplierBillLineDto> Lines { get; init; }
}

// ==========================================
// 4. Supplier Payments & Settlement DTOs
// ==========================================

public sealed record SupplierPaymentAllocationRequest
{
    public required Guid ApItemId { get; init; }
    public required long AllocatedAmountPaise { get; init; }
}

public sealed record CreateSupplierPaymentRequest
{
    public required Guid SupplierId { get; init; }
    public required DateOnly PaymentDate { get; init; }
    public required string PaymentMethod { get; init; } // Cash, BankTransfer, UPI, Cheque
    public string? ReferenceNumber { get; init; }
    public required Guid BankCashAccountId { get; init; }
    public required long TotalAmountPaise { get; init; }
    public string? Narration { get; init; }
    public IReadOnlyList<SupplierPaymentAllocationRequest>? Allocations { get; init; }
}

public sealed record SupplierPaymentAllocationDto
{
    public required Guid Id { get; init; }
    public required Guid ApItemId { get; init; }
    public required string DocumentNumber { get; init; }
    public required long AllocatedAmountPaise { get; init; }
    public required DateOnly AllocationDate { get; init; }
}

public sealed record SupplierPaymentDto
{
    public required Guid Id { get; init; }
    public required string PaymentNumber { get; init; }
    public required Guid SupplierId { get; init; }
    public required string SupplierName { get; init; }
    public required DateOnly PaymentDate { get; init; }
    public required string PaymentMethod { get; init; }
    public string? ReferenceNumber { get; init; }
    public required Guid BankCashAccountId { get; init; }
    public required string BankCashAccountName { get; init; }
    public required long TotalAmountPaise { get; init; }
    public required long AllocatedAmountPaise { get; init; }
    public required long UnallocatedAmountPaise { get; init; }
    public required string Status { get; init; }
    public required Guid VoucherId { get; init; }
    public required string VoucherNumber { get; init; }
    public required string Narration { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required IReadOnlyList<SupplierPaymentAllocationDto> Allocations { get; init; }
}

public sealed record CancelPaymentRequest
{
    public required string Reason { get; init; }
}

// ==========================================
// 5. Bank & Cash Account DTOs
// ==========================================

public sealed record BankCashAccountDto
{
    public required Guid Id { get; init; }
    public required string AccountName { get; init; }
    public required string AccountType { get; init; } // Bank, Cash
    public string? BankName { get; init; }
    public string? BranchName { get; init; }
    public required string MaskedAccountNumber { get; init; }
    public string? IfscCode { get; init; }
    public required Guid GlAccountId { get; init; }
    public required string GlAccountCode { get; init; }
    public required string GlAccountName { get; init; }
    public required long OpeningBalancePaise { get; init; }
    public required long CurrentBalancePaise { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed record CreateBankCashAccountRequest
{
    public required string AccountName { get; init; }
    public required string AccountType { get; init; } // Bank, Cash
    public string? BankName { get; init; }
    public string? BranchName { get; init; }
    public required string AccountNumber { get; init; }
    public string? IfscCode { get; init; }
    public required Guid GlAccountId { get; init; }
    public long OpeningBalancePaise { get; init; }
}

public sealed record UpdateBankCashAccountRequest
{
    public required string AccountName { get; init; }
    public string? BankName { get; init; }
    public string? BranchName { get; init; }
    public string? IfscCode { get; init; }
    public required bool IsActive { get; init; }
}

public sealed record RevealAccountNumberResponse
{
    public required Guid Id { get; init; }
    public required string AccountNumber { get; init; }
    public required DateTimeOffset RevealedAtUtc { get; init; }
}

// ==========================================
// 6. Treasury DTOs
// ==========================================

public sealed record TreasuryTransactionDto
{
    public required Guid Id { get; init; }
    public required string TransactionNumber { get; init; }
    public required string TransactionType { get; init; } // CashDeposit, CashWithdrawal, BankTransfer, CashTransfer, BankCharges, BankInterest
    public required DateOnly TransactionDate { get; init; }
    public required Guid SourceAccountId { get; init; }
    public required string SourceAccountName { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public string? DestinationAccountName { get; init; }
    public required long AmountPaise { get; init; }
    public string? ReferenceNumber { get; init; }
    public required string Narration { get; init; }
    public required string Status { get; init; }
    public required Guid VoucherId { get; init; }
    public required string VoucherNumber { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed record CreateContraTransactionRequest
{
    public required string TransactionType { get; init; } // CashDeposit, CashWithdrawal, BankTransfer, CashTransfer
    public required DateOnly TransactionDate { get; init; }
    public required Guid SourceAccountId { get; init; }
    public required Guid DestinationAccountId { get; init; }
    public required long AmountPaise { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Narration { get; init; }
}

public sealed record CreateTreasuryAdjustmentRequest
{
    public required string TransactionType { get; init; } // BankCharges, BankInterest
    public required DateOnly TransactionDate { get; init; }
    public required Guid BankAccountId { get; init; }
    public required long AmountPaise { get; init; }
    public required Guid NominalAccountId { get; init; } // Expense or Income GL Account
    public string? ReferenceNumber { get; init; }
    public string? Narration { get; init; }
}

// ==========================================
// 7. Bank Reconciliation DTOs
// ==========================================

public sealed record BankStatementSummaryDto
{
    public required Guid Id { get; init; }
    public required Guid BankAccountId { get; init; }
    public required string BankAccountName { get; init; }
    public required DateOnly StatementStartDate { get; init; }
    public required DateOnly StatementEndDate { get; init; }
    public required long OpeningBalancePaise { get; init; }
    public required long ClosingBalancePaise { get; init; }
    public required string Filename { get; init; }
    public required string Status { get; init; }
    public required int TotalLines { get; init; }
    public required int ReconciledLines { get; init; }
    public required DateTimeOffset ImportedAtUtc { get; init; }
}

public sealed record BankStatementLineDto
{
    public required Guid Id { get; init; }
    public required DateOnly TransactionDate { get; init; }
    public DateOnly? ValueDate { get; init; }
    public required string Description { get; init; }
    public string? ReferenceNumber { get; init; }
    public required long WithdrawalDebitPaise { get; init; }
    public required long DepositCreditPaise { get; init; }
    public required long BalancePaise { get; init; }
    public required bool IsReconciled { get; init; }
    public Guid? MatchedVoucherLineId { get; init; }
    public string? MatchedVoucherNumber { get; init; }
    public string? MatchType { get; init; }
}

public sealed record BankStatementDetailDto
{
    public required Guid Id { get; init; }
    public required Guid BankAccountId { get; init; }
    public required string BankAccountName { get; init; }
    public required DateOnly StatementStartDate { get; init; }
    public required DateOnly StatementEndDate { get; init; }
    public required long OpeningBalancePaise { get; init; }
    public required long ClosingBalancePaise { get; init; }
    public required string Filename { get; init; }
    public required string Status { get; init; }
    public required IReadOnlyList<BankStatementLineDto> Lines { get; init; }
}

public sealed record ImportBankStatementRequest
{
    public required Guid BankAccountId { get; init; }
    public required DateOnly StatementStartDate { get; init; }
    public required DateOnly StatementEndDate { get; init; }
    public required long OpeningBalancePaise { get; init; }
    public required long ClosingBalancePaise { get; init; }
    public required string Filename { get; init; }
    public required string CsvContent { get; init; }
}

public sealed record UnmatchedLedgerTransactionDto
{
    public required Guid JournalLineId { get; init; }
    public required Guid VoucherId { get; init; }
    public required string VoucherNumber { get; init; }
    public required DateOnly VoucherDate { get; init; }
    public required long DebitPaise { get; init; }
    public required long CreditPaise { get; init; }
    public string? ReferenceNumber { get; init; }
    public required string Narration { get; init; }
}

public sealed record ReconciliationWorkspaceDto
{
    public required Guid StatementId { get; init; }
    public required Guid BankAccountId { get; init; }
    public required string BankAccountName { get; init; }
    public required long BookBalancePaise { get; init; }
    public required long StatementBalancePaise { get; init; }
    public required long DifferencePaise { get; init; }
    public required IReadOnlyList<BankStatementLineDto> StatementLines { get; init; }
    public required IReadOnlyList<UnmatchedLedgerTransactionDto> UnmatchedLedgerLines { get; init; }
}

public sealed record MatchStatementLineRequest
{
    public required Guid StatementLineId { get; init; }
    public required Guid JournalLineId { get; init; }
    public required string MatchType { get; init; } // MANUAL, AUTO_EXACT, AUTO_SUGGESTED
}

public sealed record UnmatchStatementLineRequest
{
    public required Guid StatementLineId { get; init; }
}

// ==========================================
// 8. Financial Reports DTOs
// ==========================================

public sealed record CashBookEntryDto
{
    public required DateOnly Date { get; init; }
    public required string VoucherNumber { get; init; }
    public required string VoucherType { get; init; }
    public required string OppositeAccountName { get; init; }
    public required string Narration { get; init; }
    public required long ReceiptPaise { get; init; }
    public required long PaymentPaise { get; init; }
    public required long RunningBalancePaise { get; init; }
}

public sealed record CashBookReportDto
{
    public required string AccountName { get; init; }
    public required DateOnly FromDate { get; init; }
    public required DateOnly ToDate { get; init; }
    public required long OpeningBalancePaise { get; init; }
    public required long TotalReceiptsPaise { get; init; }
    public required long TotalPaymentsPaise { get; init; }
    public required long ClosingBalancePaise { get; init; }
    public required IReadOnlyList<CashBookEntryDto> Entries { get; init; }
}

public sealed record BankBookEntryDto
{
    public required DateOnly Date { get; init; }
    public required string VoucherNumber { get; init; }
    public required string VoucherType { get; init; }
    public string? ReferenceNumber { get; init; }
    public required string OppositeAccountName { get; init; }
    public required string Narration { get; init; }
    public required long DepositPaise { get; init; }
    public required long WithdrawalPaise { get; init; }
    public required long RunningBalancePaise { get; init; }
}

public sealed record BankBookReportDto
{
    public required string AccountName { get; init; }
    public required string? BankName { get; init; }
    public required DateOnly FromDate { get; init; }
    public required DateOnly ToDate { get; init; }
    public required long OpeningBalancePaise { get; init; }
    public required long TotalDepositsPaise { get; init; }
    public required long TotalWithdrawalsPaise { get; init; }
    public required long ClosingBalancePaise { get; init; }
    public required IReadOnlyList<BankBookEntryDto> Entries { get; init; }
}

public sealed record FinanceSummaryKpiDto
{
    public required long TotalReceivablePaise { get; init; }
    public required long TotalOverdueArPaise { get; init; }
    public required long TotalPayablePaise { get; init; }
    public required long TotalOverdueApPaise { get; init; }
    public required long TotalCashBalancePaise { get; init; }
    public required long TotalBankBalancePaise { get; init; }
}
