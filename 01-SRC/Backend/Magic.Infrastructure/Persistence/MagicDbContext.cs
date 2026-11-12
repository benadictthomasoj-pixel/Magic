using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence.Converters;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Persistence;

/// <summary>
/// Authoritative PostgreSQL Entity Framework Core 10 context for MAGIC.
/// Phase 1 maps strictly platform and audit foundation schemas; business module schemas remain deferred.
/// </summary>
public sealed class MagicDbContext(DbContextOptions<MagicDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessageRecord> OutboxMessages => Set<OutboxMessageRecord>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<SystemAuditLogRecord> SystemAuditLogs => Set<SystemAuditLogRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<Money>().HaveConversion<MoneyConverter>();
        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MagicDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

