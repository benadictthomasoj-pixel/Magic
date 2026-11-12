using Magic.Application.Abstractions;
using Magic.Contracts.Common;
using Magic.Contracts.Diagnostics;
using Magic.Contracts.Security;

namespace Magic.Application.Diagnostics;

/// <summary>
/// Application use case computing current API, database, and deferred security boundary status.
/// </summary>
public interface ISystemDiagnosticsService
{
    Task<SystemHealthStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}

public sealed class SystemDiagnosticsService(
    IDatabaseConnectivityProbe databaseProbe,
    IDateTimeProvider dateTimeProvider,
    IRuntimeEnvironmentInfo runtimeInfo,
    ITokenService tokenService) : ISystemDiagnosticsService
{
    public async Task<SystemHealthStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var dbConnected = await databaseProbe.CanConnectAsync(cancellationToken);

        var components = new List<ComponentHealthDto>
        {
            new("BackendHost", "Healthy", $"{runtimeInfo.ServiceName} v{runtimeInfo.Version}"),
            new("PostgreSqlDatabase", dbConnected ? "Healthy" : "Unhealthy", dbConnected ? "Connected" : "Database unreachable"),
            new("AuthenticationBoundary", tokenService.IsConfigured ? "Configured" : "Deferred_OPEN_003", "Interfaces active; token engine pending OPEN-003"),
            new("OfflineBuffering", "Disabled", "Strict Online LAN Policy (OPEN-001 Approved)")
        };

        return new SystemHealthStatusDto
        {
            Status = dbConnected ? "Healthy" : "Degraded",
            ServiceName = runtimeInfo.ServiceName,
            Version = runtimeInfo.Version,
            Environment = runtimeInfo.EnvironmentName,
            DeploymentMode = runtimeInfo.DeploymentMode,
            DatabaseConnected = dbConnected,
            NetworkPolicy = runtimeInfo.NetworkPolicy,
            AuthenticationStatus = tokenService.IsConfigured ? "Configured" : "Deferred (OPEN-003)",
            TimestampUtc = dateTimeProvider.UtcNow,
            Components = components
        };
    }
}

