using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Magic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseGEnterpriseModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "administration");

            migrationBuilder.EnsureSchema(
                name: "automation");

            migrationBuilder.EnsureSchema(
                name: "system_management");

            migrationBuilder.EnsureSchema(
                name: "branches");

            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "approval_workflows",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinimumAmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    RequiredApprovalsCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "automation_rules",
                schema: "automation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConditionJson = table.Column<string>(type: "jsonb", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastTriggeredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutionCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "backup_records",
                schema: "system_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BackupType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InitiatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Pan = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inter_branch_transfers",
                schema: "branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceBranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationBranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inter_branch_transfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "automation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_definitions",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FilterSchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_export_logs",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExportFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FilterParametersJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_export_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_configurations",
                schema: "system_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_configurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "approval_requests",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceModule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AmountPaise = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_requests_approval_workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalSchema: "administration",
                        principalTable: "approval_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "automation_executions",
                schema: "automation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResultMessage = table.Column<string>(type: "text", nullable: true),
                    ErrorDetails = table.Column<string>(type: "text", nullable: true),
                    TraceCorrelationId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_automation_executions_automation_rules_RuleId",
                        column: x => x.RuleId,
                        principalSchema: "automation",
                        principalTable: "automation_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BranchName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    BranchManager = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_branches_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "branches",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inter_branch_transfer_lines",
                schema: "branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    DispatchedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(14,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inter_branch_transfer_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inter_branch_transfer_lines_inter_branch_transfers_Transfer~",
                        column: x => x.TransferRequestId,
                        principalSchema: "branches",
                        principalTable: "inter_branch_transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_actions",
                schema: "administration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ActedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_actions_approval_requests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalSchema: "administration",
                        principalTable: "approval_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "terminals",
                schema: "branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TerminalCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MachineName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OperatingSystem = table.Column<string>(type: "text", nullable: true),
                    AssignedCashierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAuthorized = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PosPrinterModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReceiptWidthMm = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terminals1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_terminals_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "branches",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_actions_ApprovalRequestId",
                schema: "administration",
                table: "approval_actions",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_CompanyId_Status",
                schema: "administration",
                table: "approval_requests",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_approval_requests_WorkflowId",
                schema: "administration",
                table: "approval_requests",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_workflows_CompanyId_Module_TransactionType",
                schema: "administration",
                table: "approval_workflows",
                columns: new[] { "CompanyId", "Module", "TransactionType" });

            migrationBuilder.CreateIndex(
                name: "IX_automation_executions_RuleId",
                schema: "automation",
                table: "automation_executions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_automation_rules_CompanyId_IsActive",
                schema: "automation",
                table: "automation_rules",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_branches_CompanyId_BranchCode",
                schema: "branches",
                table: "branches",
                columns: new[] { "CompanyId", "BranchCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inter_branch_transfer_lines_TransferRequestId",
                schema: "branches",
                table: "inter_branch_transfer_lines",
                column: "TransferRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_inter_branch_transfers_CompanyId_TransferNumber",
                schema: "branches",
                table: "inter_branch_transfers",
                columns: new[] { "CompanyId", "TransferNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CompanyId_IsRead_CreatedAtUtc",
                schema: "automation",
                table: "notifications",
                columns: new[] { "CompanyId", "IsRead", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_report_definitions_CompanyId_ReportCode",
                schema: "reporting",
                table: "report_definitions",
                columns: new[] { "CompanyId", "ReportCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_export_logs_CompanyId_ExportedAtUtc",
                schema: "reporting",
                table: "report_export_logs",
                columns: new[] { "CompanyId", "ExportedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_system_configurations_CompanyId_ConfigKey",
                schema: "system_management",
                table: "system_configurations",
                columns: new[] { "CompanyId", "ConfigKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_terminals_BranchId",
                schema: "branches",
                table: "terminals",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_terminals_CompanyId_TerminalCode",
                schema: "branches",
                table: "terminals",
                columns: new[] { "CompanyId", "TerminalCode" },
                unique: true);

            migrationBuilder.Sql("GRANT USAGE, CREATE ON SCHEMA administration, automation, system_management, branches, reporting TO magic_dev, magic_app;");
            migrationBuilder.Sql("GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA administration, automation, system_management, branches, reporting TO magic_dev, magic_app;");
            migrationBuilder.Sql("GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA administration, automation, system_management, branches, reporting TO magic_dev, magic_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_actions",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "automation_executions",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "backup_records",
                schema: "system_management");

            migrationBuilder.DropTable(
                name: "inter_branch_transfer_lines",
                schema: "branches");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "report_definitions",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "report_export_logs",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "system_configurations",
                schema: "system_management");

            migrationBuilder.DropTable(
                name: "terminals",
                schema: "branches");

            migrationBuilder.DropTable(
                name: "approval_requests",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "automation_rules",
                schema: "automation");

            migrationBuilder.DropTable(
                name: "inter_branch_transfers",
                schema: "branches");

            migrationBuilder.DropTable(
                name: "branches",
                schema: "branches");

            migrationBuilder.DropTable(
                name: "approval_workflows",
                schema: "administration");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "branches");
        }
    }
}
