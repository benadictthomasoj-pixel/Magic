using Magic.Application.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Magic.Infrastructure.Health;

/// <summary>
/// Readiness health check verifying PostgreSQL availability without exposing connection secrets.
/// </summary>
public sealed class PostgresHealthCheck(IDatabaseConnectivityProbe connectivityProbe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await connectivityProbe.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("PostgreSQL database connection succeeded.")
            : HealthCheckResult.Unhealthy("PostgreSQL database connection failed.");
    }
}

