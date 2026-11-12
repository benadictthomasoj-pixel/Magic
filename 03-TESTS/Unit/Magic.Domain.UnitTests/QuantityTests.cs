using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Xunit;

namespace Magic.Domain.UnitTests;

public class QuantityTests
{
    [Fact]
    public void Constructor_NormalizesUnitAndRoundsToFourDecimals()
    {
        var qty = new Quantity(12.34567m, " kg ");

        Assert.Equal(12.3457m, qty.Value);
        Assert.Equal("KG", qty.UnitOfMeasure);
    }

    [Fact]
    public void Constructor_NegativeValue_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new Quantity(-0.001m, "PCS"));
        Assert.Equal("DOMAIN_NEGATIVE_QUANTITY", ex.ErrorCode);
    }

    [Fact]
    public void Addition_SameUnit_CalculatesCorrectly()
    {
        var a = new Quantity(10.5m, "LTR");
        var b = new Quantity(5.25m, "LTR");

        var sum = a + b;

        Assert.Equal(15.75m, sum.Value);
        Assert.Equal("LTR", sum.UnitOfMeasure);
    }

    [Fact]
    public void Subtraction_SameUnit_CalculatesCorrectly()
    {
        var a = new Quantity(10.5m, "MTR");
        var b = new Quantity(3.2m, "MTR");

        var diff = a - b;

        Assert.Equal(7.3m, diff.Value);
        Assert.Equal("MTR", diff.UnitOfMeasure);
    }

    [Fact]
    public void Arithmetic_MismatchedUnits_ThrowsDomainException()
    {
        var a = new Quantity(10m, "KG");
        var b = new Quantity(5m, "PCS");

        var ex = Assert.Throws<DomainException>(() => _ = a + b);
        Assert.Equal("DOMAIN_UNIT_MISMATCH", ex.ErrorCode);
    }

    [Fact]
    public void Comparison_ComparesValueCorrectly()
    {
        var a = new Quantity(5m, "BOX");
        var b = new Quantity(10m, "BOX");

        Assert.True(a < b);
        Assert.True(a <= b);
        Assert.True(b > a);
        Assert.True(b >= a);
    }
}

