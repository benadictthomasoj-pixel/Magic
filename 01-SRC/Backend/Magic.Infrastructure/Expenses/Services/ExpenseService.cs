using Magic.Application.Accounting.Services;
using Magic.Application.Expenses.Services;
using Magic.Contracts.Expenses;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Expenses.Services;

public sealed class ExpenseService(
    MagicDbContext dbContext,
    IPostingService postingService) : IExpenseService
{
    public async Task<ExpenseSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var expenses = await dbContext.Expenses
            .Where(e => e.CompanyId == companyId && e.BranchId == branchId)
            .ToListAsync(cancellationToken);

        var total = expenses.Sum(e => e.TotalAmountPaise);
        var pending = expenses.Where(e => e.Status == "Submitted").Sum(e => e.TotalAmountPaise);
        var paid = expenses.Where(e => e.Status == "Paid").Sum(e => e.TotalAmountPaise);
        var pendingCount = expenses.Count(e => e.Status == "Submitted");

        return new ExpenseSummaryDto(total, pending, paid, pendingCount, expenses.Count);
    }

    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ExpenseCategories
            .Include(c => c.DefaultGlAccount)
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryDto(
                c.Id,
                c.CompanyId,
                c.Code,
                c.Name,
                c.DefaultGlAccountId,
                c.DefaultGlAccount != null ? c.DefaultGlAccount.Name : null,
                c.Description,
                c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExpenseCategoryDto> CreateCategoryAsync(Guid companyId, CreateExpenseCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var glAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == request.DefaultGlAccountId && a.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("GL_ACCOUNT_NOT_FOUND", "Default General Ledger account does not exist.", StatusCodes.Status404NotFound);

        var category = new ExpenseCategoryRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            DefaultGlAccountId = glAccount.Id,
            Description = request.Description,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.ExpenseCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ExpenseCategoryDto(
            category.Id,
            category.CompanyId,
            category.Code,
            category.Name,
            category.DefaultGlAccountId,
            glAccount.Name,
            category.Description,
            category.IsActive);
    }

    public async Task<(IReadOnlyList<ExpenseDto> Items, int TotalCount)> GetExpensesAsync(
        Guid companyId,
        Guid branchId,
        Guid? categoryId,
        string? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Expenses
            .Include(e => e.ExpenseCategory)
            .Include(e => e.GlAccount)
            .Include(e => e.Vendor)
            .Include(e => e.BankCashAccount)
            .Where(e => e.CompanyId == companyId && e.BranchId == branchId);

        if (categoryId.HasValue)
            query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        if (fromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= toDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapExpense(e))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<ExpenseDto> GetExpenseByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await dbContext.Expenses
            .Include(e => e.ExpenseCategory)
            .Include(e => e.GlAccount)
            .Include(e => e.Vendor)
            .Include(e => e.BankCashAccount)
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EXPENSE_NOT_FOUND", "Expense record does not exist.", StatusCodes.Status404NotFound);

        return MapExpense(expense);
    }

    public async Task<ExpenseDto> CreateExpenseAsync(Guid companyId, Guid branchId, Guid userId, CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        if (request.AmountPaise <= 0)
            throw new DomainException("INVALID_AMOUNT", "Expense amount must be greater than zero.", StatusCodes.Status400BadRequest);

        var category = await dbContext.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == request.ExpenseCategoryId && c.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("CATEGORY_NOT_FOUND", "Expense category does not exist.", StatusCodes.Status404NotFound);

        var glAccountId = request.GlAccountId ?? category.DefaultGlAccountId;
        var glAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == glAccountId && a.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("GL_ACCOUNT_NOT_FOUND", "GL account does not exist.", StatusCodes.Status404NotFound);

        var expenseCount = await dbContext.Expenses.CountAsync(e => e.CompanyId == companyId, cancellationToken);
        var expenseNumber = $"EXP-{DateTime.UtcNow:yyyyMM}-{expenseCount + 1:D4}";

        var expense = new ExpenseRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            ExpenseNumber = expenseNumber,
            ExpenseCategoryId = category.Id,
            GlAccountId = glAccount.Id,
            VendorId = request.VendorId,
            PaymentMethod = request.PaymentMethod,
            BankCashAccountId = request.BankCashAccountId,
            ExpenseDate = request.ExpenseDate,
            AmountPaise = request.AmountPaise,
            TaxAmountPaise = request.TaxAmountPaise,
            TotalAmountPaise = request.AmountPaise + request.TaxAmountPaise,
            CostCenter = request.CostCenter,
            ReferenceNumber = request.ReferenceNumber,
            Description = request.Description,
            Status = "Submitted",
            AttachmentPath = request.AttachmentPath,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetExpenseByIdAsync(companyId, expense.Id, cancellationToken);
    }

    public async Task<ExpenseDto> ApproveExpenseAsync(Guid companyId, Guid id, Guid approverUserId, CancellationToken cancellationToken = default)
    {
        var expense = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EXPENSE_NOT_FOUND", "Expense does not exist.", StatusCodes.Status404NotFound);

        if (expense.Status != "Submitted")
            throw new DomainException("INVALID_STATE", $"Cannot approve expense in {expense.Status} status.", StatusCodes.Status400BadRequest);

        expense.Status = "Approved";
        expense.ApprovedByUserId = approverUserId;
        expense.ApprovedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetExpenseByIdAsync(companyId, expense.Id, cancellationToken);
    }

    public async Task<ExpenseDto> RejectExpenseAsync(Guid companyId, Guid id, Guid approverUserId, RejectExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EXPENSE_NOT_FOUND", "Expense does not exist.", StatusCodes.Status404NotFound);

        if (expense.Status != "Submitted")
            throw new DomainException("INVALID_STATE", $"Cannot reject expense in {expense.Status} status.", StatusCodes.Status400BadRequest);

        expense.Status = "Rejected";
        expense.ApprovedByUserId = approverUserId;
        expense.ApprovedAtUtc = DateTimeOffset.UtcNow;
        expense.RejectionReason = request.Reason;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetExpenseByIdAsync(companyId, expense.Id, cancellationToken);
    }

    public async Task<ExpenseDto> PayExpenseAsync(Guid companyId, Guid id, Guid userId, PayExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = await dbContext.Expenses
            .Include(e => e.ExpenseCategory)
            .Include(e => e.GlAccount)
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EXPENSE_NOT_FOUND", "Expense does not exist.", StatusCodes.Status404NotFound);

        if (expense.Status != "Approved")
            throw new DomainException("INVALID_STATE", "Expense must be approved before payment can be disbursed.", StatusCodes.Status400BadRequest);

        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.StartDate <= request.PaymentDate && p.EndDate >= request.PaymentDate, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "No active fiscal period covers the payment date.", StatusCodes.Status400BadRequest);

        Guid creditGlAccountId;
        if (request.PaymentMethod == "PettyCash")
        {
            var pettyAccount = await dbContext.PettyCashAccounts.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.BranchId == expense.BranchId && p.IsActive, cancellationToken)
                ?? throw new DomainException("PETTY_CASH_NOT_FOUND", "No active petty cash account found for this branch.", StatusCodes.Status400BadRequest);

            if (pettyAccount.CurrentBalancePaise < expense.TotalAmountPaise)
                throw new DomainException("INSUFFICIENT_PETTY_CASH", "Insufficient petty cash balance to disburse this expense.", StatusCodes.Status400BadRequest);

            creditGlAccountId = pettyAccount.GlAccountId;
            pettyAccount.CurrentBalancePaise -= expense.TotalAmountPaise;

            dbContext.PettyCashTransactions.Add(new PettyCashTransactionRecord
            {
                Id = Guid.NewGuid(),
                PettyCashAccountId = pettyAccount.Id,
                TransactionType = "Expense",
                AmountPaise = expense.TotalAmountPaise,
                BalanceAfterPaise = pettyAccount.CurrentBalancePaise,
                ExpenseId = expense.Id,
                PerformedByUserId = userId,
                Notes = $"Disbursement for {expense.ExpenseNumber}: {expense.Description}",
                TransactionDateUtc = DateTimeOffset.UtcNow
            });
        }
        else if (request.PaymentMethod == "Bank" && request.BankCashAccountId.HasValue)
        {
            var bankAccount = await dbContext.BankCashAccounts.FirstOrDefaultAsync(b => b.Id == request.BankCashAccountId.Value && b.CompanyId == companyId, cancellationToken)
                ?? throw new DomainException("BANK_ACCOUNT_NOT_FOUND", "Bank account does not exist.", StatusCodes.Status404NotFound);
            creditGlAccountId = bankAccount.GlAccountId;
            bankAccount.CurrentBalancePaise -= expense.TotalAmountPaise;
            expense.BankCashAccountId = bankAccount.Id;
        }
        else
        {
            // Default Cash on Hand (1111)
            var cashAcc = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1111", cancellationToken)
                ?? throw new DomainException("CASH_ACCOUNT_NOT_FOUND", "Default cash account 1111 does not exist.", StatusCodes.Status400BadRequest);
            creditGlAccountId = cashAcc.Id;
        }

        // Post General Ledger Double-Entry Voucher
        var voucher = new JournalVoucher(
            companyId,
            expense.BranchId,
            period.Id,
            VoucherType.Payment,
            request.PaymentDate,
            userId,
            $"Expense Payment {expense.ExpenseNumber} - {expense.Description}",
            referenceNumber: request.ReferenceNumber,
            voucherNumber: string.Empty,
            idempotencyKey: Guid.NewGuid(),
            sourceModule: "EXPENSES",
            sourceDocumentId: expense.Id);

        // Debit Expense Account
        voucher.AddLine(expense.GlAccountId, Money.FromPaise(expense.AmountPaise), Money.Zero, expense.Description);

        // Debit Input GST if taxable
        if (expense.TaxAmountPaise > 0)
        {
            var inGstAcc = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "1131", cancellationToken);
            if (inGstAcc != null)
            {
                voucher.AddLine(inGstAcc.Id, Money.FromPaise(expense.TaxAmountPaise), Money.Zero, "Input GST Credit");
            }
            else
            {
                // Fallback add to expense line to balance
                voucher.AddLine(expense.GlAccountId, Money.FromPaise(expense.TaxAmountPaise), Money.Zero, "Input Tax");
            }
        }

        // Credit Cash/Bank/PettyCash
        voucher.AddLine(creditGlAccountId, Money.Zero, Money.FromPaise(expense.TotalAmountPaise), $"Disbursement for {expense.ExpenseNumber}");

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var postedVoucher = await postingService.PostVoucherAsync(voucher, "MAIN", fyCode, userId, hasSupervisorElevation: true, cancellationToken);

        expense.Status = "Paid";
        expense.PaymentMethod = request.PaymentMethod;
        expense.ReferenceNumber = request.ReferenceNumber;
        expense.JournalVoucherId = postedVoucher.Id;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetExpenseByIdAsync(companyId, expense.Id, cancellationToken);
    }

    public async Task<ExpenseDto> CancelExpenseAsync(Guid companyId, Guid id, Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var expense = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EXPENSE_NOT_FOUND", "Expense does not exist.", StatusCodes.Status404NotFound);

        if (expense.Status == "Cancelled")
            throw new DomainException("ALREADY_CANCELLED", "Expense is already cancelled.", StatusCodes.Status400BadRequest);

        if (expense.Status == "Paid" && expense.JournalVoucherId.HasValue)
        {
            var originalVoucher = await dbContext.Vouchers
                .FirstOrDefaultAsync(v => v.Id == expense.JournalVoucherId.Value, cancellationToken)
                ?? throw new DomainException("VOUCHER_NOT_FOUND", "Original accounting voucher not found.", StatusCodes.Status404NotFound);

            var period = await dbContext.FiscalPeriods
                .Include(p => p.FiscalYear)
                .FirstOrDefaultAsync(p => p.Id == originalVoucher.FiscalPeriodId, cancellationToken)
                ?? throw new DomainException("PERIOD_NOT_FOUND", "Fiscal period not found.", StatusCodes.Status400BadRequest);

            var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
            var postedReversal = await postingService.ReverseVoucherAsync(
                originalVoucher.Id,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "MAIN",
                fyCode,
                userId,
                reason,
                cancellationToken);

            expense.ReversalVoucherId = postedReversal.Id;

            // If petty cash, restore balance
            if (expense.PaymentMethod == "PettyCash")
            {
                var pettyAccount = await dbContext.PettyCashAccounts.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.BranchId == expense.BranchId, cancellationToken);
                if (pettyAccount != null)
                {
                    pettyAccount.CurrentBalancePaise += expense.TotalAmountPaise;
                    dbContext.PettyCashTransactions.Add(new PettyCashTransactionRecord
                    {
                        Id = Guid.NewGuid(),
                        PettyCashAccountId = pettyAccount.Id,
                        TransactionType = "Allocation",
                        AmountPaise = expense.TotalAmountPaise,
                        BalanceAfterPaise = pettyAccount.CurrentBalancePaise,
                        ExpenseId = expense.Id,
                        PerformedByUserId = userId,
                        Notes = $"Restoration from cancelled expense {expense.ExpenseNumber}: {reason}",
                        TransactionDateUtc = DateTimeOffset.UtcNow
                    });
                }
            }
        }

        expense.Status = "Cancelled";
        expense.RejectionReason = reason;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetExpenseByIdAsync(companyId, expense.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<PettyCashAccountDto>> GetPettyCashAccountsAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PettyCashAccounts
            .Include(p => p.GlAccount)
            .Include(p => p.CustodianUser)
            .Where(p => p.CompanyId == companyId && p.BranchId == branchId && p.IsActive)
            .Select(p => new PettyCashAccountDto(
                p.Id,
                p.CompanyId,
                p.BranchId,
                p.AccountName,
                p.GlAccountId,
                p.GlAccount != null ? p.GlAccount.Name : "Petty Cash",
                p.CustodianUserId,
                p.CustodianUser != null ? p.CustodianUser.Username : "Custodian",
                p.FloatLimitPaise,
                p.CurrentBalancePaise,
                p.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<PettyCashAccountDto> CreatePettyCashAccountAsync(Guid companyId, Guid branchId, CreatePettyCashAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = new PettyCashAccountRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            AccountName = request.AccountName.Trim(),
            GlAccountId = request.GlAccountId,
            CustodianUserId = request.CustodianUserId,
            FloatLimitPaise = request.FloatLimitPaise,
            CurrentBalancePaise = 0,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.PettyCashAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPettyCashAccountsAsync(companyId, branchId, cancellationToken)).First(a => a.Id == account.Id);
    }

    public async Task<IReadOnlyList<PettyCashTransactionDto>> GetPettyCashTransactionsAsync(Guid companyId, Guid accountId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PettyCashTransactions
            .Where(t => t.PettyCashAccountId == accountId)
            .OrderByDescending(t => t.TransactionDateUtc)
            .Select(t => new PettyCashTransactionDto(
                t.Id,
                t.PettyCashAccountId,
                t.TransactionType,
                t.AmountPaise,
                t.BalanceAfterPaise,
                t.ExpenseId,
                t.JournalVoucherId,
                t.PerformedByUserId,
                t.Notes,
                t.TransactionDateUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PettyCashTransactionDto> ReplenishPettyCashAsync(Guid companyId, Guid accountId, Guid userId, ReplenishPettyCashRequest request, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.PettyCashAccounts.FirstOrDefaultAsync(p => p.Id == accountId && p.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Petty cash account not found.", StatusCodes.Status404NotFound);

        var bankAccount = await dbContext.BankCashAccounts.FirstOrDefaultAsync(b => b.Id == request.SourceBankAccountId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BANK_NOT_FOUND", "Source bank account not found.", StatusCodes.Status404NotFound);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var period = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.StartDate <= today && p.EndDate >= today, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "Active fiscal period not found.", StatusCodes.Status400BadRequest);

        // Contra Voucher: Debit Petty Cash GL, Credit Bank GL
        var voucher = new JournalVoucher(
            companyId,
            account.BranchId,
            period.Id,
            VoucherType.Contra,
            today,
            userId,
            $"Petty Cash Replenishment for {account.AccountName}",
            referenceNumber: null,
            voucherNumber: string.Empty,
            idempotencyKey: Guid.NewGuid(),
            sourceModule: "PETTY_CASH",
            sourceDocumentId: account.Id);

        voucher.AddLine(account.GlAccountId, Money.FromPaise(request.AmountPaise), Money.Zero, "Petty Cash Replenishment");
        voucher.AddLine(bankAccount.GlAccountId, Money.Zero, Money.FromPaise(request.AmountPaise), $"Disbursement to {account.AccountName}");

        var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
        var posted = await postingService.PostVoucherAsync(voucher, "MAIN", fyCode, userId, hasSupervisorElevation: true, cancellationToken);

        account.CurrentBalancePaise += request.AmountPaise;
        bankAccount.CurrentBalancePaise -= request.AmountPaise;

        var tx = new PettyCashTransactionRecord
        {
            Id = Guid.NewGuid(),
            PettyCashAccountId = account.Id,
            TransactionType = "Replenishment",
            AmountPaise = request.AmountPaise,
            BalanceAfterPaise = account.CurrentBalancePaise,
            JournalVoucherId = posted.Id,
            PerformedByUserId = userId,
            Notes = request.Notes ?? "Replenishment from Bank",
            TransactionDateUtc = DateTimeOffset.UtcNow
        };

        dbContext.PettyCashTransactions.Add(tx);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PettyCashTransactionDto(
            tx.Id,
            tx.PettyCashAccountId,
            tx.TransactionType,
            tx.AmountPaise,
            tx.BalanceAfterPaise,
            tx.ExpenseId,
            tx.JournalVoucherId,
            tx.PerformedByUserId,
            tx.Notes,
            tx.TransactionDateUtc);
    }

    public async Task<PettyCashTransactionDto> ReconcilePettyCashAsync(Guid companyId, Guid accountId, Guid userId, ReconcilePettyCashRequest request, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.PettyCashAccounts.FirstOrDefaultAsync(p => p.Id == accountId && p.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", "Petty cash account not found.", StatusCodes.Status404NotFound);

        var differencePaise = request.ActualCountPaise - account.CurrentBalancePaise;
        string txType = differencePaise < 0 ? "Shortage" : "Excess";

        Guid? voucherId = null;
        if (differencePaise != 0)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var period = await dbContext.FiscalPeriods
                .Include(p => p.FiscalYear)
                .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.StartDate <= today && p.EndDate >= today, cancellationToken);

            if (period != null)
            {
                var discrepancyAcc = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "5290", cancellationToken);
                if (discrepancyAcc != null)
                {
                    var voucher = new JournalVoucher(
                        companyId,
                        account.BranchId,
                        period.Id,
                        VoucherType.Journal,
                        today,
                        userId,
                        $"Petty Cash Count Reconciliation: {txType} ({account.AccountName})",
                        referenceNumber: null,
                        voucherNumber: string.Empty,
                        idempotencyKey: Guid.NewGuid(),
                        sourceModule: "PETTY_CASH",
                        sourceDocumentId: account.Id);

                    var diffAbs = Math.Abs(differencePaise);
                    if (differencePaise < 0)
                    {
                        // Shortage: Debit Expense 5290, Credit Petty Cash Asset
                        voucher.AddLine(discrepancyAcc.Id, Money.FromPaise(diffAbs), Money.Zero, "Petty Cash Shortage");
                        voucher.AddLine(account.GlAccountId, Money.Zero, Money.FromPaise(diffAbs), "Petty Cash Reconciliation Deduction");
                    }
                    else
                    {
                        // Excess: Debit Petty Cash Asset, Credit Discrepancy/Gain
                        voucher.AddLine(account.GlAccountId, Money.FromPaise(diffAbs), Money.Zero, "Petty Cash Excess");
                        voucher.AddLine(discrepancyAcc.Id, Money.Zero, Money.FromPaise(diffAbs), "Petty Cash Reconciliation Gain");
                    }

                    var fyCode = period.FiscalYear?.Code ?? "FY2026-27";
                    var posted = await postingService.PostVoucherAsync(voucher, "MAIN", fyCode, userId, hasSupervisorElevation: true, cancellationToken);
                    voucherId = posted.Id;
                }
            }
        }

        account.CurrentBalancePaise = request.ActualCountPaise;

        var tx = new PettyCashTransactionRecord
        {
            Id = Guid.NewGuid(),
            PettyCashAccountId = account.Id,
            TransactionType = txType,
            AmountPaise = Math.Abs(differencePaise),
            BalanceAfterPaise = account.CurrentBalancePaise,
            JournalVoucherId = voucherId,
            PerformedByUserId = userId,
            Notes = request.Notes ?? $"Reconciliation: {txType} adjusted.",
            TransactionDateUtc = DateTimeOffset.UtcNow
        };

        dbContext.PettyCashTransactions.Add(tx);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PettyCashTransactionDto(
            tx.Id,
            tx.PettyCashAccountId,
            tx.TransactionType,
            tx.AmountPaise,
            tx.BalanceAfterPaise,
            tx.ExpenseId,
            tx.JournalVoucherId,
            tx.PerformedByUserId,
            tx.Notes,
            tx.TransactionDateUtc);
    }

    private static ExpenseDto MapExpense(ExpenseRecord e) => new(
        e.Id,
        e.CompanyId,
        e.BranchId,
        e.ExpenseNumber,
        e.ExpenseCategoryId,
        e.ExpenseCategory != null ? e.ExpenseCategory.Name : "Unknown",
        e.GlAccountId,
        e.GlAccount != null ? e.GlAccount.Name : "Expense Account",
        e.VendorId,
        e.Vendor != null ? e.Vendor.Name : null,
        e.PaymentMethod,
        e.BankCashAccountId,
        e.BankCashAccount != null ? e.BankCashAccount.AccountName : null,
        e.ExpenseDate,
        e.AmountPaise,
        e.TaxAmountPaise,
        e.TotalAmountPaise,
        e.CostCenter,
        e.ReferenceNumber,
        e.Description,
        e.Status,
        e.ApprovedByUserId,
        e.ApprovedAtUtc,
        e.RejectionReason,
        e.JournalVoucherId,
        e.ReversalVoucherId,
        e.AttachmentPath,
        e.CreatedAtUtc);
}
