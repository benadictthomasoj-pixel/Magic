using Magic.Contracts.Sales;

namespace Magic.Application.Sales.Services;

public record GstTaxBreakdown(
    long TaxableAmountPaise,
    decimal GstRate,
    long CgstPaise,
    long SgstPaise,
    long IgstPaise,
    long TotalTaxPaise);

public interface IGstCalculationService
{
    GstTaxBreakdown CalculateTax(long taxableAmountPaise, decimal gstRate, bool isInterState);
}

public interface IPosService
{
    Task<IReadOnlyList<PosProductLookupDto>> LookupProductsAsync(
        Guid companyId,
        Guid warehouseId,
        string query,
        CancellationToken cancellationToken = default);

    Task<SalesInvoiceDto> CheckoutAsync(
        Guid companyId,
        Guid branchId,
        Guid currentUserId,
        CreatePosSaleRequest request,
        CancellationToken cancellationToken = default);
}

public interface ISalesInvoiceService
{
    Task<(IReadOnlyList<SalesInvoiceDto> Items, int TotalCount)> GetInvoicesAsync(
        Guid companyId,
        Guid? branchId,
        string? search,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SalesInvoiceDto> GetInvoiceByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SalesInvoiceDto> CancelInvoiceAsync(
        Guid companyId,
        Guid id,
        Guid currentUserId,
        CancelInvoiceRequest request,
        CancellationToken cancellationToken = default);
}

public interface ISalesReturnService
{
    Task<(IReadOnlyList<SalesReturnDto> Items, int TotalCount)> GetReturnsAsync(
        Guid companyId,
        Guid? branchId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SalesReturnDto> GetReturnByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SalesReturnDto> CreateReturnAsync(
        Guid companyId,
        Guid branchId,
        Guid currentUserId,
        CreateSalesReturnRequest request,
        CancellationToken cancellationToken = default);

    Task<CreditNoteDto> GetCreditNoteByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CreditNoteDto> Items, int TotalCount)> GetCreditNotesAsync(
        Guid companyId,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface ICashierSessionService
{
    Task<CashierSessionDto?> GetActiveSessionAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CashierSessionDto> Items, int TotalCount)> GetSessionsAsync(
        Guid companyId,
        Guid? branchId,
        Guid? userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CashierSessionDto> GetSessionByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CashierSessionDto> OpenSessionAsync(
        Guid companyId,
        Guid branchId,
        Guid currentUserId,
        OpenCashierSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<CashMovementDto> RecordMovementAsync(
        Guid companyId,
        Guid sessionId,
        Guid currentUserId,
        RecordCashMovementRequest request,
        CancellationToken cancellationToken = default);

    Task<CashierSessionDto> CloseSessionAsync(
        Guid companyId,
        Guid sessionId,
        Guid currentUserId,
        CloseCashierSessionRequest request,
        CancellationToken cancellationToken = default);
}

public interface ISalesReportingService
{
    Task<SalesReportSummaryDto> GetSummaryAsync(
        Guid companyId,
        Guid? branchId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesByPaymentMethodDto>> GetPaymentBreakdownAsync(
        Guid companyId,
        Guid? branchId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesByProductDto>> GetTopSellingProductsAsync(
        Guid companyId,
        Guid? branchId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HourlySalesDto>> GetHourlySalesAsync(
        Guid companyId,
        Guid? branchId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default);
}
