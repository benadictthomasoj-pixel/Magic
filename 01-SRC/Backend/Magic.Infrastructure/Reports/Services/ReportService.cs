using System.Globalization;
using System.Text;
using Magic.Application.Reports.Services;
using Magic.Contracts.Reports;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Reports.Services;

public sealed class ReportService(MagicDbContext dbContext) : IReportService
{
    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(Guid companyId, Guid? branchId, CancellationToken cancellationToken = default)
    {
        // 1. Sales metrics from SalesInvoices
        var salesQuery = dbContext.SalesInvoices.AsNoTracking().Where(i => i.CompanyId == companyId);
        if (branchId.HasValue) salesQuery = salesQuery.Where(i => i.BranchId == branchId.Value);

        var invoices = await salesQuery.ToListAsync(cancellationToken);
        var totalSales = invoices.Sum(i => i.GrandTotalPaise);
        var netSales = invoices.Sum(i => i.SubtotalPaise);

        // 2. Purchases from SupplierInvoices / GRN
        var purchaseQuery = dbContext.SupplierInvoices.AsNoTracking().Where(p => p.CompanyId == companyId);
        var totalPurchases = await purchaseQuery.SumAsync(p => (long?)p.TotalAmountPaise, cancellationToken) ?? 0L;

        // 3. Operating Expenses from Expenses
        var expenseQuery = dbContext.Expenses.AsNoTracking().Where(e => e.CompanyId == companyId && e.Status == "Paid");
        if (branchId.HasValue) expenseQuery = expenseQuery.Where(e => e.BranchId == branchId.Value);
        var opex = await expenseQuery.SumAsync(e => (long?)e.TotalAmountPaise, cancellationToken) ?? 0L;

        // 4. DEC-ACC-002 Invariant: Gross & Net Profit require approved inventory valuation
        // Until DEC-ACC-002 is resolved, inventory COGS is provisional
        long? grossProfit = netSales - totalPurchases;
        string gpStatus = "Provisional - DEC-ACC-002 (Inventory Valuation) Unresolved";
        long? netProfit = grossProfit - opex;
        string npStatus = "Provisional - DEC-ACC-002 Unresolved";

        // 5. Cash & Bank balances from BankCashAccounts
        var accountsQuery = dbContext.BankCashAccounts.AsNoTracking().Where(b => b.CompanyId == companyId);
        if (branchId.HasValue) accountsQuery = accountsQuery.Where(b => b.BranchId == branchId.Value);
        var bankAccounts = await accountsQuery.ToListAsync(cancellationToken);
        var cashPosition = bankAccounts.Where(b => b.AccountType == "Cash").Sum(b => b.CurrentBalancePaise);
        var bankBalances = bankAccounts.Where(b => b.AccountType != "Cash").Sum(b => b.CurrentBalancePaise);

        // 6. AR & AP from Subledgers / Invoices
        var arQuery = dbContext.SalesInvoices.AsNoTracking().Where(i => i.CompanyId == companyId && (i.Status == "Unpaid" || i.Status == "PartiallyPaid"));
        var apQuery = dbContext.SupplierInvoices.AsNoTracking().Where(s => s.CompanyId == companyId && (s.Status == "APPROVED" || s.Status == "PARTIALLY_PAID"));
        var accountsReceivable = await arQuery.SumAsync(i => (long?)i.OutstandingBalancePaise, cancellationToken) ?? 0L;
        var accountsPayable = await apQuery.SumAsync(s => (long?)(s.TotalAmountPaise - s.SettledAmountPaise), cancellationToken) ?? 0L;

        // 7. Inventory & Stock Alerts
        var stockQuery = dbContext.StockBalances.AsNoTracking().Where(s => s.CompanyId == companyId);
        if (branchId.HasValue) stockQuery = stockQuery.Where(s => s.BranchId == branchId.Value);
        var stockItems = await stockQuery.ToListAsync(cancellationToken);
        var totalStockQty = stockItems.Sum(s => s.QuantityOnHand);

        var reorderRules = await dbContext.ReorderRules.AsNoTracking().Where(r => r.CompanyId == companyId).ToListAsync(cancellationToken);
        int lowStockCount = 0;
        foreach (var rule in reorderRules)
        {
            var bal = stockItems.FirstOrDefault(s => s.WarehouseId == rule.WarehouseId && s.ProductId == rule.ProductId);
            if ((bal?.QuantityOnHand ?? 0m) <= rule.ReorderPoint) lowStockCount++;
        }

        // 8. Pending POs & Orders
        var pendingPOs = await dbContext.PurchaseOrders.AsNoTracking().CountAsync(p => p.CompanyId == companyId && p.Status == "Submitted", cancellationToken);
        var pendingOrders = await dbContext.SalesOrders.AsNoTracking().CountAsync(o => o.CompanyId == companyId && (o.Status == "Submitted" || o.Status == "Confirmed"), cancellationToken);
        var pendingSupplierInvoices = await dbContext.SupplierInvoices.AsNoTracking().CountAsync(s => s.CompanyId == companyId && s.Status != "Paid", cancellationToken);

        // 9. HR & Payroll
        var activeEmployees = await dbContext.Employees.AsNoTracking().CountAsync(e => e.CompanyId == companyId && e.Status == "Active", cancellationToken);
        var lastPayroll = await dbContext.PayrollPeriods.AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Status == "Finalized")
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        long lastPayrollPaise = lastPayroll?.TotalGrossPaise ?? 0L;

        // 10. Branch Performance
        var branchPerformance = invoices
            .GroupBy(i => i.BranchId)
            .Select(g => new BranchSalesPerformanceDto(
                g.Key,
                "BRANCH-" + g.Key.ToString()[..4].ToUpperInvariant(),
                "Branch " + g.Key.ToString()[..4],
                g.Sum(x => x.GrandTotalPaise),
                g.Count()))
            .ToList();

        // 11. Daily Trends (last 14 days)
        var fourteenDaysAgo = DateTimeOffset.UtcNow.AddDays(-14);
        var dailyTrends = invoices
            .Where(i => i.InvoiceDateUtc >= fourteenDaysAgo)
            .GroupBy(i => DateOnly.FromDateTime(i.InvoiceDateUtc.DateTime))
            .OrderBy(g => g.Key)
            .Select(g => new DailySalesTrendDto(
                g.Key,
                g.Sum(x => x.GrandTotalPaise),
                g.Count()))
            .ToList();

        return new ExecutiveDashboardDto(
            totalSales,
            netSales,
            grossProfit,
            gpStatus,
            totalPurchases,
            opex,
            netProfit,
            npStatus,
            cashPosition,
            bankBalances,
            accountsReceivable,
            accountsPayable,
            totalStockQty,
            lowStockCount,
            pendingPOs,
            pendingOrders,
            pendingSupplierInvoices,
            activeEmployees,
            lastPayrollPaise,
            branchPerformance,
            dailyTrends,
            DateTime.UtcNow);
    }

    public async Task<ReportResultDto> GetSalesReportAsync(Guid companyId, DateOnly startDate, DateOnly endDate, Guid? branchId, string? groupByType, CancellationToken cancellationToken = default)
    {
        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var query = dbContext.SalesInvoices.AsNoTracking().Where(i => i.CompanyId == companyId && i.InvoiceDateUtc >= startUtc && i.InvoiceDateUtc <= endUtc);
        if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId.Value);

        var invoices = await query.Include(i => i.Lines).Include(i => i.Customer).ToListAsync(cancellationToken);

        var rows = new List<GenericReportRowDto>();
        long totalAmount = invoices.Sum(i => i.GrandTotalPaise);
        long totalTax = invoices.Sum(i => i.TotalTaxPaise);
        decimal totalQty = 0;

        if (string.Equals(groupByType, "product", StringComparison.OrdinalIgnoreCase))
        {
            var productLines = invoices.SelectMany(i => i.Lines).GroupBy(l => l.ProductId);
            foreach (var g in productLines)
            {
                var prod = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == g.Key, cancellationToken);
                var qty = g.Sum(l => l.Quantity);
                var amt = g.Sum(l => l.LineTotalPaise);
                var tax = g.Sum(l => l.TaxAmountPaise);
                totalQty += qty;
                rows.Add(new GenericReportRowDto(
                    g.Key.ToString(),
                    prod?.Name ?? "Product " + g.Key.ToString()[..6],
                    prod?.ProductCode ?? "SKU",
                    qty,
                    amt,
                    tax,
                    "Completed",
                    prod?.ProductCode));
            }
        }
        else
        {
            foreach (var inv in invoices)
            {
                var qty = inv.Lines.Sum(l => l.Quantity);
                totalQty += qty;
                rows.Add(new GenericReportRowDto(
                    inv.Id.ToString(),
                    inv.InvoiceNumber,
                    inv.Customer?.Name ?? "Retail Customer",
                    qty,
                    inv.GrandTotalPaise,
                    inv.TotalTaxPaise,
                    inv.Status,
                    inv.InvoiceDateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            }
        }

        return new ReportResultDto(
            "Sales Analysis Report",
            FormattableString.Invariant($"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"),
            rows,
            totalAmount,
            totalTax,
            totalQty,
            rows.Count,
            DateTime.UtcNow);
    }

    public async Task<ReportResultDto> GetPurchaseReportAsync(Guid companyId, DateOnly startDate, DateOnly endDate, Guid? supplierId, CancellationToken cancellationToken = default)
    {
        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var query = dbContext.PurchaseOrders.AsNoTracking().Where(p => p.CompanyId == companyId && p.OrderDateUtc >= startUtc && p.OrderDateUtc <= endUtc);
        if (supplierId.HasValue) query = query.Where(p => p.SupplierId == supplierId.Value);

        var pos = await query.Include(p => p.Lines).ToListAsync(cancellationToken);
        var rows = new List<GenericReportRowDto>();
        long totalAmount = pos.Sum(p => p.TotalAmountPaise);
        long totalTax = pos.Sum(p => p.CgstPaise + p.SgstPaise + p.IgstPaise);
        decimal totalQty = pos.SelectMany(p => p.Lines).Sum(l => l.OrderedQuantity);

        foreach (var po in pos)
        {
            var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == po.SupplierId, cancellationToken);
            rows.Add(new GenericReportRowDto(
                po.Id.ToString(),
                po.OrderNumber,
                supplier?.Name ?? "Supplier",
                po.Lines.Sum(l => l.OrderedQuantity),
                po.TotalAmountPaise,
                po.CgstPaise + po.SgstPaise + po.IgstPaise,
                po.Status,
                po.OrderDateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        return new ReportResultDto(
            "Purchase Register Report",
            FormattableString.Invariant($"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"),
            rows,
            totalAmount,
            totalTax,
            totalQty,
            rows.Count,
            DateTime.UtcNow);
    }

    public async Task<ReportResultDto> GetInventoryReportAsync(Guid companyId, Guid? warehouseId, bool? lowStockOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockBalances.AsNoTracking().Where(s => s.CompanyId == companyId);
        if (warehouseId.HasValue) query = query.Where(s => s.WarehouseId == warehouseId.Value);

        var balances = await query.ToListAsync(cancellationToken);
        var reorders = await dbContext.ReorderRules.AsNoTracking().Where(r => r.CompanyId == companyId).ToListAsync(cancellationToken);

        var rows = new List<GenericReportRowDto>();
        decimal totalQty = 0;
        long totalValuation = 0;

        foreach (var bal in balances)
        {
            var rule = reorders.FirstOrDefault(r => r.WarehouseId == bal.WarehouseId && r.ProductId == bal.ProductId);
            var isLow = rule != null && bal.QuantityOnHand <= rule.ReorderPoint;

            if (lowStockOnly == true && !isLow) continue;

            var prod = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == bal.ProductId, cancellationToken);
            var warehouse = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == bal.WarehouseId, cancellationToken);

            totalQty += bal.QuantityOnHand;
            totalValuation += (long)(bal.QuantityOnHand * bal.UnitCostPaise);

            rows.Add(new GenericReportRowDto(
                bal.Id.ToString(),
                prod?.Name ?? "Product " + bal.ProductId.ToString()[..6],
                warehouse?.Name ?? "Main Warehouse",
                bal.QuantityOnHand,
                (long)(bal.QuantityOnHand * bal.UnitCostPaise),
                0,
                isLow ? "Low Stock" : "Normal",
                prod?.ProductCode));
        }

        return new ReportResultDto(
            lowStockOnly == true ? "Reorder & Low Stock Alert Report" : "Inventory Status & Valuation Report",
            "Current Active Stock",
            rows,
            totalValuation,
            0,
            totalQty,
            rows.Count,
            DateTime.UtcNow);
    }

    public async Task<ReportResultDto> GetFinancialSummaryReportAsync(Guid companyId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var vouchers = await dbContext.Vouchers.AsNoTracking()
            .Include(v => v.Lines)
            .Where(v => v.CompanyId == companyId && v.VoucherDate >= startDate && v.VoucherDate <= endDate)
            .ToListAsync(cancellationToken);

        var rows = new List<GenericReportRowDto>();
        long totalDebit = 0;
        long totalCredit = 0;

        foreach (var v in vouchers)
        {
            var debit = v.Lines.Sum(l => l.Debit.Paise);
            var credit = v.Lines.Sum(l => l.Credit.Paise);
            totalDebit += debit;
            totalCredit += credit;

            rows.Add(new GenericReportRowDto(
                v.Id.ToString(),
                v.VoucherNumber,
                v.VoucherType.ToString(),
                v.Lines.Count,
                debit,
                credit,
                v.Status.ToString(),
                v.VoucherDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        return new ReportResultDto(
            "General Ledger Financial Summary",
            FormattableString.Invariant($"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"),
            rows,
            totalDebit,
            totalCredit,
            vouchers.Count,
            rows.Count,
            DateTime.UtcNow);
    }

    public async Task<ReportResultDto> GetHrPayrollReportAsync(Guid companyId, int? month, int? year, CancellationToken cancellationToken = default)
    {
        var empQuery = dbContext.Employees.AsNoTracking().Include(e => e.Department).Where(e => e.CompanyId == companyId);
        var employees = await empQuery.ToListAsync(cancellationToken);

        var rows = new List<GenericReportRowDto>();
        foreach (var emp in employees)
        {
            rows.Add(new GenericReportRowDto(
                emp.Id.ToString(),
                $"{emp.FirstName} {emp.LastName}",
                emp.Department?.Name ?? "General",
                1,
                0,
                0,
                emp.Status,
                emp.EmployeeCode));
        }

        return new ReportResultDto(
            "HR & Employee Master Directory",
            FormattableString.Invariant($"Total Headcount: {employees.Count}"),
            rows,
            0,
            0,
            employees.Count,
            rows.Count,
            DateTime.UtcNow);
    }

    public async Task<ReportResultDto> GetMarketingOrdersServicesReportAsync(Guid companyId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.SalesOrders.AsNoTracking()
            .Where(o => o.CompanyId == companyId && o.OrderDate >= startDate && o.OrderDate <= endDate)
            .ToListAsync(cancellationToken);

        var services = await dbContext.ServiceRequests.AsNoTracking()
            .Where(s => s.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        var rows = new List<GenericReportRowDto>();
        long totalVal = 0;

        foreach (var o in orders)
        {
            totalVal += o.TotalAmountPaise;
            rows.Add(new GenericReportRowDto(
                o.Id.ToString(),
                o.OrderNumber,
                "Sales Order",
                o.Lines.Count,
                o.TotalAmountPaise,
                o.TaxAmountPaise,
                o.Status,
                o.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }

        foreach (var s in services)
        {
            totalVal += 50000;
            rows.Add(new GenericReportRowDto(
                s.Id.ToString(),
                s.RequestNumber,
                "Service Request",
                1,
                50000,
                0,
                s.Status,
                s.Priority));
        }

        return new ReportResultDto(
            "Orders & Service Operations Report",
            FormattableString.Invariant($"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"),
            rows,
            totalVal,
            0,
            rows.Count,
            rows.Count,
            DateTime.UtcNow);
    }

    public async Task<ReportExportResultDto> ExportReportAsync(Guid companyId, ReportExportRequest request, Guid userId, string userName, CancellationToken cancellationToken = default)
    {
        var start = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var end = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        ReportResultDto report = request.ReportType.ToLowerInvariant() switch
        {
            "purchase" => await GetPurchaseReportAsync(companyId, start, end, request.SupplierId, cancellationToken),
            "inventory" => await GetInventoryReportAsync(companyId, request.WarehouseId, false, cancellationToken),
            "financial" => await GetFinancialSummaryReportAsync(companyId, start, end, cancellationToken),
            "hr" => await GetHrPayrollReportAsync(companyId, null, null, cancellationToken),
            "operations" => await GetMarketingOrdersServicesReportAsync(companyId, start, end, cancellationToken),
            _ => await GetSalesReportAsync(companyId, start, end, request.BranchId, null, cancellationToken)
        };

        // Audit report export in reporting.report_export_logs
        dbContext.ReportExportLogs.Add(new ReportExportLogRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ReportCode = request.ReportType,
            ExportFormat = request.Format,
            FilterParametersJson = $"{{\"startDate\":\"{start}\",\"endDate\":\"{end}\"}}",
            ExportedByUserId = userId,
            ExportedByUserName = userName,
            ExportedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        string ext = request.Format.ToUpperInvariant() switch
        {
            "CSV" => "csv",
            "XLSX" => "xlsx",
            _ => "pdf"
        };

        string mime = ext switch
        {
            "csv" => "text/csv",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/pdf"
        };

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# {report.ReportTitle}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Period: {report.PeriodDescription}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Generated By: {userName} on {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("Key,Label,Category,Quantity,Amount(INR),Tax(INR),Status,Reference");

        foreach (var r in report.Rows)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"\"{r.Key}\",\"{r.Label}\",\"{r.Category}\",{r.Quantity},{(double)r.AmountPaise / 100:F2},{(double)r.TaxAmountPaise / 100:F2},\"{r.Status}\",\"{r.Reference}\"");
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"# TOTALS,,,,{(double)report.TotalAmountPaise / 100:F2},{(double)report.TotalTaxAmountPaise / 100:F2},Records: {report.RecordCount}");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"{request.ReportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{ext}";

        return new ReportExportResultDto(fileName, mime, bytes, DateTime.UtcNow);
    }
}
