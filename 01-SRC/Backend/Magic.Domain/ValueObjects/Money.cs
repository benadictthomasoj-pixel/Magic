namespace Magic.Domain.ValueObjects;

/// <summary>
/// Immutable 64-bit integer monetary value stored strictly in paise (1 INR = 100 paise).
/// Eliminates IEEE-754 binary floating-point rounding drift across all financial operations.
/// </summary>
public readonly record struct Money(long Paise) : IComparable<Money>
{
    public const long PaisePerRupee = 100L;

    public static Money Zero => new(0L);

    public bool IsZero => Paise == 0L;
    public bool IsPositive => Paise > 0L;
    public bool IsNegative => Paise < 0L;

    public static Money FromPaise(long paise) => new(paise);

    public static Money FromRupees(long wholeRupees) => new(checked(wholeRupees * PaisePerRupee));

    public Money Abs() => new(Paise == long.MinValue
        ? throw new OverflowException("Cannot negate Int64.MinValue paise.")
        : Math.Abs(Paise));

    public static Money operator +(Money left, Money right) => new(checked(left.Paise + right.Paise));

    public static Money operator -(Money left, Money right) => new(checked(left.Paise - right.Paise));

    public static Money operator -(Money value) => new(checked(-value.Paise));

    public static bool operator <(Money left, Money right) => left.Paise < right.Paise;

    public static bool operator >(Money left, Money right) => left.Paise > right.Paise;

    public static bool operator <=(Money left, Money right) => left.Paise <= right.Paise;

    public static bool operator >=(Money left, Money right) => left.Paise >= right.Paise;

    public int CompareTo(Money other) => Paise.CompareTo(other.Paise);

    public override string ToString()
    {
        var sign = Paise < 0 ? "-" : string.Empty;
        var abs = Paise == long.MinValue ? (ulong)long.MaxValue + 1UL : (ulong)Math.Abs(Paise);
        var rupees = abs / (ulong)PaisePerRupee;
        var remainder = abs % (ulong)PaisePerRupee;
        return $"{sign}{rupees}.{remainder:D2} INR";
    }
}

