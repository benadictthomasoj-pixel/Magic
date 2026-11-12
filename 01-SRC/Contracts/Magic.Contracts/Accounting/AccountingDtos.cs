namespace Magic.Contracts.Accounting;

/// <summary>
/// High-level executive dashboard summary derived from real General Ledger vouchers.
/// </summary>
public sealed record AccountingDashboardDto
{
    public long TotalDebitPaise { get; init; }
    public long TotalCreditPaise { get; init; }
    public int PostedVouchersCount { get; init; }
    public int DraftVouchersCount { get; init; }
    public string? ActiveFiscalYearName { get; init; }
    public string? ActiveFiscalPeriodCode { get; init; }
    public bool IsActivePeriodLocked { get; init; }
    public IReadOnlyList<VoucherSummaryDto> RecentVouchers { get; init; } = [];
    public string SystemValidationStatus { get; init; } = "BALANCED"; // BALANCED, EMPTY, UNBALANCED
}

/// <summary>
/// Chart of Accounts classification group.
/// </summary>
public sealed record AccountGroupDto
{
    public Guid Id { get; init; }
    public Guid? ParentGroupId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string RootClass { get; init; } = string.Empty;
    public int HierarchyDepth { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Detail account in Chart of Accounts.
/// </summary>
public sealed record AccountDto
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string RootClass { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = "INR";
    public bool IsActive { get; init; }
    public string? ControlAccountType { get; init; }
    public long CurrentBalancePaise { get; init; }
}

/// <summary>
/// Request to create a new Detail Account.
/// </summary>
public sealed record CreateAccountRequest
{
    public Guid GroupId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string RootClass { get; init; } = string.Empty;
    public string? ControlAccountType { get; init; }
}

/// <summary>
/// Request to update an existing Detail Account.
/// </summary>
public sealed record UpdateAccountRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

/// <summary>
/// Summarized voucher for list tables and dashboard feeds.
/// </summary>
public sealed record VoucherSummaryDto
{
    public Guid Id { get; init; }
    public string VoucherNumber { get; init; } = string.Empty;
    public string VoucherType { get; init; } = string.Empty;
    public DateOnly VoucherDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Narration { get; init; } = string.Empty;
    public string? ReferenceNumber { get; init; }
    public long TotalDebitPaise { get; init; }
    public long TotalCreditPaise { get; init; }
    public bool IsReversed { get; init; }
    public string? ReversalVoucherNumber { get; init; }
    public int LinesCount { get; init; }
    public DateTimeOffset? PostedAtUtc { get; init; }
}

/// <summary>
/// Complete voucher details with journal lines for inspection.
/// </summary>
public sealed record VoucherDetailDto
{
    public Guid Id { get; init; }
    public Guid BranchId { get; init; }
    public Guid FiscalPeriodId { get; init; }
    public string PeriodCode { get; init; } = string.Empty;
    public string VoucherNumber { get; init; } = string.Empty;
    public string VoucherType { get; init; } = string.Empty;
    public DateOnly VoucherDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Narration { get; init; } = string.Empty;
    public string? ReferenceNumber { get; init; }
    public long TotalDebitPaise { get; init; }
    public long TotalCreditPaise { get; init; }
    public bool IsReversed { get; init; }
    public Guid? ReversalVoucherId { get; init; }
    public string? ReversalVoucherNumber { get; init; }
    public Guid TraceCorrelationId { get; init; }
    public DateTimeOffset? PostedAtUtc { get; init; }
    public Guid? PostedByUserId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public IReadOnlyList<JournalLineDto> Lines { get; init; } = [];
}

/// <summary>
/// Line item in a journal voucher.
/// </summary>
public sealed record JournalLineDto
{
    public Guid Id { get; init; }
    public int LineNumber { get; init; }
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public long DebitPaise { get; init; }
    public long CreditPaise { get; init; }
    public string Narration { get; init; } = string.Empty;
    public Guid? PartyId { get; init; }
    public string? PartyType { get; init; }
}

/// <summary>
/// Payload to create or update a draft journal entry.
/// </summary>
public sealed record CreateJournalDraftRequest
{
    public Guid FiscalPeriodId { get; init; }
    public string VoucherType { get; init; } = "Journal";
    public DateOnly VoucherDate { get; init; }
    public string Narration { get; init; } = string.Empty;
    public string? ReferenceNumber { get; init; }
    public IReadOnlyList<JournalLineInputDto> Lines { get; init; } = [];
}

/// <summary>
/// Line item input for journal drafts.
/// </summary>
public sealed record JournalLineInputDto
{
    public Guid AccountId { get; init; }
    public long DebitPaise { get; init; }
    public long CreditPaise { get; init; }
    public string Narration { get; init; } = string.Empty;
    public Guid? PartyId { get; init; }
    public string? PartyType { get; init; }
}

/// <summary>
/// Detailed evaluation result of a journal voucher against financial invariants.
/// </summary>
public sealed record JournalValidationResultDto
{
    public bool IsValid { get; init; }
    public long TotalDebitPaise { get; init; }
    public long TotalCreditPaise { get; init; }
    public long DifferencePaise { get; init; }
    public bool IsBalanced { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// Request to post an existing draft voucher.
/// </summary>
public sealed record PostVoucherRequest
{
    public string BranchCode { get; init; } = "MAIN";
    public string FyCode { get; init; } = "FY26";
    public string? SupervisorAuthToken { get; init; }
}

/// <summary>
/// Request to non-destructively reverse a posted voucher.
/// </summary>
public sealed record ReverseVoucherRequest
{
    public string BranchCode { get; init; } = "MAIN";
    public string FyCode { get; init; } = "FY26";
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// General Ledger transactions and running balances for a specific account.
/// </summary>
public sealed record GeneralLedgerReportDto
{
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string RootClass { get; init; } = string.Empty;
    public DateOnly DateFrom { get; init; }
    public DateOnly DateTo { get; init; }
    public long OpeningBalancePaise { get; init; }
    public long TotalDebitPaise { get; init; }
    public long TotalCreditPaise { get; init; }
    public long ClosingBalancePaise { get; init; }
    public IReadOnlyList<GeneralLedgerEntryDto> Entries { get; init; } = [];
}

/// <summary>
/// General Ledger movement entry linking to source voucher.
/// </summary>
public sealed record GeneralLedgerEntryDto
{
    public Guid VoucherId { get; init; }
    public string VoucherNumber { get; init; } = string.Empty;
    public string VoucherType { get; init; } = string.Empty;
    public DateOnly VoucherDate { get; init; }
    public string Narration { get; init; } = string.Empty;
    public long DebitPaise { get; init; }
    public long CreditPaise { get; init; }
    public long RunningBalancePaise { get; init; }
}

/// <summary>
/// Comprehensive Trial Balance statement balancing all open accounts.
/// </summary>
public sealed record TrialBalanceReportDto
{
    public string FiscalYearCode { get; init; } = string.Empty;
    public string? FiscalPeriodCode { get; init; }
    public DateOnly AsOfDate { get; init; }
    public long TotalOpeningDebitPaise { get; init; }
    public long TotalOpeningCreditPaise { get; init; }
    public long TotalPeriodDebitPaise { get; init; }
    public long TotalPeriodCreditPaise { get; init; }
    public long TotalClosingDebitPaise { get; init; }
    public long TotalClosingCreditPaise { get; init; }
    public long DifferencePaise { get; init; }
    public bool IsBalanced { get; init; }
    public IReadOnlyList<TrialBalanceAccountRowDto> Accounts { get; init; } = [];
}

/// <summary>
/// Account row within a Trial Balance statement.
/// </summary>
public sealed record TrialBalanceAccountRowDto
{
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string GroupCode { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public string RootClass { get; init; } = string.Empty;
    public long OpeningDebitPaise { get; init; }
    public long OpeningCreditPaise { get; init; }
    public long PeriodDebitPaise { get; init; }
    public long PeriodCreditPaise { get; init; }
    public long ClosingDebitPaise { get; init; }
    public long ClosingCreditPaise { get; init; }
}

/// <summary>
/// Fiscal Year governance configuration.
/// </summary>
public sealed record FiscalYearDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsClosed { get; init; }
    public IReadOnlyList<FiscalPeriodDto> Periods { get; init; } = [];
}

/// <summary>
/// Fiscal accounting period with locking and closure controls.
/// </summary>
public sealed record FiscalPeriodDto
{
    public Guid Id { get; init; }
    public Guid FiscalYearId { get; init; }
    public int PeriodNumber { get; init; }
    public string PeriodCode { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsLocked { get; init; }
    public bool IsClosed { get; init; }
}

/// <summary>
/// Foundation subledger summary report (AR / AP).
/// </summary>
public sealed record SubledgerReportDto
{
    public string SubledgerType { get; init; } = "AR";
    public long TotalOutstandingPaise { get; init; }
    public int ItemsCount { get; init; }
    public IReadOnlyList<SubledgerItemDto> Items { get; init; } = [];
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Subledger open item.
/// </summary>
public sealed record SubledgerItemDto
{
    public Guid Id { get; init; }
    public Guid PartyId { get; init; }
    public string PartyName { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public DateOnly DocumentDate { get; init; }
    public DateOnly DueDate { get; init; }
    public long TotalAmountPaise { get; init; }
    public long OutstandingBalancePaise { get; init; }
    public string Status { get; init; } = string.Empty;
}
