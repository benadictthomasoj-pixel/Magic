using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Xunit;

namespace Magic.Domain.UnitTests;

public class GstMathTests
{
    [Fact]
    public void GstRate_PercentAndBasisPoints_ConvertAccurately()
    {
        var rate18 = GstRate.FromPercent(18m);
        Assert.Equal(1800, rate18.BasisPoints);
        Assert.Equal(18m, rate18.Percent);
        Assert.Equal("18%", rate18.ToString());

        var ratePoint25 = GstRate.FromBasisPoints(25);
        Assert.Equal(0.25m, ratePoint25.Percent);
        Assert.Equal("0.25%", ratePoint25.ToString());
    }

    [Fact]
    public void GstRate_InvalidPercent_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => GstRate.FromPercent(105m));
        Assert.Equal("DOMAIN_INVALID_GST_RATE", ex.ErrorCode);
    }

    [Fact]
    public void CalculateExclusive_IntraState_SplitsOddPaiseDeterministicallyWithoutLeakage()
    {
        // 103 paise at 18% = round(103 * 1800 / 10000) = round(18.54) = 19 paise tax (odd paise)
        var taxable = Money.FromPaise(103);
        var rate = GstRate.Rate18;

        var breakdown = GstBreakdown.CalculateExclusive(taxable, rate, isInterState: false);

        Assert.Equal(taxable, breakdown.TaxableAmount);
        Assert.Equal(Money.FromPaise(19), breakdown.TotalTaxAmount);
        Assert.Equal(Money.FromPaise(9), breakdown.CgstAmount); // 19 / 2 = 9
        Assert.Equal(Money.FromPaise(10), breakdown.SgstAmount); // 19 - 9 = 10
        Assert.Equal(Money.Zero, breakdown.IgstAmount);
        Assert.Equal(Money.FromPaise(122), breakdown.GrossAmount);

        // Zero leakage verification
        Assert.Equal(breakdown.GrossAmount, breakdown.TaxableAmount + breakdown.CgstAmount + breakdown.SgstAmount);
    }

    [Fact]
    public void CalculateExclusive_InterState_AssignsTotalTaxToIgst()
    {
        var taxable = Money.FromPaise(10_000); // Rs 100.00
        var rate = GstRate.Rate18;

        var breakdown = GstBreakdown.CalculateExclusive(taxable, rate, isInterState: true);

        Assert.Equal(Money.FromPaise(1800), breakdown.IgstAmount);
        Assert.Equal(Money.Zero, breakdown.CgstAmount);
        Assert.Equal(Money.Zero, breakdown.SgstAmount);
        Assert.Equal(Money.FromPaise(11_800), breakdown.GrossAmount);
        Assert.Equal(breakdown.GrossAmount, breakdown.TaxableAmount + breakdown.IgstAmount);
    }

    [Fact]
    public void CalculateInclusive_ReverseCalculatesTaxableAndTaxExactly()
    {
        // Gross of Rs 118.00 at 18% should reverse exactly to Rs 100.00 taxable and Rs 18.00 tax
        var gross = Money.FromPaise(11_800);
        var rate = GstRate.Rate18;

        var breakdown = GstBreakdown.CalculateInclusive(gross, rate, isInterState: false);

        Assert.Equal(Money.FromPaise(10_000), breakdown.TaxableAmount);
        Assert.Equal(Money.FromPaise(1800), breakdown.TotalTaxAmount);
        Assert.Equal(Money.FromPaise(900), breakdown.CgstAmount);
        Assert.Equal(Money.FromPaise(900), breakdown.SgstAmount);
        Assert.Equal(gross, breakdown.TaxableAmount + breakdown.CgstAmount + breakdown.SgstAmount);
    }

    [Theory]
    [InlineData(10049, -49, 10000)] // 100.49 -> 100.00 (-49 paise)
    [InlineData(10050, 50, 10100)]  // 100.50 -> 101.00 (+50 paise)
    [InlineData(10000, 0, 10000)]   // 100.00 -> 100.00 (0 paise)
    [InlineData(10075, 25, 10100)]  // 100.75 -> 101.00 (+25 paise)
    [InlineData(10025, -25, 10000)] // 100.25 -> 100.00 (-25 paise)
    public void Section170Rounding_RoundsToNearestWholeRupeePerStatute(long exactPaise, long expectedDeltaPaise, long expectedFinalPaise)
    {
        var exact = Money.FromPaise(exactPaise);
        var rounding = Section170Rounding.Calculate(exact);

        Assert.Equal(Money.FromPaise(expectedDeltaPaise), rounding.RoundOffDelta);
        Assert.Equal(Money.FromPaise(expectedFinalPaise), rounding.FinalPayable);
        Assert.Equal(rounding.FinalPayable, rounding.ExactTotal + rounding.RoundOffDelta);
        Assert.Equal(0, rounding.FinalPayable.Paise % 100);
    }
}

