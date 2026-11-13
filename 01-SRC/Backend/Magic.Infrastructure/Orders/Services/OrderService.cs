using Magic.Application.Accounting.Services;
using Magic.Application.Orders.Services;
using Magic.Contracts.Orders;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Orders.Services;

public sealed class OrderService(
    MagicDbContext dbContext,
    IPostingService postingService) : IOrderService
{
    public async Task<OrderSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.SalesOrders
            .Where(o => o.CompanyId == companyId && o.BranchId == branchId)
            .ToListAsync(cancellationToken);

        var total = orders.Count;
        var pendingFulfillment = orders.Count(o => o.Status == "Confirmed" || o.Status == "PartiallyFulfilled");
        var backorders = await dbContext.Backorders
            .CountAsync(b => b.SalesOrder!.CompanyId == companyId && b.SalesOrder.BranchId == branchId && b.Status == "Pending", cancellationToken);
        var openValue = orders.Where(o => o.Status != "Invoiced" && o.Status != "Cancelled").Sum(o => o.TotalAmountPaise);

        return new OrderSummaryDto(total, pendingFulfillment, backorders, openValue);
    }

    public async Task<(IReadOnlyList<SalesOrderDto> Items, int TotalCount)> GetOrdersAsync(
        Guid companyId,
        Guid branchId,
        Guid? customerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Warehouse)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .Where(o => o.CompanyId == companyId && o.BranchId == branchId);

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => MapOrder(o))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<SalesOrderDto> GetOrderByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Warehouse)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ORDER_NOT_FOUND", "Sales order not found.", StatusCodes.Status404NotFound);

        return MapOrder(order);
    }

    public async Task<SalesOrderDto> CreateOrderAsync(Guid companyId, Guid branchId, CreateSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("CUSTOMER_NOT_FOUND", "Customer not found.", StatusCodes.Status404NotFound);

        var warehouse = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("WAREHOUSE_NOT_FOUND", "Warehouse not found.", StatusCodes.Status404NotFound);

        var orderCount = await dbContext.SalesOrders.CountAsync(cancellationToken);
        var orderNumber = $"SO-{DateTime.UtcNow:yyyyMM}-{orderCount + 1:D4}";

        long subtotal = 0;
        long totalTax = 0;

        var order = new SalesOrderRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            WarehouseId = warehouse.Id,
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            OrderDate = request.OrderDate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            DeliveryAddress = request.DeliveryAddress,
            Notes = request.Notes,
            Status = "Submitted",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var lineReq in request.Lines)
        {
            var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == lineReq.ProductId && p.CompanyId == companyId, cancellationToken)
                ?? throw new DomainException("PRODUCT_NOT_FOUND", $"Product {lineReq.ProductId} not found.", StatusCodes.Status404NotFound);

            var lineSubtotal = (long)Math.Round((double)lineReq.Quantity * lineReq.UnitPricePaise) - lineReq.DiscountAmountPaise;
            var lineTax = (long)Math.Round(lineSubtotal * ((double)lineReq.TaxRatePercent / 100.0));
            var lineTotal = lineSubtotal + lineTax;

            subtotal += lineSubtotal;
            totalTax += lineTax;

            order.Lines.Add(new SalesOrderLineRecord
            {
                Id = Guid.NewGuid(),
                SalesOrderId = order.Id,
                ProductId = product.Id,
                VariantId = lineReq.VariantId,
                Quantity = lineReq.Quantity,
                FulfilledQuantity = 0,
                InvoicedQuantity = 0,
                UnitPricePaise = lineReq.UnitPricePaise,
                DiscountAmountPaise = lineReq.DiscountAmountPaise,
                TaxRatePercent = lineReq.TaxRatePercent,
                TaxAmountPaise = lineTax,
                LineTotalPaise = lineTotal,
                Status = "Pending"
            });
        }

        order.SubtotalPaise = subtotal;
        order.TaxAmountPaise = totalTax;
        order.TotalAmountPaise = subtotal + totalTax;

        dbContext.SalesOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetOrderByIdAsync(companyId, order.Id, cancellationToken);
    }

    public async Task<SalesOrderDto> ConfirmOrderAsync(Guid companyId, Guid id, Guid confirmedByUserId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ORDER_NOT_FOUND", "Sales order not found.", StatusCodes.Status404NotFound);

        if (order.Status != "Submitted")
            throw new DomainException("INVALID_STATE", $"Cannot confirm order in {order.Status} status.", StatusCodes.Status400BadRequest);

        order.Status = "Confirmed";
        order.ConfirmedByUserId = confirmedByUserId;
        order.ConfirmedAtUtc = DateTimeOffset.UtcNow;

        // Check stock availability and create backorders if stock is insufficient
        foreach (var line in order.Lines)
        {
            var stockBalance = await dbContext.StockBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == order.WarehouseId && b.ProductId == line.ProductId, cancellationToken);

            var currentStock = stockBalance?.QuantityOnHand ?? 0m;
            if (currentStock < line.Quantity)
            {
                var shortage = line.Quantity - Math.Max(0, currentStock);
                dbContext.Backorders.Add(new BackorderRecord
                {
                    Id = Guid.NewGuid(),
                    SalesOrderId = order.Id,
                    SalesOrderLineId = line.Id,
                    ProductId = line.ProductId,
                    PendingQuantity = shortage,
                    Status = "Pending",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOrderByIdAsync(companyId, order.Id, cancellationToken);
    }

    public async Task<OrderFulfillmentDto> FulfillOrderAsync(Guid companyId, Guid id, FulfillOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ORDER_NOT_FOUND", "Sales order not found.", StatusCodes.Status404NotFound);

        if (order.Status != "Confirmed" && order.Status != "PartiallyFulfilled")
            throw new DomainException("INVALID_STATE", $"Cannot fulfill order in {order.Status} status.", StatusCodes.Status400BadRequest);

        var fulfillmentCount = await dbContext.OrderFulfillments.CountAsync(cancellationToken);
        var fulfillmentNumber = $"FUL-{DateTime.UtcNow:yyyyMM}-{fulfillmentCount + 1:D4}";

        var fulfillment = new OrderFulfillmentRecord
        {
            Id = Guid.NewGuid(),
            SalesOrderId = order.Id,
            FulfillmentNumber = fulfillmentNumber,
            WarehouseId = order.WarehouseId,
            CarrierName = request.CarrierName,
            TrackingNumber = request.TrackingNumber,
            DispatchedAtUtc = DateTimeOffset.UtcNow,
            Status = "Dispatched",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var lineReq in request.Lines)
        {
            var line = order.Lines.FirstOrDefault(l => l.Id == lineReq.SalesOrderLineId)
                ?? throw new DomainException("LINE_NOT_FOUND", "Sales order line not found.", StatusCodes.Status404NotFound);

            var remaining = line.Quantity - line.FulfilledQuantity;
            if (lineReq.QuantityDispatched > remaining)
                throw new DomainException("EXCESS_FULFILLMENT", $"Cannot fulfill more than remaining quantity ({remaining}).", StatusCodes.Status400BadRequest);

            line.FulfilledQuantity += lineReq.QuantityDispatched;
            if (line.FulfilledQuantity >= line.Quantity)
                line.Status = "Fulfilled";
            else
                line.Status = "PartiallyFulfilled";

            fulfillment.Lines.Add(new OrderFulfillmentLineRecord
            {
                Id = Guid.NewGuid(),
                OrderFulfillmentId = fulfillment.Id,
                SalesOrderLineId = line.Id,
                QuantityDispatched = lineReq.QuantityDispatched
            });

            // Physical inventory deduction via StockMovementRecord
            var stockBalance = await dbContext.StockBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == order.WarehouseId && b.ProductId == line.ProductId, cancellationToken);

            var prevQty = stockBalance?.QuantityOnHand ?? 0m;
            if (prevQty < lineReq.QuantityDispatched)
            {
                throw new DomainException("INSUFFICIENT_STOCK", $"Cannot dispatch {lineReq.QuantityDispatched} units of product {line.ProductId}. Only {prevQty} units available in warehouse.", StatusCodes.Status400BadRequest);
            }

            var newQty = prevQty - lineReq.QuantityDispatched;

            if (stockBalance == null)
            {
                stockBalance = new StockBalanceRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BranchId = order.BranchId,
                    WarehouseId = order.WarehouseId,
                    ProductId = line.ProductId,
                    QuantityOnHand = newQty,
                    QuantityAvailable = newQty,
                    QuantityReserved = 0,
                    QuantityDamaged = 0,
                    QuantityExpired = 0,
                    QuantityBlocked = 0,
                    UnitCostPaise = line.UnitPricePaise,
                    LastMovementAtUtc = DateTimeOffset.UtcNow
                };
                dbContext.StockBalances.Add(stockBalance);
            }
            else
            {
                stockBalance.QuantityOnHand = newQty;
                stockBalance.QuantityAvailable = newQty;
                stockBalance.LastMovementAtUtc = DateTimeOffset.UtcNow;
            }

            dbContext.StockMovements.Add(new StockMovementRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = order.BranchId,
                WarehouseId = order.WarehouseId,
                ProductId = line.ProductId,
                MovementType = "SalesIssue",
                Direction = "Out",
                Quantity = lineReq.QuantityDispatched,
                BalanceAfterOnHand = newQty,
                ReferenceType = "SO",
                ReferenceId = order.Id,
                ReferenceNumber = order.OrderNumber,
                PerformedByUserId = Guid.Empty,
                MovementDateUtc = DateTimeOffset.UtcNow,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        dbContext.OrderFulfillments.Add(fulfillment);

        // Update overall order status
        if (order.Lines.All(l => l.FulfilledQuantity >= l.Quantity))
            order.Status = "Fulfilled";
        else
            order.Status = "PartiallyFulfilled";

        await dbContext.SaveChangesAsync(cancellationToken);

        var linesDto = fulfillment.Lines.Select(fl => new OrderFulfillmentLineDto(
            fl.Id,
            fl.SalesOrderLineId,
            order.Lines.First(l => l.Id == fl.SalesOrderLineId).Product?.Name ?? "Product",
            fl.QuantityDispatched)).ToList();

        var warehouse = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == order.WarehouseId, cancellationToken);

        return new OrderFulfillmentDto(
            fulfillment.Id,
            fulfillment.SalesOrderId,
            fulfillment.FulfillmentNumber,
            fulfillment.WarehouseId,
            warehouse?.Name ?? "Warehouse",
            fulfillment.CarrierName,
            fulfillment.TrackingNumber,
            fulfillment.DispatchedAtUtc,
            fulfillment.DeliveredAtUtc,
            fulfillment.Status,
            linesDto);
    }

    public async Task<SalesOrderDto> CancelOrderAsync(Guid companyId, Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ORDER_NOT_FOUND", "Sales order not found.", StatusCodes.Status404NotFound);

        if (order.Status == "Invoiced")
            throw new DomainException("INVALID_STATE", "Cannot cancel an invoiced sales order. Issue a sales return / credit note instead.", StatusCodes.Status400BadRequest);

        order.Status = "Cancelled";
        order.Notes = (order.Notes != null ? order.Notes + " | " : "") + $"Cancelled: {reason}";

        // Cancel associated backorders
        var backorders = await dbContext.Backorders
            .Where(b => b.SalesOrderId == order.Id && b.Status == "Pending")
            .ToListAsync(cancellationToken);

        foreach (var b in backorders)
        {
            b.Status = "Cancelled";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOrderByIdAsync(companyId, order.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<BackorderDto>> GetBackordersAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Backorders
            .Include(b => b.SalesOrder)
            .Include(b => b.Product)
            .Where(b => b.SalesOrder!.CompanyId == companyId && b.SalesOrder.BranchId == branchId && b.Status == "Pending")
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new BackorderDto(
                b.Id,
                b.SalesOrderId,
                b.SalesOrder != null ? b.SalesOrder.OrderNumber : "UNKNOWN",
                b.SalesOrderLineId,
                b.ProductId,
                b.Product != null ? b.Product.ProductCode : "PROD",
                b.Product != null ? b.Product.Name : "Product",
                b.PendingQuantity,
                b.Status,
                b.ExpectedRestockDate,
                b.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> ConvertOrderToInvoiceAsync(Guid companyId, Guid id, Guid cashierUserId, string paymentMethod, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ORDER_NOT_FOUND", "Sales order not found.", StatusCodes.Status404NotFound);

        if (order.Status == "Invoiced")
            throw new DomainException("ALREADY_INVOICED", "Sales order has already been invoiced.", StatusCodes.Status400BadRequest);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.StartDate <= today && p.EndDate >= today, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "No active fiscal period covers today's date.", StatusCodes.Status400BadRequest);

        var invCount = await dbContext.SalesInvoices.CountAsync(cancellationToken);
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{invCount + 1:D5}";

        var invoice = new SalesInvoiceRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = order.BranchId,
            InvoiceNumber = invoiceNumber,
            CustomerId = order.CustomerId,
            CashierUserId = cashierUserId,
            InvoiceDateUtc = DateTimeOffset.UtcNow,
            Status = "Completed",
            SubtotalPaise = order.SubtotalPaise,
            DiscountPaise = 0,
            CgstPaise = order.TaxAmountPaise / 2,
            SgstPaise = order.TaxAmountPaise / 2,
            IgstPaise = 0,
            TotalTaxPaise = order.TaxAmountPaise,
            GrandTotalPaise = order.TotalAmountPaise,
            AmountPaidPaise = order.TotalAmountPaise,
            OutstandingBalancePaise = 0,
            PlaceOfSupplyStateCode = "29", // Karnataka standard
            SellerGstin = "29AABCS1429B1Z1",
            Notes = $"Generated from Sales Order {order.OrderNumber}",
            WarehouseId = order.WarehouseId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var line in order.Lines)
        {
            invoice.Lines.Add(new SalesInvoiceLineRecord
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ProductId = line.ProductId,
                ProductCode = line.Product?.ProductCode ?? "PROD",
                ProductName = line.Product?.Name ?? "Product",
                Quantity = line.Quantity,
                UnitPricePaise = line.UnitPricePaise,
                DiscountPercentage = 0,
                DiscountPaise = line.DiscountAmountPaise,
                GstRatePercentage = line.TaxRatePercent,
                CgstPercentage = line.TaxRatePercent / 2,
                SgstPercentage = line.TaxRatePercent / 2,
                IgstPercentage = 0,
                TaxAmountPaise = line.TaxAmountPaise,
                LineTotalPaise = line.LineTotalPaise
            });

            line.InvoicedQuantity = line.Quantity;
        }

        // Post Sales Double-Entry Voucher: Debit Cash/Bank/Debtors, Credit Sales 4101, Credit Output GST 2121/2122
        var voucher = new JournalVoucher(
            companyId,
            order.BranchId,
            period.Id,
            VoucherType.Sales,
            today,
            cashierUserId,
            $"Invoice {invoice.InvoiceNumber} from Order {order.OrderNumber}",
            referenceNumber: order.OrderNumber,
            voucherNumber: string.Empty,
            idempotencyKey: Guid.NewGuid(),
            sourceModule: "ORDERS",
            sourceDocumentId: invoice.Id);

        var accSales = await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "4101", cancellationToken);
        var accCgst = await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "2121", cancellationToken);
        var accSgst = await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "2122", cancellationToken);
        var accTender = paymentMethod == "Credit"
            ? await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "1201", cancellationToken) // Debtors
            : await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "1111", cancellationToken); // Cash

        voucher.AddLine(accTender.Id, Money.FromPaise(invoice.GrandTotalPaise), Money.Zero, $"Tender received: {paymentMethod}", partyId: order.CustomerId);
        voucher.AddLine(accSales.Id, Money.Zero, Money.FromPaise(invoice.SubtotalPaise), "Commercial Sales Revenue");
        if (invoice.CgstPaise > 0)
            voucher.AddLine(accCgst.Id, Money.Zero, Money.FromPaise(invoice.CgstPaise), "Output CGST Payable");
        if (invoice.SgstPaise > 0)
            voucher.AddLine(accSgst.Id, Money.Zero, Money.FromPaise(invoice.SgstPaise), "Output SGST Payable");

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var posted = await postingService.PostVoucherAsync(voucher, "MAIN", fyCode, cashierUserId, hasSupervisorElevation: true, cancellationToken);
        invoice.AccountingVoucherId = posted.Id;

        dbContext.SalesInvoices.Add(invoice);
        order.Status = "Invoiced";

        await dbContext.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    private static SalesOrderDto MapOrder(SalesOrderRecord o) => new(
        o.Id,
        o.CompanyId,
        o.BranchId,
        o.WarehouseId,
        o.Warehouse?.Name ?? "Warehouse",
        o.OrderNumber,
        o.CustomerId,
        o.Customer?.Name ?? "Customer",
        o.OrderDate,
        o.ExpectedDeliveryDate,
        o.SubtotalPaise,
        o.TaxAmountPaise,
        o.TotalAmountPaise,
        o.DeliveryAddress,
        o.Notes,
        o.Status,
        o.ConfirmedByUserId,
        o.ConfirmedAtUtc,
        o.Lines.Select(l => new SalesOrderLineDto(
            l.Id,
            l.SalesOrderId,
            l.ProductId,
            l.Product?.ProductCode ?? "PROD",
            l.Product?.Name ?? "Product",
            l.VariantId,
            l.Quantity,
            l.FulfilledQuantity,
            l.InvoicedQuantity,
            l.UnitPricePaise,
            l.DiscountAmountPaise,
            l.TaxRatePercent,
            l.TaxAmountPaise,
            l.LineTotalPaise,
            l.Status)).ToList());
}
