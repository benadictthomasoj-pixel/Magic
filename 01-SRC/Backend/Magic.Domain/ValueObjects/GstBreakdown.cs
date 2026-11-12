using System.Text.Json.Serialization;
using Magic.Domain.Common;

namespace Magic.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a complete Goods and Services Tax (GST) breakdown.
/// Enforces integer paise arithmetic with deterministic odd-cent split between CGST and SGST,
/// guaranteeing zero leakage invariant: Taxable + CGST + SGST + IGST == Gross.
/// </summary>
public readonly record struct GstBreakdown
{
    public Money TaxableAmount { get; }
    public GstRate Rate { get; }
    public bool IsInterState { get; }
    public Money CgstAmount { get; }
    public Money SgstAmount { get; }
    public Money IgstAmount { get; }
    public Money TotalTaxAmount { get; }
    public Money GrossAmount { get; }

    [JsonConstructor]
    public GstBreakdown(
        Money taxableAmount,
        GstRate rate,
        bool isInterState,
        Money cgstAmount,
        Money sgstAmount,
        Money igstAmount,
        Money totalTaxAmount,
        Money grossAmount)
    {
        TaxableAmount = taxableAmount;
        Rate = rate;
        IsInterState = isInterState;
        CgstAmount = cgstAmount;
        SgstAmount = sgstAmount;
        IgstAmount = igstAmount;
        TotalTaxAmount = totalTaxAmount;
        GrossAmount = grossAmount;

        // Invariant assertion: TotalTax must equal component sum
        if (totalTaxAmount != cgstAmount + sgstAmount + igstAmount)
        {
            throw new DomainException(
                "DOMAIN_GST_BREAKDOWN_INVALID",
                $"Total tax {totalTaxAmount} does not match components sum {cgstAmount + sgstAmount + igstAmount}.");
        }

        // Invariant assertion: Gross must equal Taxable + TotalTax
        if (grossAmount != taxableAmount + totalTaxAmount)
        {
            throw new DomainException(
                "DOMAIN_GST_BREAKDOWN_INVALID",
                $"Gross amount {grossAmount} does not equal Taxable {taxableAmount} + TotalTax {totalTaxAmount}.");
        }
    }

    /// <summary>
    /// Calculates tax forward from a known taxable base amount (Tax Exclusive).
    /// </summary>
    public static GstBreakdown CalculateExclusive(Money taxableAmount, GstRate rate, bool isInterState)
    {
        if (rate.BasisPoints == 0)
        {
            return new GstBreakdown(
                taxableAmount,
                rate,
                isInterState,
                Money.Zero,
                Money.Zero,
                Money.Zero,
                Money.Zero,
                taxableAmount);
        }

        // Standard integer midpoint rounding: (Paise * bps + 5000) / 10000
        var totalTaxPaise = (taxableAmount.Paise * rate.BasisPoints + 5000) / 10000;
        var totalTax = Money.FromPaise(totalTaxPaise);
        var gross = taxableAmount + totalTax;

        if (isInterState)
        {
            return new GstBreakdown(
                taxableAmount,
                rate,
                true,
                Money.Zero,
                Money.Zero,
                totalTax,
                totalTax,
                gross);
        }

        // Intra-state split: half to CGST, remainder to SGST to prevent 1-paise leakage on odd amounts
        var cgstPaise = totalTaxPaise / 2;
        var sgstPaise = totalTaxPaise - cgstPaise;

        return new GstBreakdown(
            taxableAmount,
            rate,
            false,
            Money.FromPaise(cgstPaise),
            Money.FromPaise(sgstPaise),
            Money.Zero,
            totalTax,
            gross);
    }

    /// <summary>
    /// Calculates taxable base and tax reverse from a known gross amount (Tax Inclusive).
    /// </summary>
    public static GstBreakdown CalculateInclusive(Money grossAmount, GstRate rate, bool isInterState)
    {
        if (rate.BasisPoints == 0)
        {
            return new GstBreakdown(
                grossAmount,
                rate,
                isInterState,
                Money.Zero,
                Money.Zero,
                Money.Zero,
                Money.Zero,
                grossAmount);
        }

        // Reverse calculation: Taxable = (Gross * 10000 + divisor / 2) / divisor where divisor = (10000 + bps)
        var divisor = 10000 + rate.BasisPoints;
        var taxablePaise = (grossAmount.Paise * 10000 + divisor / 2) / divisor;
        var taxable = Money.FromPaise(taxablePaise);
        var totalTax = grossAmount - taxable;
        var totalTaxPaise = totalTax.Paise;

        if (isInterState)
        {
            return new GstBreakdown(
                taxable,
                rate,
                true,
                Money.Zero,
                Money.Zero,
                totalTax,
                totalTax,
                grossAmount);
        }

        var cgstPaise = totalTaxPaise / 2;
        var sgstPaise = totalTaxPaise - cgstPaise;

        return new GstBreakdown(
            taxable,
            rate,
            false,
            Money.FromPaise(cgstPaise),
            Money.FromPaise(sgstPaise),
            Money.Zero,
            totalTax,
            grossAmount);
    }
}

