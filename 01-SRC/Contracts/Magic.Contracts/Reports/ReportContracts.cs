namespace Magic.Contracts.Reports;

public sealed record ExecutiveDashboardDto(
    long TotalSalesPaise,
    long NetSalesPaise,
    long? GrossProfitPaise,
    string GrossProfitStatus, // "Calculated" or "Provisional - DEC-ACC-002 Unresolved"
    long TotalPurchasesPaise,
    long OperatingExpensesPaise,
    long? NetProfitPaise,
    string NetProfitStatus,
    long CashPositionPaise,
    long BankBalancesPaise,
    long AccountsReceivablePaise,
    long AccountsPayablePaise,
    decimal TotalInventoryQuantity,
    int LowStockAlertsCount,
    int PendingPurchaseOrdersCount,
    int PendingCustomerOrdersCount,
    int OutstandingSupplierInvoicesCount,
    int ActiveEmployeesCount,
    long LastMonthPayrollPaise,
    IReadOnlyList<BranchSalesPerformanceDto> BranchPerformance,
    IReadOnlyList<DailySalesTrendDto> DailyTrends,
    DateTime GeneratedAtUtc);

public sealed record BranchSalesPerformanceDto(
    Guid BranchId,
    string BranchCode,
    string BranchName,
    long SalesPaise,
    int OrderCount);

public sealed record DailySalesTrendDto(
    DateOnly Date,
    long SalesPaise,
    int TransactionCount);

public sealed record GenericReportRowDto(
    string Key,
    string Label,
    string? Category,
    decimal Quantity,
    long AmountPaise,
    long TaxAmountPaise,
    string? Status,
    string? Reference);

public sealed record ReportResultDto(
    string ReportTitle,
    string PeriodDescription,
    IReadOnlyList<GenericReportRowDto> Rows,
    long TotalAmountPaise,
    long TotalTaxAmountPaise,
    decimal TotalQuantity,
    int RecordCount,
    DateTime GeneratedAtUtc);

public sealed record ReportExportRequest(
    string ReportType,
    string Format, // "PDF", "XLSX", "CSV"
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid? BranchId,
    Guid? WarehouseId,
    Guid? CustomerId,
    Guid? SupplierId,
    string? Status);

public sealed record ReportExportResultDto(
    string FileName,
    string ContentType,
    byte[] Content,
    DateTime GeneratedAtUtc);

