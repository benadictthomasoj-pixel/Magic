using Magic.Application.Accounting.Services;
using Magic.Application.Services.Services;
using Magic.Contracts.Services;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Services.Services;

public sealed class ServiceDeskService(
    MagicDbContext dbContext,
    IPostingService postingService) : IServiceDeskService
{
    public async Task<ServiceSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.ServiceRequests
            .Where(r => r.CompanyId == companyId && r.BranchId == branchId)
            .ToListAsync(cancellationToken);

        var open = requests.Count(r => r.Status != "Delivered" && r.Status != "Cancelled");
        var inProgress = requests.Count(r => r.Status == "InProgress");
        var waiting = requests.Count(r => r.Status == "WaitingForParts");
        var completed = requests.Count(r => r.Status == "Completed");

        return new ServiceSummaryDto(open, inProgress, waiting, completed);
    }

    public async Task<IReadOnlyList<ServiceCatalogDto>> GetCatalogAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ServiceCatalogs
            .Where(s => s.CompanyId == companyId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new ServiceCatalogDto(
                s.Id,
                s.CompanyId,
                s.Code,
                s.Name,
                s.Category,
                s.Description,
                s.StandardLaborChargePaise,
                s.EstimatedDurationMinutes,
                s.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceCatalogDto> CreateCatalogEntryAsync(Guid companyId, CreateServiceCatalogRequest request, CancellationToken cancellationToken = default)
    {
        var entry = new ServiceCatalogRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Category = request.Category,
            Description = request.Description,
            StandardLaborChargePaise = request.StandardLaborChargePaise,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.ServiceCatalogs.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ServiceCatalogDto(
            entry.Id,
            entry.CompanyId,
            entry.Code,
            entry.Name,
            entry.Category,
            entry.Description,
            entry.StandardLaborChargePaise,
            entry.EstimatedDurationMinutes,
            entry.IsActive);
    }

    public async Task<(IReadOnlyList<ServiceRequestDto> Items, int TotalCount)> GetRequestsAsync(
        Guid companyId,
        Guid branchId,
        Guid? customerId,
        string? status,
        string? priority,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ServiceRequests
            .Include(r => r.Customer)
            .Include(r => r.ServiceCatalog)
            .Include(r => r.AssignedEmployee)
            .Where(r => r.CompanyId == companyId && r.BranchId == branchId);

        if (customerId.HasValue)
            query = query.Where(r => r.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(r => r.Priority == priority);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => MapRequest(r, null, null, null))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<ServiceRequestDto> GetRequestByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.ServiceRequests
            .Include(r => r.Customer)
            .Include(r => r.ServiceCatalog)
            .Include(r => r.AssignedEmployee)
            .Include(r => r.Quotations)
                .ThenInclude(q => q.Lines)
                    .ThenInclude(l => l.Product)
            .Include(r => r.ConsumedParts)
                .ThenInclude(p => p.Product)
            .Include(r => r.ConsumedParts)
                .ThenInclude(p => p.Warehouse)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("REQUEST_NOT_FOUND", "Service request ticket not found.", StatusCodes.Status404NotFound);

        var link = await dbContext.ServiceInvoiceLinks
            .FirstOrDefaultAsync(l => l.ServiceRequestId == request.Id, cancellationToken);

        var quotesDto = request.Quotations.Select(q => new ServiceQuotationDto(
            q.Id,
            q.ServiceRequestId,
            q.QuotationNumber,
            q.LaborTotalPaise,
            q.PartsTotalPaise,
            q.DiscountPaise,
            q.TaxAmountPaise,
            q.TotalAmountPaise,
            q.ValidUntil,
            q.Status,
            q.ApprovedAtUtc,
            q.Lines.Select(l => new ServiceQuotationLineDto(
                l.Id,
                l.ServiceQuotationId,
                l.LineType,
                l.ProductId,
                l.Product?.ProductCode,
                l.Description,
                l.Quantity,
                l.UnitRatePaise,
                l.LineTotalPaise)).ToList())).ToList();

        var partsDto = request.ConsumedParts.Select(p => new ServicePartConsumptionDto(
            p.Id,
            p.ServiceRequestId,
            p.ProductId,
            p.Product?.ProductCode ?? "PART",
            p.Product?.Name ?? "Spare Part",
            p.WarehouseId,
            p.Warehouse?.Name ?? "Warehouse",
            p.Quantity,
            p.UnitCostPaise,
            p.StockMovementId,
            p.ConsumedAtUtc)).ToList();

        return MapRequest(request, quotesDto, partsDto, link?.SalesInvoiceId);
    }

    public async Task<ServiceRequestDto> CreateRequestAsync(Guid companyId, Guid branchId, CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("CUSTOMER_NOT_FOUND", "Customer not found.", StatusCodes.Status404NotFound);

        var catalog = await dbContext.ServiceCatalogs.FirstOrDefaultAsync(s => s.Id == request.ServiceCatalogId && s.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("SERVICE_NOT_FOUND", "Service catalog entry not found.", StatusCodes.Status404NotFound);

        var reqCount = await dbContext.ServiceRequests.CountAsync(cancellationToken);
        var reqNumber = $"REQ-{DateTime.UtcNow:yyyyMM}-{reqCount + 1:D4}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

        var serviceRequest = new ServiceRequestRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            RequestNumber = reqNumber,
            CustomerId = customer.Id,
            ServiceCatalogId = catalog.Id,
            ItemDescription = request.ItemDescription,
            SerialNumber = request.SerialNumber,
            ReportedIssue = request.ReportedIssue,
            Priority = request.Priority,
            Status = "NewRequest",
            ReceivedDate = request.ReceivedDate,
            TargetCompletionDate = request.TargetCompletionDate,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.ServiceRequests.Add(serviceRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRequestByIdAsync(companyId, serviceRequest.Id, cancellationToken);
    }

    public async Task<ServiceRequestDto> UpdateRequestStatusAsync(Guid companyId, Guid id, UpdateServiceRequestStatusRequest request, CancellationToken cancellationToken = default)
    {
        var serviceRequest = await dbContext.ServiceRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("REQUEST_NOT_FOUND", "Service request ticket not found.", StatusCodes.Status404NotFound);

        serviceRequest.Status = request.Status;
        if (request.AssignedEmployeeId.HasValue)
            serviceRequest.AssignedEmployeeId = request.AssignedEmployeeId.Value;

        if (!string.IsNullOrWhiteSpace(request.TechnicianNotes))
            serviceRequest.TechnicianNotes = request.TechnicianNotes;

        if (request.Status == "Completed")
            serviceRequest.CompletedAtUtc = DateTimeOffset.UtcNow;
        else if (request.Status == "Delivered")
            serviceRequest.DeliveredAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequestByIdAsync(companyId, serviceRequest.Id, cancellationToken);
    }

    public async Task<ServiceQuotationDto> CreateQuotationAsync(Guid companyId, Guid requestId, CreateServiceQuotationRequest request, CancellationToken cancellationToken = default)
    {
        var serviceRequest = await dbContext.ServiceRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("REQUEST_NOT_FOUND", "Service request ticket not found.", StatusCodes.Status404NotFound);

        var quoteCount = await dbContext.ServiceQuotations.CountAsync(cancellationToken);
        var quoteNumber = $"SQ-{DateTime.UtcNow:yyyyMM}-{quoteCount + 1:D4}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

        long laborTotal = 0;
        long partsTotal = 0;

        var quotation = new ServiceQuotationRecord
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            QuotationNumber = quoteNumber,
            DiscountPaise = request.DiscountPaise,
            ValidUntil = request.ValidUntil,
            Status = "Draft",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var lineReq in request.Lines)
        {
            var lineTotal = (long)Math.Round((double)lineReq.Quantity * lineReq.UnitRatePaise);
            if (lineReq.LineType == "Labor")
                laborTotal += lineTotal;
            else
                partsTotal += lineTotal;

            quotation.Lines.Add(new ServiceQuotationLineRecord
            {
                Id = Guid.NewGuid(),
                ServiceQuotationId = quotation.Id,
                LineType = lineReq.LineType,
                ProductId = lineReq.ProductId,
                Description = lineReq.Description,
                Quantity = lineReq.Quantity,
                UnitRatePaise = lineReq.UnitRatePaise,
                LineTotalPaise = lineTotal
            });
        }

        var taxable = laborTotal + partsTotal - request.DiscountPaise;
        var tax = (long)Math.Round(taxable * ((double)request.TaxRatePercent / 100.0));

        quotation.LaborTotalPaise = laborTotal;
        quotation.PartsTotalPaise = partsTotal;
        quotation.TaxAmountPaise = tax;
        quotation.TotalAmountPaise = taxable + tax;

        serviceRequest.Status = "Quotation";

        dbContext.ServiceQuotations.Add(quotation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var linesDto = quotation.Lines.Select(l => new ServiceQuotationLineDto(
            l.Id,
            l.ServiceQuotationId,
            l.LineType,
            l.ProductId,
            null,
            l.Description,
            l.Quantity,
            l.UnitRatePaise,
            l.LineTotalPaise)).ToList();

        return new ServiceQuotationDto(
            quotation.Id,
            quotation.ServiceRequestId,
            quotation.QuotationNumber,
            quotation.LaborTotalPaise,
            quotation.PartsTotalPaise,
            quotation.DiscountPaise,
            quotation.TaxAmountPaise,
            quotation.TotalAmountPaise,
            quotation.ValidUntil,
            quotation.Status,
            quotation.ApprovedAtUtc,
            linesDto);
    }

    public async Task<ServiceQuotationDto> ApproveQuotationAsync(Guid companyId, Guid quotationId, CancellationToken cancellationToken = default)
    {
        var quotation = await dbContext.ServiceQuotations
            .Include(q => q.ServiceRequest)
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == quotationId && q.ServiceRequest!.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("QUOTATION_NOT_FOUND", "Quotation not found.", StatusCodes.Status404NotFound);

        quotation.Status = "Approved";
        quotation.ApprovedAtUtc = DateTimeOffset.UtcNow;
        quotation.ServiceRequest!.Status = "Approved";

        await dbContext.SaveChangesAsync(cancellationToken);

        var linesDto = quotation.Lines.Select(l => new ServiceQuotationLineDto(
            l.Id,
            l.ServiceQuotationId,
            l.LineType,
            l.ProductId,
            null,
            l.Description,
            l.Quantity,
            l.UnitRatePaise,
            l.LineTotalPaise)).ToList();

        return new ServiceQuotationDto(
            quotation.Id,
            quotation.ServiceRequestId,
            quotation.QuotationNumber,
            quotation.LaborTotalPaise,
            quotation.PartsTotalPaise,
            quotation.DiscountPaise,
            quotation.TaxAmountPaise,
            quotation.TotalAmountPaise,
            quotation.ValidUntil,
            quotation.Status,
            quotation.ApprovedAtUtc,
            linesDto);
    }

    public async Task<ServicePartConsumptionDto> ConsumePartAsync(Guid companyId, Guid requestId, ConsumeServicePartRequest request, CancellationToken cancellationToken = default)
    {
        var serviceRequest = await dbContext.ServiceRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("REQUEST_NOT_FOUND", "Service request ticket not found.", StatusCodes.Status404NotFound);

        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("PRODUCT_NOT_FOUND", "Product not found.", StatusCodes.Status404NotFound);

        var warehouse = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("WAREHOUSE_NOT_FOUND", "Warehouse not found.", StatusCodes.Status404NotFound);

        var stockBalance = await dbContext.StockBalances
            .FirstOrDefaultAsync(b => b.WarehouseId == warehouse.Id && b.ProductId == product.Id, cancellationToken);

        var prevQty = stockBalance?.QuantityOnHand ?? 0m;
        var newQty = prevQty - request.Quantity;

        var price = await dbContext.ProductPrices
            .FirstOrDefaultAsync(p => p.ProductId == product.Id && p.IsActive, cancellationToken);
        var unitCost = price?.UnitPricePaise ?? 0;

        if (stockBalance == null)
        {
            stockBalance = new StockBalanceRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = serviceRequest.BranchId,
                WarehouseId = warehouse.Id,
                ProductId = product.Id,
                QuantityOnHand = newQty,
                QuantityAvailable = newQty,
                QuantityReserved = 0,
                QuantityDamaged = 0,
                QuantityExpired = 0,
                QuantityBlocked = 0,
                UnitCostPaise = unitCost,
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

        var movement = new StockMovementRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = serviceRequest.BranchId,
            WarehouseId = warehouse.Id,
            ProductId = product.Id,
            MovementType = "ServicePartConsumption",
            Direction = "Out",
            Quantity = request.Quantity,
            BalanceAfterOnHand = newQty,
            ReferenceType = "ServiceRequest",
            ReferenceId = serviceRequest.Id,
            PerformedByUserId = Guid.Empty,
            MovementDateUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.StockMovements.Add(movement);

        var consumption = new ServicePartConsumptionRecord
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = request.Quantity,
            UnitCostPaise = unitCost,
            StockMovementId = movement.Id,
            ConsumedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.ServicePartConsumptions.Add(consumption);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ServicePartConsumptionDto(
            consumption.Id,
            consumption.ServiceRequestId,
            product.Id,
            product.ProductCode,
            product.Name,
            warehouse.Id,
            warehouse.Name,
            consumption.Quantity,
            consumption.UnitCostPaise,
            consumption.StockMovementId,
            consumption.ConsumedAtUtc);
    }

    public async Task<Guid> InvoiceServiceRequestAsync(Guid companyId, Guid requestId, Guid cashierUserId, InvoiceServiceRequest request, CancellationToken cancellationToken = default)
    {
        var serviceRequest = await dbContext.ServiceRequests
            .Include(r => r.Customer)
            .Include(r => r.Quotations)
                .ThenInclude(q => q.Lines)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("REQUEST_NOT_FOUND", "Service request ticket not found.", StatusCodes.Status404NotFound);

        var approvedQuote = serviceRequest.Quotations.FirstOrDefault(q => q.Status == "Approved")
            ?? throw new DomainException("NO_APPROVED_QUOTATION", "Service request must have an approved quotation before invoicing.", StatusCodes.Status400BadRequest);

        var existingLink = await dbContext.ServiceInvoiceLinks.FirstOrDefaultAsync(l => l.ServiceRequestId == serviceRequest.Id, cancellationToken);
        if (existingLink != null)
            throw new DomainException("ALREADY_INVOICED", "This service request has already been invoiced.", StatusCodes.Status400BadRequest);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.StartDate <= today && p.EndDate >= today, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "No active fiscal period covers today's date.", StatusCodes.Status400BadRequest);

        var invCount = await dbContext.SalesInvoices.CountAsync(cancellationToken);
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{invCount + 1:D5}";

        var subtotal = approvedQuote.LaborTotalPaise + approvedQuote.PartsTotalPaise - approvedQuote.DiscountPaise;
        var tax = approvedQuote.TaxAmountPaise;
        var grandTotal = approvedQuote.TotalAmountPaise;

        var invoice = new SalesInvoiceRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = serviceRequest.BranchId,
            InvoiceNumber = invoiceNumber,
            CustomerId = serviceRequest.CustomerId,
            CashierUserId = cashierUserId,
            InvoiceDateUtc = DateTimeOffset.UtcNow,
            Status = "Completed",
            SubtotalPaise = subtotal,
            DiscountPaise = approvedQuote.DiscountPaise,
            CgstPaise = tax / 2,
            SgstPaise = tax / 2,
            IgstPaise = 0,
            TotalTaxPaise = tax,
            GrandTotalPaise = grandTotal,
            AmountPaidPaise = grandTotal,
            OutstandingBalancePaise = 0,
            PlaceOfSupplyStateCode = "29",
            SellerGstin = "29AABCS1429B1Z1",
            Notes = $"Generated from Service Ticket {serviceRequest.RequestNumber}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var fallbackProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.IsActive, cancellationToken);

        foreach (var qLine in approvedQuote.Lines)
        {
            var pCode = qLine.Product?.ProductCode ?? (qLine.LineType == "Labor" ? "SRV-LABOR" : "SRV-PART");
            var pName = !string.IsNullOrWhiteSpace(qLine.Description) ? qLine.Description : "Service Item";
            var taxAmount = (long)Math.Round(qLine.LineTotalPaise * 0.18);
            invoice.Lines.Add(new SalesInvoiceLineRecord
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ProductId = qLine.ProductId ?? fallbackProduct?.Id ?? Guid.Empty,
                ProductCode = pCode,
                ProductName = pName,
                Quantity = qLine.Quantity,
                UnitPricePaise = qLine.UnitRatePaise,
                DiscountPercentage = 0,
                DiscountPaise = 0,
                GstRatePercentage = 18m,
                CgstPercentage = 9m,
                SgstPercentage = 9m,
                IgstPercentage = 0,
                TaxAmountPaise = taxAmount,
                LineTotalPaise = qLine.LineTotalPaise + taxAmount
            });
        }

        // Double-entry Voucher
        var voucher = new JournalVoucher(
            companyId,
            serviceRequest.BranchId,
            period.Id,
            VoucherType.Sales,
            today,
            cashierUserId,
            $"Service Invoice {invoice.InvoiceNumber} for {serviceRequest.RequestNumber}",
            referenceNumber: serviceRequest.RequestNumber,
            voucherNumber: string.Empty,
            idempotencyKey: Guid.NewGuid(),
            sourceModule: "SERVICES",
            sourceDocumentId: invoice.Id);

        var accLaborRev = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "4102", cancellationToken)
            ?? await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "4101", cancellationToken);
        var accPartsRev = await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "4101", cancellationToken);
        var accCgst = await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "2121", cancellationToken);
        var accSgst = await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "2122", cancellationToken);

        var accTender = request.PaymentMethod == "Credit"
            ? await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "1201", cancellationToken)
            : await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "1111", cancellationToken);

        voucher.AddLine(accTender.Id, Money.FromPaise(invoice.GrandTotalPaise), Money.Zero, $"Tender received: {request.PaymentMethod}", partyId: serviceRequest.CustomerId);
        if (approvedQuote.LaborTotalPaise > 0)
            voucher.AddLine(accLaborRev.Id, Money.Zero, Money.FromPaise(approvedQuote.LaborTotalPaise), "Service & Labor Revenue");
        if (approvedQuote.PartsTotalPaise > 0)
            voucher.AddLine(accPartsRev.Id, Money.Zero, Money.FromPaise(approvedQuote.PartsTotalPaise), "Spare Parts Revenue");
        if (invoice.CgstPaise > 0)
            voucher.AddLine(accCgst.Id, Money.Zero, Money.FromPaise(invoice.CgstPaise), "Output CGST Payable");
        if (invoice.SgstPaise > 0)
            voucher.AddLine(accSgst.Id, Money.Zero, Money.FromPaise(invoice.SgstPaise), "Output SGST Payable");

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var posted = await postingService.PostVoucherAsync(voucher, "MAIN", fyCode, cashierUserId, hasSupervisorElevation: true, cancellationToken);
        invoice.AccountingVoucherId = posted.Id;

        dbContext.SalesInvoices.Add(invoice);

        dbContext.ServiceInvoiceLinks.Add(new ServiceInvoiceLinkRecord
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            SalesInvoiceId = invoice.Id,
            InvoicedAtUtc = DateTimeOffset.UtcNow
        });

        serviceRequest.Status = "Delivered";
        serviceRequest.DeliveredAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    private static ServiceRequestDto MapRequest(
        ServiceRequestRecord r,
        IReadOnlyList<ServiceQuotationDto>? quotes,
        IReadOnlyList<ServicePartConsumptionDto>? parts,
        Guid? invoiceId) => new(
            r.Id,
            r.CompanyId,
            r.BranchId,
            r.RequestNumber,
            r.CustomerId,
            r.Customer?.Name ?? "Customer",
            r.ServiceCatalogId,
            r.ServiceCatalog?.Name ?? "Service",
            r.ItemDescription,
            r.SerialNumber,
            r.ReportedIssue,
            r.Priority,
            r.Status,
            r.AssignedEmployeeId,
            r.AssignedEmployee != null ? $"{r.AssignedEmployee.FirstName} {r.AssignedEmployee.LastName}" : null,
            r.ReceivedDate,
            r.TargetCompletionDate,
            r.CompletedAtUtc,
            r.DeliveredAtUtc,
            r.TechnicianNotes,
            invoiceId,
            r.CreatedAtUtc,
            quotes,
            parts);
}
