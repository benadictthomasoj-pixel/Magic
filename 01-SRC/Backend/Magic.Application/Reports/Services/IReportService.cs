using Magic.Contracts.Reports;

namespace Magic.Application.Reports.Services;

public interface IReportService
{
    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(Guid companyId, Guid? branchId, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetSalesReportAsync(Guid companyId, DateOnly startDate, DateOnly endDate, Guid? branchId, string? groupByType, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetPurchaseReportAsync(Guid companyId, DateOnly startDate, DateOnly endDate, Guid? supplierId, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetInventoryReportAsync(Guid companyId, Guid? warehouseId, bool? lowStockOnly, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetFinancialSummaryReportAsync(Guid companyId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetHrPayrollReportAsync(Guid companyId, int? month, int? year, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetMarketingOrdersServicesReportAsync(Guid companyId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<ReportExportResultDto> ExportReportAsync(Guid companyId, ReportExportRequest request, Guid userId, string userName, CancellationToken cancellationToken = default);
}
