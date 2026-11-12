using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Magic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogImmutabilityTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION audit.prevent_audit_log_tampering()
                RETURNS TRIGGER AS $$
                BEGIN
                    RAISE EXCEPTION 'audit.system_audit_logs is an immutable append-only ledger. UPDATE and DELETE operations are strictly prohibited.'
                        USING ERRCODE = '55000';
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_prevent_audit_log_tampering ON audit.system_audit_logs;

                CREATE TRIGGER trg_prevent_audit_log_tampering
                BEFORE UPDATE OR DELETE ON audit.system_audit_logs
                FOR EACH ROW
                EXECUTE FUNCTION audit.prevent_audit_log_tampering();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_prevent_audit_log_tampering ON audit.system_audit_logs;
                DROP FUNCTION IF EXISTS audit.prevent_audit_log_tampering();
                """);
        }
    }
}
