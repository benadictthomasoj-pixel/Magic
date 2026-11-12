namespace Magic.Application.Abstractions;

/// <summary>
/// Controls PostgreSQL transaction and persistence boundaries without exposing EF Core types to Application/Domain.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Probes relational persistence availability for readiness and terminal startup checks.
/// </summary>
public interface IDatabaseConnectivityProbe
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides runtime deployment metadata resolved from Platform configuration.
/// </summary>
public interface IRuntimeEnvironmentInfo
{
    string ServiceName { get; }
    string Version { get; }
    string EnvironmentName { get; }
    string DeploymentMode { get; }
    string NetworkPolicy { get; }
}

