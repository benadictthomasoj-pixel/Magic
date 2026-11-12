using Magic.Domain.Accounting.Entities;

namespace Magic.Application.Accounting.Services;

/// <summary>
/// Authoritative application posting service orchestrating the atomic, transaction-safe lifecycle of general ledger postings.
/// Executes pre-posting validation, concurrency-safe sequential voucher number allocation, aggregate commitment,
/// audit logging, and outbox event dispatch in an atomic PostgreSQL transaction.
/// </summary>
public interface IPostingService
{
    Task<JournalVoucher> PostVoucherAsync(
        JournalVoucher voucher,
        string branchCode,
        string fyCode,
        Guid userId,
        bool hasSupervisorElevation = false,
        CancellationToken cancellationToken = default);

    Task<JournalVoucher> ReverseVoucherAsync(
        Guid originalVoucherId,
        DateOnly reversalDate,
        string branchCode,
        string fyCode,
        Guid supervisorUserId,
        string reason,
        CancellationToken cancellationToken = default);
}
