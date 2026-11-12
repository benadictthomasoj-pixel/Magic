using Magic.Domain.Common;
using Magic.Domain.ValueObjects;

namespace Magic.Domain.Accounting.Entities;

/// <summary>
/// Domain entity representing an individual debit or credit leg within a double-entry journal voucher.
/// Enforces strictly one-sided invariant: exactly one of Debit or Credit must be positive; the other zero.
/// </summary>
public class JournalLine : DomainEntity
{
    public Guid VoucherId { get; private set; }
    public Guid AccountId { get; private set; }
    public Money Debit { get; private set; }
    public Money Credit { get; private set; }
    public int LineNumber { get; private set; }
    public string Narration { get; private set; } = string.Empty;
    public Guid? CostCenterId { get; private set; }
    public Guid? PartyId { get; private set; }

    private JournalLine() { } // EF Core

    public JournalLine(
        Guid voucherId,
        Guid accountId,
        Money debit,
        Money credit,
        int lineNumber,
        string narration = "",
        Guid? costCenterId = null,
        Guid? partyId = null)
    {
        if (voucherId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_VOUCHER_ID", "Voucher identity is required.");
        if (accountId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_ACCOUNT_ID", "Account identity is required.");
        if (lineNumber < 1) throw new DomainException("DOMAIN_INVALID_LINE_NUMBER", "Line number must be positive.");

        // Strictly one-sided invariant
        var isDebitValid = debit.Paise > 0 && credit.Paise == 0;
        var isCreditValid = credit.Paise > 0 && debit.Paise == 0;

        if (!isDebitValid && !isCreditValid)
        {
            throw new DomainException(
                "DOMAIN_INVALID_JOURNAL_LINE_AMOUNTS",
                $"A journal line must be strictly one-sided: either Debit > 0 (given: {debit}) or Credit > 0 (given: {credit}), with the opposite side strictly zero.");
        }

        VoucherId = voucherId;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        LineNumber = lineNumber;
        Narration = narration.Trim();
        CostCenterId = costCenterId;
        PartyId = partyId;
    }

    /// <summary>
    /// Creates an inverted companion line for non-destructive reversal vouchers (Debit becomes Credit, Credit becomes Debit).
    /// </summary>
    public JournalLine CreateInvertedLeg(Guid reversalVoucherId, int newLineNumber, string reversalReason)
    {
        var lineNarration = string.IsNullOrWhiteSpace(reversalReason)
            ? $"Reversal of line {LineNumber}"
            : $"Reversal of line {LineNumber}: {reversalReason.Trim()}";

        return new JournalLine(
            reversalVoucherId,
            AccountId,
            debit: Credit, // Swapped
            credit: Debit, // Swapped
            lineNumber: newLineNumber,
            narration: lineNarration,
            costCenterId: CostCenterId,
            partyId: PartyId);
    }
}

