using Magic.Domain.Accounting.Enums;
using Magic.Domain.Accounting.Events;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;

namespace Magic.Domain.Accounting.Entities;

/// <summary>
/// Aggregate root representing a double-entry general ledger journal voucher.
/// Enforces fundamental accounting invariants:
/// - INV-FIN-01: 64-bit integer paise arithmetic.
/// - INV-FIN-02: Double-entry equality (∑ Debits ≡ ∑ Credits > 0).
/// - INV-FIN-03: Absolute immutability of posted records.
/// - INV-FIN-04: Non-destructive reversals via companion reversal vouchers.
/// - INV-FIN-05: Closed fiscal period posting rejection.
/// </summary>
public class JournalVoucher : DomainEntity, IAggregateRoot, IAuditableEntity
{
    private readonly List<JournalLine> _lines = [];

    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid FiscalPeriodId { get; private set; }
    public string VoucherNumber { get; private set; } = string.Empty;
    public VoucherType VoucherType { get; private set; }
    public DateOnly VoucherDate { get; private set; }
    public VoucherStatus Status { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string Narration { get; private set; } = string.Empty;
    public bool IsReversed { get; private set; }
    public Guid? ReversalVoucherId { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public string? SourceModule { get; private set; }
    public Guid? SourceDocumentId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public int ConcurrencyVersion { get; private set; }

    public IReadOnlyList<JournalLine> Lines => _lines.AsReadOnly();

    public Money TotalDebit => _lines.Aggregate(Money.Zero, (acc, line) => acc + line.Debit);
    public Money TotalCredit => _lines.Aggregate(Money.Zero, (acc, line) => acc + line.Credit);

    public bool IsBalanced => _lines.Count >= 2 && TotalDebit == TotalCredit && TotalDebit.Paise > 0;

    private JournalVoucher() { } // EF Core

    public JournalVoucher(
        Guid companyId,
        Guid branchId,
        Guid fiscalPeriodId,
        VoucherType voucherType,
        DateOnly voucherDate,
        Guid createdByUserId,
        string narration = "",
        string? referenceNumber = null,
        string voucherNumber = "",
        Guid? idempotencyKey = null,
        string? sourceModule = null,
        Guid? sourceDocumentId = null)
    {
        if (companyId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_COMPANY", "Company identity is required.");
        if (branchId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_BRANCH", "Branch identity is required.");
        if (fiscalPeriodId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_FISCAL_PERIOD", "Fiscal period identity is required.");

        CompanyId = companyId;
        BranchId = branchId;
        FiscalPeriodId = fiscalPeriodId;
        VoucherType = voucherType;
        VoucherDate = voucherDate;
        VoucherNumber = voucherNumber.Trim().ToUpperInvariant();
        Status = VoucherStatus.Draft;
        Narration = narration.Trim();
        ReferenceNumber = referenceNumber?.Trim();
        IsReversed = false;
        ReversalVoucherId = null;
        IdempotencyKey = idempotencyKey ?? Guid.NewGuid();
        SourceModule = sourceModule?.Trim();
        SourceDocumentId = sourceDocumentId;

        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        CreatedByUserId = createdByUserId;
        UpdatedAtUtc = now;
        ConcurrencyVersion = 1;
    }

    public void AddLine(
        Guid accountId,
        Money debit,
        Money credit,
        string narration = "",
        Guid? costCenterId = null,
        Guid? partyId = null)
    {
        AssertNotPostedOrReversed("Cannot add lines to a posted or finalized voucher.");

        var lineNumber = _lines.Count + 1;
        var line = new JournalLine(
            Id,
            accountId,
            debit,
            credit,
            lineNumber,
            narration,
            costCenterId,
            partyId);

        _lines.Add(line);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Approve(Guid supervisorUserId)
    {
        if (Status != VoucherStatus.Draft)
        {
            throw new DomainException(
                "DOMAIN_INVALID_VOUCHER_STATUS_TRANSITION",
                $"Only DRAFT vouchers can be approved. Current status: {Status}.");
        }

        Status = VoucherStatus.Approved;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = supervisorUserId;
    }

    /// <summary>
    /// Posts the voucher to the General Ledger, strictly enforcing double-entry and period invariance.
    /// Once posted, this record becomes permanently immutable (INV-FIN-03).
    /// </summary>
    public void Post(FiscalPeriod period, string sequentialVoucherNumber, Guid postedByUserId)
    {
        ArgumentNullException.ThrowIfNull(period);
        DomainGuard.NotNullOrWhiteSpace(sequentialVoucherNumber, nameof(sequentialVoucherNumber));

        if (Status != VoucherStatus.Draft && Status != VoucherStatus.Approved)
        {
            throw new DomainException(
                "DOMAIN_VOUCHER_CANNOT_POST",
                $"Cannot post voucher in status '{Status}'. Only DRAFT or APPROVED vouchers can be posted.");
        }

        // Invariant INV-FIN-05: Closed Period Posting Rejection
        period.AssertCanPost(VoucherDate);

        // Invariant INV-FIN-02: Double-Entry Balance Requirement
        if (_lines.Count < 2)
        {
            throw new DomainException(
                "DOMAIN_DOUBLE_ENTRY_MINIMUM_LINES",
                $"A double-entry voucher requires at least two lines (one debit, one credit). Line count: {_lines.Count}.");
        }

        var totalDebit = TotalDebit;
        var totalCredit = TotalCredit;

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

        VoucherNumber = sequentialVoucherNumber.Trim().ToUpperInvariant();
        FiscalPeriodId = period.Id;
        Status = VoucherStatus.Posted;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = postedByUserId;

        RaiseDomainEvent(new VoucherPostedDomainEvent(
            Id,
            VoucherNumber,
            VoucherType,
            VoucherDate,
            totalDebit,
            BranchId,
            postedByUserId));
    }

    /// <summary>
    /// Implements non-destructive reversal (INV-FIN-04).
    /// Generates and posts a companion reversal voucher mirroring all debit and credit legs in reverse.
    /// </summary>
    public JournalVoucher CreateReversal(
        string reversalVoucherNumber,
        DateOnly reversalDate,
        FiscalPeriod reversalPeriod,
        Guid supervisorUserId,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(reversalPeriod);
        DomainGuard.NotNullOrWhiteSpace(reversalVoucherNumber, nameof(reversalVoucherNumber));
        DomainGuard.NotNullOrWhiteSpace(reason, nameof(reason));

        if (IsReversed)
        {
            throw new DomainException(
                "DOMAIN_VOUCHER_ALREADY_REVERSED",
                $"Voucher '{VoucherNumber}' has already been reversed by voucher ID '{ReversalVoucherId}'.");
        }

        if (Status != VoucherStatus.Posted)
        {
            throw new DomainException(
                "DOMAIN_REVERSAL_INVALID_STATUS",
                $"Only POSTED vouchers can be reversed. Current status: {Status}.");
        }

        reversalPeriod.AssertCanPost(reversalDate);

        var reversalVoucher = new JournalVoucher(
            CompanyId,
            BranchId,
            reversalPeriod.Id,
            VoucherType.Reversal,
            reversalDate,
            supervisorUserId,
            narration: $"Reversal of {VoucherNumber}: {reason.Trim()}",
            referenceNumber: VoucherNumber,
            idempotencyKey: Guid.NewGuid(),
            sourceModule: "ACCOUNTING",
            sourceDocumentId: Id);

        // Mirror legs with inverted positions (Debit becomes Credit, Credit becomes Debit)
        var lineNumber = 1;
        foreach (var originalLine in _lines)
        {
            var invertedLine = originalLine.CreateInvertedLeg(reversalVoucher.Id, lineNumber++, reason);
            reversalVoucher._lines.Add(invertedLine);
        }

        reversalVoucher.Post(reversalPeriod, reversalVoucherNumber, supervisorUserId);

        // Mark current voucher as reversed
        IsReversed = true;
        ReversalVoucherId = reversalVoucher.Id;
        Status = VoucherStatus.Reversed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = supervisorUserId;

        RaiseDomainEvent(new VoucherReversedDomainEvent(
            Id,
            reversalVoucher.Id,
            reversalVoucher.VoucherNumber,
            supervisorUserId));

        return reversalVoucher;
    }

    private void AssertNotPostedOrReversed(string message)
    {
        if (Status == VoucherStatus.Posted || Status == VoucherStatus.Reversed)
        {
            throw new DomainException("DOMAIN_POSTED_VOUCHER_IMMUTABLE", message);
        }
    }
}

