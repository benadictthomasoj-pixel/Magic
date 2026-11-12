using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magic.Infrastructure.Persistence.Configurations;

internal sealed class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("fiscal_years", "accounting", t =>
        {
            t.HasCheckConstraint("chk_fiscal_years_dates", "end_date > start_date");
        });

        builder.HasKey(x => x.Id).HasName("pk_fiscal_years");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(x => x.IsClosed).HasColumnName("is_closed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ClosedAtUtc).HasColumnName("closed_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(x => x.ConcurrencyVersion).HasColumnName("concurrency_version").IsConcurrencyToken();

        builder.HasIndex(x => new { x.CompanyId, x.Code })
            .IsUnique()
            .HasDatabaseName("uq_fiscal_years_company_code");

        builder.HasIndex(x => new { x.CompanyId, x.StartDate, x.EndDate })
            .HasDatabaseName("ix_fiscal_years_company_dates");

        builder.HasMany(x => x.Periods)
            .WithOne(x => x.FiscalYear)
            .HasForeignKey(x => x.FiscalYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        builder.ToTable("fiscal_periods", "accounting", t =>
        {
            t.HasCheckConstraint("chk_fiscal_periods_dates", "end_date >= start_date");
            t.HasCheckConstraint("chk_fiscal_periods_state", "state IN ('Open', 'Closing', 'Locked', 'OPEN', 'CLOSING', 'LOCKED')");
        });

        builder.HasKey(x => x.Id).HasName("pk_fiscal_periods");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.FiscalYearId).HasColumnName("fiscal_year_id").IsRequired();
        builder.Property(x => x.PeriodNumber).HasColumnName("period_number").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
        builder.Property(x => x.State).HasColumnName("state").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(x => x.LockedAtUtc).HasColumnName("locked_at_utc");
        builder.Property(x => x.LockedByUserId).HasColumnName("locked_by_user_id");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(x => x.ConcurrencyVersion).HasColumnName("concurrency_version").IsConcurrencyToken();

        builder.HasIndex(x => new { x.FiscalYearId, x.PeriodNumber })
            .IsUnique()
            .HasDatabaseName("uq_fiscal_periods_year_period");

        builder.HasIndex(x => new { x.StartDate, x.EndDate })
            .HasDatabaseName("ix_fiscal_periods_dates");
    }
}

internal sealed class AccountGroupConfiguration : IEntityTypeConfiguration<AccountGroup>
{
    public void Configure(EntityTypeBuilder<AccountGroup> builder)
    {
        builder.ToTable("account_groups", "accounting", t =>
        {
            t.HasCheckConstraint("chk_account_groups_hierarchy", "id <> parent_group_id");
            t.HasCheckConstraint("chk_account_groups_root_class", "root_class IN ('Asset', 'Liability', 'Equity', 'Revenue', 'Expense', 'ASSET', 'LIABILITY', 'EQUITY', 'REVENUE', 'EXPENSE')");
        });

        builder.HasKey(x => x.Id).HasName("pk_account_groups");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.ParentGroupId).HasColumnName("parent_group_id");
        builder.Property(x => x.Code).HasColumnName("group_code").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasColumnName("group_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.RootClass).HasColumnName("root_class").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(x => x.HierarchyDepth).HasColumnName("hierarchy_depth").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(x => x.ConcurrencyVersion).HasColumnName("concurrency_version").IsConcurrencyToken();

        builder.HasIndex(x => new { x.CompanyId, x.Code })
            .IsUnique()
            .HasDatabaseName("uq_account_groups_company_code");

        builder.HasIndex(x => x.ParentGroupId)
            .HasDatabaseName("ix_account_groups_parent");

        builder.HasOne(x => x.ParentGroup)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", "accounting", t =>
        {
            t.HasCheckConstraint("chk_accounts_normal_balance", "normal_balance IN ('Debit', 'Credit', 'DEBIT', 'CREDIT')");
            t.HasCheckConstraint("chk_accounts_root_class", "root_class IN ('Asset', 'Liability', 'Equity', 'Revenue', 'Expense', 'ASSET', 'LIABILITY', 'EQUITY', 'REVENUE', 'EXPENSE')");
        });

        builder.HasKey(x => x.Id).HasName("pk_accounts");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.AccountGroupId).HasColumnName("account_group_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("account_code").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasColumnName("account_name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.RootClass).HasColumnName("root_class").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(x => x.NormalBalance).HasColumnName("normal_balance").HasMaxLength(10).HasConversion<string>().IsRequired();
        builder.Property(x => x.IsControlAccount).HasColumnName("is_control_account").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ControlAccountType).HasColumnName("control_account_type").HasMaxLength(30);
        builder.Property(x => x.AllowDirectPosting).HasColumnName("allow_direct_posting").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsSystemLocked).HasColumnName("is_system_locked").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(x => x.ConcurrencyVersion).HasColumnName("concurrency_version").IsConcurrencyToken();

        builder.HasIndex(x => new { x.CompanyId, x.Code })
            .IsUnique()
            .HasDatabaseName("uq_accounts_company_code");

        builder.HasIndex(x => x.AccountGroupId)
            .HasDatabaseName("ix_accounts_group");

        builder.HasIndex(x => new { x.CompanyId, x.IsControlAccount, x.ControlAccountType })
            .HasDatabaseName("ix_accounts_control");

        builder.HasOne(x => x.AccountGroup)
            .WithMany()
            .HasForeignKey(x => x.AccountGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VoucherSequenceConfiguration : IEntityTypeConfiguration<VoucherSequenceRecord>
{
    public void Configure(EntityTypeBuilder<VoucherSequenceRecord> builder)
    {
        builder.ToTable("voucher_sequences", "accounting", t =>
        {
            t.HasCheckConstraint("chk_voucher_sequences_positive", "current_sequence >= 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_voucher_sequences");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.FiscalYearId).HasColumnName("fiscal_year_id").IsRequired();
        builder.Property(x => x.VoucherType).HasColumnName("voucher_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CurrentSequence).HasColumnName("current_sequence").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasIndex(x => new { x.BranchId, x.FiscalYearId, x.VoucherType })
            .IsUnique()
            .HasDatabaseName("uq_voucher_sequences_branch_fy_type");
    }
}

internal sealed class JournalVoucherConfiguration : IEntityTypeConfiguration<JournalVoucher>
{
    public void Configure(EntityTypeBuilder<JournalVoucher> builder)
    {
        builder.ToTable("vouchers", "accounting", t =>
        {
            t.HasCheckConstraint("chk_vouchers_balance", "total_debit_paise = total_credit_paise");
            t.HasCheckConstraint("chk_vouchers_positive_totals", "total_debit_paise > 0 AND total_credit_paise > 0");
            t.HasCheckConstraint("chk_vouchers_status", "status IN ('Draft', 'Approved', 'Posted', 'Reversed', 'Cancelled', 'DRAFT', 'APPROVED', 'POSTED', 'REVERSED', 'CANCELLED')");
        });

        builder.HasKey(x => x.Id).HasName("pk_vouchers");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.FiscalPeriodId).HasColumnName("fiscal_period_id").IsRequired();
        builder.Property(x => x.VoucherNumber).HasColumnName("voucher_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.VoucherDate).HasColumnName("posting_date").IsRequired();
        builder.Property(x => x.VoucherType).HasColumnName("voucher_type").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(x => x.TotalDebitPaise).HasColumnName("total_debit_paise").IsRequired();
        builder.Property(x => x.TotalCreditPaise).HasColumnName("total_credit_paise").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
        builder.Property(x => x.SourceModule).HasColumnName("source_module").HasMaxLength(50);
        builder.Property(x => x.SourceTransactionType).HasColumnName("source_transaction_type").HasMaxLength(50);
        builder.Property(x => x.SourceDocumentId).HasColumnName("source_document_id");
        builder.Property(x => x.IsReversed).HasColumnName("is_reversed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ReversalVoucherId).HasColumnName("reversal_voucher_id");
        builder.Property(x => x.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(50);
        builder.Property(x => x.Narration).HasColumnName("narration").IsRequired();
        builder.Property(x => x.PostedAtUtc).HasColumnName("posted_at_utc");
        builder.Property(x => x.PostedByUserId).HasColumnName("posted_by_user_id");
        builder.Property(x => x.SupervisorApprovedByUserId).HasColumnName("supervisor_approved_by_user_id");
        builder.Property(x => x.TraceCorrelationId).HasColumnName("trace_correlation_id").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(x => x.ConcurrencyVersion).HasColumnName("concurrency_version").IsConcurrencyToken();

        // Financial fields must be ignored if purely computed in-memory
        builder.Ignore(x => x.TotalDebit);
        builder.Ignore(x => x.TotalCredit);
        builder.Ignore(x => x.IsBalanced);

        builder.HasIndex(x => new { x.BranchId, x.VoucherNumber })
            .IsUnique()
            .HasDatabaseName("uq_vouchers_branch_number");

        builder.HasIndex(x => new { x.BranchId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("uq_vouchers_branch_idempotency");

        builder.HasIndex(x => new { x.FiscalPeriodId, x.VoucherDate })
            .HasDatabaseName("ix_vouchers_period_date");

        builder.HasIndex(x => new { x.SourceModule, x.SourceDocumentId })
            .HasDatabaseName("ix_vouchers_source_document")
            .HasFilter("source_module IS NOT NULL AND source_document_id IS NOT NULL");

        builder.HasIndex(x => x.ReversalVoucherId)
            .HasDatabaseName("ix_vouchers_reversal")
            .HasFilter("reversal_voucher_id IS NOT NULL");

        builder.HasOne(x => x.FiscalPeriod)
            .WithMany()
            .HasForeignKey(x => x.FiscalPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReversalVoucher)
            .WithMany()
            .HasForeignKey(x => x.ReversalVoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Voucher)
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("journal_lines", "accounting", t =>
        {
            t.HasCheckConstraint("chk_journal_lines_debit_non_negative", "debit_paise >= 0");
            t.HasCheckConstraint("chk_journal_lines_credit_non_negative", "credit_paise >= 0");
            t.HasCheckConstraint("chk_journal_lines_exclusive_side", "(debit_paise > 0 AND credit_paise = 0) OR (credit_paise > 0 AND debit_paise = 0)");
        });

        builder.HasKey(x => x.Id).HasName("pk_journal_lines");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.VoucherId).HasColumnName("voucher_id").IsRequired();
        builder.Property(x => x.LineNumber).HasColumnName("line_number").IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.Debit).HasColumnName("debit_paise").IsRequired();
        builder.Property(x => x.Credit).HasColumnName("credit_paise").IsRequired();
        builder.Property(x => x.Narration).HasColumnName("description").HasMaxLength(255).IsRequired();
        builder.Property(x => x.CostCenterId).HasColumnName("cost_center_id");
        builder.Property(x => x.PartyId).HasColumnName("party_id");
        builder.Property(x => x.PartyType).HasColumnName("party_type").HasMaxLength(20);

        builder.HasIndex(x => new { x.VoucherId, x.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_journal_lines_voucher_line");

        builder.HasIndex(x => new { x.AccountId, x.BranchId })
            .HasDatabaseName("ix_journal_lines_account_branch");

        builder.HasIndex(x => x.VoucherId)
            .HasDatabaseName("ix_journal_lines_voucher");

        builder.HasIndex(x => new { x.PartyType, x.PartyId })
            .HasDatabaseName("ix_journal_lines_party")
            .HasFilter("party_id IS NOT NULL");

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ArSubledgerItemConfiguration : IEntityTypeConfiguration<ArSubledgerItemRecord>
{
    public void Configure(EntityTypeBuilder<ArSubledgerItemRecord> builder)
    {
        builder.ToTable("ar_subledger_items", "accounting", t =>
        {
            t.HasCheckConstraint("chk_ar_amounts", "settled_amount_paise + outstanding_amount_paise = original_amount_paise");
            t.HasCheckConstraint("chk_ar_status", "status IN ('OPEN', 'PARTIALLY_PAID', 'PAID')");
        });

        builder.HasKey(x => x.Id).HasName("pk_ar_subledger_items");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.VoucherId).HasColumnName("voucher_id").IsRequired();
        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id");
        builder.Property(x => x.ItemDate).HasColumnName("item_date").IsRequired();
        builder.Property(x => x.DueDate).HasColumnName("due_date").IsRequired();
        builder.Property(x => x.OriginalAmountPaise).HasColumnName("original_amount_paise").IsRequired();
        builder.Property(x => x.SettledAmountPaise).HasColumnName("settled_amount_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.OutstandingAmountPaise).HasColumnName("outstanding_amount_paise").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("OPEN").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.CustomerId, x.Status })
            .HasDatabaseName("ix_ar_customer_status");

        builder.HasIndex(x => new { x.CustomerId, x.DueDate })
            .HasDatabaseName("ix_ar_aging")
            .HasFilter("status <> 'PAID'");
    }
}

internal sealed class ArAllocationConfiguration : IEntityTypeConfiguration<ArAllocationRecord>
{
    public void Configure(EntityTypeBuilder<ArAllocationRecord> builder)
    {
        builder.ToTable("ar_allocations", "accounting", t =>
        {
            t.HasCheckConstraint("chk_ar_alloc_positive", "allocated_amount_paise > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_ar_allocations");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.DebitItemId).HasColumnName("debit_item_id").IsRequired();
        builder.Property(x => x.CreditVoucherId).HasColumnName("credit_voucher_id").IsRequired();
        builder.Property(x => x.AllocationDate).HasColumnName("allocation_date").IsRequired();
        builder.Property(x => x.AllocatedAmountPaise).HasColumnName("allocated_amount_paise").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
    }
}

internal sealed class ApSubledgerItemConfiguration : IEntityTypeConfiguration<ApSubledgerItemRecord>
{
    public void Configure(EntityTypeBuilder<ApSubledgerItemRecord> builder)
    {
        builder.ToTable("ap_subledger_items", "accounting", t =>
        {
            t.HasCheckConstraint("chk_ap_amounts", "settled_amount_paise + outstanding_amount_paise = original_amount_paise");
            t.HasCheckConstraint("chk_ap_status", "status IN ('OPEN', 'PARTIALLY_PAID', 'PAID')");
        });

        builder.HasKey(x => x.Id).HasName("pk_ap_subledger_items");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.VoucherId).HasColumnName("voucher_id").IsRequired();
        builder.Property(x => x.BillId).HasColumnName("bill_id");
        builder.Property(x => x.ItemDate).HasColumnName("item_date").IsRequired();
        builder.Property(x => x.DueDate).HasColumnName("due_date").IsRequired();
        builder.Property(x => x.OriginalAmountPaise).HasColumnName("original_amount_paise").IsRequired();
        builder.Property(x => x.SettledAmountPaise).HasColumnName("settled_amount_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.OutstandingAmountPaise).HasColumnName("outstanding_amount_paise").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("OPEN").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.SupplierId, x.Status })
            .HasDatabaseName("ix_ap_supplier_status");

        builder.HasIndex(x => new { x.SupplierId, x.DueDate })
            .HasDatabaseName("ix_ap_aging")
            .HasFilter("status <> 'PAID'");
    }
}

internal sealed class ApAllocationConfiguration : IEntityTypeConfiguration<ApAllocationRecord>
{
    public void Configure(EntityTypeBuilder<ApAllocationRecord> builder)
    {
        builder.ToTable("ap_allocations", "accounting", t =>
        {
            t.HasCheckConstraint("chk_ap_alloc_positive", "allocated_amount_paise > 0");
        });

        builder.HasKey(x => x.Id).HasName("pk_ap_allocations");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CreditItemId).HasColumnName("credit_item_id").IsRequired();
        builder.Property(x => x.DebitVoucherId).HasColumnName("debit_voucher_id").IsRequired();
        builder.Property(x => x.AllocationDate).HasColumnName("allocation_date").IsRequired();
        builder.Property(x => x.AllocatedAmountPaise).HasColumnName("allocated_amount_paise").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
    }
}

internal sealed class AccountPeriodBalanceConfiguration : IEntityTypeConfiguration<AccountPeriodBalanceRecord>
{
    public void Configure(EntityTypeBuilder<AccountPeriodBalanceRecord> builder)
    {
        builder.ToTable("account_period_balances", "accounting");

        builder.HasKey(x => x.Id).HasName("pk_account_period_balances");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.FiscalPeriodId).HasColumnName("fiscal_period_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.OpeningDebitPaise).HasColumnName("opening_debit_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.OpeningCreditPaise).HasColumnName("opening_credit_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.MovementDebitPaise).HasColumnName("movement_debit_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.MovementCreditPaise).HasColumnName("movement_credit_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.ClosingDebitPaise).HasColumnName("closing_debit_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.ClosingCreditPaise).HasColumnName("closing_credit_paise").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.LastUpdatedAtUtc).HasColumnName("last_updated_at_utc").IsRequired();

        builder.HasIndex(x => new { x.AccountId, x.FiscalPeriodId, x.BranchId })
            .IsUnique()
            .HasDatabaseName("uq_account_period_balances_account_period_branch");
    }
}
