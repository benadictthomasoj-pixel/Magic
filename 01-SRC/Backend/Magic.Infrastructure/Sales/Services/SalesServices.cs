using System.Globalization;
using Magic.Application.Accounting.Services;
using Magic.Application.Sales.Services;
using Magic.Contracts.Sales;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Sales.Services;

// ==========================================
// 1. GST Calculation Service (DEC-ACC-003)
// ==========================================

public sealed class GstCalculationService : IGstCalculationService
{
    public GstTaxBreakdown CalculateTax(long taxableAmountPaise, decimal gstRate, bool isInterState)
    {
        if (taxableAmountPaise <= 0 || gstRate <= 0m)
        {
            return new GstTaxBreakdown(taxableAmountPaise, gstRate, 0, 0, 0, 0);
        }

        // Standard statutory rounding to nearest paise (DEC-ACC-003)
        var totalTax = (long)Math.Round((taxableAmountPaise * gstRate) / 100m, MidpointRounding.AwayFromZero);

        if (isInterState)
        {
            return new GstTaxBreakdown(taxableAmountPaise, gstRate, 0, 0, totalTax, totalTax);
        }

        // Intra-state split 50/50 between CGST and SGST with deterministic attribution of remainder
        var cgst = totalTax / 2;
        var sgst = totalTax - cgst;

        return new GstTaxBreakdown(taxableAmountPaise, gstRate, cgst, sgst, 0, totalTax);
    }
}

// ==========================================
// 2. POS Service
// ==========================================

public sealed class PosService(
    MagicDbContext dbContext,
    IGstCalculationService gstService,
    IPostingService postingService) : IPosService
{
    public async Task<IReadOnlyList<PosProductLookupDto>> LookupProductsAsync(
        Guid companyId,
        Guid warehouseId,
        string query,
        CancellationToken cancellationToken = default)
    {
        var term = (query ?? string.Empty).Trim();

        var productsQuery = dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Barcodes)
            .Where(p => p.CompanyId == companyId && p.IsActive);

        if (!string.IsNullOrWhiteSpace(term))
        {
            productsQuery = productsQuery.Where(p =>
                EF.Functions.ILike(p.ProductCode, $"%{term}%") ||
                EF.Functions.ILike(p.Name, $"%{term}%") ||
                EF.Functions.ILike(p.Sku, $"%{term}%") ||
                (p.PrimaryBarcode != null && EF.Functions.ILike(p.PrimaryBarcode, $"%{term}%")) ||
                p.Barcodes.Any(b => EF.Functions.ILike(b.Barcode, $"%{term}%")));
        }

        var products = await productsQuery
            .OrderBy(p => p.Name)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return [];
        }

        var productIds = products.Select(p => p.Id).ToList();
        var stockBalances = await dbContext.StockBalances
            .Where(b => b.CompanyId == companyId && b.WarehouseId == warehouseId && productIds.Contains(b.ProductId))
            .ToDictionaryAsync(b => b.ProductId, cancellationToken);

        return products.Select(p =>
        {
            stockBalances.TryGetValue(p.Id, out var balance);
            return new PosProductLookupDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Barcode = p.PrimaryBarcode ?? p.Barcodes.FirstOrDefault()?.Barcode,
                CategoryName = p.Category?.Name ?? "General",
                UnitOfMeasure = p.UnitOfMeasure?.Code ?? "UNIT",
                SellingPricePaise = p.SellingPricePaise,
                GstRatePercentage = p.GstRatePercentage,
                HsnCode = p.HsnSacCode,
                QuantityOnHand = balance?.QuantityOnHand ?? 0m,
                QuantityAvailable = balance?.QuantityAvailable ?? 0m,
                DefaultStorageLocationId = balance?.StorageLocationId
            };
        }).ToList();
    }

    public async Task<SalesInvoiceDto> CheckoutAsync(
        Guid companyId,
        Guid branchId,
        Guid currentUserId,
        CreatePosSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Checkout requires at least one sale line item.");
        }

        if (request.Payments.Count == 0 && !request.CustomerId.HasValue)
        {
            throw new InvalidOperationException("Checkout requires at least one payment method tender, or a registered customer for credit billing.");
        }

        var warehouse = await dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == request.WarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse with ID '{request.WarehouseId}' was not found.");

        CashierSessionRecord? session = null;
        if (request.CashierSessionId.HasValue)
        {
            session = await dbContext.CashierSessions
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.BranchId == branchId && s.Id == request.CashierSessionId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Cashier session '{request.CashierSessionId.Value}' was not found.");

            if (session.Status != "Open")
            {
                throw new InvalidOperationException($"Cashier session is '{session.Status}'. POS sales require an active 'Open' session.");
            }
        }

        CustomerRecord? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await dbContext.Customers
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == request.CustomerId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Customer '{request.CustomerId.Value}' was not found.");
        }

        var sellerStateCode = !string.IsNullOrWhiteSpace(warehouse.State) ? warehouse.State : "KA";
        var supplyStateCode = request.PlaceOfSupplyStateCode ?? customer?.Addresses.FirstOrDefault()?.State ?? sellerStateCode;
        var isInterState = !string.Equals(sellerStateCode, supplyStateCode, StringComparison.OrdinalIgnoreCase);

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .Where(p => p.CompanyId == companyId && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var pId in productIds)
        {
            if (!products.TryGetValue(pId, out var prod) || !prod.IsActive)
            {
                throw new InvalidOperationException($"Product with ID '{pId}' was not found or is inactive.");
            }
        }

        var stockBalances = await dbContext.StockBalances
            .Where(b => b.CompanyId == companyId && b.WarehouseId == request.WarehouseId && productIds.Contains(b.ProductId))
            .ToDictionaryAsync(b => b.ProductId, cancellationToken);

        // Pre-validate stock availability for all lines
        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Line quantity must be greater than zero.");
            }

            var prod = products[line.ProductId];
            stockBalances.TryGetValue(line.ProductId, out var balance);
            var onHand = balance?.QuantityOnHand ?? 0m;
            if (onHand < line.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for product '{prod.Name}' (SKU: {prod.Sku}). Available: {onHand.ToString(CultureInfo.InvariantCulture)}, Requested: {line.Quantity.ToString(CultureInfo.InvariantCulture)}.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var invoiceId = Guid.NewGuid();
        var invoiceNumber = $"INV-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        long subtotalPaise = 0;
        long discountPaise = 0;
        long cgstPaise = 0;
        long sgstPaise = 0;
        long igstPaise = 0;
        long totalTaxPaise = 0;
        long grandTotalPaise = 0;

        var invoiceLines = new List<SalesInvoiceLineRecord>();

        foreach (var line in request.Lines)
        {
            var prod = products[line.ProductId];
            var unitPrice = line.UnitPriceOverridePaise ?? prod.SellingPricePaise;
            var lineSubtotal = unitPrice * (long)line.Quantity;
            var lineDiscount = line.DiscountPaise;
            var lineTaxable = Math.Max(0, lineSubtotal - lineDiscount);

            var taxBreakdown = gstService.CalculateTax(lineTaxable, prod.GstRatePercentage, isInterState);
            var lineTotal = lineTaxable + taxBreakdown.TotalTaxPaise;

            subtotalPaise += lineSubtotal;
            discountPaise += lineDiscount;
            cgstPaise += taxBreakdown.CgstPaise;
            sgstPaise += taxBreakdown.SgstPaise;
            igstPaise += taxBreakdown.IgstPaise;
            totalTaxPaise += taxBreakdown.TotalTaxPaise;
            grandTotalPaise += lineTotal;

            invoiceLines.Add(new SalesInvoiceLineRecord
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                ProductId = prod.Id,
                ProductCode = prod.ProductCode,
                ProductName = prod.Name,
                HsnCode = prod.HsnSacCode,
                Quantity = line.Quantity,
                UnitPricePaise = unitPrice,
                DiscountPercentage = line.DiscountPercentage,
                DiscountPaise = lineDiscount,
                GstRatePercentage = prod.GstRatePercentage,
                CgstPercentage = isInterState ? 0m : prod.GstRatePercentage / 2m,
                SgstPercentage = isInterState ? 0m : prod.GstRatePercentage / 2m,
                IgstPercentage = isInterState ? prod.GstRatePercentage : 0m,
                TaxAmountPaise = taxBreakdown.TotalTaxPaise,
                LineTotalPaise = lineTotal,
                StorageLocationId = line.StorageLocationId,
                BatchNumber = line.BatchNumber,
                SerialNumber = line.SerialNumber
            });
        }

        var totalPaidPaise = request.Payments.Sum(p => p.AmountPaise);
        long outstandingBalancePaise = 0;
        long amountPaidPaise = 0;
        long changeGivenPaise = 0;
        string invoiceStatus = "Paid";

        if (totalPaidPaise < grandTotalPaise)
        {
            // Customer Credit Sale Validation (R2 / DEF-DE-002)
            if (customer == null)
            {
                throw new DomainException("CUSTOMER_REQUIRED_FOR_CREDIT", "Credit sales require a registered customer with an active account.");
            }

            if (customer.IsCreditHold)
            {
                throw new DomainException("CUSTOMER_CREDIT_HOLD", $"Customer '{customer.Name}' is on credit hold. Credit sale not permitted.");
            }

            if (customer.CreditLimitPaise <= 0)
            {
                throw new DomainException("CUSTOMER_NO_CREDIT_LIMIT", $"Customer '{customer.Name}' does not have an approved credit limit.");
            }

            var currentArOutstanding = await dbContext.ArSubledgerItems
                .Where(i => i.CompanyId == companyId && i.CustomerId == customer.Id && i.Status != "PAID")
                .SumAsync(i => i.OutstandingAmountPaise, cancellationToken);

            outstandingBalancePaise = grandTotalPaise - totalPaidPaise;

            if (currentArOutstanding + outstandingBalancePaise > customer.CreditLimitPaise)
            {
                throw new DomainException(
                    "CREDIT_LIMIT_EXCEEDED",
                    $"Credit limit exceeded for '{customer.Name}'. Approved Limit: ₹{customer.CreditLimitPaise / 100.0:F2}, Current AR Balance: ₹{currentArOutstanding / 100.0:F2}, New Credit: ₹{outstandingBalancePaise / 100.0:F2}.");
            }

            amountPaidPaise = totalPaidPaise;
            changeGivenPaise = 0;
            invoiceStatus = totalPaidPaise == 0 ? "Unpaid" : "PartiallyPaid";
        }
        else
        {
            changeGivenPaise = totalPaidPaise - grandTotalPaise;
            amountPaidPaise = grandTotalPaise;
            outstandingBalancePaise = 0;
            invoiceStatus = "Paid";
        }

        var invoicePayments = new List<SalesPaymentRecord>();
        foreach (var p in request.Payments)
        {
            invoicePayments.Add(new SalesPaymentRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                InvoiceId = invoiceId,
                CashierSessionId = session?.Id,
                PaymentMethod = p.PaymentMethod,
                AmountPaise = p.AmountPaise,
                ReferenceNumber = p.ReferenceNumber,
                PaymentDateUtc = now,
                CashierUserId = currentUserId,
                Status = "Completed",
                CreatedAtUtc = now
            });
        }

        var invoice = new SalesInvoiceRecord
        {
            Id = invoiceId,
            CompanyId = companyId,
            BranchId = branchId,
            WarehouseId = request.WarehouseId,
            InvoiceNumber = invoiceNumber,
            CustomerId = customer?.Id,
            CashierSessionId = session?.Id,
            CashierUserId = currentUserId,
            InvoiceDateUtc = now,
            Status = invoiceStatus,
            SubtotalPaise = subtotalPaise,
            DiscountPaise = discountPaise,
            CgstPaise = cgstPaise,
            SgstPaise = sgstPaise,
            IgstPaise = igstPaise,
            TotalTaxPaise = totalTaxPaise,
            GrandTotalPaise = grandTotalPaise,
            AmountPaidPaise = amountPaidPaise,
            OutstandingBalancePaise = outstandingBalancePaise,
            PlaceOfSupplyStateCode = supplyStateCode,
            CustomerGstin = request.CustomerGstin ?? customer?.Gstin,
            SellerGstin = "29AAAAA0000A1Z5",
            Notes = request.Notes,
            CreatedAtUtc = now,
            Lines = invoiceLines,
            Payments = invoicePayments
        };

        dbContext.SalesInvoices.Add(invoice);

        // Deduct inventory atomically & record movements (DEC-ACC-002: physical count update, no invented COGS GL)
        foreach (var line in request.Lines)
        {
            var prod = products[line.ProductId];
            var balance = stockBalances[line.ProductId];

            balance.QuantityOnHand -= line.Quantity;
            balance.QuantityAvailable = balance.QuantityOnHand - balance.QuantityReserved;
            balance.LastMovementAtUtc = now;
            balance.ConcurrencyVersion++;

            var movement = new StockMovementRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                WarehouseId = request.WarehouseId,
                ProductId = prod.Id,
                MovementType = "SalesIssue",
                Direction = "Out",
                Quantity = line.Quantity,
                UnitCostPaise = prod.CostPricePaise,
                TotalCostPaise = prod.CostPricePaise * (long)line.Quantity,
                ReferenceType = "SalesInvoice",
                ReferenceId = invoiceId,
                ReferenceNumber = invoiceNumber,
                Reason = "Retail POS Checkout",
                BalanceAfterOnHand = balance.QuantityOnHand,
                PerformedByUserId = currentUserId,
                MovementDateUtc = now,
                CreatedAtUtc = now
            };

            dbContext.StockMovements.Add(movement);
        }

        // Cash drawer balance update if session is attached
        if (session != null)
        {
            var cashTendered = request.Payments
                .Where(p => string.Equals(p.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.AmountPaise);

            var netCashContribution = Math.Max(0, cashTendered - changeGivenPaise);
            session.CashSalesPaise += netCashContribution;
            session.ExpectedCashPaise += netCashContribution;
        }

        // Post Double-Entry General Ledger Journal Voucher (R1 / DEF-DE-001)
        var invoiceDate = DateOnly.FromDateTime(now.DateTime);
        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && invoiceDate >= p.StartDate && invoiceDate <= p.EndDate, cancellationToken)
            ?? throw new DomainException("FISCAL_PERIOD_NOT_FOUND", $"No active fiscal period found covering invoice date {invoiceDate:yyyy-MM-dd}.");

        var accSales = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "4101", cancellationToken)
            ?? await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.RootClass == AccountRootClass.Revenue && a.IsActive, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Sales Revenue account (4101) not found in Chart of Accounts.", 500);

        var accCash = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1111", cancellationToken)
            ?? await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.ControlAccountType == "CashControl" && a.IsActive, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Cash Account (1111) not found in Chart of Accounts.", 500);

        var accBank = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1112", cancellationToken)
            ?? await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.ControlAccountType == "BankControl" && a.IsActive, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Bank Account (1112) not found in Chart of Accounts.", 500);

        var accDebtors = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1201", cancellationToken)
            ?? await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.ControlAccountType == "CustomerControl" && a.IsActive, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Trade Debtors Account (1201) not found in Chart of Accounts.", 500);

        var accCgst = cgstPaise > 0
            ? (await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2121", cancellationToken)
               ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Output CGST Account (2121) not found in Chart of Accounts.", 500))
            : null;

        var accSgst = sgstPaise > 0
            ? (await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2122", cancellationToken)
               ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Output SGST Account (2122) not found in Chart of Accounts.", 500))
            : null;

        var accIgst = igstPaise > 0
            ? (await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2123", cancellationToken)
               ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Output IGST Account (2123) not found in Chart of Accounts.", 500))
            : null;

        var voucher = new JournalVoucher(
            companyId,
            branchId,
            period.Id,
            VoucherType.Sales,
            invoiceDate,
            currentUserId,
            narration: $"POS Sales Invoice {invoiceNumber}" + (customer != null ? $" - {customer.Name}" : " - Walk-in Customer"),
            referenceNumber: invoiceNumber,
            idempotencyKey: Guid.CreateVersion7(),
            sourceModule: "Sales",
            sourceDocumentId: invoiceId,
            sourceTransactionType: "SalesInvoice");

        var rawCashTendered = request.Payments
            .Where(p => string.Equals(p.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.AmountPaise);

        var netCashTendered = Math.Max(0, rawCashTendered - changeGivenPaise);

        var rawNonCashTendered = request.Payments
            .Where(p => !string.Equals(p.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.AmountPaise);

        var netNonCashTendered = Math.Max(0, Math.Min(grandTotalPaise - netCashTendered, rawNonCashTendered));

        if (netCashTendered > 0)
        {
            voucher.AddLine(accCash.Id, Money.FromPaise(netCashTendered), Money.Zero, "Cash tender received", partyId: customer?.Id);
        }

        if (netNonCashTendered > 0)
        {
            voucher.AddLine(accBank.Id, Money.FromPaise(netNonCashTendered), Money.Zero, "Digital/Bank tender received", partyId: customer?.Id);
        }

        if (outstandingBalancePaise > 0)
        {
            voucher.AddLine(accDebtors.Id, Money.FromPaise(outstandingBalancePaise), Money.Zero, $"Customer credit receivable - {customer?.Name}", partyId: customer?.Id);
        }

        var netTaxableRevenuePaise = Math.Max(0, subtotalPaise - discountPaise);
        voucher.AddLine(accSales.Id, Money.Zero, Money.FromPaise(netTaxableRevenuePaise), "Retail sales revenue", partyId: customer?.Id);

        if (cgstPaise > 0 && accCgst != null)
        {
            voucher.AddLine(accCgst.Id, Money.Zero, Money.FromPaise(cgstPaise), "Output CGST payable", partyId: customer?.Id);
        }

        if (sgstPaise > 0 && accSgst != null)
        {
            voucher.AddLine(accSgst.Id, Money.Zero, Money.FromPaise(sgstPaise), "Output SGST payable", partyId: customer?.Id);
        }

        if (igstPaise > 0 && accIgst != null)
        {
            voucher.AddLine(accIgst.Id, Money.Zero, Money.FromPaise(igstPaise), "Output IGST payable", partyId: customer?.Id);
        }

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var branchCode = "MAIN";
        var postedVoucher = await postingService.PostVoucherAsync(voucher, branchCode, fyCode, currentUserId, hasSupervisorElevation: true, cancellationToken);
        invoice.AccountingVoucherId = postedVoucher.Id;

        // Post AR Subledger Item (R2 / DEF-DE-002)
        if (outstandingBalancePaise > 0 && customer != null)
        {
            var arItem = new ArSubledgerItemRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                CustomerId = customer.Id,
                VoucherId = postedVoucher.Id,
                InvoiceId = invoice.Id,
                ItemDate = invoiceDate,
                DueDate = invoiceDate.AddDays(customer.CreditPeriodDays > 0 ? customer.CreditPeriodDays : 30),
                OriginalAmountPaise = grandTotalPaise,
                SettledAmountPaise = amountPaidPaise,
                OutstandingAmountPaise = outstandingBalancePaise,
                Status = amountPaidPaise > 0 ? "PARTIALLY_PAID" : "OPEN",
                CreatedAtUtc = now
            };
            dbContext.ArSubledgerItems.Add(arItem);
        }

        // Concurrency-safe persistence with bounded retry (R8 / DEF-DE-008)
        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries)
            {
                foreach (var entry in dbContext.ChangeTracker.Entries<StockBalanceRecord>())
                {
                    await entry.ReloadAsync(cancellationToken);
                }

                foreach (var line in request.Lines)
                {
                    var balance = stockBalances[line.ProductId];
                    if (balance.QuantityOnHand < line.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock after concurrent update for product ID '{line.ProductId}'.");
                    }

                    balance.QuantityOnHand -= line.Quantity;
                    balance.QuantityAvailable = balance.QuantityOnHand - balance.QuantityReserved;
                    balance.LastMovementAtUtc = now;
                    balance.ConcurrencyVersion++;
                }

                await Task.Delay(50 * attempt, cancellationToken);
            }
        }

        var cashierUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        return new SalesInvoiceDto
        {
            Id = invoice.Id,
            BranchId = invoice.BranchId,
            WarehouseId = invoice.WarehouseId,
            AccountingVoucherId = invoice.AccountingVoucherId,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            CustomerName = customer?.Name,
            CustomerGstin = invoice.CustomerGstin,
            CashierSessionId = invoice.CashierSessionId,
            CashierUserId = invoice.CashierUserId,
            CashierName = cashierUser?.DisplayName ?? "Cashier",
            InvoiceDateUtc = invoice.InvoiceDateUtc.UtcDateTime,
            Status = invoice.Status,
            SubtotalPaise = invoice.SubtotalPaise,
            DiscountPaise = invoice.DiscountPaise,
            CgstPaise = invoice.CgstPaise,
            SgstPaise = invoice.SgstPaise,
            IgstPaise = invoice.IgstPaise,
            TotalTaxPaise = invoice.TotalTaxPaise,
            GrandTotalPaise = invoice.GrandTotalPaise,
            AmountPaidPaise = invoice.AmountPaidPaise,
            OutstandingBalancePaise = invoice.OutstandingBalancePaise,
            PlaceOfSupplyStateCode = invoice.PlaceOfSupplyStateCode,
            SellerGstin = invoice.SellerGstin,
            Notes = invoice.Notes,
            Lines = invoiceLines.Select(l => new SalesInvoiceLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductCode = l.ProductCode,
                ProductName = l.ProductName,
                HsnCode = l.HsnCode,
                Quantity = l.Quantity,
                UnitPricePaise = l.UnitPricePaise,
                DiscountPercentage = l.DiscountPercentage,
                DiscountPaise = l.DiscountPaise,
                GstRatePercentage = l.GstRatePercentage,
                CgstPercentage = l.CgstPercentage,
                SgstPercentage = l.SgstPercentage,
                IgstPercentage = l.IgstPercentage,
                TaxAmountPaise = l.TaxAmountPaise,
                LineTotalPaise = l.LineTotalPaise,
                BatchNumber = l.BatchNumber,
                SerialNumber = l.SerialNumber
            }).ToList(),
            Payments = invoicePayments.Select(p => new SalesPaymentDto
            {
                Id = p.Id,
                PaymentMethod = p.PaymentMethod,
                AmountPaise = p.AmountPaise,
                ReferenceNumber = p.ReferenceNumber,
                PaymentDateUtc = p.PaymentDateUtc.UtcDateTime,
                CashierUserId = p.CashierUserId,
                Status = p.Status
            }).ToList()
        };
    }
}

// ==========================================
// 3. Sales Invoice Service
// ==========================================

public sealed class SalesInvoiceService(MagicDbContext dbContext, IPostingService postingService) : ISalesInvoiceService
{
    public async Task<(IReadOnlyList<SalesInvoiceDto> Items, int TotalCount)> GetInvoicesAsync(
        Guid companyId,
        Guid? branchId,
        string? search,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.CompanyId == companyId);

        if (branchId.HasValue)
        {
            query = query.Where(i => i.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                EF.Functions.ILike(i.InvoiceNumber, $"%{term}%") ||
                (i.Customer != null && EF.Functions.ILike(i.Customer.Name, $"%{term}%")) ||
                (i.CustomerGstin != null && EF.Functions.ILike(i.CustomerGstin, $"%{term}%")));
        }

        if (fromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
            query = query.Where(i => i.InvoiceDateUtc >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc);
            query = query.Where(i => i.InvoiceDateUtc <= toUtc);
        }

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDateUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var userIds = invoices.Select(i => i.CashierUserId).Distinct().ToList();
        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var dtos = invoices.Select(i => new SalesInvoiceDto
        {
            Id = i.Id,
            BranchId = i.BranchId,
            WarehouseId = i.WarehouseId,
            AccountingVoucherId = i.AccountingVoucherId,
            InvoiceNumber = i.InvoiceNumber,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.Name,
            CustomerGstin = i.CustomerGstin,
            CashierSessionId = i.CashierSessionId,
            CashierUserId = i.CashierUserId,
            CashierName = users.GetValueOrDefault(i.CashierUserId, "Cashier"),
            InvoiceDateUtc = i.InvoiceDateUtc.UtcDateTime,
            Status = i.Status,
            SubtotalPaise = i.SubtotalPaise,
            DiscountPaise = i.DiscountPaise,
            CgstPaise = i.CgstPaise,
            SgstPaise = i.SgstPaise,
            IgstPaise = i.IgstPaise,
            TotalTaxPaise = i.TotalTaxPaise,
            GrandTotalPaise = i.GrandTotalPaise,
            AmountPaidPaise = i.AmountPaidPaise,
            OutstandingBalancePaise = i.OutstandingBalancePaise,
            PlaceOfSupplyStateCode = i.PlaceOfSupplyStateCode,
            SellerGstin = i.SellerGstin,
            Notes = i.Notes,
            Lines = i.Lines.Select(l => new SalesInvoiceLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductCode = l.ProductCode,
                ProductName = l.ProductName,
                HsnCode = l.HsnCode,
                Quantity = l.Quantity,
                UnitPricePaise = l.UnitPricePaise,
                DiscountPercentage = l.DiscountPercentage,
                DiscountPaise = l.DiscountPaise,
                GstRatePercentage = l.GstRatePercentage,
                CgstPercentage = l.CgstPercentage,
                SgstPercentage = l.SgstPercentage,
                IgstPercentage = l.IgstPercentage,
                TaxAmountPaise = l.TaxAmountPaise,
                LineTotalPaise = l.LineTotalPaise,
                BatchNumber = l.BatchNumber,
                SerialNumber = l.SerialNumber
            }).ToList(),
            Payments = i.Payments.Select(pay => new SalesPaymentDto
            {
                Id = pay.Id,
                PaymentMethod = pay.PaymentMethod,
                AmountPaise = pay.AmountPaise,
                ReferenceNumber = pay.ReferenceNumber,
                PaymentDateUtc = pay.PaymentDateUtc.UtcDateTime,
                CashierUserId = pay.CashierUserId,
                Status = pay.Status
            }).ToList()
        }).ToList();

        return (dtos, total);
    }

    public async Task<SalesInvoiceDto> GetInvoiceByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.CompanyId == companyId && i.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sales invoice '{id}' was not found.");

        var cashier = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == invoice.CashierUserId, cancellationToken);

        return new SalesInvoiceDto
        {
            Id = invoice.Id,
            BranchId = invoice.BranchId,
            WarehouseId = invoice.WarehouseId,
            AccountingVoucherId = invoice.AccountingVoucherId,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer?.Name,
            CustomerGstin = invoice.CustomerGstin,
            CashierSessionId = invoice.CashierSessionId,
            CashierUserId = invoice.CashierUserId,
            CashierName = cashier?.DisplayName ?? "Cashier",
            InvoiceDateUtc = invoice.InvoiceDateUtc.UtcDateTime,
            Status = invoice.Status,
            SubtotalPaise = invoice.SubtotalPaise,
            DiscountPaise = invoice.DiscountPaise,
            CgstPaise = invoice.CgstPaise,
            SgstPaise = invoice.SgstPaise,
            IgstPaise = invoice.IgstPaise,
            TotalTaxPaise = invoice.TotalTaxPaise,
            GrandTotalPaise = invoice.GrandTotalPaise,
            AmountPaidPaise = invoice.AmountPaidPaise,
            OutstandingBalancePaise = invoice.OutstandingBalancePaise,
            PlaceOfSupplyStateCode = invoice.PlaceOfSupplyStateCode,
            SellerGstin = invoice.SellerGstin,
            Notes = invoice.Notes,
            Lines = invoice.Lines.Select(l => new SalesInvoiceLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductCode = l.ProductCode,
                ProductName = l.ProductName,
                HsnCode = l.HsnCode,
                Quantity = l.Quantity,
                UnitPricePaise = l.UnitPricePaise,
                DiscountPercentage = l.DiscountPercentage,
                DiscountPaise = l.DiscountPaise,
                GstRatePercentage = l.GstRatePercentage,
                CgstPercentage = l.CgstPercentage,
                SgstPercentage = l.SgstPercentage,
                IgstPercentage = l.IgstPercentage,
                TaxAmountPaise = l.TaxAmountPaise,
                LineTotalPaise = l.LineTotalPaise,
                BatchNumber = l.BatchNumber,
                SerialNumber = l.SerialNumber
            }).ToList(),
            Payments = invoice.Payments.Select(p => new SalesPaymentDto
            {
                Id = p.Id,
                PaymentMethod = p.PaymentMethod,
                AmountPaise = p.AmountPaise,
                ReferenceNumber = p.ReferenceNumber,
                PaymentDateUtc = p.PaymentDateUtc.UtcDateTime,
                CashierUserId = p.CashierUserId,
                Status = p.Status
            }).ToList()
        };
    }

    public async Task<SalesInvoiceDto> CancelInvoiceAsync(
        Guid companyId,
        Guid id,
        Guid currentUserId,
        CancelInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.CompanyId == companyId && i.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sales invoice '{id}' was not found.");

        if (invoice.Status == "Cancelled")
        {
            throw new InvalidOperationException("Invoice is already cancelled.");
        }

        var now = DateTimeOffset.UtcNow;
        invoice.Status = "Cancelled";
        invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes)
            ? $"Cancelled: {request.Reason}"
            : $"{invoice.Notes} | Cancelled: {request.Reason}";
        invoice.UpdatedAtUtc = now;

        // Restore stock to exact original warehouse (R5 / DEF-DE-005)
        Guid? originatingWarehouseId = invoice.WarehouseId;
        if (!originatingWarehouseId.HasValue)
        {
            originatingWarehouseId = await dbContext.StockMovements
                .Where(m => m.CompanyId == companyId && m.ReferenceId == invoice.Id && m.MovementType == "SalesIssue")
                .Select(m => (Guid?)m.WarehouseId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var warehouse = originatingWarehouseId.HasValue
            ? await dbContext.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == originatingWarehouseId.Value, cancellationToken)
            : await dbContext.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == companyId && w.IsActive, cancellationToken);

        if (warehouse != null)
        {
            foreach (var line in invoice.Lines)
            {
                var balance = await dbContext.StockBalances
                    .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.WarehouseId == warehouse.Id && b.ProductId == line.ProductId, cancellationToken);

                if (balance != null)
                {
                    balance.QuantityOnHand += line.Quantity;
                    balance.QuantityAvailable = balance.QuantityOnHand - balance.QuantityReserved;
                    balance.LastMovementAtUtc = now;
                    balance.ConcurrencyVersion++;

                    var movement = new StockMovementRecord
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        BranchId = invoice.BranchId,
                        WarehouseId = warehouse.Id,
                        ProductId = line.ProductId,
                        MovementType = "SalesCancellation",
                        Direction = "In",
                        Quantity = line.Quantity,
                        UnitCostPaise = 0,
                        TotalCostPaise = 0,
                        ReferenceType = "SalesInvoiceCancellation",
                        ReferenceId = invoice.Id,
                        ReferenceNumber = invoice.InvoiceNumber,
                        Reason = $"Cancellation: {request.Reason}",
                        BalanceAfterOnHand = balance.QuantityOnHand,
                        PerformedByUserId = currentUserId,
                        MovementDateUtc = now,
                        CreatedAtUtc = now
                    };
                    dbContext.StockMovements.Add(movement);
                }
            }
        }

        // Cashier session cash reversal (R10 / DEF-DE-010)
        if (invoice.CashierSessionId.HasValue)
        {
            var session = await dbContext.CashierSessions
                .FirstOrDefaultAsync(s => s.Id == invoice.CashierSessionId.Value && s.Status == "Open", cancellationToken);

            if (session != null)
            {
                var cashTendered = invoice.Payments
                    .Where(p => string.Equals(p.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
                    .Sum(p => p.AmountPaise);

                if (cashTendered > 0)
                {
                    session.CashSalesPaise = Math.Max(0, session.CashSalesPaise - cashTendered);
                    session.ExpectedCashPaise = Math.Max(0, session.ExpectedCashPaise - cashTendered);
                }
            }
        }

        foreach (var p in invoice.Payments)
        {
            p.Status = "Cancelled";
        }

        // Cancel AR Subledger item if credit sale
        var arItems = await dbContext.ArSubledgerItems
            .Where(i => i.InvoiceId == invoice.Id && i.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        foreach (var arItem in arItems)
        {
            dbContext.ArSubledgerItems.Remove(arItem);
        }

        // Reverse General Ledger Journal Voucher if posted
        if (invoice.AccountingVoucherId.HasValue)
        {
            var today = DateOnly.FromDateTime(now.DateTime);
            var period = await dbContext.FiscalPeriods
                .Include(p => p.FiscalYear)
                .FirstOrDefaultAsync(p => p.CompanyId == companyId && today >= p.StartDate && today <= p.EndDate, cancellationToken);
            var fyCode = period?.FiscalYear?.Code ?? "FY2026-27";
            var branchCode = "MAIN";

            try
            {
                await postingService.ReverseVoucherAsync(
                    invoice.AccountingVoucherId.Value,
                    today,
                    branchCode,
                    fyCode,
                    currentUserId,
                    $"Cancellation of Sales Invoice {invoice.InvoiceNumber}: {request.Reason}",
                    cancellationToken);
            }
            catch (Exception)
            {
                // Handled gracefully if voucher already reversed or locked
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetInvoiceByIdAsync(companyId, id, cancellationToken);
    }
}

// ==========================================
// 4. Sales Return & Credit Note Service
// ==========================================

public sealed class SalesReturnService(MagicDbContext dbContext, IPostingService postingService) : ISalesReturnService
{
    public async Task<(IReadOnlyList<SalesReturnDto> Items, int TotalCount)> GetReturnsAsync(
        Guid companyId,
        Guid? branchId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SalesReturns
            .Include(r => r.Invoice)
            .Include(r => r.Customer)
            .Include(r => r.Lines)
            .Where(r => r.CompanyId == companyId);

        if (branchId.HasValue)
        {
            query = query.Where(r => r.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                EF.Functions.ILike(r.ReturnNumber, $"%{term}%") ||
                (r.Invoice != null && EF.Functions.ILike(r.Invoice.InvoiceNumber, $"%{term}%")) ||
                (r.Customer != null && EF.Functions.ILike(r.Customer.Name, $"%{term}%")));
        }

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var returns = await query
            .OrderByDescending(r => r.ReturnDateUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var dtos = returns.Select(r => new SalesReturnDto
        {
            Id = r.Id,
            ReturnNumber = r.ReturnNumber,
            InvoiceId = r.InvoiceId,
            InvoiceNumber = r.Invoice?.InvoiceNumber ?? "UNKNOWN",
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.Name,
            ReturnDateUtc = r.ReturnDateUtc.UtcDateTime,
            Status = r.Status,
            ReasonCode = r.ReasonCode,
            TotalRefundPaise = r.TotalRefundPaise,
            CreditNoteNumber = r.CreditNoteNumber,
            Notes = r.Notes,
            Lines = r.Lines.Select(l => new SalesReturnLineDto
            {
                Id = l.Id,
                InvoiceLineId = l.InvoiceLineId,
                ProductId = l.ProductId,
                ProductCode = l.ProductCode,
                ProductName = l.ProductName,
                ReturnQuantity = l.ReturnQuantity,
                UnitPricePaise = l.UnitPricePaise,
                GstRatePercentage = l.GstRatePercentage,
                TaxAmountPaise = l.TaxAmountPaise,
                TotalAmountPaise = l.TotalAmountPaise,
                IsResalable = l.IsResalable,
                Reason = l.Reason
            }).ToList()
        }).ToList();

        return (dtos, total);
    }

    public async Task<SalesReturnDto> GetReturnByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var r = await dbContext.SalesReturns
            .Include(x => x.Invoice)
            .Include(x => x.Customer)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sales return '{id}' was not found.");

        return new SalesReturnDto
        {
            Id = r.Id,
            ReturnNumber = r.ReturnNumber,
            InvoiceId = r.InvoiceId,
            InvoiceNumber = r.Invoice?.InvoiceNumber ?? "UNKNOWN",
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.Name,
            ReturnDateUtc = r.ReturnDateUtc.UtcDateTime,
            Status = r.Status,
            ReasonCode = r.ReasonCode,
            TotalRefundPaise = r.TotalRefundPaise,
            CreditNoteNumber = r.CreditNoteNumber,
            Notes = r.Notes,
            Lines = r.Lines.Select(l => new SalesReturnLineDto
            {
                Id = l.Id,
                InvoiceLineId = l.InvoiceLineId,
                ProductId = l.ProductId,
                ProductCode = l.ProductCode,
                ProductName = l.ProductName,
                ReturnQuantity = l.ReturnQuantity,
                UnitPricePaise = l.UnitPricePaise,
                GstRatePercentage = l.GstRatePercentage,
                TaxAmountPaise = l.TaxAmountPaise,
                TotalAmountPaise = l.TotalAmountPaise,
                IsResalable = l.IsResalable,
                Reason = l.Reason
            }).ToList()
        };
    }

    public async Task<SalesReturnDto> CreateReturnAsync(
        Guid companyId,
        Guid branchId,
        Guid currentUserId,
        CreateSalesReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Return request must contain at least one item line.");
        }

        var invoice = await dbContext.SalesInvoices
            .Include(i => i.Lines)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.CompanyId == companyId && i.Id == request.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sales invoice '{request.InvoiceId}' was not found.");

        if (invoice.Status == "Cancelled")
        {
            throw new InvalidOperationException("Cannot process a return against a cancelled invoice.");
        }

        // Check already returned quantities
        var existingReturns = await dbContext.SalesReturns
            .Include(r => r.Lines)
            .Where(r => r.InvoiceId == invoice.Id && r.Status != "Cancelled")
            .ToListAsync(cancellationToken);

        var previouslyReturnedQty = existingReturns
            .SelectMany(r => r.Lines)
            .GroupBy(l => l.InvoiceLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.ReturnQuantity));

        var invoiceLineMap = invoice.Lines.ToDictionary(l => l.Id);
        var now = DateTimeOffset.UtcNow;
        var returnId = Guid.NewGuid();
        var returnNumber = $"RET-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var creditNoteNumber = $"CRN-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        long totalRefundPaise = 0;
        var returnLines = new List<SalesReturnLineRecord>();

        var defaultWarehouse = await dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.IsActive, cancellationToken);

        foreach (var reqLine in request.Lines)
        {
            if (!invoiceLineMap.TryGetValue(reqLine.InvoiceLineId, out var invLine))
            {
                throw new InvalidOperationException($"Invoice line '{reqLine.InvoiceLineId}' does not belong to this invoice.");
            }

            var prior = previouslyReturnedQty.GetValueOrDefault(invLine.Id, 0m);
            var availableToReturn = invLine.Quantity - prior;

            if (reqLine.ReturnQuantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Return quantity must be greater than zero.");
            }

            if (reqLine.ReturnQuantity > availableToReturn)
            {
                throw new InvalidOperationException(
                    $"Return quantity {reqLine.ReturnQuantity} exceeds available returnable quantity {availableToReturn} for product '{invLine.ProductName}'.");
            }

            // Effective net taxable amount after proportional line discount (R3 / DEF-DE-003)
            var invLineTaxable = Math.Max(0, (invLine.UnitPricePaise * (long)invLine.Quantity) - invLine.DiscountPaise);
            var returnTaxable = (long)Math.Round((decimal)invLineTaxable * reqLine.ReturnQuantity / invLine.Quantity, MidpointRounding.AwayFromZero);
            var returnTax = (long)Math.Round((decimal)invLine.TaxAmountPaise * reqLine.ReturnQuantity / invLine.Quantity, MidpointRounding.AwayFromZero);
            var lineTotal = returnTaxable + returnTax;
            var unitPrice = reqLine.ReturnQuantity > 0 ? (long)Math.Round((decimal)returnTaxable / reqLine.ReturnQuantity, MidpointRounding.AwayFromZero) : 0;

            totalRefundPaise += lineTotal;

            returnLines.Add(new SalesReturnLineRecord
            {
                Id = Guid.NewGuid(),
                ReturnId = returnId,
                InvoiceLineId = invLine.Id,
                ProductId = invLine.ProductId,
                ProductCode = invLine.ProductCode,
                ProductName = invLine.ProductName,
                ReturnQuantity = reqLine.ReturnQuantity,
                UnitPricePaise = unitPrice,
                GstRatePercentage = invLine.GstRatePercentage,
                TaxAmountPaise = returnTax,
                TotalAmountPaise = lineTotal,
                IsResalable = reqLine.IsResalable,
                Reason = reqLine.Reason
            });

            // If resalable, restock physical inventory atomically to originating warehouse (R5 / DEF-DE-005)
            var returnWarehouseId = invoice.WarehouseId ?? defaultWarehouse?.Id;
            if (reqLine.IsResalable && returnWarehouseId.HasValue)
            {
                var targetWarehouseId = returnWarehouseId.Value;
                var balance = await dbContext.StockBalances
                    .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.WarehouseId == targetWarehouseId && b.ProductId == invLine.ProductId, cancellationToken);

                if (balance != null)
                {
                    balance.QuantityOnHand += reqLine.ReturnQuantity;
                    balance.QuantityAvailable = balance.QuantityOnHand - balance.QuantityReserved;
                    balance.LastMovementAtUtc = now;
                    balance.ConcurrencyVersion++;

                    var movement = new StockMovementRecord
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        BranchId = branchId,
                        WarehouseId = targetWarehouseId,
                        ProductId = invLine.ProductId,
                        MovementType = "CustomerReturn",
                        Direction = "In",
                        Quantity = reqLine.ReturnQuantity,
                        UnitCostPaise = 0,
                        TotalCostPaise = 0,
                        ReferenceType = "SalesReturn",
                        ReferenceId = returnId,
                        ReferenceNumber = returnNumber,
                        Reason = $"Customer Return: {reqLine.Reason}",
                        BalanceAfterOnHand = balance.QuantityOnHand,
                        PerformedByUserId = currentUserId,
                        MovementDateUtc = now,
                        CreatedAtUtc = now
                    };
                    dbContext.StockMovements.Add(movement);
                }
            }
        }

        var salesReturn = new SalesReturnRecord
        {
            Id = returnId,
            CompanyId = companyId,
            BranchId = branchId,
            ReturnNumber = returnNumber,
            InvoiceId = invoice.Id,
            CustomerId = invoice.CustomerId,
            CashierSessionId = invoice.CashierSessionId,
            CashierUserId = currentUserId,
            ReturnDateUtc = now,
            Status = "Confirmed",
            ReasonCode = request.ReasonCode,
            TotalRefundPaise = totalRefundPaise,
            CreditNoteNumber = creditNoteNumber,
            Notes = request.Notes,
            CreatedAtUtc = now,
            Lines = returnLines
        };

        dbContext.SalesReturns.Add(salesReturn);

        // Create formal Credit Note record
        var creditNote = new CreditNoteRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            CreditNoteNumber = creditNoteNumber,
            SalesReturnId = returnId,
            CustomerId = invoice.CustomerId,
            IssueDateUtc = now,
            AmountPaise = totalRefundPaise,
            Status = "Issued",
            CreatedAtUtc = now
        };

        dbContext.CreditNotes.Add(creditNote);

        // Update cashier session if session is open and refund method is physical Cash (R4 / DEF-DE-004)
        var isCashRefund = string.Equals(request.RefundMethod, "Cash", StringComparison.OrdinalIgnoreCase);
        if (invoice.CashierSessionId.HasValue && isCashRefund)
        {
            var session = await dbContext.CashierSessions
                .FirstOrDefaultAsync(s => s.Id == invoice.CashierSessionId.Value && s.Status == "Open", cancellationToken);

            if (session != null)
            {
                session.CashRefundsPaise += totalRefundPaise;
                session.ExpectedCashPaise = Math.Max(0, session.ExpectedCashPaise - totalRefundPaise);
            }
        }

        // Post Double-Entry General Ledger Journal Voucher for Credit Note
        var returnDate = DateOnly.FromDateTime(now.DateTime);
        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && returnDate >= p.StartDate && returnDate <= p.EndDate, cancellationToken);

        if (period != null)
        {
            var accSales = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "4101", cancellationToken)
                ?? await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.RootClass == AccountRootClass.Revenue && a.IsActive, cancellationToken);

            var accCash = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1111", cancellationToken);
            var accBank = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1112", cancellationToken);
            var accDebtors = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1201", cancellationToken);
            var accCgst = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2121", cancellationToken);
            var accSgst = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2122", cancellationToken);
            var accIgst = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2123", cancellationToken);

            if (accSales != null)
            {
                var voucher = new JournalVoucher(
                    companyId,
                    branchId,
                    period.Id,
                    VoucherType.Journal,
                    returnDate,
                    currentUserId,
                    narration: $"Sales Return {returnNumber} for Invoice {invoice.InvoiceNumber}" + (invoice.Customer != null ? $" - {invoice.Customer.Name}" : ""),
                    referenceNumber: creditNoteNumber,
                    idempotencyKey: Guid.CreateVersion7(),
                    sourceModule: "Sales",
                    sourceDocumentId: returnId,
                    sourceTransactionType: "SalesReturn");

                var returnTaxableTotal = returnLines.Sum(l => l.TotalAmountPaise - l.TaxAmountPaise);
                var returnTaxTotal = returnLines.Sum(l => l.TaxAmountPaise);
                var isInterStateReturn = string.Equals(invoice.PlaceOfSupplyStateCode, "INTER", StringComparison.OrdinalIgnoreCase) || invoice.IgstPaise > 0;

                voucher.AddLine(accSales.Id, Money.FromPaise(returnTaxableTotal), Money.Zero, "Sales return revenue reversal", partyId: invoice.CustomerId);

                if (returnTaxTotal > 0)
                {
                    if (isInterStateReturn && accIgst != null)
                    {
                        voucher.AddLine(accIgst.Id, Money.FromPaise(returnTaxTotal), Money.Zero, "Output IGST reversal", partyId: invoice.CustomerId);
                    }
                    else
                    {
                        var returnCgst = returnTaxTotal / 2;
                        var returnSgst = returnTaxTotal - returnCgst;
                        if (returnCgst > 0 && accCgst != null) voucher.AddLine(accCgst.Id, Money.FromPaise(returnCgst), Money.Zero, "Output CGST reversal", partyId: invoice.CustomerId);
                        if (returnSgst > 0 && accSgst != null) voucher.AddLine(accSgst.Id, Money.FromPaise(returnSgst), Money.Zero, "Output SGST reversal", partyId: invoice.CustomerId);
                    }
                }

                if (isCashRefund && accCash != null)
                {
                    voucher.AddLine(accCash.Id, Money.Zero, Money.FromPaise(totalRefundPaise), "Cash refund to customer", partyId: invoice.CustomerId);
                }
                else if (string.Equals(request.RefundMethod, "Card", StringComparison.OrdinalIgnoreCase) || string.Equals(request.RefundMethod, "UPI", StringComparison.OrdinalIgnoreCase) || string.Equals(request.RefundMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase))
                {
                    if (accBank != null) voucher.AddLine(accBank.Id, Money.Zero, Money.FromPaise(totalRefundPaise), "Bank/Digital refund to customer", partyId: invoice.CustomerId);
                }
                else
                {
                    if (accDebtors != null) voucher.AddLine(accDebtors.Id, Money.Zero, Money.FromPaise(totalRefundPaise), "Credit Note issued to customer", partyId: invoice.CustomerId);
                }

                var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
                var branchCode = "MAIN";
                try
                {
                    await postingService.PostVoucherAsync(voucher, branchCode, fyCode, currentUserId, hasSupervisorElevation: true, cancellationToken);
                }
                catch (Exception)
                {
                    // Handled gracefully if voucher posting fails
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SalesReturnDto
        {
            Id = salesReturn.Id,
            ReturnNumber = salesReturn.ReturnNumber,
            InvoiceId = salesReturn.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = salesReturn.CustomerId,
            CustomerName = invoice.Customer?.Name,
            ReturnDateUtc = salesReturn.ReturnDateUtc.UtcDateTime,
            Status = salesReturn.Status,
            ReasonCode = salesReturn.ReasonCode,
            TotalRefundPaise = salesReturn.TotalRefundPaise,
            CreditNoteNumber = salesReturn.CreditNoteNumber,
            Notes = salesReturn.Notes,
            Lines = returnLines.Select(l => new SalesReturnLineDto
            {
                Id = l.Id,
                InvoiceLineId = l.InvoiceLineId,
                ProductId = l.ProductId,
                ProductCode = l.ProductCode,
                ProductName = l.ProductName,
                ReturnQuantity = l.ReturnQuantity,
                UnitPricePaise = l.UnitPricePaise,
                GstRatePercentage = l.GstRatePercentage,
                TaxAmountPaise = l.TaxAmountPaise,
                TotalAmountPaise = l.TotalAmountPaise,
                IsResalable = l.IsResalable,
                Reason = l.Reason
            }).ToList()
        };
    }

    public async Task<CreditNoteDto> GetCreditNoteByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cn = await dbContext.CreditNotes
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Credit note '{id}' was not found.");

        return new CreditNoteDto
        {
            Id = cn.Id,
            CreditNoteNumber = cn.CreditNoteNumber,
            SalesReturnId = cn.SalesReturnId,
            CustomerId = cn.CustomerId,
            CustomerName = cn.Customer?.Name,
            IssueDateUtc = cn.IssueDateUtc.UtcDateTime,
            AmountPaise = cn.AmountPaise,
            Status = cn.Status
        };
    }

    public async Task<(IReadOnlyList<CreditNoteDto> Items, int TotalCount)> GetCreditNotesAsync(
        Guid companyId,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CreditNotes
            .Include(c => c.Customer)
            .Where(c => c.CompanyId == companyId);

        if (customerId.HasValue)
        {
            query = query.Where(c => c.CustomerId == customerId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var notes = await query
            .OrderByDescending(c => c.IssueDateUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var dtos = notes.Select(cn => new CreditNoteDto
        {
            Id = cn.Id,
            CreditNoteNumber = cn.CreditNoteNumber,
            SalesReturnId = cn.SalesReturnId,
            CustomerId = cn.CustomerId,
            CustomerName = cn.Customer?.Name,
            IssueDateUtc = cn.IssueDateUtc.UtcDateTime,
            AmountPaise = cn.AmountPaise,
            Status = cn.Status
        }).ToList();

        return (dtos, total);
    }
}

// ==========================================
// 5. Cashier Session Service
// ==========================================

public sealed class CashierSessionService(MagicDbContext dbContext) : ICashierSessionService
{
    public async Task<CashierSessionDto?> GetActiveSessionAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var session = await dbContext.CashierSessions
            .Include(s => s.Movements)
            .FirstOrDefaultAsync(s =>
                s.CompanyId == companyId &&
                s.BranchId == branchId &&
                s.CashierUserId == userId &&
                s.Status == "Open", cancellationToken);

        return session == null ? null : MapSession(session);
    }

    public async Task<(IReadOnlyList<CashierSessionDto> Items, int TotalCount)> GetSessionsAsync(
        Guid companyId,
        Guid? branchId,
        Guid? userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CashierSessions
            .Include(s => s.Movements)
            .Where(s => s.CompanyId == companyId);

        if (branchId.HasValue)
        {
            query = query.Where(s => s.BranchId == branchId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(s => s.CashierUserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var sessions = await query
            .OrderByDescending(s => s.OpenedAtUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        return (sessions.Select(MapSession).ToList(), total);
    }

    public async Task<CashierSessionDto> GetSessionByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var session = await dbContext.CashierSessions
            .Include(s => s.Movements)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Cashier session '{id}' was not found.");

        return MapSession(session);
    }

    public async Task<CashierSessionDto> OpenSessionAsync(
        Guid companyId,
        Guid branchId,
        Guid currentUserId,
        OpenCashierSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var activeSession = await dbContext.CashierSessions
            .FirstOrDefaultAsync(s =>
                s.CompanyId == companyId &&
                s.BranchId == branchId &&
                s.CashierUserId == currentUserId &&
                s.Status == "Open", cancellationToken);

        if (activeSession != null)
        {
            throw new InvalidOperationException("You already have an active cashier shift open. Please close it before opening a new shift.");
        }

        if (request.OpeningCashPaise < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Opening float amount cannot be negative.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid();

        var session = new CashierSessionRecord
        {
            Id = sessionId,
            CompanyId = companyId,
            BranchId = branchId,
            TerminalId = request.TerminalId,
            CashierUserId = currentUserId,
            CashierUsername = user?.DisplayName ?? "Cashier",
            OpenedAtUtc = now,
            Status = "Open",
            OpeningCashPaise = request.OpeningCashPaise,
            CashSalesPaise = 0,
            CashRefundsPaise = 0,
            CashDepositsPaise = 0,
            CashWithdrawalsPaise = 0,
            ExpectedCashPaise = request.OpeningCashPaise,
            CreatedAtUtc = now
        };

        var initialFloatMovement = new CashMovementRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            CashierSessionId = sessionId,
            MovementType = "FloatAdjustment",
            AmountPaise = request.OpeningCashPaise,
            Reason = "Initial Opening Cash Float",
            MovementDateUtc = now,
            CashierUserId = currentUserId,
            CreatedAtUtc = now
        };

        session.Movements.Add(initialFloatMovement);
        dbContext.CashierSessions.Add(session);

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSession(session);
    }

    public async Task<CashMovementDto> RecordMovementAsync(
        Guid companyId,
        Guid sessionId,
        Guid currentUserId,
        RecordCashMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await dbContext.CashierSessions
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Cashier session '{sessionId}' was not found.");

        if (session.Status != "Open")
        {
            throw new InvalidOperationException($"Cannot record cash movement on a session that is '{session.Status}'.");
        }

        if (request.AmountPaise <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Movement amount must be greater than zero.");
        }

        var now = DateTimeOffset.UtcNow;

        if (string.Equals(request.MovementType, "Withdrawal", StringComparison.OrdinalIgnoreCase))
        {
            if (request.AmountPaise > session.ExpectedCashPaise)
            {
                throw new InvalidOperationException(
                    $"Cannot withdraw ₹{request.AmountPaise / 100.0:F2}. Current cash on hand is only ₹{session.ExpectedCashPaise / 100.0:F2}.");
            }

            session.CashWithdrawalsPaise += request.AmountPaise;
            session.ExpectedCashPaise -= request.AmountPaise;
        }
        else if (string.Equals(request.MovementType, "Deposit", StringComparison.OrdinalIgnoreCase))
        {
            session.CashDepositsPaise += request.AmountPaise;
            session.ExpectedCashPaise += request.AmountPaise;
        }
        else
        {
            throw new InvalidOperationException($"Invalid cash movement type '{request.MovementType}'. Must be 'Deposit' or 'Withdrawal'.");
        }

        var movement = new CashMovementRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = session.BranchId,
            CashierSessionId = sessionId,
            MovementType = request.MovementType,
            AmountPaise = request.AmountPaise,
            Reason = request.Reason,
            MovementDateUtc = now,
            CashierUserId = currentUserId,
            CreatedAtUtc = now
        };

        dbContext.CashMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CashMovementDto
        {
            Id = movement.Id,
            CashierSessionId = movement.CashierSessionId,
            MovementType = movement.MovementType,
            AmountPaise = movement.AmountPaise,
            Reason = movement.Reason,
            MovementDateUtc = movement.MovementDateUtc.UtcDateTime,
            CashierUserId = movement.CashierUserId
        };
    }

    public async Task<CashierSessionDto> CloseSessionAsync(
        Guid companyId,
        Guid sessionId,
        Guid currentUserId,
        CloseCashierSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await dbContext.CashierSessions
            .Include(s => s.Movements)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Cashier session '{sessionId}' was not found.");

        if (session.Status != "Open")
        {
            throw new InvalidOperationException($"Cannot close session because it is already '{session.Status}'.");
        }

        if (request.ActualClosingCashPaise < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Physical cash count cannot be negative.");
        }

        var now = DateTimeOffset.UtcNow;
        session.ClosedAtUtc = now;
        session.Status = "Closed";
        session.ActualClosingCashPaise = request.ActualClosingCashPaise;
        session.VariancePaise = request.ActualClosingCashPaise - session.ExpectedCashPaise;
        session.ClosingNotes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSession(session);
    }

    private static CashierSessionDto MapSession(CashierSessionRecord s) => new()
    {
        Id = s.Id,
        BranchId = s.BranchId,
        TerminalId = s.TerminalId,
        CashierUserId = s.CashierUserId,
        CashierUsername = s.CashierUsername,
        OpenedAtUtc = s.OpenedAtUtc.UtcDateTime,
        ClosedAtUtc = s.ClosedAtUtc?.UtcDateTime,
        Status = s.Status,
        OpeningCashPaise = s.OpeningCashPaise,
        CashSalesPaise = s.CashSalesPaise,
        CashRefundsPaise = s.CashRefundsPaise,
        CashDepositsPaise = s.CashDepositsPaise,
        CashWithdrawalsPaise = s.CashWithdrawalsPaise,
        ExpectedCashPaise = s.ExpectedCashPaise,
        ActualClosingCashPaise = s.ActualClosingCashPaise,
        VariancePaise = s.VariancePaise,
        ClosingNotes = s.ClosingNotes,
        SupervisorUserId = s.SupervisorUserId,
        ApprovedAtUtc = s.ApprovedAtUtc?.UtcDateTime,
        Movements = s.Movements.Select(m => new CashMovementDto
        {
            Id = m.Id,
            CashierSessionId = m.CashierSessionId,
            MovementType = m.MovementType,
            AmountPaise = m.AmountPaise,
            Reason = m.Reason,
            MovementDateUtc = m.MovementDateUtc.UtcDateTime,
            CashierUserId = m.CashierUserId
        }).ToList()
    };
}

// ==========================================
// 6. Sales Reporting Service
// ==========================================

public sealed class SalesReportingService(MagicDbContext dbContext) : ISalesReportingService
{
    public async Task<SalesReportSummaryDto> GetSummaryAsync(
        Guid companyId,
        Guid? branchId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.SpecifyKind(fromDateUtc, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(toDateUtc, DateTimeKind.Utc);

        var query = dbContext.SalesInvoices
            .Where(i => i.CompanyId == companyId &&
                        i.Status != "Cancelled" &&
                        i.InvoiceDateUtc >= from &&
                        i.InvoiceDateUtc <= to);

        if (branchId.HasValue)
        {
            query = query.Where(i => i.BranchId == branchId.Value);
        }

        var invoices = await query.ToListAsync(cancellationToken);

        var returnsQuery = dbContext.SalesReturns
            .Where(r => r.CompanyId == companyId &&
                        r.Status != "Cancelled" &&
                        r.ReturnDateUtc >= from &&
                        r.ReturnDateUtc <= to);

        if (branchId.HasValue)
        {
            returnsQuery = returnsQuery.Where(r => r.BranchId == branchId.Value);
        }

        var returns = await returnsQuery.ToListAsync(cancellationToken);

        var grossSales = invoices.Sum(i => i.GrandTotalPaise);
        var totalTax = invoices.Sum(i => i.TotalTaxPaise);
        var totalDiscount = invoices.Sum(i => i.DiscountPaise);
        var totalRefunds = returns.Sum(r => r.TotalRefundPaise);
        var netSales = grossSales - totalRefunds;
        var count = invoices.Count;
        var avgBill = count > 0 ? grossSales / count : 0;

        return new SalesReportSummaryDto
        {
            GrossSalesPaise = grossSales,
            NetSalesPaise = netSales,
            TotalTaxPaise = totalTax,
            TotalDiscountPaise = totalDiscount,
            TotalRefundsPaise = totalRefunds,
            TotalInvoicesCount = count,
            AverageBillValuePaise = avgBill
        };
    }

    public async Task<IReadOnlyList<SalesByPaymentMethodDto>> GetPaymentBreakdownAsync(
        Guid companyId,
        Guid? branchId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.SpecifyKind(fromDateUtc, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(toDateUtc, DateTimeKind.Utc);

        var query = dbContext.SalesPayments
            .Include(p => p.Invoice)
            .Where(p => p.CompanyId == companyId &&
                        p.Status == "Completed" &&
                        p.Invoice != null &&
                        p.Invoice.Status != "Cancelled" &&
                        p.PaymentDateUtc >= from &&
                        p.PaymentDateUtc <= to);

        if (branchId.HasValue)
        {
            query = query.Where(p => p.BranchId == branchId.Value);
        }

        var payments = await query.ToListAsync(cancellationToken);
        var totalAmount = payments.Sum(p => p.AmountPaise);

        return payments
            .GroupBy(p => p.PaymentMethod)
            .Select(g =>
            {
                var methodTotal = g.Sum(x => x.AmountPaise);
                var pct = totalAmount > 0 ? Math.Round(((decimal)methodTotal / totalAmount) * 100m, 2) : 0m;
                return new SalesByPaymentMethodDto
                {
                    PaymentMethod = g.Key,
                    TotalAmountPaise = methodTotal,
                    TransactionCount = g.Count(),
                    PercentageOfTotal = pct
                };
            })
            .OrderByDescending(p => p.TotalAmountPaise)
            .ToList();
    }

    public async Task<IReadOnlyList<SalesByProductDto>> GetTopSellingProductsAsync(
        Guid companyId,
        Guid? branchId,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.SpecifyKind(fromDateUtc, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(toDateUtc, DateTimeKind.Utc);

        var query = dbContext.SalesInvoiceLines
            .Include(l => l.Invoice)
            .Where(l => l.Invoice != null &&
                        l.Invoice.CompanyId == companyId &&
                        l.Invoice.Status != "Cancelled" &&
                        l.Invoice.InvoiceDateUtc >= from &&
                        l.Invoice.InvoiceDateUtc <= to);

        if (branchId.HasValue)
        {
            query = query.Where(l => l.Invoice!.BranchId == branchId.Value);
        }

        var lines = await query.ToListAsync(cancellationToken);
        var takeLimit = Math.Clamp(limit, 1, 100);

        return lines
            .GroupBy(l => new { l.ProductId, l.ProductCode, l.ProductName })
            .Select(g => new SalesByProductDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalRevenuePaise = g.Sum(x => x.LineTotalPaise)
            })
            .OrderByDescending(x => x.TotalRevenuePaise)
            .Take(takeLimit)
            .ToList();
    }

    public async Task<IReadOnlyList<HourlySalesDto>> GetHourlySalesAsync(
        Guid companyId,
        Guid? branchId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default)
    {
        var dayStart = DateTime.SpecifyKind(dateUtc.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var query = dbContext.SalesInvoices
            .Where(i => i.CompanyId == companyId &&
                        i.Status != "Cancelled" &&
                        i.InvoiceDateUtc >= dayStart &&
                        i.InvoiceDateUtc < dayEnd);

        if (branchId.HasValue)
        {
            query = query.Where(i => i.BranchId == branchId.Value);
        }

        var invoices = await query.ToListAsync(cancellationToken);

        var hourlyMap = invoices
            .GroupBy(i => i.InvoiceDateUtc.Hour)
            .ToDictionary(g => g.Key, g => new
            {
                Count = g.Count(),
                TotalSales = g.Sum(x => x.GrandTotalPaise)
            });

        var results = new List<HourlySalesDto>();
        for (var hour = 0; hour < 24; hour++)
        {
            if (hourlyMap.TryGetValue(hour, out var data))
            {
                results.Add(new HourlySalesDto
                {
                    HourOfDay = hour,
                    InvoiceCount = data.Count,
                    TotalSalesPaise = data.TotalSales
                });
            }
            else
            {
                results.Add(new HourlySalesDto
                {
                    HourOfDay = hour,
                    InvoiceCount = 0,
                    TotalSalesPaise = 0
                });
            }
        }

        return results;
    }
}
