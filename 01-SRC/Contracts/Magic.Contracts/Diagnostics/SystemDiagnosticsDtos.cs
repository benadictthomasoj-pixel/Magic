namespace Magic.Contracts.Diagnostics;

/// <summary>
/// Public health and platform diagnostic summary exposed by v1 system endpoints.
/// </summary>
public sealed record SystemHealthStatusDto
{
    public required string Status { get; init; }
    public required string ServiceName { get; init; }
    public required string Version { get; init; }
    public required string Environment { get; init; }
    public required string DeploymentMode { get; init; }
    public required bool DatabaseConnected { get; init; }
    public required string NetworkPolicy { get; init; }
    public required string AuthenticationStatus { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public IReadOnlyList<ComponentHealthDto> Components { get; init; } = [];
}

/// <summary>
/// Individual dependency health indicator.
/// </summary>
public sealed record ComponentHealthDto(string Name, string Status, string? Detail = null);

