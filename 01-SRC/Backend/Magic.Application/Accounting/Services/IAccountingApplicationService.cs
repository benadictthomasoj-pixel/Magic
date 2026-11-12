using Magic.Contracts.Accounting;

namespace Magic.Application.Accounting.Services;

/// <summary>
/// Primary application orchestrator for financial accounting operations, CQRS queries, and double-entry ledger commands.
/// </summary>
public interface IAccountingApplicationService
{
    Task<AccountingDashboardDto> GetDashboardAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountGroupDto>> GetAccountGroupsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(Guid companyId, string? rootClass, bool? activeOnly, string? search, CancellationToken cancellationToken = default);
    Task<AccountDto> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountDto> CreateAccountAsync(Guid companyId, Guid branchId, CreateAccountRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<AccountDto> UpdateAccountAsync(Guid id, UpdateAccountRequest request, Guid userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<VoucherSummaryDto> Items, int TotalCount)> GetVouchersAsync(
        Guid companyId,
        Guid branchId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? voucherType,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<VoucherDetailDto> GetVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VoucherDetailDto> CreateJournalDraftAsync(Guid companyId, Guid branchId, CreateJournalDraftRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<JournalValidationResultDto> ValidateJournalAsync(Guid companyId, Guid branchId, CreateJournalDraftRequest request, CancellationToken cancellationToken = default);
    Task<VoucherDetailDto> PostVoucherAsync(Guid voucherId, PostVoucherRequest request, Guid userId, bool hasSupervisorElevation = false, CancellationToken cancellationToken = default);
    Task<VoucherDetailDto> ReverseVoucherAsync(Guid voucherId, ReverseVoucherRequest request, Guid userId, CancellationToken cancellationToken = default);

    Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(Guid companyId, Guid branchId, Guid accountId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<TrialBalanceReportDto> GetTrialBalanceAsync(Guid companyId, Guid branchId, Guid? fiscalYearId, Guid? fiscalPeriodId, DateOnly? asOfDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FiscalYearDto>> GetFiscalYearsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto> LockFiscalPeriodAsync(Guid periodId, Guid userId, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto> CloseFiscalPeriodAsync(Guid periodId, Guid userId, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto> ReopenFiscalPeriodAsync(Guid periodId, Guid userId, CancellationToken cancellationToken = default);

    Task<SubledgerReportDto> GetSubledgerReportAsync(Guid companyId, Guid branchId, string subledgerType, CancellationToken cancellationToken = default);
}
