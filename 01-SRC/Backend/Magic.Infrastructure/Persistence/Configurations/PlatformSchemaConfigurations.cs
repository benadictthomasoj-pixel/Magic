using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Magic.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageRecord>
{
    public void Configure(EntityTypeBuilder<OutboxMessageRecord> builder)
    {
        builder.ToTable("outbox_messages", "platform");
        builder.HasKey(x => x.Id).HasName("pk_outbox_messages");

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
        builder.Property(x => x.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.AggregateId).HasColumnName("aggregate_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(150).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TraceCorrelationId).HasColumnName("trace_correlation_id").IsRequired();
        builder.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.LockToken).HasColumnName("lock_token");
        builder.Property(x => x.LockExpiresAtUtc).HasColumnName("lock_expires_at_utc");

        builder.HasIndex(x => x.OccurredOnUtc)
            .HasDatabaseName("ix_outbox_messages_unprocessed")
            .HasFilter("processed_at_utc IS NULL");
    }
}

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records", "platform");
        builder.HasKey(x => x.Id).HasName("pk_idempotency_records");

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.EndpointPath).HasColumnName("endpoint_path").HasMaxLength(250).IsRequired();
        builder.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResponseStatusCode).HasColumnName("response_status_code").IsRequired();
        builder.Property(x => x.ResponseBodyJson).HasColumnName("response_body_json").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();

        builder.HasIndex(x => new { x.BranchId, x.IdempotencyKey, x.EndpointPath })
            .IsUnique()
            .HasDatabaseName("uq_idempotency_records_branch_key_endpoint");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("ix_idempotency_records_expires_at_utc");
    }
}

internal sealed class SystemAuditLogConfiguration : IEntityTypeConfiguration<SystemAuditLogRecord>
{
    public void Configure(EntityTypeBuilder<SystemAuditLogRecord> builder)
    {
        builder.ToTable("system_audit_logs", "audit");
        builder.HasKey(x => x.Id).HasName("pk_system_audit_logs");

        builder.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(x => x.EventTimestampUtc).HasColumnName("event_timestamp_utc").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(x => x.TerminalId).HasColumnName("terminal_id");
        builder.Property(x => x.ClientIp).HasColumnName("client_ip").HasMaxLength(45).IsRequired();
        builder.Property(x => x.ActionName).HasColumnName("action_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityName).HasColumnName("entity_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(x => x.BeforeStateJson).HasColumnName("before_state").HasColumnType("jsonb");
        builder.Property(x => x.AfterStateJson).HasColumnName("after_state").HasColumnType("jsonb");
        builder.Property(x => x.OverrideBySupervisorId).HasColumnName("override_by_supervisor_id");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();

        builder.HasIndex(x => x.EventTimestampUtc).HasDatabaseName("ix_system_audit_logs_event_timestamp");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("ix_system_audit_logs_correlation_id");
        builder.HasIndex(x => new { x.EntityName, x.EntityId }).HasDatabaseName("ix_system_audit_logs_entity");
    }
}

