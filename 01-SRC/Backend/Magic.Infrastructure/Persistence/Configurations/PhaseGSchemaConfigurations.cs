using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magic.Infrastructure.Persistence.Configurations;

// ==========================================
// 1. BRANCHES SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<CompanyRecord>
{
    public void Configure(EntityTypeBuilder<CompanyRecord> builder)
    {
        builder.ToTable("companies", "branches");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.TradeName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Gstin).HasMaxLength(15);
        builder.Property(c => c.Pan).HasMaxLength(10);
        builder.Property(c => c.Address).HasMaxLength(500).IsRequired();
        builder.Property(c => c.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.HasMany(c => c.Branches).WithOne(b => b.Company).HasForeignKey(b => b.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BranchConfiguration : IEntityTypeConfiguration<BranchRecord>
{
    public void Configure(EntityTypeBuilder<BranchRecord> builder)
    {
        builder.ToTable("branches", "branches");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.CompanyId, b.BranchCode }).IsUnique();
        builder.Property(b => b.BranchCode).HasMaxLength(50).IsRequired();
        builder.Property(b => b.BranchName).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Gstin).HasMaxLength(15);
        builder.HasMany(b => b.Terminals).WithOne(t => t.Branch).HasForeignKey(t => t.BranchId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TerminalRegistrationConfiguration : IEntityTypeConfiguration<TerminalRegistrationRecord>
{
    public void Configure(EntityTypeBuilder<TerminalRegistrationRecord> builder)
    {
        builder.ToTable("terminals", "branches");
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => new { t.CompanyId, t.TerminalCode }).IsUnique();
        builder.Property(t => t.TerminalCode).HasMaxLength(50).IsRequired();
        builder.Property(t => t.MachineName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.PosPrinterModel).HasMaxLength(100);
    }
}

internal sealed class InterBranchTransferRequestConfiguration : IEntityTypeConfiguration<InterBranchTransferRequestRecord>
{
    public void Configure(EntityTypeBuilder<InterBranchTransferRequestRecord> builder)
    {
        builder.ToTable("inter_branch_transfers", "branches");
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => new { t.CompanyId, t.TransferNumber }).IsUnique();
        builder.Property(t => t.TransferNumber).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Status).HasMaxLength(50).IsRequired();
        builder.HasMany(t => t.Lines).WithOne(l => l.TransferRequest).HasForeignKey(l => l.TransferRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class InterBranchTransferLineConfiguration : IEntityTypeConfiguration<InterBranchTransferLineRecord>
{
    public void Configure(EntityTypeBuilder<InterBranchTransferLineRecord> builder)
    {
        builder.ToTable("inter_branch_transfer_lines", "branches");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.DispatchedQuantity).HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(l => l.ReceivedQuantity).HasColumnType("numeric(14,4)").IsRequired();
    }
}

// ==========================================
// 2. ADMINISTRATION SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflowRecord>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowRecord> builder)
    {
        builder.ToTable("approval_workflows", "administration");
        builder.HasKey(w => w.Id);
        builder.HasIndex(w => new { w.CompanyId, w.Module, w.TransactionType });
        builder.Property(w => w.Module).HasMaxLength(100).IsRequired();
        builder.Property(w => w.TransactionType).HasMaxLength(100).IsRequired();
        builder.Property(w => w.MinimumAmountPaise).HasColumnType("bigint").IsRequired();
        builder.HasMany(w => w.Requests).WithOne(r => r.Workflow).HasForeignKey(r => r.WorkflowId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequestRecord>
{
    public void Configure(EntityTypeBuilder<ApprovalRequestRecord> builder)
    {
        builder.ToTable("approval_requests", "administration");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.CompanyId, r.Status });
        builder.Property(r => r.SourceModule).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ReferenceNumber).HasMaxLength(100).IsRequired();
        builder.Property(r => r.AmountPaise).HasColumnType("bigint").IsRequired();
        builder.Property(r => r.Status).HasMaxLength(50).IsRequired();
        builder.Property(r => r.RequestedByUserName).HasMaxLength(100).IsRequired();
        builder.HasMany(r => r.Actions).WithOne(a => a.ApprovalRequest).HasForeignKey(a => a.ApprovalRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ApprovalActionConfiguration : IEntityTypeConfiguration<ApprovalActionRecord>
{
    public void Configure(EntityTypeBuilder<ApprovalActionRecord> builder)
    {
        builder.ToTable("approval_actions", "administration");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ApproverName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
    }
}

// ==========================================
// 3. AUTOMATION SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRuleRecord>
{
    public void Configure(EntityTypeBuilder<AutomationRuleRecord> builder)
    {
        builder.ToTable("automation_rules", "automation");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.CompanyId, r.IsActive });
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.TriggerType).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ConditionJson).HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.ActionType).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ActionPayloadJson).HasColumnType("jsonb").IsRequired();
        builder.HasMany(r => r.Executions).WithOne(e => e.Rule).HasForeignKey(e => e.RuleId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AutomationExecutionConfiguration : IEntityTypeConfiguration<AutomationExecutionRecord>
{
    public void Configure(EntityTypeBuilder<AutomationExecutionRecord> builder)
    {
        builder.ToTable("automation_executions", "automation");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<NotificationRecord>
{
    public void Configure(EntityTypeBuilder<NotificationRecord> builder)
    {
        builder.ToTable("notifications", "automation");
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => new { n.CompanyId, n.IsRead, n.CreatedAtUtc });
        builder.Property(n => n.Category).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Priority).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
    }
}

// ==========================================
// 4. SYSTEM MANAGEMENT SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class BackupRecordConfiguration : IEntityTypeConfiguration<BackupRecord>
{
    public void Configure(EntityTypeBuilder<BackupRecord> builder)
    {
        builder.ToTable("backup_records", "system_management");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BackupType).HasMaxLength(50).IsRequired();
        builder.Property(b => b.FileName).HasMaxLength(255).IsRequired();
        builder.Property(b => b.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Status).HasMaxLength(50).IsRequired();
        builder.Property(b => b.InitiatedByUserName).HasMaxLength(100).IsRequired();
    }
}

internal sealed class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfigurationRecord>
{
    public void Configure(EntityTypeBuilder<SystemConfigurationRecord> builder)
    {
        builder.ToTable("system_configurations", "system_management");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.CompanyId, c.ConfigKey }).IsUnique();
        builder.Property(c => c.ConfigKey).HasMaxLength(100).IsRequired();
        builder.Property(c => c.ConfigValue).IsRequired();
        builder.Property(c => c.Category).HasMaxLength(100).IsRequired();
    }
}

// ==========================================
// 5. REPORTING SCHEMA CONFIGURATIONS
// ==========================================

internal sealed class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinitionRecord>
{
    public void Configure(EntityTypeBuilder<ReportDefinitionRecord> builder)
    {
        builder.ToTable("report_definitions", "reporting");
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.CompanyId, r.ReportCode }).IsUnique();
        builder.Property(r => r.ReportCode).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Category).HasMaxLength(100).IsRequired();
        builder.Property(r => r.FilterSchemaJson).HasColumnType("jsonb").IsRequired();
    }
}

internal sealed class ReportExportLogConfiguration : IEntityTypeConfiguration<ReportExportLogRecord>
{
    public void Configure(EntityTypeBuilder<ReportExportLogRecord> builder)
    {
        builder.ToTable("report_export_logs", "reporting");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.CompanyId, l.ExportedAtUtc });
        builder.Property(l => l.ReportCode).HasMaxLength(100).IsRequired();
        builder.Property(l => l.ExportFormat).HasMaxLength(50).IsRequired();
        builder.Property(l => l.FilterParametersJson).HasColumnType("jsonb").IsRequired();
        builder.Property(l => l.ExportedByUserName).HasMaxLength(100).IsRequired();
    }
}
