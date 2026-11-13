using Magic.Contracts.Finance;

namespace Magic.Application.Finance.Services;

public interface IReceivablesService
{
    Task<ReceivablesSummaryDto> GetSummaryAsync(
        Guid companyId,
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerReceivableDto> Items, int TotalCount)> GetReceivablesAsync(
        Guid companyId,
        Guid branchId,
        Guid? customerId,
        string? status,
        bool overdueOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CustomerAgingReportDto> GetAgingReportAsync(
        Guid companyId,
        Guid branchId,
        DateOnly reportDate,
        CancellationToken cancellationToken = default);

    Task<CustomerStatementDto> GetCustomerStatementAsync(
        Guid companyId,
        Guid branchId,
        Guid customerId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}

public interface ICustomerReceiptService
{
    Task<(IReadOnlyList<CustomerReceiptDto> Items, int TotalCount)> GetReceiptsAsync(
        Guid companyId,
        Guid branchId,
        Guid? customerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CustomerReceiptDto> GetReceiptByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CustomerReceiptDto> CreateReceiptAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateCustomerReceiptRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerReceiptDto> CancelReceiptAsync(
        Guid companyId,
        Guid branchId,
        Guid id,
        Guid userId,
        CancelReceiptRequest request,
        CancellationToken cancellationToken = default);
}

public interface IPayablesService
{
    Task<PayablesSummaryDto> GetSummaryAsync(
        Guid companyId,
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SupplierPayableDto> Items, int TotalCount)> GetPayablesAsync(
        Guid companyId,
        Guid branchId,
        Guid? supplierId,
        string? status,
        bool overdueOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SupplierAgingReportDto> GetAgingReportAsync(
        Guid companyId,
        Guid branchId,
        DateOnly reportDate,
        CancellationToken cancellationToken = default);

    Task<SupplierStatementDto> GetSupplierStatementAsync(
        Guid companyId,
        Guid branchId,
        Guid supplierId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<SupplierBillDto> CreateSupplierBillAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateSupplierBillRequest request,
        CancellationToken cancellationToken = default);

    Task<SupplierBillDto> GetSupplierBillByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);
}

public interface ISupplierPaymentService
{
    Task<(IReadOnlyList<SupplierPaymentDto> Items, int TotalCount)> GetPaymentsAsync(
        Guid companyId,
        Guid branchId,
        Guid? supplierId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SupplierPaymentDto> GetPaymentByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SupplierPaymentDto> CreatePaymentAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateSupplierPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<SupplierPaymentDto> CancelPaymentAsync(
        Guid companyId,
        Guid branchId,
        Guid id,
        Guid userId,
        CancelPaymentRequest request,
        CancellationToken cancellationToken = default);
}

public interface IBankCashAccountService
{
    Task<IReadOnlyList<BankCashAccountDto>> GetAccountsAsync(
        Guid companyId,
        Guid branchId,
        string? accountType,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<BankCashAccountDto> GetAccountByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BankCashAccountDto> CreateAccountAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateBankCashAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<BankCashAccountDto> UpdateAccountAsync(
        Guid companyId,
        Guid id,
        Guid userId,
        UpdateBankCashAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<RevealAccountNumberResponse> RevealAccountNumberAsync(
        Guid companyId,
        Guid id,
        Guid userId,
        string? clientIp,
        CancellationToken cancellationToken = default);
}

public interface ITreasuryService
{
    Task<(IReadOnlyList<TreasuryTransactionDto> Items, int TotalCount)> GetTransactionsAsync(
        Guid companyId,
        Guid branchId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? transactionType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TreasuryTransactionDto> CreateContraTransactionAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateContraTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<TreasuryTransactionDto> CreateAdjustmentTransactionAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateTreasuryAdjustmentRequest request,
        CancellationToken cancellationToken = default);
}

public interface IBankReconciliationService
{
    Task<IReadOnlyList<BankStatementSummaryDto>> GetStatementsAsync(
        Guid companyId,
        Guid branchId,
        Guid? bankAccountId,
        CancellationToken cancellationToken = default);

    Task<BankStatementDetailDto> ImportStatementCsvAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        ImportBankStatementRequest request,
        CancellationToken cancellationToken = default);

    Task<ReconciliationWorkspaceDto> GetReconciliationWorkspaceAsync(
        Guid companyId,
        Guid branchId,
        Guid statementId,
        CancellationToken cancellationToken = default);

    Task<ReconciliationWorkspaceDto> MatchStatementLineAsync(
        Guid companyId,
        Guid branchId,
        Guid statementId,
        MatchStatementLineRequest request,
        CancellationToken cancellationToken = default);

    Task<ReconciliationWorkspaceDto> UnmatchStatementLineAsync(
        Guid companyId,
        Guid branchId,
        Guid statementId,
        UnmatchStatementLineRequest request,
        CancellationToken cancellationToken = default);

    Task<BankStatementSummaryDto> CompleteReconciliationAsync(
        Guid companyId,
        Guid branchId,
        Guid statementId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public interface IFinancialReportingService
{
    Task<CashBookReportDto> GetCashBookAsync(
        Guid companyId,
        Guid branchId,
        Guid? accountId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<BankBookReportDto> GetBankBookAsync(
        Guid companyId,
        Guid branchId,
        Guid? bankAccountId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<FinanceSummaryKpiDto> GetSummaryKpisAsync(
        Guid companyId,
        Guid branchId,
        CancellationToken cancellationToken = default);
}
