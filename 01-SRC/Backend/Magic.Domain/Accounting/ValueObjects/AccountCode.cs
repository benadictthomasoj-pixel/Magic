using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Magic.Domain.Common;

namespace Magic.Domain.Accounting.ValueObjects;

/// <summary>
/// Immutable value object representing a standardized Chart of Accounts code (e.g. "1121", "2110", "4110").
/// </summary>
public readonly partial record struct AccountCode : IComparable<AccountCode>
{
    private static readonly Regex CodePattern = new(@"^[A-Z0-9\-_]{2,20}$", RegexOptions.Compiled);

    public string Value { get; }

    [JsonConstructor]
    public AccountCode(string value)
    {
        DomainGuard.NotNullOrWhiteSpace(value, nameof(value));
        var trimmed = value.Trim().ToUpperInvariant();

        if (!CodePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                "DOMAIN_INVALID_ACCOUNT_CODE",
                $"Account code '{trimmed}' is invalid. Codes must be 2 to 20 alphanumeric uppercase characters or dashes/underscores.");
        }

        Value = trimmed;
    }

    public static implicit operator string(AccountCode code) => code.Value;
    public static explicit operator AccountCode(string value) => new(value);

    public int CompareTo(AccountCode other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public static bool operator <(AccountCode left, AccountCode right) => left.CompareTo(right) < 0;
    public static bool operator <=(AccountCode left, AccountCode right) => left.CompareTo(right) <= 0;
    public static bool operator >(AccountCode left, AccountCode right) => left.CompareTo(right) > 0;
    public static bool operator >=(AccountCode left, AccountCode right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value;
}
