using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magic.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<DepartmentRecord>
{
    public void Configure(EntityTypeBuilder<DepartmentRecord> builder)
    {
        builder.ToTable("departments", "commercial");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.HasIndex(d => new { d.CompanyId, d.Code }).IsUnique();
        builder.HasIndex(d => d.CompanyId);
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<CategoryRecord>
{
    public void Configure(EntityTypeBuilder<CategoryRecord> builder)
    {
        builder.ToTable("categories", "commercial");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.HsnCode).HasMaxLength(20);
        builder.Property(c => c.DefaultGstRate).HasColumnType("numeric(5,2)");
        builder.Property(c => c.ImageUrl).HasMaxLength(500);

        builder.HasIndex(c => new { c.CompanyId, c.Code }).IsUnique();
        builder.HasIndex(c => c.CompanyId);
        builder.HasIndex(c => c.ParentCategoryId);

        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Department)
            .WithMany(d => d.Categories)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class BrandConfiguration : IEntityTypeConfiguration<BrandRecord>
{
    public void Configure(EntityTypeBuilder<BrandRecord> builder)
    {
        builder.ToTable("brands", "commercial");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Code).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(1000);
        builder.Property(b => b.LogoUrl).HasMaxLength(500);
        builder.Property(b => b.WebsiteUrl).HasMaxLength(500);

        builder.HasIndex(b => new { b.CompanyId, b.Code }).IsUnique();
        builder.HasIndex(b => b.CompanyId);
    }
}

public sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasureRecord>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasureRecord> builder)
    {
        builder.ToTable("units_of_measure", "commercial");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Code).HasMaxLength(20).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(u => new { u.CompanyId, u.Code }).IsUnique();
        builder.HasIndex(u => u.CompanyId);
    }
}

public sealed class TaxCodeConfiguration : IEntityTypeConfiguration<TaxCodeRecord>
{
    public void Configure(EntityTypeBuilder<TaxCodeRecord> builder)
    {
        builder.ToTable("tax_codes", "commercial");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(200).IsRequired();
        builder.Property(t => t.HsnSacCode).HasMaxLength(20);
        builder.Property(t => t.RatePercentage).HasColumnType("numeric(5,2)");
        builder.Property(t => t.CgstPercentage).HasColumnType("numeric(5,2)");
        builder.Property(t => t.SgstPercentage).HasColumnType("numeric(5,2)");
        builder.Property(t => t.IgstPercentage).HasColumnType("numeric(5,2)");
        builder.Property(t => t.CessPercentage).HasColumnType("numeric(5,2)");

        builder.HasIndex(t => new { t.CompanyId, t.Code }).IsUnique();
        builder.HasIndex(t => t.CompanyId);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<ProductRecord>
{
    public void Configure(EntityTypeBuilder<ProductRecord> builder)
    {
        builder.ToTable("products", "commercial");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(250).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.HsnSacCode).HasMaxLength(20).IsRequired();
        builder.Property(p => p.GstRatePercentage).HasColumnType("numeric(5,2)");
        builder.Property(p => p.Sku).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PrimaryBarcode).HasMaxLength(100);
        builder.Property(p => p.PurchasePricePaise).HasColumnType("bigint");
        builder.Property(p => p.CostPricePaise).HasColumnType("bigint");
        builder.Property(p => p.SellingPricePaise).HasColumnType("bigint");
        builder.Property(p => p.MrpPaise).HasColumnType("bigint");
        builder.Property(p => p.MinSellingPricePaise).HasColumnType("bigint");
        builder.Property(p => p.WholesalePricePaise).HasColumnType("bigint");
        builder.Property(p => p.ReorderLevel).HasColumnType("numeric(12,4)");
        builder.Property(p => p.MinStockLevel).HasColumnType("numeric(12,4)");
        builder.Property(p => p.MaxStockLevel).HasColumnType("numeric(12,4)");
        builder.Property(p => p.ImageUrl).HasMaxLength(500);

        builder.HasIndex(p => new { p.CompanyId, p.ProductCode }).IsUnique();
        builder.HasIndex(p => new { p.CompanyId, p.Sku }).IsUnique();
        builder.HasIndex(p => new { p.CompanyId, p.PrimaryBarcode }).IsUnique().HasFilter("\"PrimaryBarcode\" IS NOT NULL");
        builder.HasIndex(p => new { p.CompanyId, p.Name });
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.BrandId);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Department)
            .WithMany(d => d.Products)
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.UnitOfMeasure)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.TaxCode)
            .WithMany(t => t.Products)
            .HasForeignKey(p => p.TaxCodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariantRecord>
{
    public void Configure(EntityTypeBuilder<ProductVariantRecord> builder)
    {
        builder.ToTable("product_variants", "commercial");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Sku).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Barcode).HasMaxLength(100);
        builder.Property(v => v.VariantName).HasMaxLength(200).IsRequired();
        builder.Property(v => v.AttributesJson).HasMaxLength(2000).IsRequired();
        builder.Property(v => v.SellingPricePaise).HasColumnType("bigint");
        builder.Property(v => v.MrpPaise).HasColumnType("bigint");
        builder.Property(v => v.PurchasePricePaise).HasColumnType("bigint");

        builder.HasIndex(v => v.Sku).IsUnique();
        builder.HasIndex(v => v.ProductId);

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcodeRecord>
{
    public void Configure(EntityTypeBuilder<ProductBarcodeRecord> builder)
    {
        builder.ToTable("product_barcodes", "commercial");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Barcode).HasMaxLength(100).IsRequired();
        builder.Property(b => b.BarcodeType).HasMaxLength(20).IsRequired();

        builder.HasIndex(b => new { b.CompanyId, b.Barcode }).IsUnique();
        builder.HasIndex(b => b.ProductId);

        builder.HasOne(b => b.Product)
            .WithMany(p => p.Barcodes)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Variant)
            .WithMany()
            .HasForeignKey(b => b.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPriceRecord>
{
    public void Configure(EntityTypeBuilder<ProductPriceRecord> builder)
    {
        builder.ToTable("product_prices", "commercial");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PriceTier).HasMaxLength(50).IsRequired();
        builder.Property(p => p.MinQuantity).HasColumnType("numeric(12,4)");
        builder.Property(p => p.UnitPricePaise).HasColumnType("bigint");
        builder.Property(p => p.DiscountPercentage).HasColumnType("numeric(5,2)");

        builder.HasIndex(p => p.ProductId);
        builder.HasIndex(p => new { p.CompanyId, p.ProductId, p.PriceTier });

        builder.HasOne(p => p.Product)
            .WithMany(p => p.PriceTiers)
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Variant)
            .WithMany()
            .HasForeignKey(p => p.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductPriceHistoryConfiguration : IEntityTypeConfiguration<ProductPriceHistoryRecord>
{
    public void Configure(EntityTypeBuilder<ProductPriceHistoryRecord> builder)
    {
        builder.ToTable("product_price_history", "commercial");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.PriceType).HasMaxLength(50).IsRequired();
        builder.Property(h => h.OldPricePaise).HasColumnType("bigint");
        builder.Property(h => h.NewPricePaise).HasColumnType("bigint");
        builder.Property(h => h.Reason).HasMaxLength(250);

        builder.HasIndex(h => h.ProductId);
        builder.HasIndex(h => new { h.CompanyId, h.ChangedAtUtc });

        builder.HasOne(h => h.Product)
            .WithMany(p => p.PriceHistories)
            .HasForeignKey(h => h.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroupRecord>
{
    public void Configure(EntityTypeBuilder<CustomerGroupRecord> builder)
    {
        builder.ToTable("customer_groups", "commercial");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Code).HasMaxLength(50).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(150).IsRequired();
        builder.Property(g => g.DefaultDiscountPercentage).HasColumnType("numeric(5,2)");
        builder.Property(g => g.DefaultCreditLimitPaise).HasColumnType("bigint");

        builder.HasIndex(g => new { g.CompanyId, g.Code }).IsUnique();
        builder.HasIndex(g => g.CompanyId);
    }
}

public sealed class CustomerConfiguration : IEntityTypeConfiguration<CustomerRecord>
{
    public void Configure(EntityTypeBuilder<CustomerRecord> builder)
    {
        builder.ToTable("customers", "commercial");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CustomerCode).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(250).IsRequired();
        builder.Property(c => c.Gstin).HasMaxLength(15);
        builder.Property(c => c.Pan).HasMaxLength(10);
        builder.Property(c => c.ContactPerson).HasMaxLength(150);
        builder.Property(c => c.Phone).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.CreditLimitPaise).HasColumnType("bigint");
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasIndex(c => new { c.CompanyId, c.CustomerCode }).IsUnique();
        builder.HasIndex(c => new { c.CompanyId, c.Gstin }).HasFilter("\"Gstin\" IS NOT NULL");
        builder.HasIndex(c => new { c.CompanyId, c.Phone });
        builder.HasIndex(c => new { c.CompanyId, c.Name });

        builder.HasOne(c => c.CustomerGroup)
            .WithMany(g => g.Customers)
            .HasForeignKey(c => c.CustomerGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddressRecord>
{
    public void Configure(EntityTypeBuilder<CustomerAddressRecord> builder)
    {
        builder.ToTable("customer_addresses", "commercial");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AddressLine1).HasMaxLength(250).IsRequired();
        builder.Property(a => a.AddressLine2).HasMaxLength(250);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.District).HasMaxLength(100);
        builder.Property(a => a.State).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StateCode).HasMaxLength(2).IsRequired();
        builder.Property(a => a.PinCode).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(50).IsRequired();

        builder.HasIndex(a => a.CustomerId);

        builder.HasOne(a => a.Customer)
            .WithMany(c => c.Addresses)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SupplierConfiguration : IEntityTypeConfiguration<SupplierRecord>
{
    public void Configure(EntityTypeBuilder<SupplierRecord> builder)
    {
        builder.ToTable("suppliers", "commercial");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SupplierCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(250).IsRequired();
        builder.Property(s => s.Category).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Gstin).HasMaxLength(15);
        builder.Property(s => s.Pan).HasMaxLength(10);
        builder.Property(s => s.ContactPerson).HasMaxLength(150);
        builder.Property(s => s.Phone).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.PaymentTerms).HasMaxLength(100).IsRequired();
        builder.Property(s => s.MsmeType).HasMaxLength(50);
        builder.Property(s => s.UdyamRegistrationNumber).HasMaxLength(50);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.HasIndex(s => new { s.CompanyId, s.SupplierCode }).IsUnique();
        builder.HasIndex(s => new { s.CompanyId, s.Gstin }).HasFilter("\"Gstin\" IS NOT NULL");
        builder.HasIndex(s => new { s.CompanyId, s.Name });
    }
}

public sealed class SupplierAddressConfiguration : IEntityTypeConfiguration<SupplierAddressRecord>
{
    public void Configure(EntityTypeBuilder<SupplierAddressRecord> builder)
    {
        builder.ToTable("supplier_addresses", "commercial");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AddressLine1).HasMaxLength(250).IsRequired();
        builder.Property(a => a.AddressLine2).HasMaxLength(250);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.State).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StateCode).HasMaxLength(2).IsRequired();
        builder.Property(a => a.PinCode).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(50).IsRequired();

        builder.HasIndex(a => a.SupplierId);

        builder.HasOne(a => a.Supplier)
            .WithMany(s => s.Addresses)
            .HasForeignKey(a => a.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SupplierBankAccountConfiguration : IEntityTypeConfiguration<SupplierBankAccountRecord>
{
    public void Configure(EntityTypeBuilder<SupplierBankAccountRecord> builder)
    {
        builder.ToTable("supplier_bank_accounts", "commercial");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BankName).HasMaxLength(150).IsRequired();
        builder.Property(b => b.BranchName).HasMaxLength(150).IsRequired();
        builder.Property(b => b.AccountNumberEncrypted).HasMaxLength(500).IsRequired();
        builder.Property(b => b.AccountNumberMasked).HasMaxLength(30).IsRequired();
        builder.Property(b => b.IfscCode).HasMaxLength(15).IsRequired();
        builder.Property(b => b.AccountType).HasMaxLength(30).IsRequired();

        builder.HasIndex(b => b.SupplierId);

        builder.HasOne(b => b.Supplier)
            .WithMany(s => s.BankAccounts)
            .HasForeignKey(b => b.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

