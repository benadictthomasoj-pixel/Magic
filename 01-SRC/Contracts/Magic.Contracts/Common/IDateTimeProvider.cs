namespace Magic.Contracts.Common;

/// <summary>
/// Authoritative UTC clock abstraction for deterministic domain and test behavior.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

