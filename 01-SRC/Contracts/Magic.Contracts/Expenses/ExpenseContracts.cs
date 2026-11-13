namespace Magic.Contracts.Expenses;

public sealed record ExpenseCategoryDto(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Name,
    Guid DefaultGlAccountId,
    string? DefaultGlAccountName,
    string? Description,
    bool IsActive);

public sealed record CreateExpenseCategoryRequest(
    string Code,
    string Name,
    Guid DefaultGlAccountId,
    string? Description);

public sealed record ExpenseDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    string ExpenseNumber,
    Guid ExpenseCategoryId,
    string ExpenseCategoryName,
    Guid GlAccountId,
    string GlAccountName,
    Guid? VendorId,
    string? VendorName,
    string PaymentMethod,
    Guid? BankCashAccountId,
    string? BankCashAccountName,
    DateOnly ExpenseDate,
    long AmountPaise,
    long TaxAmountPaise,
    long TotalAmountPaise,
    string? CostCenter,
    string? ReferenceNumber,
    string Description,
    string Status,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAtUtc,
    string? RejectionReason,
    Guid? JournalVoucherId,
    Guid? ReversalVoucherId,
    string? AttachmentPath,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateExpenseRequest(
    Guid ExpenseCategoryId,
    Guid? GlAccountId,
    Guid? VendorId,
    string PaymentMethod,
    Guid? BankCashAccountId,
    DateOnly ExpenseDate,
    long AmountPaise,
    long TaxAmountPaise,
    string? CostCenter,
    string? ReferenceNumber,
    string Description,
    string? AttachmentPath);

public sealed record RejectExpenseRequest(string Reason);
public sealed record CancelExpenseRequest(string Reason);

public sealed record PayExpenseRequest(
    string PaymentMethod,
    Guid? BankCashAccountId,
    DateOnly PaymentDate,
    string? ReferenceNumber);

public sealed record PettyCashAccountDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    string AccountName,
    Guid GlAccountId,
    string GlAccountName,
    Guid CustodianUserId,
    string CustodianUserName,
    long FloatLimitPaise,
    long CurrentBalancePaise,
    bool IsActive);

public sealed record CreatePettyCashAccountRequest(
    string AccountName,
    Guid GlAccountId,
    Guid CustodianUserId,
    long FloatLimitPaise);

public sealed record PettyCashTransactionDto(
    Guid Id,
    Guid PettyCashAccountId,
    string TransactionType,
    long AmountPaise,
    long BalanceAfterPaise,
    Guid? ExpenseId,
    Guid? JournalVoucherId,
    Guid PerformedByUserId,
    string? Notes,
    DateTimeOffset TransactionDateUtc);

public sealed record ReplenishPettyCashRequest(
    Guid SourceBankAccountId,
    long AmountPaise,
    string? Notes);

public sealed record ReconcilePettyCashRequest(
    long ActualCountPaise,
    string? Notes);

public sealed record ExpenseSummaryDto(
    long TotalExpensesPaise,
    long PendingApprovalPaise,
    long PaidExpensesPaise,
    int PendingApprovalCount,
    int TotalExpensesCount);

