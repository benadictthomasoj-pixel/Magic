using System.Text.Json.Serialization;
using Magic.Domain.Common;

namespace Magic.Domain.Accounting.ValueObjects;

/// <summary>
/// Immutable value object representing a validated human-readable General Ledger account name.
/// </summary>
public readonly record struct AccountName : IComparable<AccountName>
{
    public const int MinLength = 2;
    public const int MaxLength = 120;

    public string Value { get; }

    [JsonConstructor]
    public AccountName(string value)
    {
        DomainGuard.NotNullOrWhiteSpace(value, nameof(value));
        var trimmed = value.Trim();

        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
        {
            throw new DomainException(
                "DOMAIN_INVALID_ACCOUNT_NAME",
                $"Account name length must be between {MinLength} and {MaxLength} characters. Given: {trimmed.Length}.");
        }

        Value = trimmed;
    }

    public static implicit operator string(AccountName name) => name.Value;
    public static explicit operator AccountName(string value) => new(value);

    public int CompareTo(AccountName other) => string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public static bool operator <(AccountName left, AccountName right) => left.CompareTo(right) < 0;
    public static bool operator <=(AccountName left, AccountName right) => left.CompareTo(right) <= 0;
    public static bool operator >(AccountName left, AccountName right) => left.CompareTo(right) > 0;
    public static bool operator >=(AccountName left, AccountName right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value;
}
