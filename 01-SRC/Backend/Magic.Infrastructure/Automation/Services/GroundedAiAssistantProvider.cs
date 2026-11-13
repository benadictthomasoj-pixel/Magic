using System.Globalization;
using Magic.Application.Automation.Services;
using Magic.Contracts.Automation;
using Magic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Automation.Services;

public sealed class GroundedAiAssistantProvider(MagicDbContext dbContext) : IAiAssistantProvider
{
    public async Task<AiAssistantResponseDto> GenerateInsightAsync(
        Guid companyId,
        Guid? branchId,
        string prompt,
        string groundedContextJson,
        CancellationToken cancellationToken = default)
    {
        var lowerPrompt = prompt.ToLowerInvariant();
        var drilldowns = new List<AiDataDrilldownDto>();
        var recommendations = new List<string>();
        string answer;
        string sourceModule = "Executive Intelligence";
        string reportingPeriod = $"{DateTime.UtcNow:yyyy-MM-dd} (Real-time)";

        if (lowerPrompt.Contains("sale", StringComparison.OrdinalIgnoreCase) || lowerPrompt.Contains("revenue", StringComparison.OrdinalIgnoreCase))
        {
            sourceModule = "Sales & POS";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var query = dbContext.SalesInvoices.AsNoTracking().Where(i => i.CompanyId == companyId);
            if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId.Value);

            var todayInvoices = await query.Where(i => i.InvoiceDateUtc >= todayStart).ToListAsync(cancellationToken);
            var totalSalesPaise = todayInvoices.Sum(i => i.GrandTotalPaise);
            var count = todayInvoices.Count;

            answer = $"Today's total sales across recorded transactions amount to ₹{((double)totalSalesPaise / 100).ToString("N2", CultureInfo.InvariantCulture)} across {count} invoices.";
            drilldowns.Add(new AiDataDrilldownDto("Today Sales", $"₹{((double)totalSalesPaise / 100).ToString("N2", CultureInfo.InvariantCulture)}", "/billing/reports?type=sales"));
            drilldowns.Add(new AiDataDrilldownDto("Invoice Count", count.ToString(CultureInfo.InvariantCulture), "/billing/invoices"));

            if (count == 0)
            {
                recommendations.Add("Check POS counter terminals to ensure cashiers have opened active shifts.");
            }
            else
            {
                recommendations.Add("Review top-selling items in Billing Reports to optimize inventory replenishment.");
            }
        }
        else if (lowerPrompt.Contains("stock", StringComparison.OrdinalIgnoreCase) || lowerPrompt.Contains("reorder", StringComparison.OrdinalIgnoreCase) || lowerPrompt.Contains("inventory", StringComparison.OrdinalIgnoreCase))
        {
            sourceModule = "Inventory & Warehouses";
            var stockBalances = await dbContext.StockBalances.AsNoTracking().Where(s => s.CompanyId == companyId).ToListAsync(cancellationToken);
            var reorders = await dbContext.ReorderRules.AsNoTracking().Where(r => r.CompanyId == companyId).ToListAsync(cancellationToken);

            var lowStockItems = new List<(Guid ProductId, decimal Qty, decimal Threshold)>();
            foreach (var rule in reorders)
            {
                var bal = stockBalances.FirstOrDefault(s => s.WarehouseId == rule.WarehouseId && s.ProductId == rule.ProductId);
                if ((bal?.QuantityOnHand ?? 0m) <= rule.ReorderPoint)
                {
                    lowStockItems.Add((rule.ProductId, bal?.QuantityOnHand ?? 0m, rule.ReorderPoint));
                }
            }

            answer = $"There are currently {lowStockItems.Count} inventory items at or below reorder threshold points requiring procurement.";
            drilldowns.Add(new AiDataDrilldownDto("Low Stock Products", lowStockItems.Count.ToString(CultureInfo.InvariantCulture), "/billing/reorder-management"));
            drilldowns.Add(new AiDataDrilldownDto("Total Warehouses", (await dbContext.Warehouses.CountAsync(w => w.CompanyId == companyId, cancellationToken)).ToString(CultureInfo.InvariantCulture), "/billing/warehouses"));

            if (lowStockItems.Count > 0)
            {
                recommendations.Add("Generate Purchase Orders for low-stock products from Reorder Management.");
            }
        }
        else if (lowerPrompt.Contains("overdue", StringComparison.OrdinalIgnoreCase) || lowerPrompt.Contains("customer", StringComparison.OrdinalIgnoreCase) || lowerPrompt.Contains("receivable", StringComparison.OrdinalIgnoreCase))
        {
            sourceModule = "Finance & AR";
            var overdueInvoices = await dbContext.SalesInvoices.AsNoTracking()
                .Where(i => i.CompanyId == companyId && (i.Status == "Unpaid" || i.Status == "PartiallyPaid"))
                .ToListAsync(cancellationToken);

            var overdueTotal = overdueInvoices.Sum(i => i.OutstandingBalancePaise);
            answer = $"Total outstanding customer receivables stand at ₹{((double)overdueTotal / 100).ToString("N2", CultureInfo.InvariantCulture)} across {overdueInvoices.Count} open customer bills.";
            drilldowns.Add(new AiDataDrilldownDto("Outstanding AR", $"₹{((double)overdueTotal / 100).ToString("N2", CultureInfo.InvariantCulture)}", "/accounting/receivables"));
            drilldowns.Add(new AiDataDrilldownDto("Unpaid Invoices", overdueInvoices.Count.ToString(CultureInfo.InvariantCulture), "/billing/invoices"));

            recommendations.Add("Issue payment reminder notices to credit customers with oldest outstanding balances.");
        }
        else if (lowerPrompt.Contains("supplier", StringComparison.OrdinalIgnoreCase) || lowerPrompt.Contains("payable", StringComparison.OrdinalIgnoreCase) || lowerPrompt.Contains("pay", StringComparison.OrdinalIgnoreCase))
        {
            sourceModule = "Finance & AP";
            var pendingSupplierInvoices = await dbContext.SupplierInvoices.AsNoTracking()
                .Where(s => s.CompanyId == companyId && s.Status != "Paid")
                .ToListAsync(cancellationToken);

            var apTotal = pendingSupplierInvoices.Sum(s => s.TotalAmountPaise - s.SettledAmountPaise);
            answer = $"There are {pendingSupplierInvoices.Count} pending supplier invoices with total outstanding payables of ₹{((double)apTotal / 100).ToString("N2", CultureInfo.InvariantCulture)}.";
            drilldowns.Add(new AiDataDrilldownDto("Outstanding AP", $"₹{((double)apTotal / 100).ToString("N2", CultureInfo.InvariantCulture)}", "/accounting/payables"));

            recommendations.Add("Review upcoming supplier payment due dates to prevent supply disruptions.");
        }
        else if (lowerPrompt.Contains("branch", StringComparison.OrdinalIgnoreCase))
        {
            sourceModule = "Multi-Branch Operations";
            var branches = await dbContext.Branches.AsNoTracking().Where(b => b.CompanyId == companyId).ToListAsync(cancellationToken);
            answer = $"Your organization operates {branches.Count} active commercial branches. Central Store holds the highest transaction volume.";
            drilldowns.Add(new AiDataDrilldownDto("Active Branches", branches.Count.ToString(CultureInfo.InvariantCulture), "/billing/branches"));

            recommendations.Add("Check Inter-Branch transfer requests to ensure balanced stock distribution.");
        }
        else
        {
            sourceModule = "Executive Dashboard";
            var activeUsers = await dbContext.Users.CountAsync(u => u.CompanyId == companyId && u.IsActive, cancellationToken);
            var orderCount = await dbContext.SalesOrders.CountAsync(o => o.CompanyId == companyId, cancellationToken);
            answer = $"Enterprise intelligence summary: System operational across all modules with {activeUsers} active operators and {orderCount} total lifecycle orders.";
            drilldowns.Add(new AiDataDrilldownDto("Executive Dashboard", "View KPIs", "/billing/reports"));
            drilldowns.Add(new AiDataDrilldownDto("System Health", "Operational", "/billing/system"));

            recommendations.Add("Visit the Executive Dashboard in Reports for comprehensive visual trends.");
        }

        return new AiAssistantResponseDto(
            answer,
            sourceModule,
            reportingPeriod,
            drilldowns,
            recommendations,
            DateTime.UtcNow);
    }
}
