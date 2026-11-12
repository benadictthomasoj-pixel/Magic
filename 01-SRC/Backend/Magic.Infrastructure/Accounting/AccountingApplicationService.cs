using System.Globalization;
using FluentValidation;
using Magic.Application.Abstractions;
using Magic.Application.Accounting.Persistence;
using Magic.Application.Accounting.Services;
using Magic.Application.Accounting.Validation;
using Magic.Application.Validation;
using Magic.Contracts.Accounting;
using Magic.Contracts.Common;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Accounting.Services;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Accounting;

/// <summary>
/// Authoritative application service implementing accounting CQRS queries, draft management,
/// and orchestrating postings/reversals with transaction safety.
/// </summary>
public sealed class AccountingApplicationService(
    MagicDbContext dbContext,
    IUnitOfWork unitOfWork,
    IPostingService postingService,
    IChartOfAccountsRepository chartOfAccountsRepository,
    IFiscalGovernanceRepository fiscalGovernanceRepository,
    IGeneralLedgerRepository generalLedgerRepository,
    IDateTimeProvider dateTimeProvider,
    IValidator<CreateAccountRequest> createAccountValidator,
    IValidator<UpdateAccountRequest> updateAccountValidator,
    IValidator<CreateJournalDraftRequest> createJournalDraftValidator,
    IValidator<PostVoucherRequest> postVoucherValidator,
    IValidator<ReverseVoucherRequest> reverseVoucherValidator) : IAccountingApplicationService
{
    public async Task<AccountingDashboardDto> GetDashboardAsync(
        Guid companyId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var vouchersQuery = dbContext.Vouchers
            .AsNoTracking()
            .Where(v => v.CompanyId == companyId && v.BranchId == branchId);

        var postedVouchers = await vouchersQuery
            .Where(v => v.Status == VoucherStatus.Posted || v.Status == VoucherStatus.Reversed)
            .Select(v => new { v.TotalDebitPaise, v.TotalCreditPaise })
            .ToListAsync(cancellationToken);

        var totalDebit = postedVouchers.Sum(v => v.TotalDebitPaise);
        var totalCredit = postedVouchers.Sum(v => v.TotalCreditPaise);
        var postedCount = postedVouchers.Count;

        var draftCount = await vouchersQuery
            .CountAsync(v => v.Status == VoucherStatus.Draft, cancellationToken);

        // Active fiscal period
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow.Date);
        var activePeriod = await dbContext.FiscalPeriods
            .AsNoTracking()
            .Include(fp => fp.FiscalYear)
            .Where(fp => fp.CompanyId == companyId && fp.StartDate <= today && fp.EndDate >= today)
            .FirstOrDefaultAsync(cancellationToken);

        // Fallback to most recent period if no exact date match
        activePeriod ??= await dbContext.FiscalPeriods
            .AsNoTracking()
            .Include(fp => fp.FiscalYear)
            .Where(fp => fp.CompanyId == companyId)
            .OrderByDescending(fp => fp.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        // Recent 5 vouchers
        var recentEntities = await vouchersQuery
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.CreatedAtUtc)
            .Take(5)
            .Select(v => new VoucherSummaryDto
            {
                Id = v.Id,
                VoucherNumber = v.VoucherNumber,
                VoucherType = v.VoucherType.ToString(),
                VoucherDate = v.VoucherDate,
                Status = v.Status.ToString(),
                Narration = v.Narration,
                ReferenceNumber = v.ReferenceNumber,
                TotalDebitPaise = v.TotalDebitPaise,
                TotalCreditPaise = v.TotalCreditPaise,
                IsReversed = v.IsReversed,
                LinesCount = v.Lines.Count,
                PostedAtUtc = v.PostedAtUtc
            })
            .ToListAsync(cancellationToken);

        var validationStatus = postedCount == 0 ? "EMPTY" : (totalDebit == totalCredit ? "BALANCED" : "UNBALANCED");

        return new AccountingDashboardDto
        {
            TotalDebitPaise = totalDebit,
            TotalCreditPaise = totalCredit,
            PostedVouchersCount = postedCount,
            DraftVouchersCount = draftCount,
            ActiveFiscalYearName = activePeriod?.FiscalYear?.Name ?? activePeriod?.FiscalYear?.Code,
            ActiveFiscalPeriodCode = activePeriod?.Code,
            IsActivePeriodLocked = activePeriod != null && activePeriod.State == PeriodState.Locked,
            RecentVouchers = recentEntities,
            SystemValidationStatus = validationStatus
        };
    }

    public async Task<IReadOnlyList<AccountGroupDto>> GetAccountGroupsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AccountGroups
            .AsNoTracking()
            .Where(g => g.CompanyId == companyId)
            .OrderBy(g => g.RootClass)
            .ThenBy(g => g.HierarchyDepth)
            .ThenBy(g => g.Code)
            .Select(g => new AccountGroupDto
            {
                Id = g.Id,
                ParentGroupId = g.ParentGroupId,
                Code = g.Code,
                Name = g.Name,
                RootClass = g.RootClass.ToString(),
                HierarchyDepth = g.HierarchyDepth,
                IsSystem = g.IsSystem,
                IsActive = g.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(
        Guid companyId,
        string? rootClass,
        bool? activeOnly,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.AccountGroup)
            .Where(a => a.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(rootClass) && Enum.TryParse<AccountRootClass>(rootClass, true, out var rcEnum))
        {
            query = query.Where(a => a.RootClass == rcEnum);
        }

        if (activeOnly == true)
        {
            query = query.Where(a => a.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchClean = search.Trim();
            query = query.Where(a => EF.Functions.ILike(a.Code, $"%{searchClean}%") || EF.Functions.ILike(a.Name, $"%{searchClean}%"));
        }

        var accounts = await query
            .OrderBy(a => a.Code)
            .Select(a => new
            {
                Account = a,
                GroupName = a.AccountGroup != null ? a.AccountGroup.Name : string.Empty
            })
            .ToListAsync(cancellationToken);

        var accountIds = accounts.Select(a => a.Account.Id).ToArray();

        // Calculate current balances from posted journal lines
        var lines = await dbContext.JournalLines
            .AsNoTracking()
            .Where(l => accountIds.Contains(l.AccountId) &&
                        (l.Voucher!.Status == VoucherStatus.Posted || l.Voucher!.Status == VoucherStatus.Reversed))
            .Select(l => new { l.AccountId, l.Debit, l.Credit })
            .ToListAsync(cancellationToken);

        var balances = lines
            .GroupBy(l => l.AccountId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    TotalDebit = g.Sum(x => x.Debit.Paise),
                    TotalCredit = g.Sum(x => x.Credit.Paise)
                });

        return accounts.Select(item =>
        {
            var a = item.Account;
            var bal = balances.TryGetValue(a.Id, out var b) ? b : null;
            var debit = bal?.TotalDebit ?? 0L;
            var credit = bal?.TotalCredit ?? 0L;
            var netBalance = (a.RootClass is AccountRootClass.Asset or AccountRootClass.Expense)
                ? debit - credit
                : credit - debit;

            return new AccountDto
            {
                Id = a.Id,
                GroupId = a.AccountGroupId,
                GroupName = item.GroupName,
                Code = a.Code,
                Name = a.Name,
                RootClass = a.RootClass.ToString(),
                CurrencyCode = "INR",
                IsActive = a.IsActive,
                ControlAccountType = a.ControlAccountType?.ToString(),
                CurrentBalancePaise = netBalance
            };
        }).ToList();
    }

    public async Task<AccountDto> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.AccountGroup)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", $"Account '{id}' was not found.", 404);

        var lines = await dbContext.JournalLines
            .AsNoTracking()
            .Where(l => l.AccountId == id &&
                        (l.Voucher!.Status == VoucherStatus.Posted || l.Voucher!.Status == VoucherStatus.Reversed))
            .Select(l => new { l.Debit, l.Credit })
            .ToListAsync(cancellationToken);

        var debit = lines.Sum(l => l.Debit.Paise);
        var credit = lines.Sum(l => l.Credit.Paise);
        var netBalance = (account.RootClass is AccountRootClass.Asset or AccountRootClass.Expense)
            ? debit - credit
            : credit - debit;

        return new AccountDto
        {
            Id = account.Id,
            GroupId = account.AccountGroupId,
            GroupName = account.AccountGroup?.Name ?? string.Empty,
            Code = account.Code,
            Name = account.Name,
            RootClass = account.RootClass.ToString(),
            CurrencyCode = "INR",
            IsActive = account.IsActive,
            ControlAccountType = account.ControlAccountType?.ToString(),
            CurrentBalancePaise = netBalance
        };
    }

    public async Task<AccountDto> CreateAccountAsync(
        Guid companyId,
        Guid branchId,
        CreateAccountRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await ValidationRunner.ValidateAndThrowAsync(createAccountValidator, request, cancellationToken);

        var group = await chartOfAccountsRepository.GetGroupByIdAsync(request.GroupId, cancellationToken)
            ?? throw new DomainException("GROUP_NOT_FOUND", $"Account group '{request.GroupId}' was not found.", 404);

        if (!group.IsActive)
        {
            throw new DomainException("GROUP_INACTIVE", $"Account group '{group.Name}' is inactive. Cannot create accounts in an inactive group.");
        }

        var existingWithCode = await chartOfAccountsRepository.GetAccountByCodeAsync(companyId, request.Code, cancellationToken);
        if (existingWithCode != null)
        {
            throw new DomainException("DUPLICATE_ACCOUNT_CODE", $"Account code '{request.Code}' already exists for this company.");
        }

        if (!Enum.TryParse<AccountRootClass>(request.RootClass, true, out var rootClass))
        {
            throw new DomainException("INVALID_ROOT_CLASS", $"Invalid root class '{request.RootClass}'.");
        }

        var isControl = !string.IsNullOrWhiteSpace(request.ControlAccountType);
        var account = new Account(
            companyId,
            branchId,
            group.Id,
            request.Code,
            request.Name,
            rootClass,
            userId,
            isControlAccount: isControl,
            controlAccountType: request.ControlAccountType);

        await chartOfAccountsRepository.AddAccountAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AccountDto
        {
            Id = account.Id,
            GroupId = account.AccountGroupId,
            GroupName = group.Name,
            Code = account.Code,
            Name = account.Name,
            RootClass = account.RootClass.ToString(),
            CurrencyCode = "INR",
            IsActive = account.IsActive,
            ControlAccountType = account.ControlAccountType?.ToString(),
            CurrentBalancePaise = 0
        };
    }

    public async Task<AccountDto> UpdateAccountAsync(
        Guid id,
        UpdateAccountRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await ValidationRunner.ValidateAndThrowAsync(updateAccountValidator, request, cancellationToken);

        var account = await chartOfAccountsRepository.GetAccountByIdAsync(id, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", $"Account '{id}' was not found.", 404);

        account.UpdateName(request.Name, userId);
        if (request.IsActive && !account.IsActive)
        {
            account.Activate(userId);
        }
        else if (!request.IsActive && account.IsActive)
        {
            account.Deactivate(userId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetAccountByIdAsync(id, cancellationToken);
    }

    public async Task<(IReadOnlyList<VoucherSummaryDto> Items, int TotalCount)> GetVouchersAsync(
        Guid companyId,
        Guid branchId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? voucherType,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = dbContext.Vouchers
            .AsNoTracking()
            .Where(v => v.CompanyId == companyId && v.BranchId == branchId);

        if (dateFrom.HasValue)
        {
            query = query.Where(v => v.VoucherDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(v => v.VoucherDate <= dateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(voucherType) && Enum.TryParse<VoucherType>(voucherType, true, out var vtEnum))
        {
            query = query.Where(v => v.VoucherType == vtEnum);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<VoucherStatus>(status, true, out var vsEnum))
        {
            query = query.Where(v => v.Status == vsEnum);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchClean = search.Trim();
            query = query.Where(v => EF.Functions.ILike(v.VoucherNumber, $"%{searchClean}%") ||
                                     EF.Functions.ILike(v.Narration, $"%{searchClean}%") ||
                                     (v.ReferenceNumber != null && EF.Functions.ILike(v.ReferenceNumber, $"%{searchClean}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VoucherSummaryDto
            {
                Id = v.Id,
                VoucherNumber = v.VoucherNumber,
                VoucherType = v.VoucherType.ToString(),
                VoucherDate = v.VoucherDate,
                Status = v.Status.ToString(),
                Narration = v.Narration,
                ReferenceNumber = v.ReferenceNumber,
                TotalDebitPaise = v.TotalDebitPaise,
                TotalCreditPaise = v.TotalCreditPaise,
                IsReversed = v.IsReversed,
                LinesCount = v.Lines.Count,
                PostedAtUtc = v.PostedAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<VoucherDetailDto> GetVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var voucher = await dbContext.Vouchers
            .AsNoTracking()
            .Include(v => v.FiscalPeriod)
            .Include(v => v.ReversalVoucher)
            .Include(v => v.Lines)
                .ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new DomainException("VOUCHER_NOT_FOUND", $"Voucher '{id}' was not found.", 404);

        return new VoucherDetailDto
        {
            Id = voucher.Id,
            BranchId = voucher.BranchId,
            FiscalPeriodId = voucher.FiscalPeriodId,
            PeriodCode = voucher.FiscalPeriod?.Code ?? string.Empty,
            VoucherNumber = voucher.VoucherNumber,
            VoucherType = voucher.VoucherType.ToString(),
            VoucherDate = voucher.VoucherDate,
            Status = voucher.Status.ToString(),
            Narration = voucher.Narration,
            ReferenceNumber = voucher.ReferenceNumber,
            TotalDebitPaise = voucher.TotalDebitPaise,
            TotalCreditPaise = voucher.TotalCreditPaise,
            IsReversed = voucher.IsReversed,
            ReversalVoucherId = voucher.ReversalVoucherId,
            ReversalVoucherNumber = voucher.ReversalVoucher?.VoucherNumber,
            TraceCorrelationId = voucher.TraceCorrelationId,
            PostedAtUtc = voucher.PostedAtUtc,
            PostedByUserId = voucher.PostedByUserId,
            CreatedAtUtc = voucher.CreatedAtUtc,
            Lines = voucher.Lines.OrderBy(l => l.LineNumber).Select(l => new JournalLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                AccountId = l.AccountId,
                AccountCode = l.Account?.Code ?? string.Empty,
                AccountName = l.Account?.Name ?? string.Empty,
                DebitPaise = l.Debit.Paise,
                CreditPaise = l.Credit.Paise,
                Narration = l.Narration,
                PartyId = l.PartyId,
                PartyType = l.PartyType
            }).ToList()
        };
    }

    public async Task<VoucherDetailDto> CreateJournalDraftAsync(
        Guid companyId,
        Guid branchId,
        CreateJournalDraftRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await ValidationRunner.ValidateAndThrowAsync(createJournalDraftValidator, request, cancellationToken);

        if (!Enum.TryParse<VoucherType>(request.VoucherType, true, out var vType))
        {
            vType = VoucherType.Journal;
        }

        var voucher = new JournalVoucher(
            companyId,
            branchId,
            request.FiscalPeriodId,
            vType,
            request.VoucherDate,
            userId,
            narration: request.Narration,
            referenceNumber: request.ReferenceNumber,
            idempotencyKey: Guid.CreateVersion7());

        foreach (var line in request.Lines)
        {
            voucher.AddLine(
                line.AccountId,
                Money.FromPaise(line.DebitPaise),
                Money.FromPaise(line.CreditPaise),
                line.Narration,
                partyId: line.PartyId);
        }

        await generalLedgerRepository.AddVoucherAsync(voucher, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetVoucherByIdAsync(voucher.Id, cancellationToken);
    }

    public async Task<JournalValidationResultDto> ValidateJournalAsync(
        Guid companyId,
        Guid branchId,
        CreateJournalDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (request.Lines == null || request.Lines.Count < 2)
        {
            errors.Add("Journal voucher requires at least two lines (one debit and one credit).");
        }

        var totalDebit = request.Lines?.Sum(l => l.DebitPaise) ?? 0L;
        var totalCredit = request.Lines?.Sum(l => l.CreditPaise) ?? 0L;
        var diff = Math.Abs(totalDebit - totalCredit);
        var isBalanced = totalDebit > 0 && totalDebit == totalCredit;

        if (totalDebit == 0 && totalCredit == 0)
        {
            errors.Add("Voucher amount must be greater than zero.");
        }
        else if (totalDebit != totalCredit)
        {
            errors.Add($"Double-entry invariant violated: Debit total ({totalDebit / 100.0:F2}) does not match Credit total ({totalCredit / 100.0:F2}). Difference: {diff / 100.0:F2} INR.");
        }

        // Validate fiscal period
        var period = await fiscalGovernanceRepository.GetPeriodByIdAsync(request.FiscalPeriodId, cancellationToken);
        if (period == null)
        {
            errors.Add($"Fiscal period '{request.FiscalPeriodId}' was not found.");
        }
        else
        {
            if (period.State == PeriodState.Closing)
            {
                errors.Add($"Fiscal period '{period.Code}' is in closing reconciliation state.");
            }
            if (period.State == PeriodState.Locked)
            {
                warnings.Add($"Fiscal period '{period.Code}' is locked. Posting requires authorized supervisor elevation.");
            }
            if (request.VoucherDate < period.StartDate || request.VoucherDate > period.EndDate)
            {
                errors.Add($"Voucher date '{request.VoucherDate:yyyy-MM-dd}' falls outside fiscal period '{period.Code}' range ({period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd}).");
            }
        }

        // Validate accounts
        if (request.Lines != null)
        {
            var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToArray();
            var accounts = await chartOfAccountsRepository.GetAccountsByIdsAsync(accountIds, cancellationToken);

            foreach (var line in request.Lines)
            {
                if (!accounts.TryGetValue(line.AccountId, out var acc))
                {
                    errors.Add($"Account '{line.AccountId}' does not exist.");
                    continue;
                }

                if (!acc.IsActive)
                {
                    errors.Add($"Account '{acc.Code} - {acc.Name}' is inactive.");
                }

                if ((line.DebitPaise > 0 && line.CreditPaise > 0) || (line.DebitPaise == 0 && line.CreditPaise == 0))
                {
                    errors.Add($"Line for account '{acc.Code}' must have either Debit or Credit strictly greater than 0, never both.");
                }
            }
        }

        return new JournalValidationResultDto
        {
            IsValid = errors.Count == 0,
            TotalDebitPaise = totalDebit,
            TotalCreditPaise = totalCredit,
            DifferencePaise = diff,
            IsBalanced = isBalanced,
            Errors = errors,
            Warnings = warnings
        };
    }

    public async Task<VoucherDetailDto> PostVoucherAsync(
        Guid voucherId,
        PostVoucherRequest request,
        Guid userId,
        bool hasSupervisorElevation = false,
        CancellationToken cancellationToken = default)
    {
        await ValidationRunner.ValidateAndThrowAsync(postVoucherValidator, request, cancellationToken);

        var voucher = await generalLedgerRepository.GetVoucherByIdAsync(voucherId, cancellationToken)
            ?? throw new DomainException("VOUCHER_NOT_FOUND", $"Voucher '{voucherId}' was not found.", 404);

        if (voucher.Status == VoucherStatus.Posted)
        {
            throw new DomainException("VOUCHER_ALREADY_POSTED", $"Voucher '{voucher.VoucherNumber}' has already been posted.");
        }

        var posted = await postingService.PostVoucherAsync(
            voucher,
            request.BranchCode,
            request.FyCode,
            userId,
            hasSupervisorElevation,
            cancellationToken);

        return await GetVoucherByIdAsync(posted.Id, cancellationToken);
    }

    public async Task<VoucherDetailDto> ReverseVoucherAsync(
        Guid voucherId,
        ReverseVoucherRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await ValidationRunner.ValidateAndThrowAsync(reverseVoucherValidator, request, cancellationToken);

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow.Date);
        var reversal = await postingService.ReverseVoucherAsync(
            voucherId,
            today,
            request.BranchCode,
            request.FyCode,
            userId,
            request.Reason,
            cancellationToken);

        return await GetVoucherByIdAsync(reversal.Id, cancellationToken);
    }

    public async Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(
        Guid companyId,
        Guid branchId,
        Guid accountId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId && a.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ACCOUNT_NOT_FOUND", $"Account '{accountId}' was not found.", 404);

        var start = dateFrom ?? new DateOnly(2026, 4, 1);
        var end = dateTo ?? new DateOnly(2027, 3, 31);

        // Calculate opening balance before 'start'
        var openingLines = await dbContext.JournalLines
            .AsNoTracking()
            .Where(l => l.AccountId == accountId &&
                        l.BranchId == branchId &&
                        l.Voucher!.VoucherDate < start &&
                        (l.Voucher!.Status == VoucherStatus.Posted || l.Voucher!.Status == VoucherStatus.Reversed))
            .Select(l => new { l.Debit, l.Credit })
            .ToListAsync(cancellationToken);

        var opDebit = openingLines.Sum(l => l.Debit.Paise);
        var opCredit = openingLines.Sum(l => l.Credit.Paise);
        var openingBalance = (account.RootClass is AccountRootClass.Asset or AccountRootClass.Expense)
            ? opDebit - opCredit
            : opCredit - opDebit;

        // Load period lines
        var periodLines = await dbContext.JournalLines
            .AsNoTracking()
            .Include(l => l.Voucher)
            .Where(l => l.AccountId == accountId &&
                        l.BranchId == branchId &&
                        l.Voucher!.VoucherDate >= start &&
                        l.Voucher!.VoucherDate <= end &&
                        (l.Voucher!.Status == VoucherStatus.Posted || l.Voucher!.Status == VoucherStatus.Reversed))
            .OrderBy(l => l.Voucher!.VoucherDate)
            .ThenBy(l => l.Voucher!.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var entries = new List<GeneralLedgerEntryDto>();
        var running = openingBalance;
        long totalDebit = 0;
        long totalCredit = 0;

        foreach (var line in periodLines)
        {
            var dr = line.Debit.Paise;
            var cr = line.Credit.Paise;
            totalDebit += dr;
            totalCredit += cr;

            if (account.RootClass is AccountRootClass.Asset or AccountRootClass.Expense)
            {
                running += (dr - cr);
            }
            else
            {
                running += (cr - dr);
            }

            entries.Add(new GeneralLedgerEntryDto
            {
                VoucherId = line.VoucherId,
                VoucherNumber = line.Voucher?.VoucherNumber ?? string.Empty,
                VoucherType = line.Voucher?.VoucherType.ToString() ?? string.Empty,
                VoucherDate = line.Voucher?.VoucherDate ?? start,
                Narration = string.IsNullOrWhiteSpace(line.Narration) ? (line.Voucher?.Narration ?? string.Empty) : line.Narration,
                DebitPaise = dr,
                CreditPaise = cr,
                RunningBalancePaise = running
            });
        }

        return new GeneralLedgerReportDto
        {
            AccountId = account.Id,
            AccountCode = account.Code,
            AccountName = account.Name,
            RootClass = account.RootClass.ToString(),
            DateFrom = start,
            DateTo = end,
            OpeningBalancePaise = openingBalance,
            TotalDebitPaise = totalDebit,
            TotalCreditPaise = totalCredit,
            ClosingBalancePaise = running,
            Entries = entries
        };
    }

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(
        Guid companyId,
        Guid branchId,
        Guid? fiscalYearId,
        Guid? fiscalPeriodId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken = default)
    {
        var targetDate = asOfDate ?? DateOnly.FromDateTime(dateTimeProvider.UtcNow.Date);

        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Include(a => a.AccountGroup)
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);

        var lines = await dbContext.JournalLines
            .AsNoTracking()
            .Where(l => l.BranchId == branchId &&
                        l.Voucher!.VoucherDate <= targetDate &&
                        (l.Voucher!.Status == VoucherStatus.Posted || l.Voucher!.Status == VoucherStatus.Reversed))
            .Select(l => new { l.AccountId, l.Debit, l.Credit })
            .ToListAsync(cancellationToken);

        var movements = lines
            .GroupBy(l => l.AccountId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    TotalDebit = g.Sum(x => x.Debit.Paise),
                    TotalCredit = g.Sum(x => x.Credit.Paise)
                });

        var rowDtos = new List<TrialBalanceAccountRowDto>();
        long sumClosingDebit = 0;
        long sumClosingCredit = 0;
        long sumPeriodDebit = 0;
        long sumPeriodCredit = 0;

        foreach (var acc in accounts)
        {
            movements.TryGetValue(acc.Id, out var m);
            var debit = m?.TotalDebit ?? 0L;
            var credit = m?.TotalCredit ?? 0L;

            sumPeriodDebit += debit;
            sumPeriodCredit += credit;

            long closingDr = 0;
            long closingCr = 0;

            if (acc.RootClass is AccountRootClass.Asset or AccountRootClass.Expense)
            {
                var net = debit - credit;
                if (net >= 0) closingDr = net;
                else closingCr = Math.Abs(net);
            }
            else
            {
                var net = credit - debit;
                if (net >= 0) closingCr = net;
                else closingDr = Math.Abs(net);
            }

            sumClosingDebit += closingDr;
            sumClosingCredit += closingCr;

            rowDtos.Add(new TrialBalanceAccountRowDto
            {
                AccountId = acc.Id,
                AccountCode = acc.Code,
                AccountName = acc.Name,
                GroupCode = acc.AccountGroup?.Code ?? string.Empty,
                GroupName = acc.AccountGroup?.Name ?? string.Empty,
                RootClass = acc.RootClass.ToString(),
                OpeningDebitPaise = 0,
                OpeningCreditPaise = 0,
                PeriodDebitPaise = debit,
                PeriodCreditPaise = credit,
                ClosingDebitPaise = closingDr,
                ClosingCreditPaise = closingCr
            });
        }

        var diff = Math.Abs(sumClosingDebit - sumClosingCredit);

        return new TrialBalanceReportDto
        {
            FiscalYearCode = "FY2026-27",
            FiscalPeriodCode = null,
            AsOfDate = targetDate,
            TotalOpeningDebitPaise = 0,
            TotalOpeningCreditPaise = 0,
            TotalPeriodDebitPaise = sumPeriodDebit,
            TotalPeriodCreditPaise = sumPeriodCredit,
            TotalClosingDebitPaise = sumClosingDebit,
            TotalClosingCreditPaise = sumClosingCredit,
            DifferencePaise = diff,
            IsBalanced = diff == 0,
            Accounts = rowDtos
        };
    }

    public async Task<IReadOnlyList<FiscalYearDto>> GetFiscalYearsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.FiscalYears
            .AsNoTracking()
            .Include(fy => fy.Periods)
            .Where(fy => fy.CompanyId == companyId)
            .OrderByDescending(fy => fy.StartDate)
            .Select(fy => new FiscalYearDto
            {
                Id = fy.Id,
                Code = fy.Code,
                Name = fy.Name,
                StartDate = fy.StartDate,
                EndDate = fy.EndDate,
                IsClosed = fy.IsClosed,
                Periods = fy.Periods.OrderBy(p => p.PeriodNumber).Select(p => new FiscalPeriodDto
                {
                    Id = p.Id,
                    FiscalYearId = p.FiscalYearId,
                    PeriodNumber = p.PeriodNumber,
                    PeriodCode = p.Code,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    IsLocked = p.State == PeriodState.Locked,
                    IsClosed = p.State == PeriodState.Closing
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<FiscalPeriodDto> LockFiscalPeriodAsync(
        Guid periodId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var period = await fiscalGovernanceRepository.GetPeriodByIdAsync(periodId, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", $"Fiscal period '{periodId}' was not found.", 404);

        period.Lock(userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new FiscalPeriodDto
        {
            Id = period.Id,
            FiscalYearId = period.FiscalYearId,
            PeriodNumber = period.PeriodNumber,
            PeriodCode = period.Code,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IsLocked = period.State == PeriodState.Locked,
            IsClosed = period.State == PeriodState.Closing
        };
    }

    public async Task<FiscalPeriodDto> CloseFiscalPeriodAsync(
        Guid periodId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var period = await fiscalGovernanceRepository.GetPeriodByIdAsync(periodId, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", $"Fiscal period '{periodId}' was not found.", 404);

        period.TransitionToClosing(userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new FiscalPeriodDto
        {
            Id = period.Id,
            FiscalYearId = period.FiscalYearId,
            PeriodNumber = period.PeriodNumber,
            PeriodCode = period.Code,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IsLocked = period.State == PeriodState.Locked,
            IsClosed = period.State == PeriodState.Closing
        };
    }

    public async Task<FiscalPeriodDto> ReopenFiscalPeriodAsync(
        Guid periodId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var period = await fiscalGovernanceRepository.GetPeriodByIdAsync(periodId, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", $"Fiscal period '{periodId}' was not found.", 404);

        period.Reopen(userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new FiscalPeriodDto
        {
            Id = period.Id,
            FiscalYearId = period.FiscalYearId,
            PeriodNumber = period.PeriodNumber,
            PeriodCode = period.Code,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IsLocked = period.State == PeriodState.Locked,
            IsClosed = period.State == PeriodState.Closing
        };
    }

    public async Task<SubledgerReportDto> GetSubledgerReportAsync(
        Guid companyId,
        Guid branchId,
        string subledgerType,
        CancellationToken cancellationToken = default)
    {
        var isAr = string.Equals(subledgerType, "AR", StringComparison.OrdinalIgnoreCase);

        if (isAr)
        {
            var items = await dbContext.ArSubledgerItems
                .AsNoTracking()
                .Where(i => i.CompanyId == companyId && i.BranchId == branchId)
                .OrderBy(i => i.DueDate)
                .Select(i => new SubledgerItemDto
                {
                    Id = i.Id,
                    PartyId = i.CustomerId,
                    PartyName = "Customer Account",
                    DocumentNumber = i.InvoiceId.HasValue ? $"INV-{i.InvoiceId.Value}" : $"REC-{i.Id}",
                    DocumentDate = i.ItemDate,
                    DueDate = i.DueDate,
                    TotalAmountPaise = i.OriginalAmountPaise,
                    OutstandingBalancePaise = i.OutstandingAmountPaise,
                    Status = i.Status
                })
                .ToListAsync(cancellationToken);

            var totalOutstanding = items.Sum(i => i.OutstandingBalancePaise);

            return new SubledgerReportDto
            {
                SubledgerType = "AR",
                TotalOutstandingPaise = totalOutstanding,
                ItemsCount = items.Count,
                Items = items,
                Message = items.Count == 0
                    ? "Accounts Receivable subledger foundation active. No outstanding customer receivables recorded."
                    : string.Empty
            };
        }
        else
        {
            var items = await dbContext.ApSubledgerItems
                .AsNoTracking()
                .Where(i => i.CompanyId == companyId && i.BranchId == branchId)
                .OrderBy(i => i.DueDate)
                .Select(i => new SubledgerItemDto
                {
                    Id = i.Id,
                    PartyId = i.SupplierId,
                    PartyName = "Supplier Account",
                    DocumentNumber = i.BillId.HasValue ? $"BILL-{i.BillId.Value}" : $"PAY-{i.Id}",
                    DocumentDate = i.ItemDate,
                    DueDate = i.DueDate,
                    TotalAmountPaise = i.OriginalAmountPaise,
                    OutstandingBalancePaise = i.OutstandingAmountPaise,
                    Status = i.Status
                })
                .ToListAsync(cancellationToken);

            var totalOutstanding = items.Sum(i => i.OutstandingBalancePaise);

            return new SubledgerReportDto
            {
                SubledgerType = "AP",
                TotalOutstandingPaise = totalOutstanding,
                ItemsCount = items.Count,
                Items = items,
                Message = items.Count == 0
                    ? "Accounts Payable subledger foundation active. No outstanding supplier payables recorded."
                    : string.Empty
            };
        }
    }
}
