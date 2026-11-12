using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;

namespace Magic.Domain.Accounting.Services;

/// <summary>
/// Domain-level validation service executing exhaustive business invariant checks prior to General Ledger posting.
/// Verifies double-entry balance, account validity and status, control account restrictions,
/// fiscal period governance, source document traceability, and idempotency key requirements.
/// </summary>
public static class PostingValidator
{
    public static void ValidateForPosting(
        JournalVoucher voucher,
        FiscalPeriod period,
        IReadOnlyDictionary<Guid, Account> accountLookup,
        bool hasSupervisorElevation = false)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(accountLookup);

        // 1. Voucher status check
        if (voucher.Status != VoucherStatus.Draft && voucher.Status != VoucherStatus.Approved)
        {
            throw new DomainException(
                "DOMAIN_VOUCHER_CANNOT_POST",
                $"Cannot post voucher in status '{voucher.Status}'. Only DRAFT or APPROVED vouchers can be posted.");
        }

        // 2. Minimum line requirement
        if (voucher.Lines.Count < 2)
        {
            throw new DomainException(
                "DOMAIN_DOUBLE_ENTRY_MINIMUM_LINES",
                $"A double-entry voucher requires at least two lines (one debit, one credit). Line count: {voucher.Lines.Count}.");
        }

        // 3. Double-entry financial balance invariant (INV-FIN-02)
        var totalDebit = voucher.TotalDebit;
        var totalCredit = voucher.TotalCredit;

        if (totalDebit != totalCredit)
        {
            var deltaPaise = Math.Abs(totalDebit.Paise - totalCredit.Paise);
            throw new DomainException(
                "DOMAIN_DOUBLE_ENTRY_UNBALANCED",
                $"Double-entry invariant violated: Total Debits ({totalDebit}) does not equal Total Credits ({totalCredit}). Variance: {deltaPaise} paise.");
        }

        if (totalDebit.Paise <= 0)
        {
            throw new DomainException(
                "DOMAIN_ZERO_AMOUNT_VOUCHER",
                "A journal voucher cannot be posted with zero or negative total financial value.");
        }

        // 4. Fiscal period governance check (INV-FIN-05)
        period.AssertCanPost(voucher.VoucherDate, hasSupervisorElevation);

        // 5. Account existence, status, and control account restrictions (INV-FIN-06)
        foreach (var line in voucher.Lines)
        {
            if (!accountLookup.TryGetValue(line.AccountId, out var account))
            {
                throw new DomainException(
                    "DOMAIN_ACCOUNT_NOT_FOUND",
                    $"Journal line {line.LineNumber} references account ID '{line.AccountId}' which does not exist.");
            }

            if (!account.IsActive)
            {
                throw new DomainException(
                    "DOMAIN_ACCOUNT_INACTIVE",
                    $"Cannot post to inactive account '{account.Code} - {account.Name}' (Line {line.LineNumber}).");
            }

            // Direct manual journal entries to protected control accounts are prohibited
            if (account.IsControlAccount && !account.AllowDirectPosting && voucher.VoucherType == VoucherType.Journal)
            {
                throw new DomainException(
                    "DOMAIN_CONTROL_ACCOUNT_POSTING_BLOCKED",
                    $"Direct journal posting to control account '{account.Code} - {account.Name}' is prohibited. Postings must originate via subledgers.");
            }
        }

        // 6. Idempotency key requirement (INV-FIN-09)
        if (voucher.IdempotencyKey == Guid.Empty)
        {
            throw new DomainException(
                "DOMAIN_EMPTY_IDEMPOTENCY_KEY",
                "Financial mutation requires a non-empty unique Idempotency-Key.");
        }

        // 7. Automated voucher source document traceability (INV-FIN-10)
        if (voucher.VoucherType is VoucherType.Sales or VoucherType.Purchase)
        {
            if (string.IsNullOrWhiteSpace(voucher.SourceModule) || voucher.SourceDocumentId is null || voucher.SourceDocumentId == Guid.Empty)
            {
                throw new DomainException(
                    "DOMAIN_MISSING_SOURCE_TRACEABILITY",
                    $"Automated {voucher.VoucherType} vouchers must provide valid SourceModule and SourceDocumentId.");
            }
        }
    }
}
