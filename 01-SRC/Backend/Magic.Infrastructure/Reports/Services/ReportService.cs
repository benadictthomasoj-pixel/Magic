using System.Globalization;
using System.IO.Compression;
using System.Security;
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

        string format = (request.Format ?? "CSV").ToUpperInvariant();
        string ext = format switch
        {
            "XLSX" => "xlsx",
            "PDF" => "pdf",
            _ => "csv"
        };

        string mime = ext switch
        {
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "pdf" => "application/pdf",
            _ => "text/csv"
        };

        byte[] bytes = ext switch
        {
            "xlsx" => GenerateXlsx(report, userName),
            "pdf" => GeneratePdf(report, userName),
            _ => GenerateCsv(report, userName)
        };

        var fileName = $"{request.ReportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{ext}";
        return new ReportExportResultDto(fileName, mime, bytes, DateTime.UtcNow);
    }

    private static string EscapeXml(string? value)
    {
        return SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
    }

    private static byte[] GenerateCsv(ReportResultDto report, string userName)
    {
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
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateXlsx(ReportResultDto report, string userName)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            // 1. [Content_Types].xml
            var contentTypes = archive.CreateEntry("[Content_Types].xml");
            using (var writer = new StreamWriter(contentTypes.Open(), Encoding.UTF8))
            {
                writer.Write("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
            }

            // 2. _rels/.rels
            var rels = archive.CreateEntry("_rels/.rels");
            using (var writer = new StreamWriter(rels.Open(), Encoding.UTF8))
            {
                writer.Write("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            }

            // 3. xl/workbook.xml
            var workbook = archive.CreateEntry("xl/workbook.xml");
            using (var writer = new StreamWriter(workbook.Open(), Encoding.UTF8))
            {
                writer.Write("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Report" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            }

            // 4. xl/_rels/workbook.xml.rels
            var wbRels = archive.CreateEntry("xl/_rels/workbook.xml.rels");
            using (var writer = new StreamWriter(wbRels.Open(), Encoding.UTF8))
            {
                writer.Write("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
            }

            // 5. xl/worksheets/sheet1.xml
            var sheet = archive.CreateEntry("xl/worksheets/sheet1.xml");
            using (var writer = new StreamWriter(sheet.Open(), Encoding.UTF8))
            {
                writer.WriteLine("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
                writer.WriteLine("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
                writer.WriteLine("""<sheetData>""");

                int rowIdx = 1;
                // Title row
                writer.WriteLine($"""<row r="{rowIdx}"><c r="A{rowIdx}" t="inlineStr"><is><t>{EscapeXml(report.ReportTitle)}</t></is></c></row>""");
                rowIdx++;
                // Period row
                writer.WriteLine($"""<row r="{rowIdx}"><c r="A{rowIdx}" t="inlineStr"><is><t>Period: {EscapeXml(report.PeriodDescription)}</t></is></c></row>""");
                rowIdx++;
                // Metadata row
                writer.WriteLine($"""<row r="{rowIdx}"><c r="A{rowIdx}" t="inlineStr"><is><t>Generated: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC by {EscapeXml(userName)}</t></is></c></row>""");
                rowIdx++;
                // Header row
                writer.WriteLine($"""<row r="{rowIdx}">""" +
                    $"""<c r="A{rowIdx}" t="inlineStr"><is><t>Key</t></is></c>""" +
                    $"""<c r="B{rowIdx}" t="inlineStr"><is><t>Label</t></is></c>""" +
                    $"""<c r="C{rowIdx}" t="inlineStr"><is><t>Category</t></is></c>""" +
                    $"""<c r="D{rowIdx}" t="inlineStr"><is><t>Quantity</t></is></c>""" +
                    $"""<c r="E{rowIdx}" t="inlineStr"><is><t>Amount (INR)</t></is></c>""" +
                    $"""<c r="F{rowIdx}" t="inlineStr"><is><t>Tax (INR)</t></is></c>""" +
                    $"""<c r="G{rowIdx}" t="inlineStr"><is><t>Status</t></is></c>""" +
                    $"""<c r="H{rowIdx}" t="inlineStr"><is><t>Reference</t></is></c>""" +
                    """</row>""");
                rowIdx++;

                // Data rows
                foreach (var r in report.Rows)
                {
                    var amt = ((double)r.AmountPaise / 100).ToString("F2", CultureInfo.InvariantCulture);
                    var tax = ((double)r.TaxAmountPaise / 100).ToString("F2", CultureInfo.InvariantCulture);
                    writer.WriteLine($"""<row r="{rowIdx}">""" +
                        $"""<c r="A{rowIdx}" t="inlineStr"><is><t>{EscapeXml(r.Key)}</t></is></c>""" +
                        $"""<c r="B{rowIdx}" t="inlineStr"><is><t>{EscapeXml(r.Label)}</t></is></c>""" +
                        $"""<c r="C{rowIdx}" t="inlineStr"><is><t>{EscapeXml(r.Category)}</t></is></c>""" +
                        $"""<c r="D{rowIdx}"><v>{r.Quantity.ToString(CultureInfo.InvariantCulture)}</v></c>""" +
                        $"""<c r="E{rowIdx}"><v>{amt}</v></c>""" +
                        $"""<c r="F{rowIdx}"><v>{tax}</v></c>""" +
                        $"""<c r="G{rowIdx}" t="inlineStr"><is><t>{EscapeXml(r.Status)}</t></is></c>""" +
                        $"""<c r="H{rowIdx}" t="inlineStr"><is><t>{EscapeXml(r.Reference ?? "")}</t></is></c>""" +
                        """</row>""");
                    rowIdx++;
                }

                // Totals row
                var totalAmt = ((double)report.TotalAmountPaise / 100).ToString("F2", CultureInfo.InvariantCulture);
                var totalTax = ((double)report.TotalTaxAmountPaise / 100).ToString("F2", CultureInfo.InvariantCulture);
                writer.WriteLine($"""<row r="{rowIdx}">""" +
                    $"""<c r="A{rowIdx}" t="inlineStr"><is><t>TOTALS</t></is></c>""" +
                    $"""<c r="B{rowIdx}" t="inlineStr"><is><t></t></is></c>""" +
                    $"""<c r="C{rowIdx}" t="inlineStr"><is><t></t></is></c>""" +
                    $"""<c r="D{rowIdx}"><v>{report.TotalQuantity.ToString(CultureInfo.InvariantCulture)}</v></c>""" +
                    $"""<c r="E{rowIdx}"><v>{totalAmt}</v></c>""" +
                    $"""<c r="F{rowIdx}"><v>{totalTax}</v></c>""" +
                    $"""<c r="G{rowIdx}" t="inlineStr"><is><t>Records: {report.RecordCount}</t></is></c>""" +
                    $"""<c r="H{rowIdx}" t="inlineStr"><is><t></t></is></c>""" +
                    """</row>""");

                writer.WriteLine("""</sheetData>""");
                writer.WriteLine("""</worksheet>""");
            }
        }
        return ms.ToArray();
    }

    private static byte[] GeneratePdf(ReportResultDto report, string userName)
    {
        var contentLines = new List<string>
        {
            report.ReportTitle,
            $"Period: {report.PeriodDescription}",
            $"Generated by {userName} on {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC",
            "",
            "Key | Label | Category | Qty | Amount(INR) | Tax(INR) | Status | Ref",
            "----------------------------------------------------------------------"
        };

        foreach (var r in report.Rows.Take(40))
        {
            var amt = ((double)r.AmountPaise / 100).ToString("F2", CultureInfo.InvariantCulture);
            var tax = ((double)r.TaxAmountPaise / 100).ToString("F2", CultureInfo.InvariantCulture);
            contentLines.Add($"{r.Key} | {r.Label} | {r.Category} | {r.Quantity} | {amt} | {tax} | {r.Status} | {r.Reference}");
        }

        var totalAmt = ((double)report.TotalAmountPaise / 100).ToString("F2", CultureInfo.InvariantCulture);
        var totalTax = ((double)report.TotalTaxAmountPaise / 100).ToString("F2", CultureInfo.InvariantCulture);
        contentLines.Add("----------------------------------------------------------------------");
        contentLines.Add($"TOTALS: Qty: {report.TotalQuantity} | Amt: {totalAmt} | Tax: {totalTax} | Records: {report.RecordCount}");

        var sb = new StringBuilder();
        sb.AppendLine("BT");
        sb.AppendLine("/F1 9 Tf");
        sb.AppendLine("40 780 Td");
        sb.AppendLine("14 TL");

        bool first = true;
        foreach (var line in contentLines)
        {
            var sanitized = line.Replace("\\", "\\\\", StringComparison.Ordinal)
                                .Replace("(", "\\(", StringComparison.Ordinal)
                                .Replace(")", "\\)", StringComparison.Ordinal);
            if (first)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"({sanitized}) Tj");
                first = false;
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"T* ({sanitized}) Tj");
            }
        }
        sb.AppendLine("ET");

        var streamBytes = Encoding.Latin1.GetBytes(sb.ToString());
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, leaveOpen: true);

        writer.Write("%PDF-1.4\n");
        writer.Flush();
        var obj1Pos = ms.Position;
        writer.Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        writer.Flush();
        var obj2Pos = ms.Position;
        writer.Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        writer.Flush();
        var obj3Pos = ms.Position;
        writer.Write("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n");
        writer.Flush();
        var obj4Pos = ms.Position;
        writer.Write($"4 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
        writer.Flush();
        ms.Write(streamBytes);
        writer.Write("\nendstream\nendobj\n");
        writer.Flush();
        var obj5Pos = ms.Position;
        writer.Write("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        writer.Flush();
        var xrefPos = ms.Position;
        writer.Write("xref\n0 6\n0000000000 65535 f \n");
        writer.Write($"{obj1Pos:D10} 00000 n \n");
        writer.Write($"{obj2Pos:D10} 00000 n \n");
        writer.Write($"{obj3Pos:D10} 00000 n \n");
        writer.Write($"{obj4Pos:D10} 00000 n \n");
        writer.Write($"{obj5Pos:D10} 00000 n \n");
        writer.Write($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF");
        writer.Flush();

        return ms.ToArray();
    }
}
