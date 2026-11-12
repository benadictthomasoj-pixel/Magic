namespace Magic.Domain.Common;

/// <summary>
/// Shared invariant validation helpers preventing duplicate guard logic across entities and value objects.
/// </summary>
public static class DomainGuard
{
    public static Guid NotEmpty(Guid value, string paramName, string errorCode = "DOMAIN_EMPTY_GUID")
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(errorCode, $"Identifier '{paramName}' must not be empty.");
        }

        return value;
    }

    public static string NotNullOrWhiteSpace(string? value, string paramName, string errorCode = "DOMAIN_REQUIRED_TEXT")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(errorCode, $"Value '{paramName}' is required.");
        }

        return value.Trim();
    }

    public static long NonNegative(long value, string paramName, string errorCode = "DOMAIN_NEGATIVE_VALUE")
    {
        if (value < 0)
        {
            throw new DomainException(errorCode, $"Value '{paramName}' must not be negative.");
        }

        return value;
    }
}

