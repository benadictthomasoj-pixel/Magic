using System.Text.Json.Serialization;
using Magic.Domain.Common;

namespace Magic.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a Goods and Services Tax (GST) rate.
/// Encapsulated in basis points (1% = 100 bps) to eliminate all floating-point rounding errors.
/// Example: 18% = 1800 basis points; 0.25% = 25 basis points.
/// </summary>
public readonly record struct GstRate : IComparable<GstRate>
{
    public const long MaxBasisPoints = 10_000; // 100%

    public long BasisPoints { get; }

    public decimal Percent => (decimal)BasisPoints / 100m;

    [JsonConstructor]
    public GstRate(long basisPoints)
    {
        if (basisPoints < 0 || basisPoints > MaxBasisPoints)
        {
            throw new DomainException(
                "DOMAIN_INVALID_GST_RATE",
                $"GST rate basis points must be between 0 and {MaxBasisPoints}. Given: {basisPoints}.");
        }

        BasisPoints = basisPoints;
    }

    public static GstRate Zero => new(0);
    public static GstRate Rate0 => new(0);
    public static GstRate Rate5 => new(500);
    public static GstRate Rate12 => new(1200);
    public static GstRate Rate18 => new(1800);
    public static GstRate Rate28 => new(2800);

    public static GstRate FromBasisPoints(long basisPoints) => new(basisPoints);

    public static GstRate FromPercent(decimal percent)
    {
        if (percent < 0m || percent > 100m)
        {
            throw new DomainException(
                "DOMAIN_INVALID_GST_RATE",
                $"GST percentage must be between 0 and 100. Given: {percent}.");
        }

        var bps = (long)Math.Round(percent * 100m, MidpointRounding.AwayFromZero);
        return new GstRate(bps);
    }

    public int CompareTo(GstRate other) => BasisPoints.CompareTo(other.BasisPoints);

    public static bool operator <(GstRate left, GstRate right) => left.BasisPoints < right.BasisPoints;
    public static bool operator <=(GstRate left, GstRate right) => left.BasisPoints <= right.BasisPoints;
    public static bool operator >(GstRate left, GstRate right) => left.BasisPoints > right.BasisPoints;
    public static bool operator >=(GstRate left, GstRate right) => left.BasisPoints >= right.BasisPoints;

    public override string ToString() => $"{Percent:0.##}%";
}

