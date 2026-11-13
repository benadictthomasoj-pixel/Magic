using Magic.Domain.Accounting.Entities;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magic.Infrastructure.Persistence.Configurations;

internal sealed class BankCashAccountConfiguration : IEntityTypeConfiguration<BankCashAccountRecord>
{
    public void Configure(EntityTypeBuilder<BankCashAccountRecord> builder)
    {
        builder.ToTable("bank_cash_accounts", "finance");

        builder.HasKey(x => x.Id).HasName("pk_bank_cash_accounts");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.AccountName).HasColumnName("account_name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.AccountType).HasColumnName("account_type").HasMaxLength(30).HasDefaultValue("Bank").IsRequired();
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(100);
        builder.Property(x => x.BranchName).HasColumnName("branch_name").HasMaxLength(100);
        builder.Property(x => x.AccountNumber).HasColumnName("account_number").HasMaxLength(100).IsRequired();
        builder.Property(x => x.MaskedAccountNumber).HasColumnName("masked_account_number").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IfscCode).HasColumnName("ifsc_code").HasMaxLength(20);
        builder.Property(x => x.GlAccountId).HasColumnName("gl_account_id").IsRequired();
        builder.Property(x => x.OpeningBalancePaise).HasColumnName("opening_balance_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.CurrentBalancePaise).HasColumnName("current_balance_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.AccountName })
            .HasDatabaseName("ix_bank_cash_accounts_company_branch");

        builder.HasOne(x => x.GlAccount)
            .WithMany()
            .HasForeignKey(x => x.GlAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CustomerReceiptConfiguration : IEntityTypeConfiguration<CustomerReceiptRecord>
{
    public void Configure(EntityTypeBuilder<CustomerReceiptRecord> builder)
    {
        builder.ToTable("customer_receipts", "finance", t =>
        {
            t.HasCheckConstraint("chk_receipt_amounts", "allocated_amount_paise + unallocated_amount_paise = total_amount_paise");
            t.HasCheckConstraint("chk_receipt_total_positive", "total_amount_paise > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_customer_receipts");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.ReceiptNumber).HasColumnName("receipt_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.ReceiptDate).HasColumnName("receipt_date").IsRequired();
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100);
        builder.Property(x => x.BankCashAccountId).HasColumnName("bank_cash_account_id").IsRequired();
        builder.Property(x => x.TotalAmountPaise).HasColumnName("total_amount_paise").IsRequired();
        builder.Property(x => x.AllocatedAmountPaise).HasColumnName("allocated_amount_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.UnallocatedAmountPaise).HasColumnName("unallocated_amount_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("SUBMITTED").IsRequired();
        builder.Property(x => x.VoucherId).HasColumnName("voucher_id").IsRequired();
        builder.Property(x => x.Narration).HasColumnName("narration").HasMaxLength(500).HasDefaultValue("").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.HasIndex(x => x.ReceiptNumber).IsUnique().HasDatabaseName("ix_customer_receipts_number");
        builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.CustomerId }).HasDatabaseName("ix_customer_receipts_customer");
        builder.HasIndex(x => new { x.CompanyId, x.ReceiptDate }).HasDatabaseName("ix_customer_receipts_date");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BankCashAccount)
            .WithMany()
            .HasForeignKey(x => x.BankCashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CustomerReceiptAllocationConfiguration : IEntityTypeConfiguration<CustomerReceiptAllocationRecord>
{
    public void Configure(EntityTypeBuilder<CustomerReceiptAllocationRecord> builder)
    {
        builder.ToTable("customer_receipt_allocations", "finance", t =>
        {
            t.HasCheckConstraint("chk_receipt_alloc_positive", "allocated_amount_paise > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_customer_receipt_allocations");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ReceiptId).HasColumnName("receipt_id").IsRequired();
        builder.Property(x => x.ArItemId).HasColumnName("ar_item_id").IsRequired();
        builder.Property(x => x.AllocatedAmountPaise).HasColumnName("allocated_amount_paise").IsRequired();
        builder.Property(x => x.AllocationDate).HasColumnName("allocation_date").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasOne(x => x.Receipt)
            .WithMany(r => r.Allocations)
            .HasForeignKey(x => x.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ArItem)
            .WithMany()
            .HasForeignKey(x => x.ArItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoiceRecord>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceRecord> builder)
    {
        builder.ToTable("supplier_invoices", "finance", t =>
        {
            t.HasCheckConstraint("chk_supplier_invoice_amounts", "settled_amount_paise + outstanding_amount_paise = total_amount_paise");
            t.HasCheckConstraint("chk_supplier_invoice_positive", "total_amount_paise > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_supplier_invoices");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SystemInvoiceNumber).HasColumnName("system_invoice_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.InvoiceDate).HasColumnName("invoice_date").IsRequired();
        builder.Property(x => x.DueDate).HasColumnName("due_date").IsRequired();
        builder.Property(x => x.PoId).HasColumnName("po_id");
        builder.Property(x => x.GrnId).HasColumnName("grn_id");
        builder.Property(x => x.SubTotalPaise).HasColumnName("sub_total_paise").IsRequired();
        builder.Property(x => x.TaxAmountPaise).HasColumnName("tax_amount_paise").IsRequired();
        builder.Property(x => x.TotalAmountPaise).HasColumnName("total_amount_paise").IsRequired();
        builder.Property(x => x.SettledAmountPaise).HasColumnName("settled_amount_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.OutstandingAmountPaise).HasColumnName("outstanding_amount_paise").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("APPROVED").IsRequired();
        builder.Property(x => x.VoucherId).HasColumnName("voucher_id").IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500).HasDefaultValue("").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.HasIndex(x => x.SystemInvoiceNumber).IsUnique().HasDatabaseName("ix_supplier_invoices_system_number");
        builder.HasIndex(x => new { x.CompanyId, x.SupplierId, x.InvoiceNumber }).HasDatabaseName("ix_supplier_invoices_supplier_number");
        builder.HasIndex(x => new { x.CompanyId, x.DueDate }).HasDatabaseName("ix_supplier_invoices_due_date");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SupplierInvoiceLineConfiguration : IEntityTypeConfiguration<SupplierInvoiceLineRecord>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceLineRecord> builder)
    {
        builder.ToTable("supplier_invoice_lines", "finance");

        builder.HasKey(x => x.Id).HasName("pk_supplier_invoice_lines");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(250).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(14, 4).IsRequired();
        builder.Property(x => x.UnitPricePaise).HasColumnName("unit_price_paise").IsRequired();
        builder.Property(x => x.TaxRatePercentage).HasColumnName("tax_rate_percentage").HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.TaxAmountPaise).HasColumnName("tax_amount_paise").IsRequired();
        builder.Property(x => x.TotalAmountPaise).HasColumnName("total_amount_paise").IsRequired();

        builder.HasOne(x => x.Invoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPaymentRecord>
{
    public void Configure(EntityTypeBuilder<SupplierPaymentRecord> builder)
    {
        builder.ToTable("supplier_payments", "finance", t =>
        {
            t.HasCheckConstraint("chk_payment_amounts", "allocated_amount_paise + unallocated_amount_paise = total_amount_paise");
            t.HasCheckConstraint("chk_payment_total_positive", "total_amount_paise > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_supplier_payments");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.PaymentNumber).HasColumnName("payment_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.PaymentDate).HasColumnName("payment_date").IsRequired();
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100);
        builder.Property(x => x.BankCashAccountId).HasColumnName("bank_cash_account_id").IsRequired();
        builder.Property(x => x.TotalAmountPaise).HasColumnName("total_amount_paise").IsRequired();
        builder.Property(x => x.AllocatedAmountPaise).HasColumnName("allocated_amount_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.UnallocatedAmountPaise).HasColumnName("unallocated_amount_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("SUBMITTED").IsRequired();
        builder.Property(x => x.VoucherId).HasColumnName("voucher_id").IsRequired();
        builder.Property(x => x.Narration).HasColumnName("narration").HasMaxLength(500).HasDefaultValue("").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.HasIndex(x => x.PaymentNumber).IsUnique().HasDatabaseName("ix_supplier_payments_number");
        builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.SupplierId }).HasDatabaseName("ix_supplier_payments_supplier");
        builder.HasIndex(x => new { x.CompanyId, x.PaymentDate }).HasDatabaseName("ix_supplier_payments_date");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BankCashAccount)
            .WithMany()
            .HasForeignKey(x => x.BankCashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SupplierPaymentAllocationConfiguration : IEntityTypeConfiguration<SupplierPaymentAllocationRecord>
{
    public void Configure(EntityTypeBuilder<SupplierPaymentAllocationRecord> builder)
    {
        builder.ToTable("supplier_payment_allocations", "finance", t =>
        {
            t.HasCheckConstraint("chk_payment_alloc_positive", "allocated_amount_paise > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_supplier_payment_allocations");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.PaymentId).HasColumnName("payment_id").IsRequired();
        builder.Property(x => x.ApItemId).HasColumnName("ap_item_id").IsRequired();
        builder.Property(x => x.AllocatedAmountPaise).HasColumnName("allocated_amount_paise").IsRequired();
        builder.Property(x => x.AllocationDate).HasColumnName("allocation_date").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasOne(x => x.Payment)
            .WithMany(p => p.Allocations)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ApItem)
            .WithMany()
            .HasForeignKey(x => x.ApItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TreasuryTransactionConfiguration : IEntityTypeConfiguration<TreasuryTransactionRecord>
{
    public void Configure(EntityTypeBuilder<TreasuryTransactionRecord> builder)
    {
        builder.ToTable("treasury_transactions", "finance", t =>
        {
            t.HasCheckConstraint("chk_treasury_amount_positive", "amount_paise > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_treasury_transactions");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.TransactionNumber).HasColumnName("transaction_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.TransactionType).HasColumnName("transaction_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.TransactionDate).HasColumnName("transaction_date").IsRequired();
        builder.Property(x => x.SourceAccountId).HasColumnName("source_account_id").IsRequired();
        builder.Property(x => x.DestinationAccountId).HasColumnName("destination_account_id");
        builder.Property(x => x.AmountPaise).HasColumnName("amount_paise").IsRequired();
        builder.Property(x => x.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100);
        builder.Property(x => x.Narration).HasColumnName("narration").HasMaxLength(500).HasDefaultValue("").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("POSTED").IsRequired();
        builder.Property(x => x.VoucherId).HasColumnName("voucher_id").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.HasIndex(x => x.TransactionNumber).IsUnique().HasDatabaseName("ix_treasury_transactions_number");
        builder.HasIndex(x => new { x.CompanyId, x.TransactionDate }).HasDatabaseName("ix_treasury_transactions_date");

        builder.HasOne(x => x.SourceAccount)
            .WithMany()
            .HasForeignKey(x => x.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationAccount)
            .WithMany()
            .HasForeignKey(x => x.DestinationAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BankStatementConfiguration : IEntityTypeConfiguration<BankStatementRecord>
{
    public void Configure(EntityTypeBuilder<BankStatementRecord> builder)
    {
        builder.ToTable("bank_statements", "finance");

        builder.HasKey(x => x.Id).HasName("pk_bank_statements");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.BankAccountId).HasColumnName("bank_account_id").IsRequired();
        builder.Property(x => x.StatementStartDate).HasColumnName("statement_start_date").IsRequired();
        builder.Property(x => x.StatementEndDate).HasColumnName("statement_end_date").IsRequired();
        builder.Property(x => x.OpeningBalancePaise).HasColumnName("opening_balance_paise").IsRequired();
        builder.Property(x => x.ClosingBalancePaise).HasColumnName("closing_balance_paise").IsRequired();
        builder.Property(x => x.Filename).HasColumnName("filename").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("OPEN").IsRequired();
        builder.Property(x => x.ImportedAtUtc).HasColumnName("imported_at_utc").IsRequired();
        builder.Property(x => x.ImportedByUserId).HasColumnName("imported_by_user_id").IsRequired();

        builder.HasOne(x => x.BankAccount)
            .WithMany()
            .HasForeignKey(x => x.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLineRecord>
{
    public void Configure(EntityTypeBuilder<BankStatementLineRecord> builder)
    {
        builder.ToTable("bank_statement_lines", "finance", t =>
        {
            t.HasCheckConstraint("chk_stmt_line_amounts", "withdrawal_debit_paise >= 0 AND deposit_credit_paise >= 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_bank_statement_lines");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.StatementId).HasColumnName("statement_id").IsRequired();
        builder.Property(x => x.TransactionDate).HasColumnName("transaction_date").IsRequired();
        builder.Property(x => x.ValueDate).HasColumnName("value_date");
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(250).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100);
        builder.Property(x => x.WithdrawalDebitPaise).HasColumnName("withdrawal_debit_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.DepositCreditPaise).HasColumnName("deposit_credit_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.BalancePaise).HasColumnName("balance_paise").IsRequired();
        builder.Property(x => x.IsReconciled).HasColumnName("is_reconciled").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.MatchedVoucherLineId).HasColumnName("matched_voucher_line_id");
        builder.Property(x => x.MatchedAtUtc).HasColumnName("matched_at_utc");
        builder.Property(x => x.MatchType).HasColumnName("match_type").HasMaxLength(40);

        builder.HasIndex(x => new { x.StatementId, x.IsReconciled }).HasDatabaseName("ix_bank_statement_lines_reconciled");

        builder.HasOne(x => x.Statement)
            .WithMany(s => s.Lines)
            .HasForeignKey(x => x.StatementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MatchedVoucherLine)
            .WithMany()
            .HasForeignKey(x => x.MatchedVoucherLineId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
