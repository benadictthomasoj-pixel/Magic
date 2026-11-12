DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'platform') THEN
        CREATE SCHEMA platform;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS platform.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'platform') THEN
            CREATE SCHEMA platform;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'audit') THEN
            CREATE SCHEMA audit;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    CREATE TABLE platform.idempotency_records (
        id uuid NOT NULL,
        idempotency_key uuid NOT NULL,
        branch_id uuid NOT NULL,
        endpoint_path character varying(250) NOT NULL,
        request_hash character varying(128) NOT NULL,
        response_status_code integer NOT NULL,
        response_body_json jsonb,
        created_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_idempotency_records PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    CREATE TABLE platform.outbox_messages (
        id uuid NOT NULL,
        occurred_on_utc timestamp with time zone NOT NULL,
        aggregate_type character varying(100) NOT NULL,
        aggregate_id uuid NOT NULL,
        event_type character varying(150) NOT NULL,
        payload_json jsonb NOT NULL,
        trace_correlation_id uuid NOT NULL,
        processed_at_utc timestamp with time zone,
        error_message text,
        retry_count integer NOT NULL DEFAULT 0,
        lock_token uuid,
        lock_expires_at_utc timestamp with time zone,
        CONSTRAINT pk_outbox_messages PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    CREATE TABLE audit.system_audit_logs (
        id bigint GENERATED ALWAYS AS IDENTITY,
        event_timestamp_utc timestamp with time zone NOT NULL,
        user_id uuid NOT NULL,
        branch_id uuid NOT NULL,
        terminal_id uuid,
        client_ip character varying(45) NOT NULL,
        action_name character varying(100) NOT NULL,
        entity_name character varying(100) NOT NULL,
        entity_id uuid NOT NULL,
        before_state jsonb,
        after_state jsonb,
        override_by_supervisor_id uuid,
        correlation_id uuid NOT NULL,
        CONSTRAINT pk_system_audit_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    CREATE INDEX ix_idempotency_records_expires_at_utc ON platform.idempotency_records (expires_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    CREATE UNIQUE INDEX uq_idempotency_records_branch_key_endpoint ON platform.idempotency_records (branch_id, idempotency_key, endpoint_path);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    CREATE INDEX ix_outbox_messages_unprocessed ON platform.outbox_messages (occurred_on_utc) WHERE processed_at_utc IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    CREATE INDEX ix_system_audit_logs_correlation_id ON audit.system_audit_logs (correlation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    CREATE INDEX ix_system_audit_logs_entity ON audit.system_audit_logs (entity_name, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    CREATE INDEX ix_system_audit_logs_event_timestamp ON audit.system_audit_logs (event_timestamp_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM platform.__ef_migrations_history WHERE "MigrationId" = '20261112043621_InitialPlatformFoundation') THEN
    INSERT INTO platform.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20261112043621_InitialPlatformFoundation', '10.0.15');
    END IF;
END $EF$;
COMMIT;

