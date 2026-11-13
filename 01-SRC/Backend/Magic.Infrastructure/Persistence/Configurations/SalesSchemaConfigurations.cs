using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magic.Infrastructure.Persistence.Configurations;

public sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoiceRecord>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceRecord> builder)
    {
        builder.ToTable("sales_invoices", "sales");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Status).HasMaxLength(30).IsRequired();
        builder.Property(i => i.PlaceOfSupplyStateCode).HasMaxLength(10).IsRequired();
        builder.Property(i => i.CustomerGstin).HasMaxLength(15);
        builder.Property(i => i.SellerGstin).HasMaxLength(15).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.Property(i => i.ConcurrencyVersion).IsConcurrencyToken();

        builder.HasIndex(i => new { i.CompanyId, i.InvoiceNumber }).IsUnique();
        builder.HasIndex(i => new { i.CompanyId, i.BranchId });
        builder.HasIndex(i => i.InvoiceDateUtc);
        builder.HasIndex(i => i.CustomerId);
        builder.HasIndex(i => i.CashierSessionId);

        builder.HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.CashierSession)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.CashierSessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SalesInvoiceLineConfiguration : IEntityTypeConfiguration<SalesInvoiceLineRecord>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceLineRecord> builder)
    {
        builder.ToTable("sales_invoice_lines", "sales");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.HsnCode).HasMaxLength(20);
        builder.Property(l => l.Quantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.DiscountPercentage).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(l => l.GstRatePercentage).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(l => l.CgstPercentage).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(l => l.SgstPercentage).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(l => l.IgstPercentage).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(l => l.BatchNumber).HasMaxLength(50);
        builder.Property(l => l.SerialNumber).HasMaxLength(100);

        builder.HasIndex(l => l.InvoiceId);
        builder.HasIndex(l => l.ProductId);

        builder.HasOne(l => l.Invoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SalesPaymentConfiguration : IEntityTypeConfiguration<SalesPaymentRecord>
{
    public void Configure(EntityTypeBuilder<SalesPaymentRecord> builder)
    {
        builder.ToTable("sales_payments", "sales");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PaymentMethod).HasMaxLength(30).IsRequired();
        builder.Property(p => p.ReferenceNumber).HasMaxLength(100);
        builder.Property(p => p.Status).HasMaxLength(20).IsRequired();

        builder.HasIndex(p => new { p.CompanyId, p.BranchId });
        builder.HasIndex(p => p.InvoiceId);
        builder.HasIndex(p => p.CashierSessionId);
        builder.HasIndex(p => p.PaymentDateUtc);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CashierSession)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.CashierSessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturnRecord>
{
    public void Configure(EntityTypeBuilder<SalesReturnRecord> builder)
    {
        builder.ToTable("sales_returns", "sales");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ReturnNumber).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(30).IsRequired();
        builder.Property(r => r.ReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.CreditNoteNumber).HasMaxLength(50);
        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.HasIndex(r => new { r.CompanyId, r.ReturnNumber }).IsUnique();
        builder.HasIndex(r => new { r.CompanyId, r.BranchId });
        builder.HasIndex(r => r.InvoiceId);

        builder.HasOne(r => r.Invoice)
            .WithMany(i => i.Returns)
            .HasForeignKey(r => r.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.CashierSession)
            .WithMany()
            .HasForeignKey(r => r.CashierSessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLineRecord>
{
    public void Configure(EntityTypeBuilder<SalesReturnLineRecord> builder)
    {
        builder.ToTable("sales_return_lines", "sales");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.ReturnQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.GstRatePercentage).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(l => l.Reason).HasMaxLength(250).IsRequired();

        builder.HasIndex(l => l.ReturnId);
        builder.HasIndex(l => l.InvoiceLineId);
        builder.HasIndex(l => l.ProductId);

        builder.HasOne(l => l.Return)
            .WithMany(r => r.Lines)
            .HasForeignKey(l => l.ReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.InvoiceLine)
            .WithMany()
            .HasForeignKey(l => l.InvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNoteRecord>
{
    public void Configure(EntityTypeBuilder<CreditNoteRecord> builder)
    {
        builder.ToTable("credit_notes", "sales");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CreditNoteNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Status).HasMaxLength(20).IsRequired();

        builder.HasIndex(c => new { c.CompanyId, c.CreditNoteNumber }).IsUnique();
        builder.HasIndex(c => new { c.CompanyId, c.BranchId });
        builder.HasIndex(c => c.SalesReturnId);

        builder.HasOne(c => c.SalesReturn)
            .WithMany()
            .HasForeignKey(c => c.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class CashierSessionConfiguration : IEntityTypeConfiguration<CashierSessionRecord>
{
    public void Configure(EntityTypeBuilder<CashierSessionRecord> builder)
    {
        builder.ToTable("cashier_sessions", "cashier");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.CashierUsername).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Status).HasMaxLength(20).IsRequired();
        builder.Property(s => s.ClosingNotes).HasMaxLength(500);

        builder.HasIndex(s => new { s.CompanyId, s.BranchId });
        builder.HasIndex(s => s.CashierUserId);
        builder.HasIndex(s => s.Status);
    }
}

public sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovementRecord>
{
    public void Configure(EntityTypeBuilder<CashMovementRecord> builder)
    {
        builder.ToTable("cash_movements", "cashier");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MovementType).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Reason).HasMaxLength(250).IsRequired();

        builder.HasIndex(m => m.CashierSessionId);
        builder.HasIndex(m => new { m.CompanyId, m.BranchId });

        builder.HasOne(m => m.CashierSession)
            .WithMany(s => s.Movements)
            .HasForeignKey(m => m.CashierSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
