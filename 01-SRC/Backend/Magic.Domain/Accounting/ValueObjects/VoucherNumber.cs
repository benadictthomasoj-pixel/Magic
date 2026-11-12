using System.Text.Json.Serialization;
using Magic.Domain.Common;

namespace Magic.Domain.Accounting.ValueObjects;

/// <summary>
/// Immutable value object representing a formal, sequential accounting voucher number.
/// Standard format pattern: VCH-{BranchCode}-{FYYear}-{TypePrefix}-{000001}
/// </summary>
public readonly record struct VoucherNumber : IComparable<VoucherNumber>
{
    public const int MaxLength = 64;

    public string Value { get; }

    [JsonConstructor]
    public VoucherNumber(string value)
    {
        DomainGuard.NotNullOrWhiteSpace(value, nameof(value));
        var trimmed = value.Trim().ToUpperInvariant();

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException(
                "DOMAIN_INVALID_VOUCHER_NUMBER",
                $"Voucher number length cannot exceed {MaxLength} characters. Given: {trimmed.Length}.");
        }

        Value = trimmed;
    }

    public static implicit operator string(VoucherNumber number) => number.Value;
    public static explicit operator VoucherNumber(string value) => new(value);

    public int CompareTo(VoucherNumber other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public static bool operator <(VoucherNumber left, VoucherNumber right) => left.CompareTo(right) < 0;
    public static bool operator <=(VoucherNumber left, VoucherNumber right) => left.CompareTo(right) <= 0;
    public static bool operator >(VoucherNumber left, VoucherNumber right) => left.CompareTo(right) > 0;
    public static bool operator >=(VoucherNumber left, VoucherNumber right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value;
}
