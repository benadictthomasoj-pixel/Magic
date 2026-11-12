using System.Text.Json.Serialization;
using Magic.Domain.Common;

namespace Magic.Domain.ValueObjects;

/// <summary>
/// Value object implementing statutory invoice rounding pursuant to Section 170 of the CGST Act.
/// Mandates that final invoice amounts be rounded to the nearest whole rupee:
/// amounts with 50 paise or more are rounded up to the nearest rupee, and amounts less than 50 paise are rounded down.
/// Guarantees exact invariant: ExactTotal + RoundOffDelta == FinalPayable.
/// </summary>
public readonly record struct Section170Rounding
{
    public Money ExactTotal { get; }
    public Money RoundOffDelta { get; }
    public Money FinalPayable { get; }

    [JsonConstructor]
    public Section170Rounding(Money exactTotal, Money roundOffDelta, Money finalPayable)
    {
        ExactTotal = exactTotal;
        RoundOffDelta = roundOffDelta;
        FinalPayable = finalPayable;

        if (exactTotal + roundOffDelta != finalPayable)
        {
            throw new DomainException(
                "DOMAIN_SECTION_170_ROUNDING_INVALID",
                $"Exact total {exactTotal} plus delta {roundOffDelta} does not equal final payable {finalPayable}.");
        }

        if (finalPayable.Paise % 100 != 0)
        {
            throw new DomainException(
                "DOMAIN_SECTION_170_ROUNDING_INVALID",
                $"Final payable amount {finalPayable} must be rounded to a whole rupee (0 paise).");
        }
    }

    public static Section170Rounding Calculate(Money exactTotal)
    {
        var paise = exactTotal.Paise;
        long roundOffPaise;

        if (paise >= 0)
        {
            var remainder = paise % 100;
            if (remainder >= 50)
            {
                roundOffPaise = 100 - remainder;
            }
            else
            {
                roundOffPaise = -remainder;
            }
        }
        else
        {
            var remainder = paise % 100; // negative in C#
            if (remainder <= -50)
            {
                roundOffPaise = -(100 + remainder);
            }
            else
            {
                roundOffPaise = -remainder;
            }
        }

        var delta = Money.FromPaise(roundOffPaise);
        var finalPayable = Money.FromPaise(paise + roundOffPaise);

        return new Section170Rounding(exactTotal, delta, finalPayable);
    }
}

