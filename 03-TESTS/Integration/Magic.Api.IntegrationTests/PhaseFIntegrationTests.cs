using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Magic.Api.Controllers.Orders;
using Magic.Contracts.Api;
using Magic.Contracts.Expenses;
using Magic.Contracts.Hr;
using Magic.Contracts.Marketing;
using Magic.Contracts.Orders;
using Magic.Contracts.Payroll;
using Magic.Contracts.Security;
using Magic.Contracts.Services;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class PhaseFIntegrationTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;

    public PhaseFIntegrationTests(MagicTestFixture factory)
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
        var defaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var defaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var adminUser = await db.Users.FirstAsync();

        // 1. Seed Expense Categories if none
        if (!await db.ExpenseCategories.AnyAsync(c => c.CompanyId == defaultCompanyId))
        {
            var glAcc = await db.Accounts.FirstOrDefaultAsync(a => a.CompanyId == defaultCompanyId && a.Code == "5201")
                ?? await db.Accounts.FirstAsync(a => a.CompanyId == defaultCompanyId);
            db.ExpenseCategories.Add(new ExpenseCategoryRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = defaultCompanyId,
                Code = "EXP-OFFICE",
                Name = "Office Supplies & Maintenance",
                DefaultGlAccountId = glAcc.Id,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        // 2. Seed Petty Cash Account if none
        if (!await db.PettyCashAccounts.AnyAsync(p => p.CompanyId == defaultCompanyId))
        {
            var glCash = await db.Accounts.FirstOrDefaultAsync(a => a.CompanyId == defaultCompanyId && a.Code == "1111");
            db.PettyCashAccounts.Add(new PettyCashAccountRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = defaultCompanyId,
                BranchId = defaultBranchId,
                AccountName = "Main Branch Petty Cash Float",
                FloatLimitPaise = 500000,
                CurrentBalancePaise = 500000,
                GlAccountId = glCash?.Id ?? Guid.Empty,
                CustodianUserId = adminUser.Id,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        // 3. Seed Leave Types if none
        if (!await db.LeaveTypes.AnyAsync(l => l.CompanyId == defaultCompanyId))
        {
            db.LeaveTypes.AddRange(
                new LeaveTypeRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = defaultCompanyId,
                    Code = "CL",
                    Name = "Casual Leave",
                    AnnualDays = 12,
                    IsPaid = true,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                },
                new LeaveTypeRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = defaultCompanyId,
                    Code = "SL",
                    Name = "Sick Leave",
                    AnnualDays = 12,
                    IsPaid = true,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                }
            );
        }

        // 4. Seed Salary Structure if none
        if (!await db.SalaryStructures.AnyAsync(s => s.CompanyId == defaultCompanyId))
        {
            var structId = Guid.NewGuid();
            db.SalaryStructures.Add(new SalaryStructureRecord
            {
                Id = structId,
                CompanyId = defaultCompanyId,
                Name = "General Staff Structure",
                Description = "Standard operational salary package",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Components =
                [
                    new SalaryStructureComponentRecord
                    {
                        Id = Guid.NewGuid(),
                        SalaryStructureId = structId,
                        Name = "Basic Salary",
                        ComponentType = "Basic",
                        CalculationType = "Fixed",
                        DefaultValue = 20000,
                        IsDeduction = false,
                        IsEmployerContribution = false
                    }
                ]
            });
        }

        // 5. Seed Service Catalog if none
        if (!await db.ServiceCatalogs.AnyAsync(c => c.CompanyId == defaultCompanyId))
        {
            db.ServiceCatalogs.Add(new ServiceCatalogRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = defaultCompanyId,
                Code = "SRV-HARDWARE",
                Name = "Hardware Diagnostic & Component Replacement",
                Category = "Repair",
                Description = "Bench repair and overhaul for electronics and POS hardware",
                StandardLaborChargePaise = 150000,
                EstimatedDurationMinutes = 120,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
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
    // Workflow A: Expense Lifecycle & GL Posting
    // ==========================================
    [Fact]
    public async Task WorkflowA_ExpenseLifecycle_CreatesApprovesAndPostsGLVoucher()
    {
        var client = CreateAdminClient();

        // 1. Get Categories
        var catRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<ExpenseCategoryDto>>>("/api/v1/expenses/categories");
        Assert.NotNull(catRes?.Data);
        var category = catRes.Data[0];

        // 2. Create Expense
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createRes = await client.PostAsJsonAsync("/api/v1/expenses", new CreateExpenseRequest(
            category.Id,
            GlAccountId: null,
            VendorId: null,
            PaymentMethod: "Cash",
            BankCashAccountId: null,
            ExpenseDate: today,
            AmountPaise: 250000,
            TaxAmountPaise: 0,
            CostCenter: "Admin",
            ReferenceNumber: "REC-EXP-01",
            Description: "Office Stationery and Supplies",
            AttachmentPath: null));

        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var expense = (await createRes.Content.ReadFromJsonAsync<ApiResponse<ExpenseDto>>())!.Data;
        Assert.Equal("Submitted", expense.Status);

        // 3. Approve Expense
        var approveRes = await client.PostAsync($"/api/v1/expenses/{expense.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);
        var approved = (await approveRes.Content.ReadFromJsonAsync<ApiResponse<ExpenseDto>>())!.Data;
        Assert.Equal("Approved", approved.Status);

        // 4. Pay Expense (Posts Journal Voucher)
        var payRes = await client.PostAsJsonAsync($"/api/v1/expenses/{expense.Id}/pay", new PayExpenseRequest(
            PaymentMethod: "Cash",
            BankCashAccountId: null,
            PaymentDate: today,
            ReferenceNumber: "CASH-DISB-01"));

        Assert.Equal(HttpStatusCode.OK, payRes.StatusCode);
        var paid = (await payRes.Content.ReadFromJsonAsync<ApiResponse<ExpenseDto>>())!.Data;
        Assert.Equal("Paid", paid.Status);
        Assert.NotNull(paid.JournalVoucherId);

        // 5. Verify GL Voucher in Persistence
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var voucher = await db.Vouchers
            .Include(v => v.Lines)
            .FirstOrDefaultAsync(v => v.Id == paid.JournalVoucherId.Value);

        Assert.NotNull(voucher);
        Assert.True(voucher.IsBalanced);
        Assert.Equal(250000, voucher.TotalDebitPaise);
        Assert.Equal(250000, voucher.TotalCreditPaise);
    }

    // ==========================================
    // Workflow B: Expense Rejection
    // ==========================================
    [Fact]
    public async Task WorkflowB_ExpenseRejection_RejectsWithoutPostingGL()
    {
        var client = CreateAdminClient();
        var catRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<ExpenseCategoryDto>>>("/api/v1/expenses/categories");
        var category = catRes!.Data[0];

        var createRes = await client.PostAsJsonAsync("/api/v1/expenses", new CreateExpenseRequest(
            category.Id,
            GlAccountId: null,
            VendorId: null,
            PaymentMethod: "Cash",
            BankCashAccountId: null,
            ExpenseDate: DateOnly.FromDateTime(DateTime.UtcNow),
            AmountPaise: 900000,
            TaxAmountPaise: 0,
            CostCenter: "Admin",
            ReferenceNumber: "REC-REJ-01",
            Description: "Excessive Entertainment Expense",
            AttachmentPath: null));

        var expense = (await createRes.Content.ReadFromJsonAsync<ApiResponse<ExpenseDto>>())!.Data;

        // Reject
        var rejectRes = await client.PostAsJsonAsync($"/api/v1/expenses/{expense.Id}/reject", new RejectExpenseRequest("Budget exceeded for Q3"));
        Assert.Equal(HttpStatusCode.OK, rejectRes.StatusCode);

        var rejected = (await rejectRes.Content.ReadFromJsonAsync<ApiResponse<ExpenseDto>>())!.Data;
        Assert.Equal("Rejected", rejected.Status);
        Assert.Equal("Budget exceeded for Q3", rejected.RejectionReason);
        Assert.Null(rejected.JournalVoucherId);
    }

    // ==========================================
    // Workflow C: Petty Cash Management
    // ==========================================
    [Fact]
    public async Task WorkflowC_PettyCashLifecycle_ReplenishesAndReconciles()
    {
        var client = CreateAdminClient();

        // 1. Get Accounts
        var accsRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<PettyCashAccountDto>>>("/api/v1/expenses/petty-cash");
        Assert.NotNull(accsRes?.Data);
        var pcAccount = accsRes.Data[0];

        // 2. Get bank account for replenishment
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var bankAcc = await db.BankCashAccounts.FirstAsync(b => b.CompanyId == pcAccount.CompanyId);

        // 3. Replenish Float
        var replRes = await client.PostAsJsonAsync($"/api/v1/expenses/petty-cash/{pcAccount.Id}/replenish", new ReplenishPettyCashRequest(
            bankAcc.Id,
            AmountPaise: 200000,
            Notes: "Monthly replenishment"));

        Assert.Equal(HttpStatusCode.OK, replRes.StatusCode);
        var tx = (await replRes.Content.ReadFromJsonAsync<ApiResponse<PettyCashTransactionDto>>())!.Data;
        Assert.Equal("Replenishment", tx.TransactionType);

        // 4. Reconcile Float
        var recRes = await client.PostAsJsonAsync($"/api/v1/expenses/petty-cash/{pcAccount.Id}/reconcile", new ReconcilePettyCashRequest(
            ActualCountPaise: tx.BalanceAfterPaise,
            Notes: "Mid-month physical cash verification"));

        Assert.Equal(HttpStatusCode.OK, recRes.StatusCode);
    }

    // ==========================================
    // Workflow D: Employee Onboarding & Lifecycle
    // ==========================================
    [Fact]
    public async Task WorkflowD_EmployeeOnboarding_CreatesAndUpdatesEmployee()
    {
        var client = CreateAdminClient();
        var uniqueCode = $"EMP-TEST-{Guid.NewGuid():N}"[..12];

        // 1. Create Employee
        var createRes = await client.PostAsJsonAsync("/api/v1/hr/employees", new CreateEmployeeRequest(
            EmployeeCode: uniqueCode,
            FirstName: "Siddharth",
            LastName: "Sharma",
            Email: $"{uniqueCode}@example.com",
            Phone: "9876543210",
            Address: "123 Indiranagar, Bangalore",
            JoiningDate: new DateOnly(2026, 4, 1),
            EmploymentType: "Permanent",
            DepartmentId: null,
            DesignationId: null,
            ReportingManagerId: null,
            EmergencyContactName: "Pooja Sharma",
            EmergencyContactPhone: "9876543211",
            BankAccountNumber: "9192939495",
            BankIfsc: "HDFC0001234",
            BankName: "HDFC Bank",
            PanNumber: "ABCDE1234F",
            LinkedUserId: null));

        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var emp = (await createRes.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>())!.Data;
        Assert.Equal(uniqueCode.ToUpperInvariant(), emp.EmployeeCode);
        Assert.Equal("Active", emp.Status);

        // 2. Fetch Employee
        var getRes = await client.GetFromJsonAsync<ApiResponse<EmployeeDto>>($"/api/v1/hr/employees/{emp.Id}");
        Assert.Equal(emp.Email, getRes!.Data.Email);
    }

    // ==========================================
    // Workflow E: Attendance Punch & Manual Correction
    // ==========================================
    [Fact]
    public async Task WorkflowE_AttendancePunchAndCorrection_RecordsAndAudits()
    {
        var client = CreateAdminClient();
        var empRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>("/api/v1/hr/employees");
        var employee = empRes!.Data[0];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Punch In
        var punchInRes = await client.PostAsJsonAsync("/api/v1/hr/attendance/punch", new PunchAttendanceRequest(
            employee.Id,
            "CheckIn",
            DateTimeOffset.UtcNow,
            "Shift Start"));
        Assert.Equal(HttpStatusCode.Created, punchInRes.StatusCode);

        // 2. Punch Out
        var punchOutRes = await client.PostAsJsonAsync("/api/v1/hr/attendance/punch", new PunchAttendanceRequest(
            employee.Id,
            "CheckOut",
            DateTimeOffset.UtcNow.AddHours(8),
            "Shift End"));
        Assert.Equal(HttpStatusCode.Created, punchOutRes.StatusCode);

        // 3. Supervisor Correction
        var correctRes = await client.PostAsJsonAsync("/api/v1/hr/attendance/correct", new CorrectAttendanceRequest(
            employee.Id,
            today,
            Status: "Present",
            CheckInTimeUtc: DateTimeOffset.UtcNow.AddHours(-1),
            CheckOutTimeUtc: DateTimeOffset.UtcNow.AddHours(8),
            CorrectionNotes: "Biometric device offline during morning rush"));

        Assert.Equal(HttpStatusCode.OK, correctRes.StatusCode);
        var corrected = (await correctRes.Content.ReadFromJsonAsync<ApiResponse<AttendanceDto>>())!.Data;
        Assert.True(corrected.IsCorrected);
    }

    // ==========================================
    // Workflow F: Leave Management Workflow
    // ==========================================
    [Fact]
    public async Task WorkflowF_LeaveWorkflow_SubmitsAndApproves()
    {
        var client = CreateAdminClient();
        var empRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>("/api/v1/hr/employees");
        var employee = empRes!.Data[0];

        var typesRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<LeaveTypeDto>>>("/api/v1/hr/leaves/types");
        var leaveType = typesRes!.Data[0];

        // Ensure quota is replenished for test isolation
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
            var bal = await db.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == employee.Id && b.LeaveTypeId == leaveType.Id);
            if (bal == null)
            {
                db.LeaveBalances.Add(new LeaveBalanceRecord
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    LeaveTypeId = leaveType.Id,
                    FiscalYearId = Guid.Empty,
                    TotalDays = 30,
                    ConsumedDays = 0,
                    PendingDays = 0
                });
            }
            else
            {
                bal.TotalDays = 30;
                bal.ConsumedDays = 0;
                bal.PendingDays = 0;
            }
            await db.SaveChangesAsync();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Apply Leave
        var applyRes = await client.PostAsJsonAsync("/api/v1/hr/leaves/apply", new SubmitLeaveApplicationRequest(
            employee.Id,
            leaveType.Id,
            StartDate: today.AddDays(5),
            EndDate: today.AddDays(6),
            TotalDays: 2m,
            Reason: "Family medical emergency"));

        Assert.Equal(HttpStatusCode.Created, applyRes.StatusCode);
        var application = (await applyRes.Content.ReadFromJsonAsync<ApiResponse<LeaveApplicationDto>>())!.Data;
        Assert.Equal("Applied", application.Status);

        // 2. Approve Leave
        var approveRes = await client.PostAsync($"/api/v1/hr/leaves/applications/{application.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);
        var approved = (await approveRes.Content.ReadFromJsonAsync<ApiResponse<LeaveApplicationDto>>())!.Data;
        Assert.Equal("Approved", approved.Status);
    }

    // ==========================================
    // Workflow G: Salary Structure & Payroll Calculation
    // ==========================================
    [Fact]
    public async Task WorkflowG_SalaryStructureAndCalculation_ComputesAttendanceAdjustedPay()
    {
        var client = CreateAdminClient();
        var empRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>("/api/v1/hr/employees");
        var employee = empRes!.Data[0];

        // 1. Create Structure
        var createStructRes = await client.PostAsJsonAsync("/api/v1/payroll/structures", new CreateSalaryStructureRequest(
            Name: $"Executive Band {Guid.NewGuid():N}"[..20],
            Description: "Standard executive CTC structure",
            Components:
            [
                new("Basic Salary", "Earning", "Percentage", 50m, false, false),
                new("HRA", "Earning", "Percentage", 30m, false, false),
                new("Special Allowance", "Earning", "Percentage", 20m, false, false)
            ]));

        Assert.Equal(HttpStatusCode.Created, createStructRes.StatusCode);
        var structure = (await createStructRes.Content.ReadFromJsonAsync<ApiResponse<SalaryStructureDto>>())!.Data;

        // 2. Assign to Employee
        var assignRes = await client.PostAsJsonAsync("/api/v1/payroll/assign", new AssignEmployeeSalaryRequest(
            employee.Id,
            structure.Id,
            MonthlyGrossPaise: 6000000, // 60,000 INR
            EffectiveFrom: new DateOnly(2026, 4, 1)));

        Assert.Equal(HttpStatusCode.Created, assignRes.StatusCode);

        // 3. Clean up and Calculate Payroll for Month 9 (September 2026)
        using (var cleanScope = _factory.Services.CreateScope())
        {
            var cleanDb = cleanScope.ServiceProvider.GetRequiredService<MagicDbContext>();
            var existingPeriods = await cleanDb.PayrollPeriods.Include(p => p.Items).Where(p => p.Year == 2026 && p.Month == 9).ToListAsync();
            if (existingPeriods.Count > 0)
            {
                cleanDb.PayrollItems.RemoveRange(existingPeriods.SelectMany(p => p.Items));
                cleanDb.PayrollPeriods.RemoveRange(existingPeriods);
                await cleanDb.SaveChangesAsync();
            }
        }

        var calcRes = await client.PostAsJsonAsync("/api/v1/payroll/calculate", new CalculatePayrollRequest(9, 2026, null));
        Assert.Equal(HttpStatusCode.Created, calcRes.StatusCode);
        var period = (await calcRes.Content.ReadFromJsonAsync<ApiResponse<PayrollPeriodDto>>())!.Data;

        Assert.Equal("Calculated", period.Status);
        Assert.True(period.TotalGrossPaise > 0);
        Assert.True(period.TotalNetPaise > 0);
    }

    // ==========================================
    // Workflow H: Payroll Accrual & Disbursement
    // ==========================================
    [Fact]
    public async Task WorkflowH_PayrollAccrualAndDisbursement_PostsGLVouchers()
    {
        var client = CreateAdminClient();

        // Clean up and Calculate Payroll for Month 8 (August 2026)
        using (var cleanScope = _factory.Services.CreateScope())
        {
            var cleanDb = cleanScope.ServiceProvider.GetRequiredService<MagicDbContext>();
            var existingPeriods = await cleanDb.PayrollPeriods.Include(p => p.Items).Where(p => p.Year == 2026 && p.Month == 8).ToListAsync();
            if (existingPeriods.Count > 0)
            {
                cleanDb.PayrollItems.RemoveRange(existingPeriods.SelectMany(p => p.Items));
                cleanDb.PayrollPeriods.RemoveRange(existingPeriods);
                await cleanDb.SaveChangesAsync();
            }
        }

        var calcRes = await client.PostAsJsonAsync("/api/v1/payroll/calculate", new CalculatePayrollRequest(8, 2026, null));
        Assert.Equal(HttpStatusCode.Created, calcRes.StatusCode);
        var period = (await calcRes.Content.ReadFromJsonAsync<ApiResponse<PayrollPeriodDto>>())!.Data;

        // 2. Approve
        var appRes = await client.PostAsync($"/api/v1/payroll/periods/{period.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);

        // 3. Finalize & Post GL Accrual Voucher (Debit 5210 Salaries, Credit 2130 Net Payable)
        var finRes = await client.PostAsync($"/api/v1/payroll/periods/{period.Id}/finalize", null);
        Assert.Equal(HttpStatusCode.OK, finRes.StatusCode);
        var finalized = (await finRes.Content.ReadFromJsonAsync<ApiResponse<PayrollPeriodDto>>())!.Data;

        Assert.Equal("Finalized", finalized.Status);
        Assert.NotNull(finalized.JournalVoucherId);

        // Verify Accrual Voucher in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
            var accrualVoucher = await db.Vouchers
                .Include(v => v.Lines)
                .FirstOrDefaultAsync(v => v.Id == finalized.JournalVoucherId.Value);

            Assert.NotNull(accrualVoucher);
            Assert.True(accrualVoucher.IsBalanced);
        }

        // 4. Disburse Payroll via Bank
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
            var bankAcc = await db.BankCashAccounts.FirstAsync(b => b.CompanyId == finalized.CompanyId);

            var disbRes = await client.PostAsJsonAsync($"/api/v1/payroll/periods/{period.Id}/disburse", new DisbursePayrollRequest(
                bankAcc.Id,
                DisbursementDate: new DateOnly(2026, 11, 30),
                PaymentReference: "NEFT-BATCH-NOV26"));

            Assert.Equal(HttpStatusCode.OK, disbRes.StatusCode);
            var disbursed = (await disbRes.Content.ReadFromJsonAsync<ApiResponse<PayrollPeriodDto>>())!.Data;

            Assert.Equal("Paid", disbursed.Status);
            Assert.NotNull(disbursed.DisbursementVoucherId);
        }
    }

    // ==========================================
    // Workflow I: Marketing Campaign & Coupon Validation
    // ==========================================
    [Fact]
    public async Task WorkflowI_MarketingCampaignAndCoupon_ValidatesDiscountAtPOS()
    {
        var client = CreateAdminClient();
        var uniqueCoupon = $"DISC{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        // 1. Create Campaign
        var campRes = await client.PostAsJsonAsync("/api/v1/marketing/campaigns", new CreateCampaignRequest(
            Name: "Holiday Festival 2026",
            Description: "Festive promotion",
            TargetSegmentId: null,
            StartDate: new DateOnly(2026, 10, 1),
            EndDate: new DateOnly(2026, 12, 31),
            BudgetPaise: 5000000));

        Assert.Equal(HttpStatusCode.Created, campRes.StatusCode);

        // 2. Create 10% Coupon with Min Order 1000 INR
        var couponRes = await client.PostAsJsonAsync("/api/v1/marketing/coupons", new CreatePromotionCouponRequest(
            Code: uniqueCoupon,
            CampaignId: null,
            DiscountType: "Percentage",
            DiscountValue: 1000,
            MinOrderAmountPaise: 100000,
            MaxDiscountAmountPaise: 50000,
            ValidFrom: DateTimeOffset.UtcNow.AddDays(-1),
            ValidTo: DateTimeOffset.UtcNow.AddDays(30),
            UsageLimitTotal: 100,
            UsageLimitPerCustomer: 1));

        Assert.Equal(HttpStatusCode.Created, couponRes.StatusCode);

        // 3. Validate at POS with 2000 INR order -> Expected 200 INR (20,000 paise) discount
        var valRes = await client.PostAsJsonAsync("/api/v1/marketing/coupons/validate", new ValidateCouponRequest(
            uniqueCoupon,
            CustomerId: Guid.NewGuid(),
            OrderSubtotalPaise: 200000));

        Assert.Equal(HttpStatusCode.OK, valRes.StatusCode);
        var valResult = (await valRes.Content.ReadFromJsonAsync<ApiResponse<ValidateCouponResponse>>())!.Data;

        Assert.True(valResult.IsValid);
        Assert.Equal(20000, valResult.CalculatedDiscountPaise);
    }

    // ==========================================
    // Workflow J: Advanced Sales Order to Fulfillment & Invoice
    // ==========================================
    [Fact]
    public async Task WorkflowJ_SalesOrderLifecycle_FulfillsAndConvertsToInvoice()
    {
        var client = CreateAdminClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db.Customers.FirstAsync();
        var product = await db.Products.FirstAsync(p => p.IsActive);
        var warehouse = await db.Warehouses.FirstAsync(w => w.IsActive);

        // Ensure stock exists in warehouse for fulfillment
        var stock = await db.StockBalances.FirstOrDefaultAsync(b => b.WarehouseId == warehouse.Id && b.ProductId == product.Id);
        if (stock == null)
        {
            db.StockBalances.Add(new StockBalanceRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = customer.CompanyId,
                BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                WarehouseId = warehouse.Id,
                ProductId = product.Id,
                QuantityOnHand = 100,
                QuantityAvailable = 100,
                QuantityReserved = 0,
                QuantityDamaged = 0,
                QuantityExpired = 0,
                QuantityBlocked = 0,
                UnitCostPaise = 50000,
                LastMovementAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
        else
        {
            stock.QuantityOnHand += 50;
            stock.QuantityAvailable += 50;
            await db.SaveChangesAsync();
        }

        // 1. Create Sales Order
        var createOrdRes = await client.PostAsJsonAsync("/api/v1/orders", new CreateSalesOrderRequest(
            customer.Id,
            warehouse.Id,
            OrderDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ExpectedDeliveryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            DeliveryAddress: "100 MG Road, Bangalore",
            Notes: "Priority Corporate Order",
            Lines:
            [
                new(product.Id, VariantId: null, Quantity: 5, UnitPricePaise: 100000, DiscountAmountPaise: 0, TaxRatePercent: 18m)
            ]));

        Assert.Equal(HttpStatusCode.Created, createOrdRes.StatusCode);
        var order = (await createOrdRes.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data;
        Assert.Equal("Submitted", order.Status);

        // 2. Confirm Order
        var confRes = await client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confRes.StatusCode);

        // 3. Fulfill Order
        var ordLine = order.Lines[0];
        var fulfillRes = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/fulfill", new FulfillOrderRequest(
            CarrierName: "DTDC Courier",
            TrackingNumber: "TRK-994821",
            Lines: [new(ordLine.Id, QuantityDispatched: 5)]));

        Assert.Equal(HttpStatusCode.Created, fulfillRes.StatusCode);

        // 4. Convert Order to Sales Invoice
        var invRes = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/invoice", new ConvertOrderToInvoiceApiRequest("Cash"));
        Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);
        var invId = (await invRes.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;
        Assert.NotEqual(Guid.Empty, invId);

        // 5. Verify Sales Invoice and Accounting Voucher in DB
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var invoice = await verifyDb.SalesInvoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == invId);
        Assert.NotNull(invoice);
        Assert.NotNull(invoice.AccountingVoucherId);

        var voucher = await verifyDb.Vouchers.Include(v => v.Lines).FirstOrDefaultAsync(v => v.Id == invoice.AccountingVoucherId.Value);
        Assert.NotNull(voucher);
        Assert.True(voucher.IsBalanced);
    }

    // ==========================================
    // Workflow K: Service Ticket, Quotation, Parts & Billing
    // ==========================================
    [Fact]
    public async Task WorkflowK_ServiceDeskLifecycle_CompletesRepairAndInvoices()
    {
        var client = CreateAdminClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db.Customers.FirstAsync();
        var product = await db.Products.FirstAsync(p => p.IsActive);
        var warehouse = await db.Warehouses.FirstAsync(w => w.IsActive);
        var catalog = await db.ServiceCatalogs.FirstAsync();

        // 1. Create Service Job Ticket
        var createReqRes = await client.PostAsJsonAsync("/api/v1/services/requests", new CreateServiceRequest(
            customer.Id,
            catalog.Id,
            ItemDescription: "Thermal Receipt Printer RP-300",
            SerialNumber: "SN-998822",
            ReportedIssue: "Gear motor failure and sensor alignment",
            Priority: "High",
            ReceivedDate: DateOnly.FromDateTime(DateTime.UtcNow),
            TargetCompletionDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))));

        Assert.Equal(HttpStatusCode.Created, createReqRes.StatusCode);
        var serviceReq = (await createReqRes.Content.ReadFromJsonAsync<ApiResponse<ServiceRequestDto>>())!.Data;
        Assert.Equal("NewRequest", serviceReq.Status);

        // 2. Draft Repair Quotation
        var quoteRes = await client.PostAsJsonAsync($"/api/v1/services/requests/{serviceReq.Id}/quotations", new CreateServiceQuotationRequest(
            ValidUntil: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            DiscountPaise: 0,
            TaxRatePercent: 18m,
            Lines:
            [
                new("Labor", ProductId: null, Description: "Motor Overhaul Labor", Quantity: 1, UnitRatePaise: 150000),
                new("Part", ProductId: product.Id, Description: "Stepper Gear Part", Quantity: 1, UnitRatePaise: 250000)
            ]));

        Assert.Equal(HttpStatusCode.Created, quoteRes.StatusCode);
        var quotation = (await quoteRes.Content.ReadFromJsonAsync<ApiResponse<ServiceQuotationDto>>())!.Data;

        // 3. Customer Approves Quotation
        var appQuoteRes = await client.PostAsync($"/api/v1/services/quotations/{quotation.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, appQuoteRes.StatusCode);

        // 4. Update Status to Completed
        var updateStatusRes = await client.PutAsJsonAsync($"/api/v1/services/requests/{serviceReq.Id}/status", new UpdateServiceRequestStatusRequest(
            Status: "Completed",
            AssignedEmployeeId: null,
            TechnicianNotes: "Gear replaced and tested OK"));
        Assert.Equal(HttpStatusCode.OK, updateStatusRes.StatusCode);

        // 5. Generate Tax Invoice & Deliver
        var invoiceRes = await client.PostAsJsonAsync($"/api/v1/services/requests/{serviceReq.Id}/invoice", new InvoiceServiceRequest(
            PaymentMethod: "Cash",
            BankCashAccountId: null));

        Assert.Equal(HttpStatusCode.OK, invoiceRes.StatusCode);
        var invoiceId = (await invoiceRes.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;
        Assert.NotEqual(Guid.Empty, invoiceId);

        // 6. Verify Service Invoice & Double-Entry Voucher
        using var vScope = _factory.Services.CreateScope();
        var vDb = vScope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var srvInvoice = await vDb.SalesInvoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == invoiceId);
        Assert.NotNull(srvInvoice);
        Assert.NotNull(srvInvoice.AccountingVoucherId);

        var voucher = await vDb.Vouchers.Include(v => v.Lines).FirstOrDefaultAsync(v => v.Id == srvInvoice.AccountingVoucherId.Value);
        Assert.NotNull(voucher);
        Assert.True(voucher.IsBalanced);
    }
}
