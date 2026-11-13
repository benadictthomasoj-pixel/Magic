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

    // Security & Identity schema (Phase A)
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<RoleRecord> Roles => Set<RoleRecord>();
    public DbSet<PermissionRecord> Permissions => Set<PermissionRecord>();
    public DbSet<UserRoleRecord> UserRoles => Set<UserRoleRecord>();
    public DbSet<RolePermissionRecord> RolePermissions => Set<RolePermissionRecord>();
    public DbSet<UserBranchAssignmentRecord> UserBranchAssignments => Set<UserBranchAssignmentRecord>();
    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();
    public DbSet<TerminalRecord> Terminals => Set<TerminalRecord>();
    public DbSet<SecurityAuditEventRecord> SecurityAuditEvents => Set<SecurityAuditEventRecord>();

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

    // Commercial & Master Data schema (Phase B)
    public DbSet<DepartmentRecord> Departments => Set<DepartmentRecord>();
    public DbSet<CategoryRecord> Categories => Set<CategoryRecord>();
    public DbSet<BrandRecord> Brands => Set<BrandRecord>();
    public DbSet<UnitOfMeasureRecord> UnitsOfMeasure => Set<UnitOfMeasureRecord>();
    public DbSet<TaxCodeRecord> TaxCodes => Set<TaxCodeRecord>();
    public DbSet<ProductRecord> Products => Set<ProductRecord>();
    public DbSet<ProductVariantRecord> ProductVariants => Set<ProductVariantRecord>();
    public DbSet<ProductBarcodeRecord> ProductBarcodes => Set<ProductBarcodeRecord>();
    public DbSet<ProductPriceRecord> ProductPrices => Set<ProductPriceRecord>();
    public DbSet<ProductPriceHistoryRecord> ProductPriceHistories => Set<ProductPriceHistoryRecord>();
    public DbSet<CustomerGroupRecord> CustomerGroups => Set<CustomerGroupRecord>();
    public DbSet<CustomerRecord> Customers => Set<CustomerRecord>();
    public DbSet<CustomerAddressRecord> CustomerAddresses => Set<CustomerAddressRecord>();
    public DbSet<SupplierRecord> Suppliers => Set<SupplierRecord>();
    public DbSet<SupplierAddressRecord> SupplierAddresses => Set<SupplierAddressRecord>();
    public DbSet<SupplierBankAccountRecord> SupplierBankAccounts => Set<SupplierBankAccountRecord>();

    // Inventory & Procurement schema (Phase C)
    public DbSet<WarehouseRecord> Warehouses => Set<WarehouseRecord>();
    public DbSet<WarehouseBranchAssignmentRecord> WarehouseBranchAssignments => Set<WarehouseBranchAssignmentRecord>();
    public DbSet<StorageLocationRecord> StorageLocations => Set<StorageLocationRecord>();
    public DbSet<ProductStorageLocationRecord> ProductStorageLocations => Set<ProductStorageLocationRecord>();
    public DbSet<StockBalanceRecord> StockBalances => Set<StockBalanceRecord>();
    public DbSet<StockMovementRecord> StockMovements => Set<StockMovementRecord>();
    public DbSet<StockAdjustmentRecord> StockAdjustments => Set<StockAdjustmentRecord>();
    public DbSet<StockAdjustmentLineRecord> StockAdjustmentLines => Set<StockAdjustmentLineRecord>();
    public DbSet<StockCountRecord> StockCounts => Set<StockCountRecord>();
    public DbSet<StockCountLineRecord> StockCountLines => Set<StockCountLineRecord>();
    public DbSet<PurchaseOrderRecord> PurchaseOrders => Set<PurchaseOrderRecord>();
    public DbSet<PurchaseOrderLineRecord> PurchaseOrderLines => Set<PurchaseOrderLineRecord>();
    public DbSet<GoodsReceiptNoteRecord> GoodsReceiptNotes => Set<GoodsReceiptNoteRecord>();
    public DbSet<GoodsReceiptLineRecord> GoodsReceiptLines => Set<GoodsReceiptLineRecord>();
    public DbSet<PurchaseReturnRecord> PurchaseReturns => Set<PurchaseReturnRecord>();
    public DbSet<PurchaseReturnLineRecord> PurchaseReturnLines => Set<PurchaseReturnLineRecord>();
    public DbSet<StockTransferRecord> StockTransfers => Set<StockTransferRecord>();
    public DbSet<StockTransferLineRecord> StockTransferLines => Set<StockTransferLineRecord>();
    public DbSet<ReorderRuleRecord> ReorderRules => Set<ReorderRuleRecord>();

    // POS & Sales Billing schema (Phase D)
    public DbSet<SalesInvoiceRecord> SalesInvoices => Set<SalesInvoiceRecord>();
    public DbSet<SalesInvoiceLineRecord> SalesInvoiceLines => Set<SalesInvoiceLineRecord>();
    public DbSet<SalesPaymentRecord> SalesPayments => Set<SalesPaymentRecord>();
    public DbSet<SalesReturnRecord> SalesReturns => Set<SalesReturnRecord>();
    public DbSet<SalesReturnLineRecord> SalesReturnLines => Set<SalesReturnLineRecord>();
    public DbSet<CreditNoteRecord> CreditNotes => Set<CreditNoteRecord>();
    public DbSet<CashierSessionRecord> CashierSessions => Set<CashierSessionRecord>();
    public DbSet<CashMovementRecord> CashMovements => Set<CashMovementRecord>();

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

