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

        // Accounting Persistence & Posting Infrastructure
        services.AddScoped<Magic.Application.Accounting.Persistence.IVoucherNumberGenerator, Magic.Infrastructure.Persistence.Repositories.PostgresVoucherNumberGenerator>();
        services.AddScoped<Magic.Application.Accounting.Persistence.IChartOfAccountsRepository, Magic.Infrastructure.Persistence.Repositories.PostgresChartOfAccountsRepository>();
        services.AddScoped<Magic.Application.Accounting.Persistence.IFiscalGovernanceRepository, Magic.Infrastructure.Persistence.Repositories.PostgresFiscalGovernanceRepository>();
        services.AddScoped<Magic.Application.Accounting.Persistence.IGeneralLedgerRepository, Magic.Infrastructure.Persistence.Repositories.PostgresGeneralLedgerRepository>();
        services.AddScoped<Magic.Application.Accounting.Services.IPostingService, Magic.Infrastructure.Accounting.PostingService>();
        services.AddScoped<Magic.Application.Accounting.Services.IAccountingApplicationService, Magic.Infrastructure.Accounting.AccountingApplicationService>();

        // Phase A: Complete Identity, JWT & Security Services (OPEN-003)
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITokenService>(sp => sp.GetRequiredService<IJwtTokenService>());
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IAuthenticationApplicationService, AuthenticationApplicationService>();
        services.AddScoped<SecurityDatabaseSeeder>();

        // Phase B: Commercial Masters & Products
        services.AddSingleton<IBankDataEncryptionService, AesGcmBankDataEncryptionService>();
        services.AddScoped<Magic.Application.Commercial.Services.IProductCatalogService, Magic.Infrastructure.Commercial.ProductCatalogService>();
        services.AddScoped<Magic.Application.Commercial.Services.ICustomerPartyService, Magic.Infrastructure.Commercial.CustomerPartyService>();
        services.AddScoped<Magic.Application.Commercial.Services.ISupplierPartyService, Magic.Infrastructure.Commercial.SupplierPartyService>();
        services.AddScoped<CommercialDatabaseSeeder>();

        // Phase C: Inventory & Procurement
        services.AddScoped<Magic.Application.Inventory.Services.IWarehouseService, Magic.Infrastructure.Inventory.Services.WarehouseService>();
        services.AddScoped<Magic.Application.Inventory.Services.IInventoryLedgerService, Magic.Infrastructure.Inventory.Services.InventoryLedgerService>();
        services.AddScoped<Magic.Application.Inventory.Services.IStockAdjustmentService, Magic.Infrastructure.Inventory.Services.StockAdjustmentService>();
        services.AddScoped<Magic.Application.Inventory.Services.IStockCountService, Magic.Infrastructure.Inventory.Services.StockCountService>();
        services.AddScoped<Magic.Application.Inventory.Services.IPurchaseOrderService, Magic.Infrastructure.Inventory.Services.PurchaseOrderService>();
        services.AddScoped<Magic.Application.Inventory.Services.IGoodsReceiptService, Magic.Infrastructure.Inventory.Services.GoodsReceiptService>();
        services.AddScoped<Magic.Application.Inventory.Services.IPurchaseReturnService, Magic.Infrastructure.Inventory.Services.PurchaseReturnService>();
        services.AddScoped<Magic.Application.Inventory.Services.IStockTransferService, Magic.Infrastructure.Inventory.Services.StockTransferService>();
        services.AddScoped<Magic.Application.Inventory.Services.IReorderService, Magic.Infrastructure.Inventory.Services.ReorderService>();

        // Scoped request security/branch/terminal context
        services.AddScoped<MutableRequestContext>();
        services.AddScoped<IRequestContextSetter>(sp => sp.GetRequiredService<MutableRequestContext>());
        services.AddScoped<ICurrentUserContext>(sp => sp.GetRequiredService<MutableRequestContext>());
        services.AddScoped<IBranchContext>(sp => sp.GetRequiredService<MutableRequestContext>());
        services.AddScoped<ITerminalContext>(sp => sp.GetRequiredService<MutableRequestContext>());
        services.AddScoped<IPermissionEvaluator, FailClosedPermissionEvaluator>();
        services.AddSingleton<ISupervisorElevationService, FailClosedSupervisorElevationService>();

        // OPEN-004: Approved native .NET 10 BackgroundService outbox processor
        services.AddHostedService<OutboxProcessorBackgroundService>();

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);

        return services;
    }
}

