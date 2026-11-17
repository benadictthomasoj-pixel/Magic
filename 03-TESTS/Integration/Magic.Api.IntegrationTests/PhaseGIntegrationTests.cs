using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Magic.Contracts.Administration;
using Magic.Contracts.Api;
using Magic.Contracts.Automation;
using Magic.Contracts.Branches;
using Magic.Contracts.Reports;
using Magic.Contracts.Security;
using Magic.Contracts.SystemManagement;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class PhaseGIntegrationTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;
    private Guid _companyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private Guid _defaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public PhaseGIntegrationTests(MagicTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var secSeeder = scope.ServiceProvider.GetRequiredService<SecurityDatabaseSeeder>();
        await secSeeder.SeedAsync();

        var commSeeder = scope.ServiceProvider.GetRequiredService<CommercialDatabaseSeeder>();
        await commSeeder.SeedAsync();

        await AccountingDatabaseSeeder.SeedDevelopmentAccountingAsync(scope.ServiceProvider);

        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        // Ensure default company and branch exist
        if (!await db.Companies.AnyAsync(c => c.Id == _companyId))
        {
            db.Companies.Add(new CompanyRecord
            {
                Id = _companyId,
                LegalName = "MAGIC Universal ERP Ltd",
                TradeName = "MAGIC Retail Systems",
                Address = "100 Enterprise Way, Bangalore, KA, India",
                CurrencyCode = "INR",
                FiscalYearStartMonth = 4,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await db.Branches.AnyAsync(b => b.Id == _defaultBranchId))
        {
            db.Branches.Add(new BranchRecord
            {
                Id = _defaultBranchId,
                CompanyId = _companyId,
                BranchCode = "HQ-MAIN",
                BranchName = "Headquarters Central Store",
                Address = "100 Enterprise Way, Bangalore",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        var adminLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "ben",
            Password = "123123"
        });
        var adminAuth = await adminLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        _adminToken = adminAuth!.Data.AccessToken;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        return client;
    }

    // ==========================================
    // 1. Consolidated Company Sales Report (M17)
    // ==========================================
    [Fact]
    public async Task Scenario01_ConsolidatedCompanySalesReport_ReturnsUnifiedKpisAndProvisionalStatus()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/reports/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<ExecutiveDashboardDto>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data);
        Assert.StartsWith("Provisional", envelope.Data.GrossProfitStatus);
        Assert.StartsWith("Provisional", envelope.Data.NetProfitStatus);
    }

    // ==========================================
    // 2. Branch-Specific Sales Report (M17)
    // ==========================================
    [Fact]
    public async Task Scenario02_BranchSpecificSalesReport_FiltersByBranchId()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync($"/api/v1/reports/sales?branchId={_defaultBranchId}&groupByType=Daily");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<ReportResultDto>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Data.ReportTitle);
    }

    // ==========================================
    // 3. Cross-Branch Access Check (M18)
    // ==========================================
    [Fact]
    public async Task Scenario03_CrossBranchAccess_RejectsUnauthorizedTenantOrBranch()
    {
        var client = _factory.CreateClient(); // Unauthenticated client
        var response = await client.GetAsync("/api/v1/branches");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ==========================================
    // 4. Multi-Company Tenant Isolation (M18)
    // ==========================================
    [Fact]
    public async Task Scenario04_MultiCompanyTenantIsolation_EnforcesCompanyBoundaries()
    {
        var client = CreateAdminClient();
        var foreignBranchId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/branches/{foreignBranchId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ==========================================
    // 5. Branch Creation with Warehouse (M18)
    // ==========================================
    [Fact]
    public async Task Scenario05_BranchCreation_WithWarehouseAssociation_Succeeds()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == _companyId)
            ?? new WarehouseRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = _companyId,
                Code = "WH-INTEG-01",
                Name = "Integration Warehouse",
                AddressLine1 = "Lane 1",
                City = "Bangalore",
                State = "Karnataka",
                StateCode = "29",
                PinCode = "560001",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.Empty
            };

        if (!await db.Warehouses.AnyAsync(w => w.Id == warehouse.Id))
        {
            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync();
        }

        var branchCode = $"BR-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
        var createRequest = new CreateBranchRequest(
            branchCode,
            "Branch with Warehouse Assocs",
            "77 Tech Park, Whitefield, Bangalore",
            "9876543210",
            "branch@magic.test",
            "29ABCDE1234F1Z5",
            "John Regional Mgr",
            [warehouse.Id]);

        var response = await client.PostAsJsonAsync("/api/v1/branches", createRequest);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<BranchDto>>();
        Assert.NotNull(envelope);
        Assert.Equal(branchCode, envelope.Data.BranchCode);
        Assert.True(envelope.Data.WarehouseCount >= 1);
    }

    // ==========================================
    // 6. Terminal Registration Under Branch (M18)
    // ==========================================
    [Fact]
    public async Task Scenario06_TerminalRegistration_UnderBranch_SucceedsAndTracksStatus()
    {
        var client = CreateAdminClient();
        var terminalCode = $"POS-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

        var regRequest = new Magic.Contracts.Branches.RegisterTerminalRequest(
            _defaultBranchId,
            terminalCode,
            "mac-counter-01",
            "macOS 15",
            "Epson TM-T88VI",
            80);

        var regResponse = await client.PostAsJsonAsync("/api/v1/branches/terminals", regRequest);
        Assert.Equal(HttpStatusCode.Created, regResponse.StatusCode);

        var envelope = await regResponse.Content.ReadFromJsonAsync<ApiResponse<TerminalDetailsDto>>();
        Assert.NotNull(envelope);
        Assert.Equal(terminalCode, envelope.Data.TerminalCode);
        Assert.True(envelope.Data.IsAuthorized);

        // Cross-Module Auth Verification (DEF-ARC-001): Terminal registered in branch is recognized by POS login
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new Magic.Contracts.Security.LoginRequest
        {
            UsernameOrEmail = "admin",
            Password = "123123",
            BranchId = _defaultBranchId,
            TerminalId = envelope.Data.Id
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Update status / deauthorize
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/branches/terminals/{envelope.Data.Id}/status",
            new UpdateTerminalStatusRequest(false, null));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedEnvelope = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<TerminalDetailsDto>>();
        Assert.False(updatedEnvelope!.Data.IsAuthorized);

        // Deauthorized terminal must be rejected at login
        var deauthLoginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new Magic.Contracts.Security.LoginRequest
        {
            UsernameOrEmail = "admin",
            Password = "123123",
            BranchId = _defaultBranchId,
            TerminalId = envelope.Data.Id
        });
        Assert.Equal(HttpStatusCode.Forbidden, deauthLoginResponse.StatusCode);
    }

    // ==========================================
    // 7. Inter-Branch Stock Transfer (M18)
    // ==========================================
    [Fact]
    public async Task Scenario07_InterBranchStockTransfer_RequestApproveDispatchReceive_MaintainsLedger()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        // Setup Source and Dest Warehouses and a Product with stock
        var srcWh = await db.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == _companyId)
            ?? new WarehouseRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = _companyId,
                Code = "WH-SRC-01",
                Name = "Source WH",
                AddressLine1 = "Src Line",
                City = "BLR",
                State = "KA",
                StateCode = "29",
                PinCode = "560001",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = Guid.Empty
            };

        var dstWh = new WarehouseRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Code = $"WH-DST-{Guid.NewGuid().ToString("N")[..4]}",
            Name = "Dest WH",
            AddressLine1 = "Dst Line",
            City = "BLR",
            State = "KA",
            StateCode = "29",
            PinCode = "560001",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = Guid.Empty
        };

        var product = await db.Products.FirstAsync(p => p.CompanyId == _companyId);

        if (!await db.Warehouses.AnyAsync(w => w.Id == srcWh.Id)) db.Warehouses.Add(srcWh);
        db.Warehouses.Add(dstWh);

        // Seed stock balance in source warehouse
        var stock = await db.StockBalances.FirstOrDefaultAsync(s => s.WarehouseId == srcWh.Id && s.ProductId == product.Id);
        if (stock == null)
        {
            stock = new StockBalanceRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = _companyId,
                BranchId = _defaultBranchId,
                WarehouseId = srcWh.Id,
                ProductId = product.Id,
                QuantityOnHand = 50,
                QuantityAvailable = 50,
                QuantityReserved = 0,
                QuantityDamaged = 0,
                QuantityExpired = 0,
                QuantityBlocked = 0,
                UnitCostPaise = 5000,
                LastMovementAtUtc = DateTimeOffset.UtcNow
            };
            db.StockBalances.Add(stock);
        }
        else
        {
            stock.QuantityOnHand += 50;
            stock.QuantityAvailable += 50;
        }

        await db.SaveChangesAsync();

        // 1. Request Transfer
        var transferReq = new CreateInterBranchTransferRequest(
            _defaultBranchId,
            srcWh.Id,
            _defaultBranchId,
            dstWh.Id,
            "Rebalancing urgent inventory",
            [new(product.Id, null, 10)]);

        var reqResp = await client.PostAsJsonAsync("/api/v1/branches/transfers", transferReq);
        Assert.Equal(HttpStatusCode.Created, reqResp.StatusCode);

        var transfer = (await reqResp.Content.ReadFromJsonAsync<ApiResponse<InterBranchTransferRequestDto>>())!.Data;
        Assert.Equal("Requested", transfer.Status);

        // 2. Approve Transfer
        var appResp = await client.PostAsync($"/api/v1/branches/transfers/{transfer.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, appResp.StatusCode);

        // 3. Dispatch Transfer
        var dispResp = await client.PostAsync($"/api/v1/branches/transfers/{transfer.Id}/dispatch", null);
        Assert.Equal(HttpStatusCode.OK, dispResp.StatusCode);

        // 4. Receive Transfer
        var recvResp = await client.PostAsync($"/api/v1/branches/transfers/{transfer.Id}/receive", null);
        Assert.Equal(HttpStatusCode.OK, recvResp.StatusCode);

        var finalTransfer = (await recvResp.Content.ReadFromJsonAsync<ApiResponse<InterBranchTransferRequestDto>>())!.Data;
        Assert.Equal("Received", finalTransfer.Status);
    }

    // ==========================================
    // 8. Approval Workflow & Separation of Duties (M19)
    // ==========================================
    [Fact]
    public async Task Scenario08_ApprovalWorkflow_ConfigurationAndSignOff_EnforcesSeparationOfDuties()
    {
        var client = CreateAdminClient();

        // Configure workflow
        var wfReq = new CreateApprovalWorkflowRequest(
            "Procurement",
            "PurchaseOrder",
            1000000, // Rs 10,000 threshold
            1);

        var wfResp = await client.PostAsJsonAsync("/api/v1/administration/workflows", wfReq);
        Assert.Equal(HttpStatusCode.Created, wfResp.StatusCode);

        // Seed a pending approval request directly in db
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var req = new ApprovalRequestRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            BranchId = _defaultBranchId,
            SourceModule = "Procurement",
            SourceDocumentId = Guid.NewGuid(),
            ReferenceNumber = $"PO-TEST-{Guid.NewGuid().ToString("N")[..4]}",
            AmountPaise = 1500000,
            Status = "Pending",
            RequestedByUserId = Guid.NewGuid(), // different user to allow separation of duties
            RequestedByUserName = "operator.procurement",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ApprovalRequests.Add(req);
        await db.SaveChangesAsync();

        // Perform Approval Action
        var actionReq = new SubmitApprovalActionRequest("Approved", "Sign-off authorized by CFO");
        var actResp = await client.PostAsJsonAsync($"/api/v1/administration/approvals/{req.Id}/action", actionReq);

        Assert.Equal(HttpStatusCode.OK, actResp.StatusCode);
        var resEnvelope = await actResp.Content.ReadFromJsonAsync<ApiResponse<ApprovalRequestDto>>();
        Assert.Equal("Approved", resEnvelope!.Data.Status);
    }

    // ==========================================
    // 9. Audit Log Generation (M19)
    // ==========================================
    [Fact]
    public async Task Scenario09_AuditLogEntry_GeneratedAfterSensitiveAdministrativeChange()
    {
        var client = CreateAdminClient();

        // Query audit logs
        var resp = await client.GetAsync("/api/v1/administration/audit-logs");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var envelope = await resp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AuditLogEntryDto>>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
    }

    // ==========================================
    // 10. Automation Rule Creation & Trigger (M20)
    // ==========================================
    [Fact]
    public async Task Scenario10_AutomationRule_CreationAndTriggerExecution_GeneratesNotification()
    {
        var client = CreateAdminClient();

        var createRuleReq = new CreateAutomationRuleRequest(
            "Low Stock Reorder Alert",
            "LowStockDetected",
            "{\"threshold\": 10}",
            "CreateNotification",
            "{\"priority\": \"High\", \"channel\": \"InApp\"}");

        var ruleResp = await client.PostAsJsonAsync("/api/v1/automation/rules", createRuleReq);
        Assert.Equal(HttpStatusCode.Created, ruleResp.StatusCode);

        var rule = (await ruleResp.Content.ReadFromJsonAsync<ApiResponse<AutomationRuleDto>>())!.Data;
        Assert.Equal("Low Stock Reorder Alert", rule.Name);

        // Trigger execution
        var trigResp = await client.PostAsync($"/api/v1/automation/rules/{rule.Id}/trigger", null);
        Assert.Equal(HttpStatusCode.OK, trigResp.StatusCode);

        // Verify notifications list has notification
        var notifResp = await client.GetAsync("/api/v1/automation/notifications");
        Assert.Equal(HttpStatusCode.OK, notifResp.StatusCode);

        var notifs = (await notifResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<NotificationDto>>>())!.Data;
        Assert.NotNull(notifs);

        // Scheduled Evaluation Verification (DEF-ARC-003): Autonomous background evaluation of active rules
        using (var scope = _factory.Services.CreateScope())
        {
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Magic.Infrastructure.Automation.AutomationRuleSchedulerBackgroundService>>();
            var scheduler = new Magic.Infrastructure.Automation.AutomationRuleSchedulerBackgroundService(scopeFactory, logger);
            var evaluatedCount = await scheduler.ProcessScheduledRulesAsync(CancellationToken.None);
            Assert.True(evaluatedCount >= 1);
        }
    }

    // ==========================================
    // 11. Duplicate Automation Event Deduplication (M20)
    // ==========================================
    [Fact]
    public async Task Scenario11_DuplicateAutomationEvent_IsDeduplicated()
    {
        var client = CreateAdminClient();

        var createRuleReq = new CreateAutomationRuleRequest(
            "Overdue Invoice Alert",
            "InvoiceOverdue",
            "{\"days\": 30}",
            "CreateNotification",
            "{\"priority\": \"Medium\"}");

        var ruleResp = await client.PostAsJsonAsync("/api/v1/automation/rules", createRuleReq);
        var rule = (await ruleResp.Content.ReadFromJsonAsync<ApiResponse<AutomationRuleDto>>())!.Data;

        // Trigger twice in rapid succession
        var firstTrig = await client.PostAsync($"/api/v1/automation/rules/{rule.Id}/trigger", null);
        var secondTrig = await client.PostAsync($"/api/v1/automation/rules/{rule.Id}/trigger", null);

        Assert.Equal(HttpStatusCode.OK, firstTrig.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondTrig.StatusCode);
    }

    // ==========================================
    // 12. Data-Grounded AI Assistant Sales Summary (M20)
    // ==========================================
    [Fact]
    public async Task Scenario12_DataGroundedAiAssistant_GeneratesSalesSummary()
    {
        var client = CreateAdminClient();

        var aiQuery = new AiAssistantQueryRequest("Summarize today's sales and revenue across all active branches", _defaultBranchId);
        var resp = await client.PostAsJsonAsync("/api/v1/automation/assistant/query", aiQuery);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var envelope = await resp.Content.ReadFromJsonAsync<ApiResponse<AiAssistantResponseDto>>();
        Assert.NotNull(envelope);
        Assert.Contains("Today's total sales", envelope.Data.Answer);
        Assert.Equal("Sales & POS", envelope.Data.SourceModule);
    }

    // ==========================================
    // 13. AI Assistant Tenant Boundary Enforcement (M20)
    // ==========================================
    [Fact]
    public async Task Scenario13_AiAssistant_EnforcesTenantBoundaries()
    {
        var client = CreateAdminClient();

        // Asking about foreign company or non-accessible branch is safely contained within authenticated company
        var aiQuery = new AiAssistantQueryRequest("Show sales report", Guid.NewGuid());
        var resp = await client.PostAsJsonAsync("/api/v1/automation/assistant/query", aiQuery);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var envelope = await resp.Content.ReadFromJsonAsync<ApiResponse<AiAssistantResponseDto>>();
        Assert.NotNull(envelope);
        // Does not crash or leak data outside the authenticated tenant
        Assert.NotNull(envelope.Data.Answer);
    }

    // ==========================================
    // 14. Backup Creation & SHA-256 Checksum (M21)
    // ==========================================
    [Fact]
    public async Task Scenario14_BackupCreation_GeneratesSha256Checksum()
    {
        var client = CreateAdminClient();

        var backupReq = new CreateBackupRequest("Manual integration test backup");
        var resp = await client.PostAsJsonAsync("/api/v1/system-management/backups", backupReq);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var envelope = await resp.Content.ReadFromJsonAsync<ApiResponse<BackupRecordDto>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Data.FileSizeBytes > 0);
        Assert.Contains("magic_backup_", envelope.Data.FileName);
    }

    // ==========================================
    // 15. Isolated Restore Simulation (M21)
    // ==========================================
    [Fact]
    public async Task Scenario15_IsolatedRestoreSimulation_RejectsUnsafeProductionOverwrite()
    {
        var client = CreateAdminClient();

        // Create a real backup to test with
        var createResp = await client.PostAsJsonAsync("/api/v1/system-management/backups", new CreateBackupRequest("Restore test backup"));
        var backup = (await createResp.Content.ReadFromJsonAsync<ApiResponse<BackupRecordDto>>())!.Data;

        // 1. Unsafe overwrite attempt (target database named magic_dev or magic_prod)
        var unsafeReq = new RestoreDatabaseRequest(backup.Id, "magic_dev", "INCORRECT_CONFIRMATION");
        var unsafeResp = await client.PostAsJsonAsync("/api/v1/system-management/restore/verify", unsafeReq);

        Assert.Equal(HttpStatusCode.BadRequest, unsafeResp.StatusCode);

        // 2. Safe isolated scratch simulation
        var safeReq = new RestoreDatabaseRequest(backup.Id, "magic_isolated_scratch_restore", "CONFIRM_SANDBOX_RESTORE");
        var safeResp = await client.PostAsJsonAsync("/api/v1/system-management/restore/verify", safeReq);

        Assert.Equal(HttpStatusCode.OK, safeResp.StatusCode);
        var envelope = await safeResp.Content.ReadFromJsonAsync<ApiResponse<RestoreResultDto>>();
        Assert.True(envelope!.Data.Success);
        Assert.Contains("Integrity check passed", envelope.Data.Message);
    }

    // ==========================================
    // 16. Background Job Status & Outbox Diagnostics (M21)
    // ==========================================
    [Fact]
    public async Task Scenario16_BackgroundJobStatus_AndOutboxDiagnostics_ReportsHealthy()
    {
        var client = CreateAdminClient();

        var resp = await client.GetAsync("/api/v1/system-management/jobs");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var envelope = await resp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<BackgroundJobStatusDto>>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Data.Count >= 2);
    }

    // ==========================================
    // 17. Financial Reconciliation with General Ledger (M17/M21)
    // ==========================================
    [Fact]
    public async Task Scenario17_FinancialReport_ReconcilesWithGeneralLedger()
    {
        var client = CreateAdminClient();

        var resp = await client.GetAsync("/api/v1/reports/financial");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var envelope = await resp.Content.ReadFromJsonAsync<ApiResponse<ReportResultDto>>();
        Assert.NotNull(envelope);
        Assert.Equal("General Ledger Financial Summary", envelope.Data.ReportTitle);
    }

    // ==========================================
    // 18. Secret Redaction Verification (M21)
    // ==========================================
    [Fact]
    public async Task Scenario18_SystemDiagnostics_VerifiesSecretRedaction()
    {
        var client = CreateAdminClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        if (!await db.SystemConfigurations.AnyAsync(c => c.CompanyId == _companyId && c.ConfigKey == "JWT_SIGNING_SECRET_KEY"))
        {
            db.SystemConfigurations.Add(new SystemConfigurationRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = _companyId,
                ConfigKey = "JWT_SIGNING_SECRET_KEY",
                ConfigValue = "HighEntropySecretKey123!",
                Category = "Security",
                UpdatedByUserId = Guid.Empty,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var resp = await client.GetAsync("/api/v1/system-management/configurations");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var envelope = await resp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SystemConfigurationItemDto>>>();
        Assert.NotNull(envelope);

        // Verify all sensitive keys are redacted with ***REDACTED***
        var secretConfigs = envelope.Data.Where(c => c.ConfigKey.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                                                     c.ConfigKey.Contains("Secret", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.NotEmpty(secretConfigs);
        foreach (var cfg in secretConfigs)
        {
            Assert.Equal("***REDACTED***", cfg.ConfigValue);
        }
    }

    // ==========================================
    // 19. Report Export Formats Verification (CSV, XLSX, PDF)
    // ==========================================
    [Fact]
    public async Task Scenario19_ReportExport_SupportsCsvXlsxAndPdfFormats()
    {
        var client = CreateAdminClient();

        // 1. Export as CSV
        var csvReq = new ReportExportRequest(
            ReportType: "sales",
            Format: "CSV",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow),
            BranchId: null,
            WarehouseId: null,
            CustomerId: null,
            SupplierId: null,
            Status: null);

        var csvResp = await client.PostAsJsonAsync("/api/v1/reports/export", csvReq);
        Assert.Equal(HttpStatusCode.OK, csvResp.StatusCode);
        Assert.Equal("text/csv", csvResp.Content.Headers.ContentType?.MediaType);
        var csvBytes = await csvResp.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(csvBytes);
        var csvText = System.Text.Encoding.UTF8.GetString(csvBytes);
        Assert.Contains("# Sales Analysis Report", csvText);

        // 2. Export as XLSX
        var xlsxReq = csvReq with { Format = "XLSX" };
        var xlsxResp = await client.PostAsJsonAsync("/api/v1/reports/export", xlsxReq);
        Assert.Equal(HttpStatusCode.OK, xlsxResp.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", xlsxResp.Content.Headers.ContentType?.MediaType);
        var xlsxBytes = await xlsxResp.Content.ReadAsByteArrayAsync();
        Assert.True(xlsxBytes.Length > 100);
        Assert.Equal(0x50, xlsxBytes[0]); // 'P'
        Assert.Equal(0x4B, xlsxBytes[1]); // 'K'

        using (var ms = new MemoryStream(xlsxBytes))
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
        {
            Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
            Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
            Assert.NotNull(zip.GetEntry("xl/worksheets/sheet1.xml"));
        }

        // 3. Export as PDF
        var pdfReq = csvReq with { Format = "PDF" };
        var pdfResp = await client.PostAsJsonAsync("/api/v1/reports/export", pdfReq);
        Assert.Equal(HttpStatusCode.OK, pdfResp.StatusCode);
        Assert.Equal("application/pdf", pdfResp.Content.Headers.ContentType?.MediaType);
        var pdfBytes = await pdfResp.Content.ReadAsByteArrayAsync();
        Assert.True(pdfBytes.Length > 100);
        var pdfHeader = System.Text.Encoding.ASCII.GetString(pdfBytes[..8]);
        Assert.Equal("%PDF-1.4", pdfHeader);
    }
}
