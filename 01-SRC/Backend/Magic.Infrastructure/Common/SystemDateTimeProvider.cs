using Magic.Contracts.Common;

namespace Magic.Infrastructure.Common;

/// <summary>
/// Production UTC clock implementation.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

