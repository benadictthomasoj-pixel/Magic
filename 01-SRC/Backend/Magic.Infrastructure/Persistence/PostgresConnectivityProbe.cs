using Magic.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Persistence;

/// <summary>
/// Executes lightweight PostgreSQL connectivity probes for API readiness checks.
/// </summary>
public sealed class PostgresConnectivityProbe(MagicDbContext dbContext) : IDatabaseConnectivityProbe
{
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception)
        {
            return false;
        }
    }
}

