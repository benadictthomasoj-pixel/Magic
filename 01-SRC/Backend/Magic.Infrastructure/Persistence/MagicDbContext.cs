using Magic.Domain.Accounting.Entities;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence.Converters;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Persistence;

/// <summary>
/// Authoritative PostgreSQL Entity Framework Core 10 context for MAGIC.
/// Maps platform, audit, and accounting schemas with strict relational and domain invariants.
/// </summary>
public sealed class MagicDbContext(DbContextOptions<MagicDbContext> options) : DbContext(options)
{
    // Platform & Audit schemas
    public DbSet<OutboxMessageRecord> OutboxMessages => Set<OutboxMessageRecord>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<SystemAuditLogRecord> SystemAuditLogs => Set<SystemAuditLogRecord>();

    // Accounting foundation schema
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<AccountGroup> AccountGroups => Set<AccountGroup>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<VoucherSequenceRecord> VoucherSequences => Set<VoucherSequenceRecord>();
    public DbSet<JournalVoucher> Vouchers => Set<JournalVoucher>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<ArSubledgerItemRecord> ArSubledgerItems => Set<ArSubledgerItemRecord>();
    public DbSet<ArAllocationRecord> ArAllocations => Set<ArAllocationRecord>();
    public DbSet<ApSubledgerItemRecord> ApSubledgerItems => Set<ApSubledgerItemRecord>();
    public DbSet<ApAllocationRecord> ApAllocations => Set<ApAllocationRecord>();
    public DbSet<AccountPeriodBalanceRecord> AccountPeriodBalances => Set<AccountPeriodBalanceRecord>();

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

