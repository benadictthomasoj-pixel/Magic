using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Magic.Application.Accounting.Services;
using Magic.Application.Finance.Services;
using Magic.Contracts.Finance;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Finance.Services;

public sealed class ReceivablesService(MagicDbContext dbContext) : IReceivablesService
{
    public async Task<ReceivablesSummaryDto> GetSummaryAsync(
        Guid companyId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var openItems = await dbContext.ArSubledgerItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.BranchId == branchId && i.Status != "PAID")
            .ToListAsync(cancellationToken);

        var totalReceivable = openItems.Sum(i => i.OutstandingAmountPaise);
        var totalOverdue = openItems.Where(i => i.DueDate < today).Sum(i => i.OutstandingAmountPaise);
        var customerCount = openItems.Select(i => i.CustomerId).Distinct().Count();

        return new ReceivablesSummaryDto
        {
            TotalReceivablePaise = totalReceivable,
            TotalOverduePaise = totalOverdue,
            CustomerCount = customerCount,
            OpenInvoiceCount = openItems.Count
        };
    }

    public async Task<(IReadOnlyList<CustomerReceivableDto> Items, int TotalCount)> GetReceivablesAsync(
        Guid companyId,
        Guid branchId,
        Guid? customerId,
        string? status,
        bool overdueOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = dbContext.ArSubledgerItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.BranchId == branchId);

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (overdueOnly)
        {
            query = query.Where(i => i.DueDate < today && i.Status != "PAID");
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var customerIds = items.Select(i => i.CustomerId).Distinct().ToList();
        var customerMap = await dbContext.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var invoiceIds = items.Where(i => i.InvoiceId.HasValue).Select(i => i.InvoiceId!.Value).Distinct().ToList();
        var invoiceMap = await dbContext.SalesInvoices
            .AsNoTracking()
            .Where(inv => invoiceIds.Contains(inv.Id))
            .ToDictionaryAsync(inv => inv.Id, inv => inv.InvoiceNumber, cancellationToken);

        var result = items.Select(i =>
        {
            var overdueDays = i.DueDate < today ? (today.DayNumber - i.DueDate.DayNumber) : 0;
            var docNumber = i.InvoiceId.HasValue && invoiceMap.TryGetValue(i.InvoiceId.Value, out var invNum)
                ? invNum
                : $"AR-{i.Id.ToString()[..8].ToUpperInvariant()}";

            return new CustomerReceivableDto
            {
                Id = i.Id,
                CustomerId = i.CustomerId,
                CustomerName = customerMap.GetValueOrDefault(i.CustomerId, "Customer"),
                DocumentNumber = docNumber,
                InvoiceId = i.InvoiceId,
                VoucherId = i.VoucherId,
                ItemDate = i.ItemDate,
                DueDate = i.DueDate,
                OriginalAmountPaise = i.OriginalAmountPaise,
                SettledAmountPaise = i.SettledAmountPaise,
                OutstandingAmountPaise = i.OutstandingAmountPaise,
                Status = i.Status,
                OverdueDays = overdueDays
            };
        }).ToList();

        return (result, totalCount);
    }

    public async Task<CustomerAgingReportDto> GetAgingReportAsync(
        Guid companyId,
        Guid branchId,
        DateOnly reportDate,
        CancellationToken cancellationToken = default)
    {
        var openItems = await dbContext.ArSubledgerItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.BranchId == branchId && i.Status != "PAID")
            .ToListAsync(cancellationToken);

        var customerIds = openItems.Select(i => i.CustomerId).Distinct().ToList();
        var customerMap = await dbContext.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var grouped = openItems.GroupBy(i => i.CustomerId).Select(g =>
        {
            var cId = g.Key;
            var cName = customerMap.GetValueOrDefault(cId, "Customer");
            long current = 0, d1To30 = 0, d31To60 = 0, d61To90 = 0, dOver90 = 0;

            foreach (var item in g)
            {
                var days = reportDate.DayNumber - item.DueDate.DayNumber;
                if (days <= 0)
                {
                    current += item.OutstandingAmountPaise;
                }
                else if (days <= 30)
                {
                    d1To30 += item.OutstandingAmountPaise;
                }
                else if (days <= 60)
                {
                    d31To60 += item.OutstandingAmountPaise;
                }
                else if (days <= 90)
                {
                    d61To90 += item.OutstandingAmountPaise;
                }
                else
                {
                    dOver90 += item.OutstandingAmountPaise;
                }
            }

            var total = current + d1To30 + d31To60 + d61To90 + dOver90;
            return new CustomerAgingRowDto
            {
                CustomerId = cId,
                CustomerName = cName,
                TotalOutstandingPaise = total,
                CurrentPaise = current,
                Overdue1To30Paise = d1To30,
                Overdue31To60Paise = d31To60,
                Overdue61To90Paise = d61To90,
                OverdueAbove90Paise = dOver90
            };
        }).OrderByDescending(r => r.TotalOutstandingPaise).ToList();

        return new CustomerAgingReportDto
        {
            ReportDate = reportDate,
            TotalOutstandingPaise = grouped.Sum(r => r.TotalOutstandingPaise),
            TotalCurrentPaise = grouped.Sum(r => r.CurrentPaise),
            Total1To30Paise = grouped.Sum(r => r.Overdue1To30Paise),
            Total31To60Paise = grouped.Sum(r => r.Overdue31To60Paise),
            Total61To90Paise = grouped.Sum(r => r.Overdue61To90Paise),
            TotalAbove90Paise = grouped.Sum(r => r.OverdueAbove90Paise),
            Customers = grouped
        };
    }

    public async Task<CustomerStatementDto> GetCustomerStatementAsync(
        Guid companyId,
        Guid branchId,
        Guid customerId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new DomainException("CUSTOMER_NOT_FOUND", $"Customer '{customerId}' not found.", 404);

        // Fetch AR items (Invoices)
        var arItems = await dbContext.ArSubledgerItems
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.ItemDate <= toDate)
            .OrderBy(i => i.ItemDate)
            .ToListAsync(cancellationToken);

        // Fetch Customer Receipts
        var receipts = await dbContext.CustomerReceipts
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId && r.ReceiptDate <= toDate && r.Status == "SUBMITTED")
            .OrderBy(r => r.ReceiptDate)
            .ToListAsync(cancellationToken);

        var lines = new List<CustomerStatementLineDto>();
        long openingBalance = 0;

        foreach (var item in arItems.Where(i => i.ItemDate < fromDate))
        {
            openingBalance += item.OriginalAmountPaise;
        }
        foreach (var r in receipts.Where(r => r.ReceiptDate < fromDate))
        {
            openingBalance -= r.TotalAmountPaise;
        }

        var runningBalance = openingBalance;
        long totalDebit = 0;
        long totalCredit = 0;

        var periodItems = arItems.Where(i => i.ItemDate >= fromDate && i.ItemDate <= toDate).Select(i => new
        {
            Date = i.ItemDate,
            Type = "Invoice",
            Number = i.InvoiceId.HasValue ? $"INV-{i.InvoiceId.Value.ToString()[..8].ToUpperInvariant()}" : $"AR-{i.Id.ToString()[..8].ToUpperInvariant()}",
            Ref = "",
            Debit = i.OriginalAmountPaise,
            Credit = 0L,
            Narration = $"Sales Invoice (Due: {i.DueDate:yyyy-MM-dd})"
        });

        var periodReceipts = receipts.Where(r => r.ReceiptDate >= fromDate && r.ReceiptDate <= toDate).Select(r => new
        {
            Date = r.ReceiptDate,
            Type = "Receipt",
            Number = r.ReceiptNumber,
            Ref = r.ReferenceNumber ?? "",
            Debit = 0L,
            Credit = r.TotalAmountPaise,
            Narration = string.IsNullOrWhiteSpace(r.Narration) ? $"Payment Received via {r.PaymentMethod}" : r.Narration
        });

        var chronological = periodItems.Concat(periodReceipts).OrderBy(x => x.Date).ToList();
        foreach (var entry in chronological)
        {
            runningBalance += entry.Debit - entry.Credit;
            totalDebit += entry.Debit;
            totalCredit += entry.Credit;

            lines.Add(new CustomerStatementLineDto
            {
                Date = entry.Date,
                DocumentType = entry.Type,
                DocumentNumber = entry.Number,
                ReferenceNumber = entry.Ref,
                DebitPaise = entry.Debit,
                CreditPaise = entry.Credit,
                RunningBalancePaise = runningBalance,
                Narration = entry.Narration
            });
        }

        return new CustomerStatementDto
        {
            CustomerId = customerId,
            CustomerName = customer.Name,
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalancePaise = openingBalance,
            TotalDebitPaise = totalDebit,
            TotalCreditPaise = totalCredit,
            ClosingBalancePaise = runningBalance,
            Lines = lines
        };
    }
}

public sealed class CustomerReceiptService(
    MagicDbContext dbContext,
    IPostingService postingService) : ICustomerReceiptService
{
    public async Task<(IReadOnlyList<CustomerReceiptDto> Items, int TotalCount)> GetReceiptsAsync(
        Guid companyId,
        Guid branchId,
        Guid? customerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CustomerReceipts
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.BankCashAccount)
            .Include(r => r.Voucher)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.ArItem)
            .Where(r => r.CompanyId == companyId && r.BranchId == branchId);

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            query = query.Where(r => r.CustomerId == customerId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.ReceiptDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.ReceiptDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var list = await query
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = list.Select(MapToReceiptDto).ToList();
        return (dtos, totalCount);
    }

    public async Task<CustomerReceiptDto> GetReceiptByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.CustomerReceipts
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.BankCashAccount)
            .Include(r => r.Voucher)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.ArItem)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("RECEIPT_NOT_FOUND", $"Customer receipt '{id}' not found.", 404);

        return MapToReceiptDto(receipt);
    }

    public async Task<CustomerReceiptDto> CreateReceiptAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateCustomerReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TotalAmountPaise <= 0)
        {
            throw new DomainException("INVALID_RECEIPT_AMOUNT", "Receipt amount must be strictly greater than zero.");
        }

        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("CUSTOMER_NOT_FOUND", "Customer not found.", 404);

        var bankAccount = await dbContext.BankCashAccounts
            .FirstOrDefaultAsync(b => b.Id == request.BankCashAccountId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BANK_ACCOUNT_NOT_FOUND", "Bank or Cash account not found.", 404);

        if (!bankAccount.IsActive)
        {
            throw new DomainException("BANK_ACCOUNT_INACTIVE", "Selected bank/cash account is inactive.");
        }

        // Customer Debtors Control Account (GL 1201)
        var debtorAccount = await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1201", cancellationToken)
            ?? throw new DomainException("CONTROL_ACCOUNT_NOT_FOUND", "Trade Debtors Control Account (1201) not found in Chart of Accounts.", 500);

        // Find active fiscal period for ReceiptDate
        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && request.ReceiptDate >= p.StartDate && request.ReceiptDate <= p.EndDate, cancellationToken)
            ?? throw new DomainException("FISCAL_PERIOD_NOT_FOUND", $"No fiscal period found covering receipt date {request.ReceiptDate:yyyy-MM-dd}.");

        var allocations = request.Allocations ?? [];
        var totalAllocated = allocations.Sum(a => a.AllocatedAmountPaise);
        if (totalAllocated > request.TotalAmountPaise)
        {
            throw new DomainException("ALLOCATION_EXCEEDS_TOTAL", "Sum of invoice allocations cannot exceed total receipt amount.");
        }

        var receiptId = Guid.NewGuid();
        var count = await dbContext.CustomerReceipts.CountAsync(r => r.CompanyId == companyId, cancellationToken);
        var receiptNumber = $"REC-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        // 1. Double-entry Journal Voucher (Receipt Type 5)
        // Debit: Bank/Cash Account (Amount)
        // Credit: Trade Debtors Control Account (Amount)
        var voucher = new JournalVoucher(
            companyId,
            branchId,
            period.Id,
            VoucherType.Receipt,
            request.ReceiptDate,
            userId,
            narration: string.IsNullOrWhiteSpace(request.Narration)
                ? $"Customer Receipt {receiptNumber} from {customer.Name}"
                : request.Narration,
            referenceNumber: request.ReferenceNumber,
            idempotencyKey: Guid.CreateVersion7(),
            sourceModule: "Finance",
            sourceDocumentId: receiptId,
            sourceTransactionType: "CustomerReceipt");

        voucher.AddLine(bankAccount.GlAccountId, Money.FromPaise(request.TotalAmountPaise), Money.Zero, $"Deposit from {customer.Name}", partyId: customer.Id);
        voucher.AddLine(debtorAccount.Id, Money.Zero, Money.FromPaise(request.TotalAmountPaise), $"Debtor settlement {customer.Name}", partyId: customer.Id);

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var branchCode = "MAIN";
        var postedVoucher = await postingService.PostVoucherAsync(voucher, branchCode, fyCode, userId, hasSupervisorElevation: true, cancellationToken);

        // 2. Create Receipt Record
        var receipt = new CustomerReceiptRecord
        {
            Id = receiptId,
            CompanyId = companyId,
            BranchId = branchId,
            ReceiptNumber = receiptNumber,
            CustomerId = customer.Id,
            ReceiptDate = request.ReceiptDate,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber,
            BankCashAccountId = bankAccount.Id,
            TotalAmountPaise = request.TotalAmountPaise,
            AllocatedAmountPaise = totalAllocated,
            UnallocatedAmountPaise = request.TotalAmountPaise - totalAllocated,
            Status = "SUBMITTED",
            VoucherId = postedVoucher.Id,
            Narration = voucher.Narration,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.CustomerReceipts.Add(receipt);

        // 3. Process Allocations against open AR items
        foreach (var allocReq in allocations)
        {
            if (allocReq.AllocatedAmountPaise <= 0) continue;

            var arItem = await dbContext.ArSubledgerItems
                .FirstOrDefaultAsync(i => i.Id == allocReq.ArItemId && i.CustomerId == customer.Id, cancellationToken)
                ?? throw new DomainException("AR_ITEM_NOT_FOUND", $"Open receivable item '{allocReq.ArItemId}' was not found for this customer.");

            if (allocReq.AllocatedAmountPaise > arItem.OutstandingAmountPaise)
            {
                throw new DomainException("OVER_ALLOCATION", $"Allocation amount ({allocReq.AllocatedAmountPaise / 100.0:F2}) exceeds outstanding balance ({arItem.OutstandingAmountPaise / 100.0:F2}).");
            }

            arItem.SettledAmountPaise += allocReq.AllocatedAmountPaise;
            arItem.OutstandingAmountPaise -= allocReq.AllocatedAmountPaise;
            arItem.Status = arItem.OutstandingAmountPaise == 0 ? "PAID" : "PARTIALLY_PAID";

            var allocRecord = new CustomerReceiptAllocationRecord
            {
                Id = Guid.NewGuid(),
                ReceiptId = receiptId,
                ArItemId = arItem.Id,
                AllocatedAmountPaise = allocReq.AllocatedAmountPaise,
                AllocationDate = request.ReceiptDate,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.CustomerReceiptAllocations.Add(allocRecord);
        }

        // 4. Update Bank/Cash Account running balance
        bankAccount.CurrentBalancePaise += request.TotalAmountPaise;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetReceiptByIdAsync(companyId, receiptId, cancellationToken);
    }

    public async Task<CustomerReceiptDto> CancelReceiptAsync(
        Guid companyId,
        Guid branchId,
        Guid id,
        Guid userId,
        CancelReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.CustomerReceipts
            .Include(r => r.Allocations)
                .ThenInclude(a => a.ArItem)
            .Include(r => r.BankCashAccount)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("RECEIPT_NOT_FOUND", "Customer receipt not found.", 404);

        if (receipt.Status == "REVERSED")
        {
            throw new DomainException("ALREADY_REVERSED", "Receipt is already reversed.");
        }

        // Reverse allocations
        foreach (var alloc in receipt.Allocations)
        {
            if (alloc.ArItem != null)
            {
                alloc.ArItem.SettledAmountPaise -= alloc.AllocatedAmountPaise;
                alloc.ArItem.OutstandingAmountPaise += alloc.AllocatedAmountPaise;
                alloc.ArItem.Status = alloc.ArItem.SettledAmountPaise == 0 ? "OPEN" : "PARTIALLY_PAID";
            }
        }

        // Reverse Voucher
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await postingService.ReverseVoucherAsync(
            receipt.VoucherId,
            today,
            "MAIN",
            "FY2026-27",
            userId,
            $"Reversal of Customer Receipt {receipt.ReceiptNumber}: {request.Reason}",
            cancellationToken);

        if (receipt.BankCashAccount != null)
        {
            receipt.BankCashAccount.CurrentBalancePaise -= receipt.TotalAmountPaise;
        }

        receipt.Status = "REVERSED";
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetReceiptByIdAsync(companyId, id, cancellationToken);
    }

    private static CustomerReceiptDto MapToReceiptDto(CustomerReceiptRecord r) => new()
    {
        Id = r.Id,
        ReceiptNumber = r.ReceiptNumber,
        CustomerId = r.CustomerId,
        CustomerName = r.Customer?.Name ?? "Customer",
        ReceiptDate = r.ReceiptDate,
        PaymentMethod = r.PaymentMethod,
        ReferenceNumber = r.ReferenceNumber,
        BankCashAccountId = r.BankCashAccountId,
        BankCashAccountName = r.BankCashAccount?.AccountName ?? "Bank/Cash Account",
        TotalAmountPaise = r.TotalAmountPaise,
        AllocatedAmountPaise = r.AllocatedAmountPaise,
        UnallocatedAmountPaise = r.UnallocatedAmountPaise,
        Status = r.Status,
        VoucherId = r.VoucherId,
        VoucherNumber = r.Voucher?.VoucherNumber ?? string.Empty,
        Narration = r.Narration,
        CreatedAtUtc = r.CreatedAtUtc,
        Allocations = r.Allocations.Select(a => new CustomerReceiptAllocationDto
        {
            Id = a.Id,
            ArItemId = a.ArItemId,
            DocumentNumber = a.ArItem?.InvoiceId.HasValue == true ? $"INV-{a.ArItem.InvoiceId.Value.ToString()[..8].ToUpperInvariant()}" : $"AR-{a.ArItemId.ToString()[..8].ToUpperInvariant()}",
            AllocatedAmountPaise = a.AllocatedAmountPaise,
            AllocationDate = a.AllocationDate
        }).ToList()
    };
}

public sealed class PayablesService(
    MagicDbContext dbContext,
    IPostingService postingService) : IPayablesService
{
    public async Task<PayablesSummaryDto> GetSummaryAsync(
        Guid companyId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var openItems = await dbContext.ApSubledgerItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.BranchId == branchId && i.Status != "PAID")
            .ToListAsync(cancellationToken);

        var totalPayable = openItems.Sum(i => i.OutstandingAmountPaise);
        var totalOverdue = openItems.Where(i => i.DueDate < today).Sum(i => i.OutstandingAmountPaise);
        var supplierCount = openItems.Select(i => i.SupplierId).Distinct().Count();

        return new PayablesSummaryDto
        {
            TotalPayablePaise = totalPayable,
            TotalOverduePaise = totalOverdue,
            SupplierCount = supplierCount,
            OpenBillCount = openItems.Count
        };
    }

    public async Task<(IReadOnlyList<SupplierPayableDto> Items, int TotalCount)> GetPayablesAsync(
        Guid companyId,
        Guid branchId,
        Guid? supplierId,
        string? status,
        bool overdueOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = dbContext.ApSubledgerItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.BranchId == branchId);

        if (supplierId.HasValue && supplierId.Value != Guid.Empty)
        {
            query = query.Where(i => i.SupplierId == supplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (overdueOnly)
        {
            query = query.Where(i => i.DueDate < today && i.Status != "PAID");
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var supplierIds = items.Select(i => i.SupplierId).Distinct().ToList();
        var supplierMap = await dbContext.Suppliers
            .AsNoTracking()
            .Where(s => supplierIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var billIds = items.Where(i => i.BillId.HasValue).Select(i => i.BillId!.Value).Distinct().ToList();
        var billMap = await dbContext.SupplierInvoices
            .AsNoTracking()
            .Where(b => billIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => (b.InvoiceNumber, b.SystemInvoiceNumber), cancellationToken);

        var result = items.Select(i =>
        {
            var overdueDays = i.DueDate < today ? (today.DayNumber - i.DueDate.DayNumber) : 0;
            var (invNum, sysNum) = i.BillId.HasValue && billMap.TryGetValue(i.BillId.Value, out var tuple)
                ? tuple
                : ($"BILL-{i.Id.ToString()[..8].ToUpperInvariant()}", $"PINV-{i.Id.ToString()[..8].ToUpperInvariant()}");

            return new SupplierPayableDto
            {
                Id = i.Id,
                SupplierId = i.SupplierId,
                SupplierName = supplierMap.GetValueOrDefault(i.SupplierId, "Supplier"),
                BillNumber = invNum,
                SystemInvoiceNumber = sysNum,
                BillId = i.BillId,
                VoucherId = i.VoucherId,
                ItemDate = i.ItemDate,
                DueDate = i.DueDate,
                OriginalAmountPaise = i.OriginalAmountPaise,
                SettledAmountPaise = i.SettledAmountPaise,
                OutstandingAmountPaise = i.OutstandingAmountPaise,
                Status = i.Status,
                OverdueDays = overdueDays
            };
        }).ToList();

        return (result, totalCount);
    }

    public async Task<SupplierAgingReportDto> GetAgingReportAsync(
        Guid companyId,
        Guid branchId,
        DateOnly reportDate,
        CancellationToken cancellationToken = default)
    {
        var openItems = await dbContext.ApSubledgerItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.BranchId == branchId && i.Status != "PAID")
            .ToListAsync(cancellationToken);

        var supplierIds = openItems.Select(i => i.SupplierId).Distinct().ToList();
        var supplierMap = await dbContext.Suppliers
            .AsNoTracking()
            .Where(s => supplierIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var grouped = openItems.GroupBy(i => i.SupplierId).Select(g =>
        {
            var sId = g.Key;
            var sName = supplierMap.GetValueOrDefault(sId, "Supplier");
            long current = 0, d1To30 = 0, d31To60 = 0, d61To90 = 0, dOver90 = 0;

            foreach (var item in g)
            {
                var days = reportDate.DayNumber - item.DueDate.DayNumber;
                if (days <= 0)
                {
                    current += item.OutstandingAmountPaise;
                }
                else if (days <= 30)
                {
                    d1To30 += item.OutstandingAmountPaise;
                }
                else if (days <= 60)
                {
                    d31To60 += item.OutstandingAmountPaise;
                }
                else if (days <= 90)
                {
                    d61To90 += item.OutstandingAmountPaise;
                }
                else
                {
                    dOver90 += item.OutstandingAmountPaise;
                }
            }

            var total = current + d1To30 + d31To60 + d61To90 + dOver90;
            return new SupplierAgingRowDto
            {
                SupplierId = sId,
                SupplierName = sName,
                TotalOutstandingPaise = total,
                CurrentPaise = current,
                Overdue1To30Paise = d1To30,
                Overdue31To60Paise = d31To60,
                Overdue61To90Paise = d61To90,
                OverdueAbove90Paise = dOver90
            };
        }).OrderByDescending(r => r.TotalOutstandingPaise).ToList();

        return new SupplierAgingReportDto
        {
            ReportDate = reportDate,
            TotalOutstandingPaise = grouped.Sum(r => r.TotalOutstandingPaise),
            TotalCurrentPaise = grouped.Sum(r => r.CurrentPaise),
            Total1To30Paise = grouped.Sum(r => r.Overdue1To30Paise),
            Total31To60Paise = grouped.Sum(r => r.Overdue31To60Paise),
            Total61To90Paise = grouped.Sum(r => r.Overdue61To90Paise),
            TotalAbove90Paise = grouped.Sum(r => r.OverdueAbove90Paise),
            Suppliers = grouped
        };
    }

    public async Task<SupplierStatementDto> GetSupplierStatementAsync(
        Guid companyId,
        Guid branchId,
        Guid supplierId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var supplier = await dbContext.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            ?? throw new DomainException("SUPPLIER_NOT_FOUND", $"Supplier '{supplierId}' not found.", 404);

        var apItems = await dbContext.ApSubledgerItems
            .AsNoTracking()
            .Where(i => i.SupplierId == supplierId && i.ItemDate <= toDate)
            .OrderBy(i => i.ItemDate)
            .ToListAsync(cancellationToken);

        var payments = await dbContext.SupplierPayments
            .AsNoTracking()
            .Where(p => p.SupplierId == supplierId && p.PaymentDate <= toDate && p.Status == "SUBMITTED")
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        var lines = new List<SupplierStatementLineDto>();
        long openingBalance = 0;

        foreach (var item in apItems.Where(i => i.ItemDate < fromDate))
        {
            openingBalance += item.OriginalAmountPaise;
        }
        foreach (var p in payments.Where(p => p.PaymentDate < fromDate))
        {
            openingBalance -= p.TotalAmountPaise;
        }

        var runningBalance = openingBalance;
        long totalDebit = 0;
        long totalCredit = 0;

        var periodItems = apItems.Where(i => i.ItemDate >= fromDate && i.ItemDate <= toDate).Select(i => new
        {
            Date = i.ItemDate,
            Type = "Bill",
            Number = i.BillId.HasValue ? $"BILL-{i.BillId.Value.ToString()[..8].ToUpperInvariant()}" : $"AP-{i.Id.ToString()[..8].ToUpperInvariant()}",
            Ref = "",
            Debit = 0L,
            Credit = i.OriginalAmountPaise,
            Narration = $"Commercial Purchase Bill (Due: {i.DueDate:yyyy-MM-dd})"
        });

        var periodPayments = payments.Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate).Select(p => new
        {
            Date = p.PaymentDate,
            Type = "Payment",
            Number = p.PaymentNumber,
            Ref = p.ReferenceNumber ?? "",
            Debit = p.TotalAmountPaise,
            Credit = 0L,
            Narration = string.IsNullOrWhiteSpace(p.Narration) ? $"Payment Disbursed via {p.PaymentMethod}" : p.Narration
        });

        var chronological = periodItems.Concat(periodPayments).OrderBy(x => x.Date).ToList();
        foreach (var entry in chronological)
        {
            runningBalance += entry.Credit - entry.Debit;
            totalDebit += entry.Debit;
            totalCredit += entry.Credit;

            lines.Add(new SupplierStatementLineDto
            {
                Date = entry.Date,
                DocumentType = entry.Type,
                DocumentNumber = entry.Number,
                ReferenceNumber = entry.Ref,
                DebitPaise = entry.Debit,
                CreditPaise = entry.Credit,
                RunningBalancePaise = runningBalance,
                Narration = entry.Narration
            });
        }

        return new SupplierStatementDto
        {
            SupplierId = supplierId,
            SupplierName = supplier.Name,
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalancePaise = openingBalance,
            TotalDebitPaise = totalDebit,
            TotalCreditPaise = totalCredit,
            ClosingBalancePaise = runningBalance,
            Lines = lines
        };
    }

    public async Task<SupplierBillDto> CreateSupplierBillAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateSupplierBillRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new DomainException("NO_BILL_LINES", "Supplier bill requires at least one line item.");
        }

        var supplier = await dbContext.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);

        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && request.InvoiceDate >= p.StartDate && request.InvoiceDate <= p.EndDate, cancellationToken)
            ?? throw new DomainException("FISCAL_PERIOD_NOT_FOUND", $"No fiscal period found covering bill date {request.InvoiceDate:yyyy-MM-dd}.");

        // GL Accounts:
        // 5101: Commercial Goods Purchases (Expense)
        // 1131: Input CGST Credit (Asset)
        // 1132: Input SGST Credit (Asset)
        // 1133: Input IGST Credit (Asset)
        // 2111: Trade Creditors Control Account (Liability)
        var accPurchases = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "5101", cancellationToken)
            ?? await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.RootClass == AccountRootClass.Expense && a.IsActive, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Purchases Account (5101) not found in Chart of Accounts.", 500);

        var accCreditors = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2111", cancellationToken)
            ?? await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.ControlAccountType == "VendorControl" && a.IsActive, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Trade Creditors Account (2111) not found in Chart of Accounts.", 500);

        var isInterState = request.IsInterState ?? false;
        if (!request.IsInterState.HasValue)
        {
            var supplierAddress = await dbContext.SupplierAddresses
                .FirstOrDefaultAsync(a => a.SupplierId == supplier.Id, cancellationToken);
            if (supplierAddress != null && !string.IsNullOrWhiteSpace(supplierAddress.State))
            {
                isInterState = !string.Equals(supplierAddress.State, "KA", StringComparison.OrdinalIgnoreCase);
            }
        }

        Account? accCgst = null;
        Account? accSgst = null;
        Account? accIgst = null;

        var accCgstLookup = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1131", cancellationToken);
        var accSgstLookup = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1132", cancellationToken);
        var accIgstLookup = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1133", cancellationToken);

        if (isInterState)
        {
            accIgst = accIgstLookup
                ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Input IGST Account (1133) not found in Chart of Accounts.", 500);
        }
        else
        {
            accCgst = accCgstLookup
                ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Input CGST Account (1131) not found in Chart of Accounts.", 500);
            accSgst = accSgstLookup
                ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Input SGST Account (1132) not found in Chart of Accounts.", 500);
        }

        long subTotal = 0;
        long totalTax = 0;
        var linesToCreate = new List<SupplierInvoiceLineRecord>();
        var billId = Guid.NewGuid();

        foreach (var l in request.Lines)
        {
            var lineTaxable = (long)Math.Round(l.Quantity * l.UnitPricePaise);
            var lineTax = (long)Math.Round(lineTaxable * (l.TaxRatePercentage / 100.0m));
            var lineTotal = lineTaxable + lineTax;

            subTotal += lineTaxable;
            totalTax += lineTax;

            linesToCreate.Add(new SupplierInvoiceLineRecord
            {
                Id = Guid.NewGuid(),
                InvoiceId = billId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPricePaise = l.UnitPricePaise,
                TaxRatePercentage = l.TaxRatePercentage,
                TaxAmountPaise = lineTax,
                TotalAmountPaise = lineTotal
            });
        }

        var totalBill = subTotal + totalTax;
        var count = await dbContext.SupplierInvoices.CountAsync(s => s.CompanyId == companyId, cancellationToken);
        var systemNumber = $"PINV-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        // 1. Post Double-entry Journal Voucher (Purchase Type 3)
        // Debit: Commercial Purchases (subtotal)
        // Debit: Input Tax (IGST or CGST+SGST)
        // Credit: Trade Creditors Control (total)
        var voucher = new JournalVoucher(
            companyId,
            branchId,
            period.Id,
            VoucherType.Purchase,
            request.InvoiceDate,
            userId,
            narration: $"Supplier Bill {request.InvoiceNumber} from {supplier.Name}",
            referenceNumber: request.InvoiceNumber,
            idempotencyKey: Guid.CreateVersion7(),
            sourceModule: "Finance",
            sourceDocumentId: billId,
            sourceTransactionType: "SupplierInvoice");

        voucher.AddLine(accPurchases.Id, Money.FromPaise(subTotal), Money.Zero, $"Purchase expense - Bill {request.InvoiceNumber}", partyId: supplier.Id);

        if (totalTax > 0)
        {
            if (isInterState && accIgst != null)
            {
                voucher.AddLine(accIgst.Id, Money.FromPaise(totalTax), Money.Zero, "Input IGST credit", partyId: supplier.Id);
            }
            else if (accCgst != null && accSgst != null)
            {
                var cgstPaise = totalTax / 2;
                var sgstPaise = totalTax - cgstPaise;
                voucher.AddLine(accCgst.Id, Money.FromPaise(cgstPaise), Money.Zero, "Input CGST credit", partyId: supplier.Id);
                voucher.AddLine(accSgst.Id, Money.FromPaise(sgstPaise), Money.Zero, "Input SGST credit", partyId: supplier.Id);
            }
        }

        voucher.AddLine(accCreditors.Id, Money.Zero, Money.FromPaise(totalBill), $"Creditor liability - {supplier.Name}", partyId: supplier.Id);

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var branchCode = "MAIN";
        var postedVoucher = await postingService.PostVoucherAsync(voucher, branchCode, fyCode, userId, hasSupervisorElevation: true, cancellationToken);

        // 2. Create Supplier Invoice Entity
        var bill = new SupplierInvoiceRecord
        {
            Id = billId,
            CompanyId = companyId,
            BranchId = branchId,
            SupplierId = supplier.Id,
            InvoiceNumber = request.InvoiceNumber,
            SystemInvoiceNumber = systemNumber,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            PoId = request.PoId,
            GrnId = request.GrnId,
            SubTotalPaise = subTotal,
            TaxAmountPaise = totalTax,
            TotalAmountPaise = totalBill,
            SettledAmountPaise = 0,
            OutstandingAmountPaise = totalBill,
            Status = "APPROVED",
            VoucherId = postedVoucher.Id,
            Notes = request.Notes ?? "",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
            Lines = linesToCreate
        };

        dbContext.SupplierInvoices.Add(bill);

        // 3. Create open AP Subledger Item
        var apItem = new ApSubledgerItemRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            SupplierId = supplier.Id,
            VoucherId = postedVoucher.Id,
            BillId = billId,
            ItemDate = request.InvoiceDate,
            DueDate = request.DueDate,
            OriginalAmountPaise = totalBill,
            SettledAmountPaise = 0,
            OutstandingAmountPaise = totalBill,
            Status = "OPEN",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.ApSubledgerItems.Add(apItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetSupplierBillByIdAsync(companyId, billId, cancellationToken);
    }

    public async Task<SupplierBillDto> GetSupplierBillByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var bill = await dbContext.SupplierInvoices
            .AsNoTracking()
            .Include(b => b.Supplier)
            .Include(b => b.Voucher)
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BILL_NOT_FOUND", "Supplier bill not found.", 404);

        return new SupplierBillDto
        {
            Id = bill.Id,
            SystemInvoiceNumber = bill.SystemInvoiceNumber,
            InvoiceNumber = bill.InvoiceNumber,
            SupplierId = bill.SupplierId,
            SupplierName = bill.Supplier?.Name ?? "Supplier",
            InvoiceDate = bill.InvoiceDate,
            DueDate = bill.DueDate,
            PoId = bill.PoId,
            GrnId = bill.GrnId,
            SubTotalPaise = bill.SubTotalPaise,
            TaxAmountPaise = bill.TaxAmountPaise,
            TotalAmountPaise = bill.TotalAmountPaise,
            SettledAmountPaise = bill.SettledAmountPaise,
            OutstandingAmountPaise = bill.OutstandingAmountPaise,
            Status = bill.Status,
            VoucherId = bill.VoucherId,
            VoucherNumber = bill.Voucher?.VoucherNumber ?? string.Empty,
            Notes = bill.Notes,
            CreatedAtUtc = bill.CreatedAtUtc,
            Lines = bill.Lines.Select(l => new SupplierBillLineDto
            {
                Id = l.Id,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPricePaise = l.UnitPricePaise,
                TaxRatePercentage = l.TaxRatePercentage,
                TaxAmountPaise = l.TaxAmountPaise,
                TotalAmountPaise = l.TotalAmountPaise
            }).ToList()
        };
    }
}

public sealed class SupplierPaymentService(
    MagicDbContext dbContext,
    IPostingService postingService) : ISupplierPaymentService
{
    public async Task<(IReadOnlyList<SupplierPaymentDto> Items, int TotalCount)> GetPaymentsAsync(
        Guid companyId,
        Guid branchId,
        Guid? supplierId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SupplierPayments
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.BankCashAccount)
            .Include(p => p.Voucher)
            .Include(p => p.Allocations)
                .ThenInclude(a => a.ApItem)
            .Where(p => p.CompanyId == companyId && p.BranchId == branchId);

        if (supplierId.HasValue && supplierId.Value != Guid.Empty)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var list = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = list.Select(MapToPaymentDto).ToList();
        return (dtos, totalCount);
    }

    public async Task<SupplierPaymentDto> GetPaymentByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.SupplierPayments
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.BankCashAccount)
            .Include(p => p.Voucher)
            .Include(p => p.Allocations)
                .ThenInclude(a => a.ApItem)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("PAYMENT_NOT_FOUND", "Supplier payment not found.", 404);

        return MapToPaymentDto(payment);
    }

    public async Task<SupplierPaymentDto> CreatePaymentAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateSupplierPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TotalAmountPaise <= 0)
        {
            throw new DomainException("INVALID_PAYMENT_AMOUNT", "Payment amount must be strictly greater than zero.");
        }

        var supplier = await dbContext.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("SUPPLIER_NOT_FOUND", "Supplier not found.", 404);

        var bankAccount = await dbContext.BankCashAccounts
            .FirstOrDefaultAsync(b => b.Id == request.BankCashAccountId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BANK_ACCOUNT_NOT_FOUND", "Bank or Cash account not found.", 404);

        if (!bankAccount.IsActive)
        {
            throw new DomainException("BANK_ACCOUNT_INACTIVE", "Selected bank/cash account is inactive.");
        }

        var creditorAccount = await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2111", cancellationToken)
            ?? throw new DomainException("CONTROL_ACCOUNT_NOT_FOUND", "Trade Creditors Control Account (2111) not found in Chart of Accounts.", 500);

        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && request.PaymentDate >= p.StartDate && request.PaymentDate <= p.EndDate, cancellationToken)
            ?? throw new DomainException("FISCAL_PERIOD_NOT_FOUND", $"No fiscal period found covering payment date {request.PaymentDate:yyyy-MM-dd}.");

        var allocations = request.Allocations ?? [];
        var totalAllocated = allocations.Sum(a => a.AllocatedAmountPaise);
        if (totalAllocated > request.TotalAmountPaise)
        {
            throw new DomainException("ALLOCATION_EXCEEDS_TOTAL", "Sum of bill allocations cannot exceed total payment amount.");
        }

        var paymentId = Guid.NewGuid();
        var count = await dbContext.SupplierPayments.CountAsync(p => p.CompanyId == companyId, cancellationToken);
        var paymentNumber = $"PAY-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        // 1. Double-entry Journal Voucher (Payment Type 4)
        // Debit: Trade Creditors Control Account (2111)
        // Credit: Bank/Cash Account
        var voucher = new JournalVoucher(
            companyId,
            branchId,
            period.Id,
            VoucherType.Payment,
            request.PaymentDate,
            userId,
            narration: string.IsNullOrWhiteSpace(request.Narration)
                ? $"Supplier Payment {paymentNumber} to {supplier.Name}"
                : request.Narration,
            referenceNumber: request.ReferenceNumber,
            idempotencyKey: Guid.CreateVersion7(),
            sourceModule: "Finance",
            sourceDocumentId: paymentId,
            sourceTransactionType: "SupplierPayment");

        voucher.AddLine(creditorAccount.Id, Money.FromPaise(request.TotalAmountPaise), Money.Zero, $"Payment to {supplier.Name}", partyId: supplier.Id);
        voucher.AddLine(bankAccount.GlAccountId, Money.Zero, Money.FromPaise(request.TotalAmountPaise), $"Disbursement from {bankAccount.AccountName}", partyId: supplier.Id);

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var branchCode = "MAIN";
        var postedVoucher = await postingService.PostVoucherAsync(voucher, branchCode, fyCode, userId, hasSupervisorElevation: true, cancellationToken);

        // 2. Create Payment Record
        var payment = new SupplierPaymentRecord
        {
            Id = paymentId,
            CompanyId = companyId,
            BranchId = branchId,
            PaymentNumber = paymentNumber,
            SupplierId = supplier.Id,
            PaymentDate = request.PaymentDate,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber,
            BankCashAccountId = bankAccount.Id,
            TotalAmountPaise = request.TotalAmountPaise,
            AllocatedAmountPaise = totalAllocated,
            UnallocatedAmountPaise = request.TotalAmountPaise - totalAllocated,
            Status = "SUBMITTED",
            VoucherId = postedVoucher.Id,
            Narration = voucher.Narration,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.SupplierPayments.Add(payment);

        // 3. Process Allocations against open AP items
        foreach (var allocReq in allocations)
        {
            if (allocReq.AllocatedAmountPaise <= 0) continue;

            var apItem = await dbContext.ApSubledgerItems
                .FirstOrDefaultAsync(i => i.Id == allocReq.ApItemId && i.SupplierId == supplier.Id, cancellationToken)
                ?? throw new DomainException("AP_ITEM_NOT_FOUND", $"Open payable item '{allocReq.ApItemId}' was not found for this supplier.");

            if (allocReq.AllocatedAmountPaise > apItem.OutstandingAmountPaise)
            {
                throw new DomainException("OVER_ALLOCATION", $"Allocation amount ({allocReq.AllocatedAmountPaise / 100.0:F2}) exceeds outstanding balance ({apItem.OutstandingAmountPaise / 100.0:F2}).");
            }

            apItem.SettledAmountPaise += allocReq.AllocatedAmountPaise;
            apItem.OutstandingAmountPaise -= allocReq.AllocatedAmountPaise;
            apItem.Status = apItem.OutstandingAmountPaise == 0 ? "PAID" : "PARTIALLY_PAID";

            if (apItem.BillId.HasValue)
            {
                var bill = await dbContext.SupplierInvoices.FirstOrDefaultAsync(b => b.Id == apItem.BillId.Value, cancellationToken);
                if (bill != null)
                {
                    bill.SettledAmountPaise += allocReq.AllocatedAmountPaise;
                    bill.OutstandingAmountPaise -= allocReq.AllocatedAmountPaise;
                    bill.Status = bill.OutstandingAmountPaise == 0 ? "PAID" : "PARTIALLY_PAID";
                }
            }

            var allocRecord = new SupplierPaymentAllocationRecord
            {
                Id = Guid.NewGuid(),
                PaymentId = paymentId,
                ApItemId = apItem.Id,
                AllocatedAmountPaise = allocReq.AllocatedAmountPaise,
                AllocationDate = request.PaymentDate,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.SupplierPaymentAllocations.Add(allocRecord);
        }

        // 4. Update Bank/Cash Account running balance
        bankAccount.CurrentBalancePaise -= request.TotalAmountPaise;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPaymentByIdAsync(companyId, paymentId, cancellationToken);
    }

    public async Task<SupplierPaymentDto> CancelPaymentAsync(
        Guid companyId,
        Guid branchId,
        Guid id,
        Guid userId,
        CancelPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.SupplierPayments
            .Include(p => p.Allocations)
                .ThenInclude(a => a.ApItem)
            .Include(p => p.BankCashAccount)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("PAYMENT_NOT_FOUND", "Supplier payment not found.", 404);

        if (payment.Status == "REVERSED")
        {
            throw new DomainException("ALREADY_REVERSED", "Payment is already reversed.");
        }

        // Restore AP items
        foreach (var alloc in payment.Allocations)
        {
            if (alloc.ApItem != null)
            {
                alloc.ApItem.SettledAmountPaise -= alloc.AllocatedAmountPaise;
                alloc.ApItem.OutstandingAmountPaise += alloc.AllocatedAmountPaise;
                alloc.ApItem.Status = alloc.ApItem.SettledAmountPaise == 0 ? "OPEN" : "PARTIALLY_PAID";

                if (alloc.ApItem.BillId.HasValue)
                {
                    var bill = await dbContext.SupplierInvoices.FirstOrDefaultAsync(b => b.Id == alloc.ApItem.BillId.Value, cancellationToken);
                    if (bill != null)
                    {
                        bill.SettledAmountPaise -= alloc.AllocatedAmountPaise;
                        bill.OutstandingAmountPaise += alloc.AllocatedAmountPaise;
                        bill.Status = bill.SettledAmountPaise == 0 ? "APPROVED" : "PARTIALLY_PAID";
                    }
                }
            }
        }

        // Reverse Voucher
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await postingService.ReverseVoucherAsync(
            payment.VoucherId,
            today,
            "MAIN",
            "FY2026-27",
            userId,
            $"Reversal of Supplier Payment {payment.PaymentNumber}: {request.Reason}",
            cancellationToken);

        if (payment.BankCashAccount != null)
        {
            payment.BankCashAccount.CurrentBalancePaise += payment.TotalAmountPaise;
        }

        payment.Status = "REVERSED";
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetPaymentByIdAsync(companyId, id, cancellationToken);
    }

    private static SupplierPaymentDto MapToPaymentDto(SupplierPaymentRecord p) => new()
    {
        Id = p.Id,
        PaymentNumber = p.PaymentNumber,
        SupplierId = p.SupplierId,
        SupplierName = p.Supplier?.Name ?? "Supplier",
        PaymentDate = p.PaymentDate,
        PaymentMethod = p.PaymentMethod,
        ReferenceNumber = p.ReferenceNumber,
        BankCashAccountId = p.BankCashAccountId,
        BankCashAccountName = p.BankCashAccount?.AccountName ?? "Bank/Cash Account",
        TotalAmountPaise = p.TotalAmountPaise,
        AllocatedAmountPaise = p.AllocatedAmountPaise,
        UnallocatedAmountPaise = p.UnallocatedAmountPaise,
        Status = p.Status,
        VoucherId = p.VoucherId,
        VoucherNumber = p.Voucher?.VoucherNumber ?? string.Empty,
        Narration = p.Narration,
        CreatedAtUtc = p.CreatedAtUtc,
        Allocations = p.Allocations.Select(a => new SupplierPaymentAllocationDto
        {
            Id = a.Id,
            ApItemId = a.ApItemId,
            DocumentNumber = a.ApItem?.BillId.HasValue == true ? $"BILL-{a.ApItem.BillId.Value.ToString()[..8].ToUpperInvariant()}" : $"AP-{a.ApItemId.ToString()[..8].ToUpperInvariant()}",
            AllocatedAmountPaise = a.AllocatedAmountPaise,
            AllocationDate = a.AllocationDate
        }).ToList()
    };
}

public sealed class BankCashAccountService(MagicDbContext dbContext) : IBankCashAccountService
{
    public async Task<IReadOnlyList<BankCashAccountDto>> GetAccountsAsync(
        Guid companyId,
        Guid branchId,
        string? accountType,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.BankCashAccounts
            .AsNoTracking()
            .Include(b => b.GlAccount)
            .Where(b => b.CompanyId == companyId && b.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(accountType))
        {
            query = query.Where(b => b.AccountType == accountType);
        }

        if (activeOnly)
        {
            query = query.Where(b => b.IsActive);
        }

        var list = await query.OrderBy(b => b.AccountName).ToListAsync(cancellationToken);
        return list.Select(MapToAccountDto).ToList();
    }

    public async Task<BankCashAccountDto> GetAccountByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.BankCashAccounts
            .AsNoTracking()
            .Include(b => b.GlAccount)
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Bank or cash account not found.", 404);

        return MapToAccountDto(account);
    }

    public async Task<BankCashAccountDto> CreateAccountAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateBankCashAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var glAccount = await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.GlAccountId && a.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("GL_ACCOUNT_NOT_FOUND", "Referenced General Ledger account does not exist.", 404);

        var plainAcc = request.AccountNumber.Trim();
        var masked = plainAcc.Length > 4
            ? $"{new string('•', plainAcc.Length - 4)}{plainAcc[^4..]}"
            : plainAcc;

        var account = new BankCashAccountRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            AccountName = request.AccountName.Trim(),
            AccountType = request.AccountType,
            BankName = request.BankName?.Trim(),
            BranchName = request.BranchName?.Trim(),
            AccountNumber = plainAcc,
            MaskedAccountNumber = masked,
            IfscCode = request.IfscCode?.Trim().ToUpperInvariant(),
            GlAccountId = glAccount.Id,
            OpeningBalancePaise = request.OpeningBalancePaise,
            CurrentBalancePaise = request.OpeningBalancePaise,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.BankCashAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetAccountByIdAsync(companyId, account.Id, cancellationToken);
    }

    public async Task<BankCashAccountDto> UpdateAccountAsync(
        Guid companyId,
        Guid id,
        Guid userId,
        UpdateBankCashAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.BankCashAccounts
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Bank or cash account not found.", 404);

        account.AccountName = request.AccountName.Trim();
        account.BankName = request.BankName?.Trim();
        account.BranchName = request.BranchName?.Trim();
        account.IfscCode = request.IfscCode?.Trim().ToUpperInvariant();
        account.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAccountByIdAsync(companyId, id, cancellationToken);
    }

    public async Task<RevealAccountNumberResponse> RevealAccountNumberAsync(
        Guid companyId,
        Guid id,
        Guid userId,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.BankCashAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Bank account not found.", 404);

        // Audit the unmasking operation
        var audit = new SystemAuditLogRecord
        {
            UserId = userId,
            BranchId = account.BranchId,
            ClientIp = clientIp ?? "127.0.0.1",
            ActionName = "REVEAL_BANK_ACCOUNT_DETAILS",
            EntityName = "BankCashAccount",
            EntityId = id,
            EventTimestampUtc = DateTimeOffset.UtcNow,
            CorrelationId = Guid.NewGuid(),
            AfterStateJson = JsonSerializer.Serialize(new
            {
                accountId = id,
                accountName = account.AccountName,
                masked = account.MaskedAccountNumber,
                revealedAt = DateTimeOffset.UtcNow
            })
        };
        dbContext.SystemAuditLogs.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RevealAccountNumberResponse
        {
            Id = id,
            AccountNumber = account.AccountNumber,
            RevealedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static BankCashAccountDto MapToAccountDto(BankCashAccountRecord b) => new()
    {
        Id = b.Id,
        AccountName = b.AccountName,
        AccountType = b.AccountType,
        BankName = b.BankName,
        BranchName = b.BranchName,
        MaskedAccountNumber = b.MaskedAccountNumber,
        IfscCode = b.IfscCode,
        GlAccountId = b.GlAccountId,
        GlAccountCode = b.GlAccount?.Code ?? string.Empty,
        GlAccountName = b.GlAccount?.Name ?? string.Empty,
        OpeningBalancePaise = b.OpeningBalancePaise,
        CurrentBalancePaise = b.CurrentBalancePaise,
        IsActive = b.IsActive,
        CreatedAtUtc = b.CreatedAtUtc
    };
}

public sealed class TreasuryService(
    MagicDbContext dbContext,
    IPostingService postingService) : ITreasuryService
{
    public async Task<(IReadOnlyList<TreasuryTransactionDto> Items, int TotalCount)> GetTransactionsAsync(
        Guid companyId,
        Guid branchId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? transactionType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TreasuryTransactions
            .AsNoTracking()
            .Include(t => t.SourceAccount)
            .Include(t => t.DestinationAccount)
            .Include(t => t.Voucher)
            .Where(t => t.CompanyId == companyId && t.BranchId == branchId);

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            query = query.Where(t => t.TransactionType == transactionType);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var list = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = list.Select(MapToTreasuryDto).ToList();
        return (dtos, totalCount);
    }

    public async Task<TreasuryTransactionDto> CreateContraTransactionAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateContraTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AmountPaise <= 0)
        {
            throw new DomainException("INVALID_AMOUNT", "Transfer amount must be strictly greater than zero.");
        }

        if (request.SourceAccountId == request.DestinationAccountId)
        {
            throw new DomainException("SAME_ACCOUNTS", "Source and destination accounts must be distinct.");
        }

        var source = await dbContext.BankCashAccounts
            .FirstOrDefaultAsync(b => b.Id == request.SourceAccountId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("SOURCE_ACCOUNT_NOT_FOUND", "Source account not found.", 404);

        var dest = await dbContext.BankCashAccounts
            .FirstOrDefaultAsync(b => b.Id == request.DestinationAccountId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("DEST_ACCOUNT_NOT_FOUND", "Destination account not found.", 404);

        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && request.TransactionDate >= p.StartDate && request.TransactionDate <= p.EndDate, cancellationToken)
            ?? throw new DomainException("FISCAL_PERIOD_NOT_FOUND", $"No fiscal period found covering date {request.TransactionDate:yyyy-MM-dd}.");

        var txId = Guid.NewGuid();
        var count = await dbContext.TreasuryTransactions.CountAsync(t => t.CompanyId == companyId, cancellationToken);
        var txNumber = $"TR-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        // Double-entry Contra Journal Voucher (VoucherType Contra = 6)
        // Debit: Destination Account
        // Credit: Source Account
        var voucher = new JournalVoucher(
            companyId,
            branchId,
            period.Id,
            VoucherType.Contra,
            request.TransactionDate,
            userId,
            narration: string.IsNullOrWhiteSpace(request.Narration)
                ? $"{request.TransactionType}: Transfer from {source.AccountName} to {dest.AccountName}"
                : request.Narration,
            referenceNumber: request.ReferenceNumber,
            idempotencyKey: Guid.CreateVersion7(),
            sourceModule: "Finance",
            sourceDocumentId: txId,
            sourceTransactionType: request.TransactionType);

        voucher.AddLine(dest.GlAccountId, Money.FromPaise(request.AmountPaise), Money.Zero, $"Contra inflow to {dest.AccountName}");
        voucher.AddLine(source.GlAccountId, Money.Zero, Money.FromPaise(request.AmountPaise), $"Contra outflow from {source.AccountName}");

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var branchCode = "MAIN";
        var postedVoucher = await postingService.PostVoucherAsync(voucher, branchCode, fyCode, userId, hasSupervisorElevation: true, cancellationToken);

        // Update running balances
        source.CurrentBalancePaise -= request.AmountPaise;
        dest.CurrentBalancePaise += request.AmountPaise;

        var tx = new TreasuryTransactionRecord
        {
            Id = txId,
            CompanyId = companyId,
            BranchId = branchId,
            TransactionNumber = txNumber,
            TransactionType = request.TransactionType,
            TransactionDate = request.TransactionDate,
            SourceAccountId = source.Id,
            DestinationAccountId = dest.Id,
            AmountPaise = request.AmountPaise,
            ReferenceNumber = request.ReferenceNumber,
            Narration = voucher.Narration,
            Status = "POSTED",
            VoucherId = postedVoucher.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.TreasuryTransactions.Add(tx);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetTransactionByIdAsync(companyId, txId, cancellationToken);
    }

    public async Task<TreasuryTransactionDto> CreateAdjustmentTransactionAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        CreateTreasuryAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AmountPaise <= 0)
        {
            throw new DomainException("INVALID_AMOUNT", "Adjustment amount must be strictly greater than zero.");
        }

        var bankAccount = await dbContext.BankCashAccounts
            .FirstOrDefaultAsync(b => b.Id == request.BankAccountId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BANK_ACCOUNT_NOT_FOUND", "Bank account not found.", 404);

        var nominalAccount = await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.NominalAccountId && a.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("NOMINAL_ACCOUNT_NOT_FOUND", "Expense or revenue account not found.", 404);

        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && request.TransactionDate >= p.StartDate && request.TransactionDate <= p.EndDate, cancellationToken)
            ?? throw new DomainException("FISCAL_PERIOD_NOT_FOUND", $"No fiscal period found covering date {request.TransactionDate:yyyy-MM-dd}.");

        var txId = Guid.NewGuid();
        var count = await dbContext.TreasuryTransactions.CountAsync(t => t.CompanyId == companyId, cancellationToken);
        var txNumber = $"TR-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        JournalVoucher voucher;
        var isCharge = string.Equals(request.TransactionType, "BankCharges", StringComparison.OrdinalIgnoreCase);

        if (isCharge)
        {
            // Bank Charges: Debit Expense, Credit Bank
            voucher = new JournalVoucher(
                companyId,
                branchId,
                period.Id,
                VoucherType.Payment,
                request.TransactionDate,
                userId,
                narration: string.IsNullOrWhiteSpace(request.Narration) ? $"Bank Charges on {bankAccount.AccountName}" : request.Narration,
                referenceNumber: request.ReferenceNumber,
                idempotencyKey: Guid.CreateVersion7(),
                sourceModule: "Finance",
                sourceDocumentId: txId,
                sourceTransactionType: "BankCharges");

            voucher.AddLine(nominalAccount.Id, Money.FromPaise(request.AmountPaise), Money.Zero, "Bank Charges expense");
            voucher.AddLine(bankAccount.GlAccountId, Money.Zero, Money.FromPaise(request.AmountPaise), $"Charge deducted from {bankAccount.AccountName}");
            bankAccount.CurrentBalancePaise -= request.AmountPaise;
        }
        else
        {
            // Bank Interest: Debit Bank, Credit Interest Revenue
            voucher = new JournalVoucher(
                companyId,
                branchId,
                period.Id,
                VoucherType.Receipt,
                request.TransactionDate,
                userId,
                narration: string.IsNullOrWhiteSpace(request.Narration) ? $"Bank Interest Credit on {bankAccount.AccountName}" : request.Narration,
                referenceNumber: request.ReferenceNumber,
                idempotencyKey: Guid.CreateVersion7(),
                sourceModule: "Finance",
                sourceDocumentId: txId,
                sourceTransactionType: "BankInterest");

            voucher.AddLine(bankAccount.GlAccountId, Money.FromPaise(request.AmountPaise), Money.Zero, $"Interest received in {bankAccount.AccountName}");
            voucher.AddLine(nominalAccount.Id, Money.Zero, Money.FromPaise(request.AmountPaise), "Interest Income credit");
            bankAccount.CurrentBalancePaise += request.AmountPaise;
        }

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var branchCode = "MAIN";
        var postedVoucher = await postingService.PostVoucherAsync(voucher, branchCode, fyCode, userId, hasSupervisorElevation: true, cancellationToken);

        var tx = new TreasuryTransactionRecord
        {
            Id = txId,
            CompanyId = companyId,
            BranchId = branchId,
            TransactionNumber = txNumber,
            TransactionType = request.TransactionType,
            TransactionDate = request.TransactionDate,
            SourceAccountId = bankAccount.Id,
            DestinationAccountId = null,
            AmountPaise = request.AmountPaise,
            ReferenceNumber = request.ReferenceNumber,
            Narration = voucher.Narration,
            Status = "POSTED",
            VoucherId = postedVoucher.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.TreasuryTransactions.Add(tx);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetTransactionByIdAsync(companyId, txId, cancellationToken);
    }

    private async Task<TreasuryTransactionDto> GetTransactionByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken)
    {
        var tx = await dbContext.TreasuryTransactions
            .AsNoTracking()
            .Include(t => t.SourceAccount)
            .Include(t => t.DestinationAccount)
            .Include(t => t.Voucher)
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("TRANSACTION_NOT_FOUND", "Treasury transaction not found.", 404);

        return MapToTreasuryDto(tx);
    }

    private static TreasuryTransactionDto MapToTreasuryDto(TreasuryTransactionRecord t) => new()
    {
        Id = t.Id,
        TransactionNumber = t.TransactionNumber,
        TransactionType = t.TransactionType,
        TransactionDate = t.TransactionDate,
        SourceAccountId = t.SourceAccountId,
        SourceAccountName = t.SourceAccount?.AccountName ?? "Source Account",
        DestinationAccountId = t.DestinationAccountId,
        DestinationAccountName = t.DestinationAccount?.AccountName,
        AmountPaise = t.AmountPaise,
        ReferenceNumber = t.ReferenceNumber,
        Narration = t.Narration,
        Status = t.Status,
        VoucherId = t.VoucherId,
        VoucherNumber = t.Voucher?.VoucherNumber ?? string.Empty,
        CreatedAtUtc = t.CreatedAtUtc
    };
}

public sealed class BankReconciliationService(
    MagicDbContext dbContext) : IBankReconciliationService
{
    public async Task<IReadOnlyList<BankStatementSummaryDto>> GetStatementsAsync(
        Guid companyId,
        Guid branchId,
        Guid? bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.BankStatements
            .AsNoTracking()
            .Include(s => s.BankAccount)
            .Include(s => s.Lines)
            .Where(s => s.CompanyId == companyId && s.BranchId == branchId);

        if (bankAccountId.HasValue && bankAccountId.Value != Guid.Empty)
        {
            query = query.Where(s => s.BankAccountId == bankAccountId.Value);
        }

        var list = await query.OrderByDescending(s => s.StatementEndDate).ToListAsync(cancellationToken);
        return list.Select(s => new BankStatementSummaryDto
        {
            Id = s.Id,
            BankAccountId = s.BankAccountId,
            BankAccountName = s.BankAccount?.AccountName ?? "Bank Account",
            StatementStartDate = s.StatementStartDate,
            StatementEndDate = s.StatementEndDate,
            OpeningBalancePaise = s.OpeningBalancePaise,
            ClosingBalancePaise = s.ClosingBalancePaise,
            Filename = s.Filename,
            Status = s.Status,
            TotalLines = s.Lines.Count,
            ReconciledLines = s.Lines.Count(l => l.IsReconciled),
            ImportedAtUtc = s.ImportedAtUtc
        }).ToList();
    }

    public async Task<BankStatementDetailDto> ImportStatementCsvAsync(
        Guid companyId,
        Guid branchId,
        Guid userId,
        ImportBankStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        var bankAccount = await dbContext.BankCashAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BankAccountId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BANK_ACCOUNT_NOT_FOUND", "Bank account not found.", 404);

        var statementId = Guid.NewGuid();
        var statement = new BankStatementRecord
        {
            Id = statementId,
            CompanyId = companyId,
            BranchId = branchId,
            BankAccountId = bankAccount.Id,
            StatementStartDate = request.StatementStartDate,
            StatementEndDate = request.StatementEndDate,
            OpeningBalancePaise = request.OpeningBalancePaise,
            ClosingBalancePaise = request.ClosingBalancePaise,
            Filename = request.Filename,
            Status = "OPEN",
            ImportedAtUtc = DateTimeOffset.UtcNow,
            ImportedByUserId = userId
        };

        var parsedLines = ParseCsvStatementLines(statementId, request.CsvContent, request.OpeningBalancePaise);
        statement.Lines = parsedLines;

        dbContext.BankStatements.Add(statement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetStatementDetailAsync(companyId, statementId, cancellationToken);
    }

    public async Task<ReconciliationWorkspaceDto> GetReconciliationWorkspaceAsync(
        Guid companyId,
        Guid branchId,
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        var statement = await dbContext.BankStatements
            .AsNoTracking()
            .Include(s => s.BankAccount)
            .Include(s => s.Lines)
                .ThenInclude(l => l.MatchedVoucherLine)
                    .ThenInclude(vl => vl!.Voucher)
            .FirstOrDefaultAsync(s => s.Id == statementId && s.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("STATEMENT_NOT_FOUND", "Bank statement not found.", 404);

        var bankAccount = statement.BankAccount!;

        // Internal Ledger lines for bank account's GL Account
        var matchedVoucherLineIds = statement.Lines
            .Where(l => l.MatchedVoucherLineId.HasValue)
            .Select(l => l.MatchedVoucherLineId!.Value)
            .ToHashSet();

        var ledgerLines = await dbContext.JournalLines
            .AsNoTracking()
            .Include(l => l.Voucher)
            .Where(l => l.AccountId == bankAccount.GlAccountId &&
                        l.BranchId == branchId &&
                        l.Voucher!.Status == VoucherStatus.Posted &&
                        !matchedVoucherLineIds.Contains(l.Id))
            .OrderBy(l => l.Voucher!.VoucherDate)
            .ToListAsync(cancellationToken);

        var unmatchedLedger = ledgerLines.Select(l => new UnmatchedLedgerTransactionDto
        {
            JournalLineId = l.Id,
            VoucherId = l.VoucherId,
            VoucherNumber = l.Voucher?.VoucherNumber ?? string.Empty,
            VoucherDate = l.Voucher?.VoucherDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            DebitPaise = l.Debit.Paise,
            CreditPaise = l.Credit.Paise,
            ReferenceNumber = l.Voucher?.ReferenceNumber,
            Narration = l.Narration
        }).ToList();

        var statementLinesDto = statement.Lines.OrderBy(l => l.TransactionDate).Select(l => new BankStatementLineDto
        {
            Id = l.Id,
            TransactionDate = l.TransactionDate,
            ValueDate = l.ValueDate,
            Description = l.Description,
            ReferenceNumber = l.ReferenceNumber,
            WithdrawalDebitPaise = l.WithdrawalDebitPaise,
            DepositCreditPaise = l.DepositCreditPaise,
            BalancePaise = l.BalancePaise,
            IsReconciled = l.IsReconciled,
            MatchedVoucherLineId = l.MatchedVoucherLineId,
            MatchedVoucherNumber = l.MatchedVoucherLine?.Voucher?.VoucherNumber,
            MatchType = l.MatchType
        }).ToList();

        var diff = statement.ClosingBalancePaise - bankAccount.CurrentBalancePaise;

        return new ReconciliationWorkspaceDto
        {
            StatementId = statement.Id,
            BankAccountId = bankAccount.Id,
            BankAccountName = bankAccount.AccountName,
            BookBalancePaise = bankAccount.CurrentBalancePaise,
            StatementBalancePaise = statement.ClosingBalancePaise,
            DifferencePaise = diff,
            StatementLines = statementLinesDto,
            UnmatchedLedgerLines = unmatchedLedger
        };
    }

    public async Task<ReconciliationWorkspaceDto> MatchStatementLineAsync(
        Guid companyId,
        Guid branchId,
        Guid statementId,
        MatchStatementLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var line = await dbContext.BankStatementLines
            .Include(l => l.Statement)
            .FirstOrDefaultAsync(l => l.Id == request.StatementLineId && l.StatementId == statementId, cancellationToken)
            ?? throw new DomainException("LINE_NOT_FOUND", "Statement line not found.", 404);

        if (line.Statement?.Status == "RECONCILED")
        {
            throw new DomainException("STATEMENT_ALREADY_RECONCILED", "Cannot modify or match lines on an already reconciled bank statement.", 400);
        }

        if (line.IsReconciled)
        {
            throw new DomainException("LINE_ALREADY_MATCHED", "Statement line is already reconciled and matched.", 400);
        }

        var bankAccount = await dbContext.BankCashAccounts
            .FirstOrDefaultAsync(b => b.Id == line.Statement!.BankAccountId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BANK_ACCOUNT_NOT_FOUND", "Bank account not found.", 404);

        var jLine = await dbContext.JournalLines
            .Include(l => l.Voucher)
            .FirstOrDefaultAsync(l => l.Id == request.JournalLineId, cancellationToken)
            ?? throw new DomainException("JOURNAL_LINE_NOT_FOUND", "Journal line not found.", 404);

        if (jLine.AccountId != bankAccount.GlAccountId)
        {
            throw new DomainException("INVALID_GL_ACCOUNT_MATCH", "Journal line does not belong to the bank account's mapped General Ledger account.", 400);
        }

        var isAlreadyMatched = await dbContext.BankStatementLines
            .AnyAsync(l => l.MatchedVoucherLineId == jLine.Id, cancellationToken);
        if (isAlreadyMatched)
        {
            throw new DomainException("JOURNAL_LINE_ALREADY_MATCHED", "Journal line is already matched to another statement line.", 400);
        }

        line.IsReconciled = true;
        line.MatchedVoucherLineId = jLine.Id;
        line.MatchedAtUtc = DateTimeOffset.UtcNow;
        line.MatchType = request.MatchType;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetReconciliationWorkspaceAsync(companyId, branchId, statementId, cancellationToken);
    }

    public async Task<ReconciliationWorkspaceDto> UnmatchStatementLineAsync(
        Guid companyId,
        Guid branchId,
        Guid statementId,
        UnmatchStatementLineRequest request,
        CancellationToken cancellationToken = default)
    {
        var line = await dbContext.BankStatementLines
            .Include(l => l.Statement)
            .FirstOrDefaultAsync(l => l.Id == request.StatementLineId && l.StatementId == statementId, cancellationToken)
            ?? throw new DomainException("LINE_NOT_FOUND", "Statement line not found.", 404);

        if (line.Statement?.Status == "RECONCILED")
        {
            throw new DomainException("STATEMENT_ALREADY_RECONCILED", "Cannot unmatch lines on an already reconciled bank statement.", 400);
        }

        line.IsReconciled = false;
        line.MatchedVoucherLineId = null;
        line.MatchedAtUtc = null;
        line.MatchType = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetReconciliationWorkspaceAsync(companyId, branchId, statementId, cancellationToken);
    }

    public async Task<BankStatementSummaryDto> CompleteReconciliationAsync(
        Guid companyId,
        Guid branchId,
        Guid statementId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var statement = await dbContext.BankStatements
            .Include(s => s.BankAccount)
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == statementId && s.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("STATEMENT_NOT_FOUND", "Statement not found.", 404);

        if (statement.Status == "RECONCILED")
        {
            throw new DomainException("STATEMENT_ALREADY_RECONCILED", "Bank statement is already reconciled.", 400);
        }

        statement.Status = "RECONCILED";
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BankStatementSummaryDto
        {
            Id = statement.Id,
            BankAccountId = statement.BankAccountId,
            BankAccountName = statement.BankAccount?.AccountName ?? "Bank Account",
            StatementStartDate = statement.StatementStartDate,
            StatementEndDate = statement.StatementEndDate,
            OpeningBalancePaise = statement.OpeningBalancePaise,
            ClosingBalancePaise = statement.ClosingBalancePaise,
            Filename = statement.Filename,
            Status = statement.Status,
            TotalLines = statement.Lines.Count,
            ReconciledLines = statement.Lines.Count(l => l.IsReconciled),
            ImportedAtUtc = statement.ImportedAtUtc
        };
    }

    private async Task<BankStatementDetailDto> GetStatementDetailAsync(Guid companyId, Guid id, CancellationToken cancellationToken)
    {
        var statement = await dbContext.BankStatements
            .AsNoTracking()
            .Include(s => s.BankAccount)
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("STATEMENT_NOT_FOUND", "Bank statement not found.", 404);

        return new BankStatementDetailDto
        {
            Id = statement.Id,
            BankAccountId = statement.BankAccountId,
            BankAccountName = statement.BankAccount?.AccountName ?? "Bank Account",
            StatementStartDate = statement.StatementStartDate,
            StatementEndDate = statement.StatementEndDate,
            OpeningBalancePaise = statement.OpeningBalancePaise,
            ClosingBalancePaise = statement.ClosingBalancePaise,
            Filename = statement.Filename,
            Status = statement.Status,
            Lines = statement.Lines.OrderBy(l => l.TransactionDate).Select(l => new BankStatementLineDto
            {
                Id = l.Id,
                TransactionDate = l.TransactionDate,
                ValueDate = l.ValueDate,
                Description = l.Description,
                ReferenceNumber = l.ReferenceNumber,
                WithdrawalDebitPaise = l.WithdrawalDebitPaise,
                DepositCreditPaise = l.DepositCreditPaise,
                BalancePaise = l.BalancePaise,
                IsReconciled = l.IsReconciled,
                MatchedVoucherLineId = l.MatchedVoucherLineId,
                MatchType = l.MatchType
            }).ToList()
        };
    }

    private static List<BankStatementLineRecord> ParseCsvStatementLines(Guid statementId, string csv, long openingBalancePaise)
    {
        var lines = new List<BankStatementLineRecord>();
        var rawLines = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var running = openingBalancePaise;

        foreach (var raw in rawLines)
        {
            var parts = raw.Split(',');
            if (parts.Length < 4) continue;

            // Skip header if first column is non-date
            if (string.Equals(parts[0].Trim(), "Date", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(parts[0].Trim(), "TxnDate", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!DateOnly.TryParse(parts[0].Trim(), CultureInfo.InvariantCulture, out var txnDate))
            {
                continue;
            }

            var desc = parts[1].Trim().Trim('"');
            var refNum = parts[2].Trim().Trim('"');
            long withdrawal = 0;
            long deposit = 0;

            if (decimal.TryParse(parts[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var debitDec))
            {
                withdrawal = (long)Math.Round(debitDec * 100);
            }

            if (parts.Length >= 5 && decimal.TryParse(parts[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var creditDec))
            {
                deposit = (long)Math.Round(creditDec * 100);
            }

            running += deposit - withdrawal;

            lines.Add(new BankStatementLineRecord
            {
                Id = Guid.NewGuid(),
                StatementId = statementId,
                TransactionDate = txnDate,
                ValueDate = txnDate,
                Description = desc,
                ReferenceNumber = string.IsNullOrWhiteSpace(refNum) ? null : refNum,
                WithdrawalDebitPaise = withdrawal,
                DepositCreditPaise = deposit,
                BalancePaise = running,
                IsReconciled = false
            });
        }

        return lines;
    }
}

public sealed class FinancialReportingService(MagicDbContext dbContext) : IFinancialReportingService
{
    public async Task<CashBookReportDto> GetCashBookAsync(
        Guid companyId,
        Guid branchId,
        Guid? accountId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        // Find Cash Accounts
        var cashAccQuery = dbContext.BankCashAccounts
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.BranchId == branchId && b.AccountType == "Cash");

        if (accountId.HasValue && accountId.Value != Guid.Empty)
        {
            cashAccQuery = cashAccQuery.Where(b => b.Id == accountId.Value);
        }

        var cashAcc = await cashAccQuery.FirstOrDefaultAsync(cancellationToken);
        var glAccountId = cashAcc?.GlAccountId;

        if (!glAccountId.HasValue)
        {
            var fallback = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1111", cancellationToken);
            glAccountId = fallback?.Id ?? Guid.Empty;
        }

        var lines = await dbContext.JournalLines
            .AsNoTracking()
            .Include(l => l.Voucher)
            .Where(l => l.AccountId == glAccountId &&
                        l.BranchId == branchId &&
                        l.Voucher!.Status == VoucherStatus.Posted &&
                        l.Voucher.VoucherDate <= toDate)
            .OrderBy(l => l.Voucher!.VoucherDate)
            .ThenBy(l => l.Voucher!.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        long openingBalance = cashAcc?.OpeningBalancePaise ?? 0L;
        foreach (var l in lines.Where(l => l.Voucher!.VoucherDate < fromDate))
        {
            openingBalance += l.Debit.Paise - l.Credit.Paise;
        }

        var running = openingBalance;
        long totalReceipts = 0;
        long totalPayments = 0;
        var entries = new List<CashBookEntryDto>();

        foreach (var l in lines.Where(l => l.Voucher!.VoucherDate >= fromDate && l.Voucher!.VoucherDate <= toDate))
        {
            var r = l.Debit.Paise;
            var p = l.Credit.Paise;
            running += r - p;
            totalReceipts += r;
            totalPayments += p;

            entries.Add(new CashBookEntryDto
            {
                Date = l.Voucher!.VoucherDate,
                VoucherNumber = l.Voucher.VoucherNumber,
                VoucherType = l.Voucher.VoucherType.ToString(),
                OppositeAccountName = "General Settlement",
                Narration = l.Narration,
                ReceiptPaise = r,
                PaymentPaise = p,
                RunningBalancePaise = running
            });
        }

        return new CashBookReportDto
        {
            AccountName = cashAcc?.AccountName ?? "Cash on Hand",
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalancePaise = openingBalance,
            TotalReceiptsPaise = totalReceipts,
            TotalPaymentsPaise = totalPayments,
            ClosingBalancePaise = running,
            Entries = entries
        };
    }

    public async Task<BankBookReportDto> GetBankBookAsync(
        Guid companyId,
        Guid branchId,
        Guid? bankAccountId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var bankAccQuery = dbContext.BankCashAccounts
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.BranchId == branchId && b.AccountType == "Bank");

        if (bankAccountId.HasValue && bankAccountId.Value != Guid.Empty)
        {
            bankAccQuery = bankAccQuery.Where(b => b.Id == bankAccountId.Value);
        }

        var bankAcc = await bankAccQuery.FirstOrDefaultAsync(cancellationToken);
        var glAccountId = bankAcc?.GlAccountId;

        if (!glAccountId.HasValue)
        {
            var fallback = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1112", cancellationToken);
            glAccountId = fallback?.Id ?? Guid.Empty;
        }

        var lines = await dbContext.JournalLines
            .AsNoTracking()
            .Include(l => l.Voucher)
            .Where(l => l.AccountId == glAccountId &&
                        l.BranchId == branchId &&
                        l.Voucher!.Status == VoucherStatus.Posted &&
                        l.Voucher.VoucherDate <= toDate)
            .OrderBy(l => l.Voucher!.VoucherDate)
            .ThenBy(l => l.Voucher!.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        long openingBalance = bankAcc?.OpeningBalancePaise ?? 0L;
        foreach (var l in lines.Where(l => l.Voucher!.VoucherDate < fromDate))
        {
            openingBalance += l.Debit.Paise - l.Credit.Paise;
        }

        var running = openingBalance;
        long totalDeposits = 0;
        long totalWithdrawals = 0;
        var entries = new List<BankBookEntryDto>();

        foreach (var l in lines.Where(l => l.Voucher!.VoucherDate >= fromDate && l.Voucher!.VoucherDate <= toDate))
        {
            var d = l.Debit.Paise;
            var w = l.Credit.Paise;
            running += d - w;
            totalDeposits += d;
            totalWithdrawals += w;

            entries.Add(new BankBookEntryDto
            {
                Date = l.Voucher!.VoucherDate,
                VoucherNumber = l.Voucher.VoucherNumber,
                VoucherType = l.Voucher.VoucherType.ToString(),
                ReferenceNumber = l.Voucher.ReferenceNumber,
                OppositeAccountName = "Bank Settlement",
                Narration = l.Narration,
                DepositPaise = d,
                WithdrawalPaise = w,
                RunningBalancePaise = running
            });
        }

        return new BankBookReportDto
        {
            AccountName = bankAcc?.AccountName ?? "Operating Bank Account",
            BankName = bankAcc?.BankName,
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalancePaise = openingBalance,
            TotalDepositsPaise = totalDeposits,
            TotalWithdrawalsPaise = totalWithdrawals,
            ClosingBalancePaise = running,
            Entries = entries
        };
    }

    public async Task<FinanceSummaryKpiDto> GetSummaryKpisAsync(
        Guid companyId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var arItems = await dbContext.ArSubledgerItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.BranchId == branchId && i.Status != "PAID")
            .ToListAsync(cancellationToken);

        var apItems = await dbContext.ApSubledgerItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.BranchId == branchId && i.Status != "PAID")
            .ToListAsync(cancellationToken);

        var accounts = await dbContext.BankCashAccounts
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.BranchId == branchId && b.IsActive)
            .ToListAsync(cancellationToken);

        var totalAr = arItems.Sum(i => i.OutstandingAmountPaise);
        var overdueAr = arItems.Where(i => i.DueDate < today).Sum(i => i.OutstandingAmountPaise);
        var totalAp = apItems.Sum(i => i.OutstandingAmountPaise);
        var overdueAp = apItems.Where(i => i.DueDate < today).Sum(i => i.OutstandingAmountPaise);

        var cashBalance = accounts.Where(b => b.AccountType == "Cash").Sum(b => b.CurrentBalancePaise);
        var bankBalance = accounts.Where(b => b.AccountType == "Bank").Sum(b => b.CurrentBalancePaise);

        return new FinanceSummaryKpiDto
        {
            TotalReceivablePaise = totalAr,
            TotalOverdueArPaise = overdueAr,
            TotalPayablePaise = totalAp,
            TotalOverdueApPaise = overdueAp,
            TotalCashBalancePaise = cashBalance,
            TotalBankBalancePaise = bankBalance
        };
    }
}
