using System.Text.Json;
using Magic.Application.Abstractions;
using Magic.Application.Accounting.Persistence;
using Magic.Application.Accounting.Services;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Accounting.Services;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Accounting;

/// <summary>
/// Authoritative implementation of IPostingService.
/// Coordinates transaction boundaries, idempotency verification, period governance,
/// sequential numbering, outbox dispatch, and audit ledger persistence in a single PostgreSQL transaction.
/// </summary>
public sealed class PostingService(
    MagicDbContext dbContext,
    IUnitOfWork unitOfWork,
    IVoucherNumberGenerator voucherNumberGenerator,
    IFiscalGovernanceRepository fiscalGovernanceRepository,
    IChartOfAccountsRepository chartOfAccountsRepository,
    IGeneralLedgerRepository generalLedgerRepository,
    IDateTimeProvider dateTimeProvider) : IPostingService
{
    public async Task<JournalVoucher> PostVoucherAsync(
        JournalVoucher voucher,
        string branchCode,
        string fyCode,
        Guid userId,
        bool hasSupervisorElevation = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        DomainGuard.NotEmpty(userId, nameof(userId));
        DomainGuard.NotNullOrWhiteSpace(branchCode, nameof(branchCode));
        DomainGuard.NotNullOrWhiteSpace(fyCode, nameof(fyCode));

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // 1. Idempotency Check: if identical key already exists for this branch, return existing
            var existing = await generalLedgerRepository.GetVoucherByIdempotencyKeyAsync(
                voucher.BranchId,
                voucher.IdempotencyKey,
                ct);

            if (existing is not null)
            {
                return existing;
            }

            // 2. Fetch Fiscal Period & governance check
            var period = await fiscalGovernanceRepository.GetPeriodByIdAsync(voucher.FiscalPeriodId, ct)
                ?? throw new DomainException(
                    "DOMAIN_PERIOD_NOT_FOUND",
                    $"Fiscal period '{voucher.FiscalPeriodId}' could not be resolved.");

            // 3. Load all referenced accounts for line validation
            var accountIds = voucher.Lines.Select(l => l.AccountId).Distinct();
            var accountLookup = await chartOfAccountsRepository.GetAccountsByIdsAsync(accountIds, ct);

            // 4. Validate double-entry equality, period state, and control account invariants
            PostingValidator.ValidateForPosting(voucher, period, accountLookup, hasSupervisorElevation);

            // 5. Concurrency-safe sequential voucher number allocation
            var sequentialVoucherNumber = await voucherNumberGenerator.NextVoucherNumberAsync(
                voucher.BranchId,
                period.FiscalYearId,
                branchCode,
                fyCode,
                voucher.VoucherType,
                ct);

            // 6. Execute domain posting
            voucher.Post(period, sequentialVoucherNumber, userId);

            // 7. Persist to EF Core DbContext
            await generalLedgerRepository.AddVoucherAsync(voucher, ct);

            // 8. Atomic Outbox Event Persistence
            var outboxMessage = new OutboxMessageRecord
            {
                Id = Guid.CreateVersion7(),
                OccurredOnUtc = dateTimeProvider.UtcNow,
                AggregateType = "JournalVoucher",
                AggregateId = voucher.Id,
                EventType = "VOUCHER_POSTED",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    voucherId = voucher.Id,
                    voucherNumber = voucher.VoucherNumber,
                    voucherType = voucher.VoucherType.ToString(),
                    postingDate = voucher.VoucherDate,
                    totalDebitPaise = voucher.TotalDebitPaise,
                    branchId = voucher.BranchId,
                    postedByUserId = userId
                }),
                TraceCorrelationId = voucher.TraceCorrelationId,
                RetryCount = 0
            };
            dbContext.OutboxMessages.Add(outboxMessage);

            // 9. Atomic System Audit Ledger Persistence
            var auditLog = new SystemAuditLogRecord
            {
                EventTimestampUtc = dateTimeProvider.UtcNow,
                UserId = userId,
                BranchId = voucher.BranchId,
                ClientIp = "127.0.0.1",
                ActionName = "POST_JOURNAL_VOUCHER",
                EntityName = "accounting.vouchers",
                EntityId = voucher.Id,
                AfterStateJson = JsonSerializer.Serialize(new
                {
                    voucherNumber = voucher.VoucherNumber,
                    voucherType = voucher.VoucherType.ToString(),
                    totalDebitPaise = voucher.TotalDebitPaise,
                    lineCount = voucher.Lines.Count
                }),
                CorrelationId = voucher.TraceCorrelationId
            };
            dbContext.SystemAuditLogs.Add(auditLog);

            return voucher;
        }, cancellationToken);
    }

    public async Task<JournalVoucher> ReverseVoucherAsync(
        Guid originalVoucherId,
        DateOnly reversalDate,
        string branchCode,
        string fyCode,
        Guid supervisorUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.NotEmpty(originalVoucherId, nameof(originalVoucherId));
        DomainGuard.NotEmpty(supervisorUserId, nameof(supervisorUserId));
        DomainGuard.NotNullOrWhiteSpace(branchCode, nameof(branchCode));
        DomainGuard.NotNullOrWhiteSpace(fyCode, nameof(fyCode));
        DomainGuard.NotNullOrWhiteSpace(reason, nameof(reason));

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var original = await generalLedgerRepository.GetVoucherByIdAsync(originalVoucherId, ct)
                ?? throw new DomainException(
                    "DOMAIN_VOUCHER_NOT_FOUND",
                    $"Original voucher '{originalVoucherId}' was not found.");

            if (original.IsReversed)
            {
                throw new DomainException(
                    "DOMAIN_VOUCHER_ALREADY_REVERSED",
                    $"Voucher '{original.VoucherNumber}' has already been reversed.");
            }

            // Find valid fiscal period for reversal date
            var reversalPeriod = await fiscalGovernanceRepository.GetPeriodByDateAsync(original.CompanyId, reversalDate, ct)
                ?? throw new DomainException(
                    "DOMAIN_REVERSAL_PERIOD_NOT_FOUND",
                    $"No open fiscal period found for reversal effective date '{reversalDate}'.");

            // Allocate reversal voucher number
            var reversalVoucherNumber = await voucherNumberGenerator.NextVoucherNumberAsync(
                original.BranchId,
                reversalPeriod.FiscalYearId,
                branchCode,
                fyCode,
                VoucherType.Reversal,
                ct);

            // Execute non-destructive reversal companion voucher creation
            var reversalVoucher = original.CreateReversal(
                reversalVoucherNumber,
                reversalDate,
                reversalPeriod,
                supervisorUserId,
                reason);

            await generalLedgerRepository.AddVoucherAsync(reversalVoucher, ct);

            // Outbox message for reversal
            var outboxMessage = new OutboxMessageRecord
            {
                Id = Guid.CreateVersion7(),
                OccurredOnUtc = dateTimeProvider.UtcNow,
                AggregateType = "JournalVoucher",
                AggregateId = reversalVoucher.Id,
                EventType = "VOUCHER_REVERSED",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    originalVoucherId = original.Id,
                    reversalVoucherId = reversalVoucher.Id,
                    reversalVoucherNumber = reversalVoucher.VoucherNumber,
                    reversalDate,
                    reversedByUserId = supervisorUserId
                }),
                TraceCorrelationId = reversalVoucher.TraceCorrelationId,
                RetryCount = 0
            };
            dbContext.OutboxMessages.Add(outboxMessage);

            // System audit log
            var auditLog = new SystemAuditLogRecord
            {
                EventTimestampUtc = dateTimeProvider.UtcNow,
                UserId = supervisorUserId,
                BranchId = original.BranchId,
                ClientIp = "127.0.0.1",
                ActionName = "REVERSE_JOURNAL_VOUCHER",
                EntityName = "accounting.vouchers",
                EntityId = original.Id,
                OverrideBySupervisorId = supervisorUserId,
                AfterStateJson = JsonSerializer.Serialize(new
                {
                    originalVoucherNumber = original.VoucherNumber,
                    reversalVoucherNumber = reversalVoucher.VoucherNumber,
                    reason
                }),
                CorrelationId = reversalVoucher.TraceCorrelationId
            };
            dbContext.SystemAuditLogs.Add(auditLog);

            return reversalVoucher;
        }, cancellationToken);
    }
}
