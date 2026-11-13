using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magic.Infrastructure.Persistence.Configurations;

// ==========================================
// 1. Warehouses & Storage Locations
// ==========================================

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<WarehouseRecord>
{
    public void Configure(EntityTypeBuilder<WarehouseRecord> builder)
    {
        builder.ToTable("warehouses", "inventory");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Code).HasMaxLength(50).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(1000);
        builder.Property(w => w.AddressLine1).HasMaxLength(250).IsRequired();
        builder.Property(w => w.AddressLine2).HasMaxLength(250);
        builder.Property(w => w.City).HasMaxLength(100).IsRequired();
        builder.Property(w => w.State).HasMaxLength(100).IsRequired();
        builder.Property(w => w.StateCode).HasMaxLength(10).IsRequired();
        builder.Property(w => w.PinCode).HasMaxLength(20).IsRequired();
        builder.Property(w => w.Country).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Phone).HasMaxLength(50);
        builder.Property(w => w.Email).HasMaxLength(256);
        builder.Property(w => w.ManagerName).HasMaxLength(200);
        builder.Property(w => w.CapacitySqFt).HasColumnType("numeric(12,2)");

        builder.HasIndex(w => new { w.CompanyId, w.Code }).IsUnique();
        builder.HasIndex(w => w.CompanyId);
    }
}

public sealed class WarehouseBranchAssignmentConfiguration : IEntityTypeConfiguration<WarehouseBranchAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<WarehouseBranchAssignmentRecord> builder)
    {
        builder.ToTable("warehouse_branch_assignments", "inventory");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.CompanyId, a.WarehouseId, a.BranchId }).IsUnique();
        builder.HasIndex(a => a.BranchId);

        builder.HasOne(a => a.Warehouse)
            .WithMany(w => w.BranchAssignments)
            .HasForeignKey(a => a.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocationRecord>
{
    public void Configure(EntityTypeBuilder<StorageLocationRecord> builder)
    {
        builder.ToTable("storage_locations", "inventory");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Code).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
        builder.Property(l => l.LocationType).HasMaxLength(30).IsRequired();
        builder.Property(l => l.Barcode).HasMaxLength(100);

        builder.HasIndex(l => new { l.WarehouseId, l.Code }).IsUnique();
        builder.HasIndex(l => l.CompanyId);

        builder.HasOne(l => l.Warehouse)
            .WithMany(w => w.StorageLocations)
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.ParentLocation)
            .WithMany(p => p.Children)
            .HasForeignKey(l => l.ParentLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductStorageLocationConfiguration : IEntityTypeConfiguration<ProductStorageLocationRecord>
{
    public void Configure(EntityTypeBuilder<ProductStorageLocationRecord> builder)
    {
        builder.ToTable("product_storage_locations", "inventory");
        builder.HasKey(psl => psl.Id);

        builder.HasIndex(psl => new { psl.CompanyId, psl.WarehouseId, psl.ProductId, psl.StorageLocationId }).IsUnique();

        builder.HasOne(psl => psl.Warehouse)
            .WithMany()
            .HasForeignKey(psl => psl.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(psl => psl.StorageLocation)
            .WithMany()
            .HasForeignKey(psl => psl.StorageLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(psl => psl.Product)
            .WithMany()
            .HasForeignKey(psl => psl.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(psl => psl.Variant)
            .WithMany()
            .HasForeignKey(psl => psl.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ==========================================
// 2. Stock Balances & Movements
// ==========================================

public sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalanceRecord>
{
    public void Configure(EntityTypeBuilder<StockBalanceRecord> builder)
    {
        builder.ToTable("stock_balances", "inventory");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BatchNumber).HasMaxLength(100);
        builder.Property(b => b.QuantityOnHand).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(b => b.QuantityReserved).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(b => b.QuantityAvailable).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(b => b.QuantityDamaged).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(b => b.QuantityExpired).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(b => b.QuantityBlocked).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(b => b.ConcurrencyVersion).IsConcurrencyToken();

        builder.HasIndex(b => new { b.CompanyId, b.WarehouseId, b.ProductId, b.VariantId, b.StorageLocationId, b.BatchNumber });
        builder.HasIndex(b => b.ProductId);
        builder.HasIndex(b => b.WarehouseId);

        builder.HasOne(b => b.Warehouse)
            .WithMany(w => w.StockBalances)
            .HasForeignKey(b => b.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Product)
            .WithMany()
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Variant)
            .WithMany()
            .HasForeignKey(b => b.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.StorageLocation)
            .WithMany()
            .HasForeignKey(b => b.StorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovementRecord>
{
    public void Configure(EntityTypeBuilder<StockMovementRecord> builder)
    {
        builder.ToTable("stock_movements", "inventory");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementType).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Direction).HasMaxLength(10).IsRequired();
        builder.Property(m => m.Quantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(m => m.BatchNumber).HasMaxLength(100);
        builder.Property(m => m.ReferenceType).HasMaxLength(50).IsRequired();
        builder.Property(m => m.ReferenceNumber).HasMaxLength(100);
        builder.Property(m => m.Reason).HasMaxLength(1000);
        builder.Property(m => m.BalanceAfterOnHand).HasColumnType("numeric(14,4)").IsRequired();

        builder.HasIndex(m => new { m.CompanyId, m.WarehouseId, m.ProductId, m.MovementDateUtc });
        builder.HasIndex(m => m.ReferenceId);

        builder.HasOne(m => m.Warehouse)
            .WithMany()
            .HasForeignKey(m => m.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Variant)
            .WithMany()
            .HasForeignKey(m => m.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.StorageLocation)
            .WithMany()
            .HasForeignKey(m => m.StorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 3. Stock Adjustments
// ==========================================

public sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustmentRecord>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentRecord> builder)
    {
        builder.ToTable("stock_adjustments", "inventory");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AdjustmentNumber).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.Status).HasMaxLength(30).IsRequired();
        builder.Property(a => a.RejectionReason).HasMaxLength(1000);

        builder.HasIndex(a => new { a.CompanyId, a.AdjustmentNumber }).IsUnique();
        builder.HasIndex(a => a.WarehouseId);

        builder.HasOne(a => a.Warehouse)
            .WithMany()
            .HasForeignKey(a => a.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockAdjustmentLineConfiguration : IEntityTypeConfiguration<StockAdjustmentLineRecord>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentLineRecord> builder)
    {
        builder.ToTable("stock_adjustment_lines", "inventory");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BatchNumber).HasMaxLength(100);
        builder.Property(l => l.SystemQuantityOnHand).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.AdjustmentQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.NewQuantityOnHand).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.LineReason).HasMaxLength(500);

        builder.HasOne(l => l.StockAdjustment)
            .WithMany(a => a.Lines)
            .HasForeignKey(l => l.StockAdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Variant)
            .WithMany()
            .HasForeignKey(l => l.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.StorageLocation)
            .WithMany()
            .HasForeignKey(l => l.StorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 4. Stock Counting
// ==========================================

public sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCountRecord>
{
    public void Configure(EntityTypeBuilder<StockCountRecord> builder)
    {
        builder.ToTable("stock_counts", "inventory");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CountNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Notes).HasMaxLength(1000);

        builder.HasIndex(c => new { c.CompanyId, c.CountNumber }).IsUnique();
        builder.HasIndex(c => c.WarehouseId);

        builder.HasOne(c => c.Warehouse)
            .WithMany()
            .HasForeignKey(c => c.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLineRecord>
{
    public void Configure(EntityTypeBuilder<StockCountLineRecord> builder)
    {
        builder.ToTable("stock_count_lines", "inventory");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.SystemQuantityOnHand).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.CountedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.DiscrepancyQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasOne(l => l.StockCount)
            .WithMany(c => c.Lines)
            .HasForeignKey(l => l.StockCountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Variant)
            .WithMany()
            .HasForeignKey(l => l.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.StorageLocation)
            .WithMany()
            .HasForeignKey(l => l.StorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 5. Purchase Orders
// ==========================================

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrderRecord>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderRecord> builder)
    {
        builder.ToTable("purchase_orders", "inventory");
        builder.HasKey(po => po.Id);

        builder.Property(po => po.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(po => po.Status).HasMaxLength(30).IsRequired();
        builder.Property(po => po.PaymentTerms).HasMaxLength(100).IsRequired();
        builder.Property(po => po.Notes).HasMaxLength(1000);
        builder.Property(po => po.CancellationReason).HasMaxLength(500);

        builder.HasIndex(po => new { po.CompanyId, po.OrderNumber }).IsUnique();
        builder.HasIndex(po => po.SupplierId);
        builder.HasIndex(po => po.WarehouseId);

        builder.HasOne(po => po.Warehouse)
            .WithMany()
            .HasForeignKey(po => po.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(po => po.Supplier)
            .WithMany()
            .HasForeignKey(po => po.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLineRecord>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLineRecord> builder)
    {
        builder.ToTable("purchase_order_lines", "inventory");
        builder.HasKey(pol => pol.Id);

        builder.Property(pol => pol.OrderedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(pol => pol.ReceivedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(pol => pol.CancelledQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(pol => pol.DiscountPercentage).HasColumnType("numeric(6,2)").IsRequired();
        builder.Property(pol => pol.GstRatePercentage).HasColumnType("numeric(5,2)").IsRequired();

        builder.HasOne(pol => pol.PurchaseOrder)
            .WithMany(po => po.Lines)
            .HasForeignKey(pol => pol.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pol => pol.Product)
            .WithMany()
            .HasForeignKey(pol => pol.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pol => pol.Variant)
            .WithMany()
            .HasForeignKey(pol => pol.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 6. Goods Receipt Notes (GRN)
// ==========================================

public sealed class GoodsReceiptNoteConfiguration : IEntityTypeConfiguration<GoodsReceiptNoteRecord>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptNoteRecord> builder)
    {
        builder.ToTable("goods_receipt_notes", "inventory");
        builder.HasKey(grn => grn.Id);

        builder.Property(grn => grn.GrnNumber).HasMaxLength(50).IsRequired();
        builder.Property(grn => grn.SupplierInvoiceNumber).HasMaxLength(100);
        builder.Property(grn => grn.Status).HasMaxLength(30).IsRequired();
        builder.Property(grn => grn.Notes).HasMaxLength(1000);

        builder.HasIndex(grn => new { grn.CompanyId, grn.GrnNumber }).IsUnique();
        builder.HasIndex(grn => grn.PurchaseOrderId);
        builder.HasIndex(grn => grn.SupplierId);

        builder.HasOne(grn => grn.Warehouse)
            .WithMany()
            .HasForeignKey(grn => grn.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(grn => grn.PurchaseOrder)
            .WithMany()
            .HasForeignKey(grn => grn.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(grn => grn.Supplier)
            .WithMany()
            .HasForeignKey(grn => grn.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLineRecord>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLineRecord> builder)
    {
        builder.ToTable("goods_receipt_lines", "inventory");
        builder.HasKey(grl => grl.Id);

        builder.Property(grl => grl.ReceivedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(grl => grl.AcceptedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(grl => grl.RejectedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(grl => grl.DamagedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(grl => grl.BatchNumber).HasMaxLength(100);
        builder.Property(grl => grl.RejectionReason).HasMaxLength(500);

        builder.HasOne(grl => grl.GoodsReceiptNote)
            .WithMany(grn => grn.Lines)
            .HasForeignKey(grl => grl.GoodsReceiptNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(grl => grl.PurchaseOrderLine)
            .WithMany()
            .HasForeignKey(grl => grl.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(grl => grl.Product)
            .WithMany()
            .HasForeignKey(grl => grl.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(grl => grl.Variant)
            .WithMany()
            .HasForeignKey(grl => grl.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(grl => grl.StorageLocation)
            .WithMany()
            .HasForeignKey(grl => grl.StorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 7. Purchase Returns
// ==========================================

public sealed class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturnRecord>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnRecord> builder)
    {
        builder.ToTable("purchase_returns", "inventory");
        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.ReturnNumber).HasMaxLength(50).IsRequired();
        builder.Property(pr => pr.SupplierCreditNoteNumber).HasMaxLength(100);
        builder.Property(pr => pr.Status).HasMaxLength(30).IsRequired();
        builder.Property(pr => pr.ReturnReason).HasMaxLength(500);
        builder.Property(pr => pr.Notes).HasMaxLength(1000);

        builder.HasIndex(pr => new { pr.CompanyId, pr.ReturnNumber }).IsUnique();
        builder.HasIndex(pr => pr.SupplierId);

        builder.HasOne(pr => pr.Warehouse)
            .WithMany()
            .HasForeignKey(pr => pr.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.GoodsReceiptNote)
            .WithMany()
            .HasForeignKey(pr => pr.GoodsReceiptNoteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pr => pr.Supplier)
            .WithMany()
            .HasForeignKey(pr => pr.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseReturnLineConfiguration : IEntityTypeConfiguration<PurchaseReturnLineRecord>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnLineRecord> builder)
    {
        builder.ToTable("purchase_return_lines", "inventory");
        builder.HasKey(prl => prl.Id);

        builder.Property(prl => prl.ReturnQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(prl => prl.Reason).HasMaxLength(500);

        builder.HasOne(prl => prl.PurchaseReturn)
            .WithMany(pr => pr.Lines)
            .HasForeignKey(prl => prl.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(prl => prl.GoodsReceiptLine)
            .WithMany()
            .HasForeignKey(prl => prl.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(prl => prl.Product)
            .WithMany()
            .HasForeignKey(prl => prl.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(prl => prl.Variant)
            .WithMany()
            .HasForeignKey(prl => prl.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(prl => prl.StorageLocation)
            .WithMany()
            .HasForeignKey(prl => prl.StorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 8. Stock Transfers
// ==========================================

public sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransferRecord>
{
    public void Configure(EntityTypeBuilder<StockTransferRecord> builder)
    {
        builder.ToTable("stock_transfers", "inventory");
        builder.HasKey(st => st.Id);

        builder.Property(st => st.TransferNumber).HasMaxLength(50).IsRequired();
        builder.Property(st => st.Status).HasMaxLength(30).IsRequired();
        builder.Property(st => st.TrackingNumber).HasMaxLength(100);
        builder.Property(st => st.CarrierName).HasMaxLength(100);
        builder.Property(st => st.Notes).HasMaxLength(1000);

        builder.HasIndex(st => new { st.CompanyId, st.TransferNumber }).IsUnique();
        builder.HasIndex(st => st.SourceWarehouseId);
        builder.HasIndex(st => st.DestinationWarehouseId);

        builder.HasOne(st => st.SourceWarehouse)
            .WithMany()
            .HasForeignKey(st => st.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(st => st.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(st => st.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLineRecord>
{
    public void Configure(EntityTypeBuilder<StockTransferLineRecord> builder)
    {
        builder.ToTable("stock_transfer_lines", "inventory");
        builder.HasKey(stl => stl.Id);

        builder.Property(stl => stl.RequestedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(stl => stl.DispatchedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(stl => stl.ReceivedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(stl => stl.DamagedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(stl => stl.MissingQuantity).HasColumnType("numeric(14,4)").IsRequired();

        builder.HasOne(stl => stl.StockTransfer)
            .WithMany(st => st.Lines)
            .HasForeignKey(stl => stl.StockTransferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(stl => stl.Product)
            .WithMany()
            .HasForeignKey(stl => stl.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(stl => stl.Variant)
            .WithMany()
            .HasForeignKey(stl => stl.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(stl => stl.SourceStorageLocation)
            .WithMany()
            .HasForeignKey(stl => stl.SourceStorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(stl => stl.DestinationStorageLocation)
            .WithMany()
            .HasForeignKey(stl => stl.DestinationStorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ==========================================
// 9. Reorder Management
// ==========================================

public sealed class ReorderRuleConfiguration : IEntityTypeConfiguration<ReorderRuleRecord>
{
    public void Configure(EntityTypeBuilder<ReorderRuleRecord> builder)
    {
        builder.ToTable("reorder_rules", "inventory");
        builder.HasKey(rr => rr.Id);

        builder.Property(rr => rr.MinStockLevel).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(rr => rr.MaxStockLevel).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(rr => rr.ReorderPoint).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(rr => rr.SafetyStock).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(rr => rr.ReorderQuantity).HasColumnType("numeric(14,4)").IsRequired();

        builder.HasIndex(rr => new { rr.CompanyId, rr.WarehouseId, rr.ProductId, rr.VariantId }).IsUnique();
        builder.HasIndex(rr => rr.ProductId);

        builder.HasOne(rr => rr.Warehouse)
            .WithMany()
            .HasForeignKey(rr => rr.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rr => rr.Product)
            .WithMany()
            .HasForeignKey(rr => rr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rr => rr.Variant)
            .WithMany()
            .HasForeignKey(rr => rr.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rr => rr.PreferredSupplier)
            .WithMany()
            .HasForeignKey(rr => rr.PreferredSupplierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
