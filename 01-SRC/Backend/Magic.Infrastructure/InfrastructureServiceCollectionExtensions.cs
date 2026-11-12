using Magic.Application.Abstractions;
using Magic.Contracts.Audit;
using Magic.Contracts.Common;
using Magic.Contracts.Events;
using Magic.Contracts.Security;
using Magic.Infrastructure.Audit;
using Magic.Infrastructure.Common;
using Magic.Infrastructure.Health;
using Magic.Infrastructure.Outbox;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Magic.Infrastructure;

/// <summary>
/// Registers PostgreSQL EF Core 10 persistence, outbox worker, audit logging, and security boundaries.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddMagicInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("MagicDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'ConnectionStrings:MagicDatabase' must be configured via environment variables, user secrets, or local configuration.");
        }

        services.AddDbContext<MagicDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(MagicDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "platform");
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            });
        });

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IUnitOfWork, PostgresUnitOfWork>();
        services.AddScoped<IDatabaseConnectivityProbe, PostgresConnectivityProbe>();
        services.AddScoped<IAuditLogger, PostgresAuditLogger>();
        services.AddScoped<IOutboxEventDispatcher, OutboxEventDispatcher>();

        // Scoped request security/branch/terminal context & deferred OPEN-003 boundaries
        services.AddScoped<MutableRequestContext>();
        services.AddScoped<IRequestContextSetter>(sp => sp.GetRequiredService<MutableRequestContext>());
        services.AddScoped<ICurrentUserContext>(sp => sp.GetRequiredService<MutableRequestContext>());
        services.AddScoped<IBranchContext>(sp => sp.GetRequiredService<MutableRequestContext>());
        services.AddScoped<ITerminalContext>(sp => sp.GetRequiredService<MutableRequestContext>());
        services.AddScoped<IPermissionEvaluator, FailClosedPermissionEvaluator>();
        services.AddSingleton<ITokenService, DeferredTokenService>();
        services.AddSingleton<ISupervisorElevationService, FailClosedSupervisorElevationService>();

        // OPEN-004: Approved native .NET 10 BackgroundService outbox processor
        services.AddHostedService<OutboxProcessorBackgroundService>();

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);

        return services;
    }
}

