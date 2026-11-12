using System.Text.Json.Serialization;
using Magic.Domain.Common;

namespace Magic.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a physical or commercial quantity.
/// Enforces non-negative values and explicit decimal precision up to 4 decimal places.
/// </summary>
public readonly record struct Quantity : IComparable<Quantity>
{
    public const int MaxDecimalPlaces = 4;

    public decimal Value { get; }
    public string UnitOfMeasure { get; }

    [JsonConstructor]
    public Quantity(decimal value, string unitOfMeasure = "PCS")
    {
        if (value < 0)
        {
            throw new DomainException("DOMAIN_NEGATIVE_QUANTITY", "Quantity value must not be negative.");
        }

        DomainGuard.NotNullOrWhiteSpace(unitOfMeasure, nameof(unitOfMeasure));

        // Truncate/round to max 4 decimal places with bankers rounding
        Value = Math.Round(value, MaxDecimalPlaces, MidpointRounding.ToEven);
        UnitOfMeasure = unitOfMeasure.Trim().ToUpperInvariant();
    }

    public static Quantity Zero(string unitOfMeasure = "PCS") => new(0m, unitOfMeasure);

    public static Quantity FromDecimal(decimal value, string unitOfMeasure = "PCS") => new(value, unitOfMeasure);

    public static Quantity operator +(Quantity a, Quantity b)
    {
        AssertSameUnit(a, b);
        return new Quantity(a.Value + b.Value, a.UnitOfMeasure);
    }

    public static Quantity operator -(Quantity a, Quantity b)
    {
        AssertSameUnit(a, b);
        var result = a.Value - b.Value;
        if (result < 0)
        {
            throw new DomainException("DOMAIN_QUANTITY_UNDERFLOW", "Quantity cannot be decremented below zero.");
        }

        return new Quantity(result, a.UnitOfMeasure);
    }

    public static Quantity operator *(Quantity a, decimal scalar)
    {
        if (scalar < 0)
        {
            throw new DomainException("DOMAIN_NEGATIVE_SCALAR", "Cannot multiply quantity by a negative scalar.");
        }

        return new Quantity(a.Value * scalar, a.UnitOfMeasure);
    }

    public static Quantity operator /(Quantity a, decimal divisor)
    {
        if (divisor <= 0)
        {
            throw new DomainException("DOMAIN_INVALID_DIVISOR", "Divisor must be strictly greater than zero.");
        }

        return new Quantity(a.Value / divisor, a.UnitOfMeasure);
    }

    public static bool operator <(Quantity a, Quantity b)
    {
        AssertSameUnit(a, b);
        return a.Value < b.Value;
    }

    public static bool operator >(Quantity a, Quantity b)
    {
        AssertSameUnit(a, b);
        return a.Value > b.Value;
    }

    public static bool operator <=(Quantity a, Quantity b)
    {
        AssertSameUnit(a, b);
        return a.Value <= b.Value;
    }

    public static bool operator >=(Quantity a, Quantity b)
    {
        AssertSameUnit(a, b);
        return a.Value >= b.Value;
    }

    public int CompareTo(Quantity other)
    {
        AssertSameUnit(this, other);
        return Value.CompareTo(other.Value);
    }

    public override string ToString() => $"{Value:0.####} {UnitOfMeasure}";

    private static void AssertSameUnit(Quantity a, Quantity b)
    {
        if (!string.Equals(a.UnitOfMeasure, b.UnitOfMeasure, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(
                "DOMAIN_UNIT_MISMATCH",
                $"Cannot perform operations between quantities of different units: '{a.UnitOfMeasure}' vs '{b.UnitOfMeasure}'.");
        }
    }
}
