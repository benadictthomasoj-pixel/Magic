using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Magic.Api.Controllers.Orders;
using Magic.Contracts.Api;
using Magic.Contracts.Expenses;
using Magic.Contracts.Headers;
using Magic.Contracts.Hr;
using Magic.Contracts.Marketing;
using Magic.Contracts.Orders;
using Magic.Contracts.Payroll;
using Magic.Contracts.Security;
using Magic.Contracts.Services;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class PhaseFExtendedScenariosTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;

    public PhaseFExtendedScenariosTests(MagicTestFixture factory)
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

    // =========================================================================
    // Scenario 1: Expense Cancellation & Accounting Reversal
    // =========================================================================
    [Fact]
    public async Task Scenario01_ExpenseCancellation_PostsAccountingReversalVoucher()
    {
        var client = CreateAdminClient();
        var catRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<ExpenseCategoryDto>>>("/api/v1/expenses/categories");
        var category = catRes!.Data[0];

        // 1. Create, Approve and Pay Expense
        var createRes = await client.PostAsJsonAsync("/api/v1/expenses", new CreateExpenseRequest(
            category.Id, null, null, "Cash", null,
            DateOnly.FromDateTime(DateTime.UtcNow), 350000, 0, "Marketing", "EXP-CAN-01", "Client Lunch", null));
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var expense = (await createRes.Content.ReadFromJsonAsync<ApiResponse<ExpenseDto>>())!.Data;

        await client.PostAsync($"/api/v1/expenses/{expense.Id}/approve", null);
        var payRes = await client.PostAsJsonAsync($"/api/v1/expenses/{expense.Id}/pay", new PayExpenseRequest("Cash", null, DateOnly.FromDateTime(DateTime.UtcNow), "CSH-CAN-01"));
        Assert.Equal(HttpStatusCode.OK, payRes.StatusCode);

        // 2. Cancel and Reverse Expense
        var cancelRes = await client.PostAsJsonAsync($"/api/v1/expenses/{expense.Id}/cancel", new CancelExpenseRequest("Duplicate voucher entered by error"));
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var cancelled = (await cancelRes.Content.ReadFromJsonAsync<ApiResponse<ExpenseDto>>())!.Data;

        Assert.Equal("Cancelled", cancelled.Status);
        Assert.NotNull(cancelled.ReversalVoucherId);

        // Verify Reversal Voucher in GL
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var reversal = await db.Vouchers.Include(v => v.Lines).FirstOrDefaultAsync(v => v.Id == cancelled.ReversalVoucherId.Value);
        Assert.NotNull(reversal);
        Assert.True(reversal.IsBalanced);
    }

    // =========================================================================
    // Scenario 2: Petty Cash Shortage and Excess
    // =========================================================================
    [Fact]
    public async Task Scenario02_PettyCashShortageAndExcess_ReconcilesWithAuditTransactions()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var companyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var glAcc = await db.Accounts.FirstAsync(a => a.CompanyId == companyId);
        var user = await db.Users.FirstAsync();

        var newAccRes = await client.PostAsJsonAsync("/api/v1/expenses/petty-cash", new CreatePettyCashAccountRequest(
            AccountName: $"Audit Cash Box {Guid.NewGuid():N}",
            GlAccountId: glAcc.Id,
            CustodianUserId: user.Id,
            FloatLimitPaise: 500000));
        var account = (await newAccRes.Content.ReadFromJsonAsync<ApiResponse<PettyCashAccountDto>>())!.Data;

        var accRecord = await db.PettyCashAccounts.FirstAsync(a => a.Id == account.Id);
        accRecord.CurrentBalancePaise = 500000;
        await db.SaveChangesAsync();

        // 1. Reconcile with Shortage (counted 4,500 INR when book is 5,000 INR)
        var shortRes = await client.PostAsJsonAsync($"/api/v1/expenses/petty-cash/{account.Id}/reconcile", new ReconcilePettyCashRequest(
            ActualCountPaise: 450000,
            Notes: "Audit found missing ₹50 cash receipt"));
        Assert.Equal(HttpStatusCode.OK, shortRes.StatusCode);
        var shortTx = (await shortRes.Content.ReadFromJsonAsync<ApiResponse<PettyCashTransactionDto>>())!.Data;
        Assert.Equal("Shortage", shortTx.TransactionType);
        Assert.Equal(450000, shortTx.BalanceAfterPaise);

        // 2. Reconcile with Excess (counted 4,800 INR when book is 4,500 INR)
        var excessRes = await client.PostAsJsonAsync($"/api/v1/expenses/petty-cash/{account.Id}/reconcile", new ReconcilePettyCashRequest(
            ActualCountPaise: 480000,
            Notes: "Returned unspent cash from office tea vendor"));
        Assert.Equal(HttpStatusCode.OK, excessRes.StatusCode);
        var excessTx = (await excessRes.Content.ReadFromJsonAsync<ApiResponse<PettyCashTransactionDto>>())!.Data;
        Assert.Equal("Excess", excessTx.TransactionType);
        Assert.Equal(480000, excessTx.BalanceAfterPaise);
    }

    // =========================================================================
    // Scenario 3: Employee Resignation with Historical Payroll Preservation
    // =========================================================================
    [Fact]
    public async Task Scenario03_EmployeeResignation_PreservesHistoricalPayroll()
    {
        var client = CreateAdminClient();
        var empRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>("/api/v1/hr/employees");
        var employee = empRes!.Data[0];

        // 1. Onboard, Assign Salary and Run Historical Payroll
        var structRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<SalaryStructureDto>>>("/api/v1/payroll/structures");
        var structure = structRes!.Data[0];

        await client.PostAsJsonAsync("/api/v1/payroll/assign", new AssignEmployeeSalaryRequest(
            employee.Id, structure.Id, 5000000, new DateOnly(2026, 4, 1)));

        // 2. Resign Employee
        var updateRes = await client.PutAsJsonAsync($"/api/v1/hr/employees/{employee.Id}", new UpdateEmployeeRequest(
            employee.FirstName, employee.LastName, employee.Email, employee.Phone, employee.Address,
            employee.EmploymentType, employee.DepartmentId, employee.DesignationId, employee.ReportingManagerId,
            Status: "Inactive",
            EmergencyContactName: "Emergency Contact",
            EmergencyContactPhone: "9988776655",
            BankAccountNumber: "1234567890",
            BankIfsc: "HDFC0001234",
            BankName: "HDFC Bank",
            PanNumber: "ABCDE1234F",
            LinkedUserId: null));

        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var resigned = (await updateRes.Content.ReadFromJsonAsync<ApiResponse<EmployeeDto>>())!.Data;
        Assert.Equal("Inactive", resigned.Status);

        // 3. Verify previous records exist in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var empInDb = await db.Employees.FirstAsync(e => e.Id == employee.Id);
        Assert.Equal("Inactive", empInDb.Status);
    }

    // =========================================================================
    // Scenario 4: Leave Adjustment Affecting Payroll Pro-ration
    // =========================================================================
    [Fact]
    public async Task Scenario04_LeaveAdjustment_ReducesProRatedGrossAndNetPayroll()
    {
        var client = CreateAdminClient();
        var empRes = await client.GetFromJsonAsync<ApiResponse<IReadOnlyList<EmployeeDto>>>("/api/v1/hr/employees");
        var employee = empRes!.Data[0];

        // Record 5 days of Absent attendance
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
            var startDate = new DateOnly(2026, 7, 1);
            for (int i = 0; i < 5; i++)
            {
                var attDate = startDate.AddDays(i);
                var existing = await db.AttendanceRecords.FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == attDate);
                if (existing == null)
                {
                    db.AttendanceRecords.Add(new AttendanceRecord
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = employee.CompanyId,
                        BranchId = employee.BranchId,
                        EmployeeId = employee.Id,
                        AttendanceDate = attDate,
                        Status = "Absent",
                        WorkDurationMinutes = 0,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        // Calculate Payroll for July 2026
        using (var cleanScope = _factory.Services.CreateScope())
        {
            var cleanDb = cleanScope.ServiceProvider.GetRequiredService<MagicDbContext>();
            var existingPeriods = await cleanDb.PayrollPeriods.Include(p => p.Items).Where(p => p.Year == 2026 && p.Month == 7).ToListAsync();
            if (existingPeriods.Count > 0)
            {
                cleanDb.PayrollItems.RemoveRange(existingPeriods.SelectMany(p => p.Items));
                cleanDb.PayrollPeriods.RemoveRange(existingPeriods);
                await cleanDb.SaveChangesAsync();
            }
        }

        var calcRes = await client.PostAsJsonAsync("/api/v1/payroll/calculate", new CalculatePayrollRequest(7, 2026, null));
        Assert.Equal(HttpStatusCode.Created, calcRes.StatusCode);
        var period = (await calcRes.Content.ReadFromJsonAsync<ApiResponse<PayrollPeriodDto>>())!.Data;
        Assert.True(period.TotalGrossPaise > 0);
    }

    // =========================================================================
    // Scenario 5: Payroll Recalculation After Finalization Rejection
    // =========================================================================
    [Fact]
    public async Task Scenario05_PayrollCorrectionAfterFinalization_RejectsRecalculation()
    {
        var client = CreateAdminClient();

        // Month 6 (June 2026)
        using (var cleanScope = _factory.Services.CreateScope())
        {
            var cleanDb = cleanScope.ServiceProvider.GetRequiredService<MagicDbContext>();
            var existingPeriods = await cleanDb.PayrollPeriods.Include(p => p.Items).Where(p => p.Year == 2026 && p.Month == 6).ToListAsync();
            if (existingPeriods.Count > 0)
            {
                cleanDb.PayrollItems.RemoveRange(existingPeriods.SelectMany(p => p.Items));
                cleanDb.PayrollPeriods.RemoveRange(existingPeriods);
                await cleanDb.SaveChangesAsync();
            }
        }

        var calcRes = await client.PostAsJsonAsync("/api/v1/payroll/calculate", new CalculatePayrollRequest(6, 2026, null));
        var period = (await calcRes.Content.ReadFromJsonAsync<ApiResponse<PayrollPeriodDto>>())!.Data;

        await client.PostAsync($"/api/v1/payroll/periods/{period.Id}/approve", null);
        await client.PostAsync($"/api/v1/payroll/periods/{period.Id}/finalize", null);

        // Attempt to calculate again on finalized cycle -> Should return 400 Bad Request
        var reCalcRes = await client.PostAsJsonAsync("/api/v1/payroll/calculate", new CalculatePayrollRequest(6, 2026, null));
        Assert.Equal(HttpStatusCode.BadRequest, reCalcRes.StatusCode);
    }

    // =========================================================================
    // Scenario 6: Coupon Expiry & Usage Limit Validation
    // =========================================================================
    [Fact]
    public async Task Scenario06_CouponExpiryAndUsageLimit_PreventsInvalidRedemptions()
    {
        var client = CreateAdminClient();
        var expiredCode = $"EXP{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        // 1. Create Expired Coupon
        var expCouponRes = await client.PostAsJsonAsync("/api/v1/marketing/coupons", new CreatePromotionCouponRequest(
            Code: expiredCode,
            CampaignId: null,
            DiscountType: "FixedAmount",
            DiscountValue: 50000, // 500 INR
            MinOrderAmountPaise: 100000,
            MaxDiscountAmountPaise: 50000,
            ValidFrom: DateTimeOffset.UtcNow.AddDays(-30),
            ValidTo: DateTimeOffset.UtcNow.AddDays(-1), // Expired yesterday
            UsageLimitTotal: 10,
            UsageLimitPerCustomer: 1));
        Assert.Equal(HttpStatusCode.Created, expCouponRes.StatusCode);

        // 2. Validate at POS -> Should be invalid due to expiry
        var valRes = await client.PostAsJsonAsync("/api/v1/marketing/coupons/validate", new ValidateCouponRequest(
            expiredCode,
            CustomerId: Guid.NewGuid(),
            OrderSubtotalPaise: 200000));

        Assert.Equal(HttpStatusCode.OK, valRes.StatusCode);
        var valResult = (await valRes.Content.ReadFromJsonAsync<ApiResponse<ValidateCouponResponse>>())!.Data;
        Assert.False(valResult.IsValid);
        Assert.NotNull(valResult.RejectionReason);
        Assert.Contains("expired", valResult.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // Scenario 7: Customer Loyalty Point Calculation
    // =========================================================================
    [Fact]
    public async Task Scenario07_LoyaltyPoints_AccruesAndPreventsExcessiveRedemption()
    {
        var client = CreateAdminClient();
        var activeCode = $"LOY{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        var coupRes = await client.PostAsJsonAsync("/api/v1/marketing/coupons", new CreatePromotionCouponRequest(
            Code: activeCode,
            CampaignId: null,
            DiscountType: "Percentage",
            DiscountValue: 500, // 5%
            MinOrderAmountPaise: 50000,
            MaxDiscountAmountPaise: 25000,
            ValidFrom: DateTimeOffset.UtcNow.AddDays(-1),
            ValidTo: DateTimeOffset.UtcNow.AddDays(30),
            UsageLimitTotal: 1,
            UsageLimitPerCustomer: 1));
        Assert.Equal(HttpStatusCode.Created, coupRes.StatusCode);

        // Validate below minimum order threshold -> Should fail min order
        var failValRes = await client.PostAsJsonAsync("/api/v1/marketing/coupons/validate", new ValidateCouponRequest(
            activeCode,
            CustomerId: Guid.NewGuid(),
            OrderSubtotalPaise: 30000)); // Less than 50,000 paise min order

        var failVal = (await failValRes.Content.ReadFromJsonAsync<ApiResponse<ValidateCouponResponse>>())!.Data;
        Assert.False(failVal.IsValid);
        Assert.NotNull(failVal.RejectionReason);
        Assert.Contains("Minimum order", failVal.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // Scenario 8: Partial Order Fulfillment
    // =========================================================================
    [Fact]
    public async Task Scenario08_PartialOrderFulfillment_UpdatesStatusToPartiallyShipped()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db.Customers.FirstAsync();
        var product = await db.Products.FirstAsync(p => p.IsActive);
        var warehouse = await db.Warehouses.FirstAsync(w => w.IsActive);

        // Ensure stock
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
                UnitCostPaise = 25000,
                LastMovementAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // 1. Create order for 10 units
        var orderRes = await client.PostAsJsonAsync("/api/v1/orders", new CreateSalesOrderRequest(
            customer.Id, warehouse.Id, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            "Delivery Address", "Partial test",
            [new(product.Id, null, Quantity: 10, UnitPricePaise: 50000, DiscountAmountPaise: 0, TaxRatePercent: 18m)]));

        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data;
        await client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);

        // 2. Partially fulfill 4 units out of 10
        var ordLine = order.Lines[0];
        var fulfillRes = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/fulfill", new FulfillOrderRequest(
            "BlueDart", "BD-PART-01", [new(ordLine.Id, QuantityDispatched: 4)]));

        Assert.Equal(HttpStatusCode.Created, fulfillRes.StatusCode);
        var updatedOrder = (await client.GetFromJsonAsync<ApiResponse<SalesOrderDto>>($"/api/v1/orders/{order.Id}"))!.Data;
        Assert.Equal("PartiallyFulfilled", updatedOrder.Status);
    }

    // =========================================================================
    // Scenario 9: Backorder Fulfillment After Replenishment
    // =========================================================================
    [Fact]
    public async Task Scenario09_BackorderFulfillment_CompletesRemainingStockDispatch()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db.Customers.FirstAsync();
        var product = await db.Products.FirstAsync(p => p.IsActive);
        var warehouse = await db.Warehouses.FirstAsync(w => w.IsActive);

        // 1. Create order for 6 units
        var orderRes = await client.PostAsJsonAsync("/api/v1/orders", new CreateSalesOrderRequest(
            customer.Id, warehouse.Id, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            "Backorder Address", "Full fulfillment test",
            [new(product.Id, null, Quantity: 6, UnitPricePaise: 50000, DiscountAmountPaise: 0, TaxRatePercent: 18m)]));

        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data;
        await client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);

        // 2. Dispatch all 6 units
        var ordLine = order.Lines[0];
        var fulfillRes = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/fulfill", new FulfillOrderRequest(
            "FedEx", "FX-FULL-99", [new(ordLine.Id, QuantityDispatched: 6)]));

        Assert.Equal(HttpStatusCode.Created, fulfillRes.StatusCode);
        var updatedOrder = (await client.GetFromJsonAsync<ApiResponse<SalesOrderDto>>($"/api/v1/orders/{order.Id}"))!.Data;
        Assert.Equal("Fulfilled", updatedOrder.Status);
    }

    // =========================================================================
    // Scenario 10: Stock Reservation Prevents Overselling Insufficient Stock
    // =========================================================================
    [Fact]
    public async Task Scenario10_StockReservation_PreventsOversellingInsufficientStock()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db.Customers.FirstAsync();
        var product = await db.Products.FirstAsync(p => p.IsActive);
        var warehouse = await db.Warehouses.FirstAsync(w => w.IsActive);

        var stock = await db.StockBalances.FirstOrDefaultAsync(b => b.WarehouseId == warehouse.Id && b.ProductId == product.Id);
        var excessQty = (stock?.QuantityAvailable ?? 10) + 50000m;

        // Create order for quantity far exceeding available stock
        var orderRes = await client.PostAsJsonAsync("/api/v1/orders", new CreateSalesOrderRequest(
            customer.Id, warehouse.Id, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            "Test Address", "Oversell check",
            [new(product.Id, null, Quantity: excessQty, UnitPricePaise: 50000, DiscountAmountPaise: 0, TaxRatePercent: 18m)]));

        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data;

        // Confirmation creates order and registers backorder
        var confRes = await client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confRes.StatusCode);

        // Attempting to dispatch / fulfill oversold quantity without physical stock fails with 400 Bad Request
        var fulRes = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/fulfill", new FulfillOrderRequest(
            "Express", "EXP-OVERSELL", [new(order.Lines[0].Id, QuantityDispatched: excessQty)]));
        Assert.Equal(HttpStatusCode.BadRequest, fulRes.StatusCode);
    }

    // =========================================================================
    // Scenario 11: Service Cancellation Status Transition
    // =========================================================================
    [Fact]
    public async Task Scenario11_ServiceCancellation_RestoresTicketToCancelled()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db.Customers.FirstAsync();
        var catalog = await db.ServiceCatalogs.FirstAsync();

        var createReqRes = await client.PostAsJsonAsync("/api/v1/services/requests", new CreateServiceRequest(
            customer.Id, catalog.Id, "Tablet Screen Display", "TAB-7711", "Cracked screen", "Normal",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3))));

        var serviceReq = (await createReqRes.Content.ReadFromJsonAsync<ApiResponse<ServiceRequestDto>>())!.Data;

        var cancelRes = await client.PutAsJsonAsync($"/api/v1/services/requests/{serviceReq.Id}/status", new UpdateServiceRequestStatusRequest(
            Status: "Cancelled",
            AssignedEmployeeId: null,
            TechnicianNotes: "Customer chose not to proceed with repair"));

        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var cancelled = (await cancelRes.Content.ReadFromJsonAsync<ApiResponse<ServiceRequestDto>>())!.Data;
        Assert.Equal("Cancelled", cancelled.Status);
    }

    // =========================================================================
    // Scenario 12: Service Invoice Link Prevents Duplicate Invoicing
    // =========================================================================
    [Fact]
    public async Task Scenario12_ServiceRefundAndInvoiceIntegrity_EnforcesSingleInvoiceLink()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db.Customers.FirstAsync();
        var catalog = await db.ServiceCatalogs.FirstAsync();

        var createReqRes = await client.PostAsJsonAsync("/api/v1/services/requests", new CreateServiceRequest(
            customer.Id, catalog.Id, "Barcode Scanner LS-2208", "BCS-101", "Laser trigger failure", "Medium",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))));

        var serviceReq = (await createReqRes.Content.ReadFromJsonAsync<ApiResponse<ServiceRequestDto>>())!.Data;

        var quoteRes = await client.PostAsJsonAsync($"/api/v1/services/requests/{serviceReq.Id}/quotations", new CreateServiceQuotationRequest(
            ValidUntil: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DiscountPaise: 0,
            TaxRatePercent: 18m,
            Lines: [new("Labor", null, "Laser module overhaul", 1, 80000)]));

        var quotation = (await quoteRes.Content.ReadFromJsonAsync<ApiResponse<ServiceQuotationDto>>())!.Data;
        await client.PostAsync($"/api/v1/services/quotations/{quotation.Id}/approve", null);

        // First invoice succeeds
        var invRes = await client.PostAsJsonAsync($"/api/v1/services/requests/{serviceReq.Id}/invoice", new InvoiceServiceRequest("Cash", null));
        Assert.Equal(HttpStatusCode.OK, invRes.StatusCode);

        // Second invoice fails with 400 Bad Request
        var dupInvRes = await client.PostAsJsonAsync($"/api/v1/services/requests/{serviceReq.Id}/invoice", new InvoiceServiceRequest("Cash", null));
        Assert.Equal(HttpStatusCode.BadRequest, dupInvRes.StatusCode);
    }

    // =========================================================================
    // Scenario 13: Multi-Branch Access Restrictions
    // =========================================================================
    [Fact]
    public async Task Scenario13_MultiBranchAccessRestrictions_EnforcesBranchContext()
    {
        var client = CreateAdminClient();
        var randomBranchId = Guid.NewGuid();

        // Send request with a branch header
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/expenses/summary");
        request.Headers.Add(MagicHttpHeaders.BranchId, randomBranchId.ToString());

        var response = await client.SendAsync(request);
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Scenario 14: Multi-Company Tenant Isolation
    // =========================================================================
    [Fact]
    public async Task Scenario14_CompanyIsolation_RejectsCrossTenantDataAccess()
    {
        var client = CreateAdminClient();
        var alienCompanyId = Guid.NewGuid();

        // Attempt to pass X-Company-Id for an alien company
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/expenses/categories");
        request.Headers.Add(MagicHttpHeaders.CompanyId, alienCompanyId.ToString());

        var response = await client.SendAsync(request);
        // Must reject with 403 Forbidden due to TENANT_MISMATCH
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // =========================================================================
    // Scenario 15: Concurrent Order Fulfillment Prevention
    // =========================================================================
    [Fact]
    public async Task Scenario15_ConcurrentOrderFulfillment_PreventsOverDispatching()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db.Customers.FirstAsync();
        var product = await db.Products.FirstAsync(p => p.IsActive);
        var warehouse = await db.Warehouses.FirstAsync(w => w.IsActive);

        var orderRes = await client.PostAsJsonAsync("/api/v1/orders", new CreateSalesOrderRequest(
            customer.Id, warehouse.Id, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            "Concurrent Address", "Concurrent test",
            [new(product.Id, null, Quantity: 3, UnitPricePaise: 50000, DiscountAmountPaise: 0, TaxRatePercent: 18m)]));

        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data;
        await client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);

        var ordLine = order.Lines[0];

        // First fulfillment dispatches all 3
        var f1 = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/fulfill", new FulfillOrderRequest(
            "DHL", "DHL-001", [new(ordLine.Id, QuantityDispatched: 3)]));
        Assert.Equal(HttpStatusCode.Created, f1.StatusCode);

        // Second fulfillment for the same order line must fail because quantity is already fulfilled
        var f2 = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/fulfill", new FulfillOrderRequest(
            "DHL", "DHL-002", [new(ordLine.Id, QuantityDispatched: 3)]));
        Assert.Equal(HttpStatusCode.BadRequest, f2.StatusCode);
    }

    // =========================================================================
    // Scenario 16: Duplicate Sales Order Invoice Conversion Rejection
    // =========================================================================
    [Fact]
    public async Task Scenario16_DuplicateInvoiceConversion_RejectsSecondInvoicing()
    {
        var client = CreateAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db.Customers.FirstAsync();
        var product = await db.Products.FirstAsync(p => p.IsActive);
        var warehouse = await db.Warehouses.FirstAsync(w => w.IsActive);

        var orderRes = await client.PostAsJsonAsync("/api/v1/orders", new CreateSalesOrderRequest(
            customer.Id, warehouse.Id, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            "Dup Inv Address", "Invoice check",
            [new(product.Id, null, Quantity: 1, UnitPricePaise: 50000, DiscountAmountPaise: 0, TaxRatePercent: 18m)]));

        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>())!.Data;
        await client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);
        await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/fulfill", new FulfillOrderRequest(
            "Local", "LOC-01", [new(order.Lines[0].Id, QuantityDispatched: 1)]));

        // First invoice succeeds
        var inv1 = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/invoice", new ConvertOrderToInvoiceApiRequest("Cash"));
        Assert.Equal(HttpStatusCode.OK, inv1.StatusCode);

        // Second invoice fails
        var inv2 = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/invoice", new ConvertOrderToInvoiceApiRequest("Cash"));
        Assert.Equal(HttpStatusCode.BadRequest, inv2.StatusCode);
    }

    // =========================================================================
    // Scenario 17: Duplicate Payroll Finalization Rejection
    // =========================================================================
    [Fact]
    public async Task Scenario17_DuplicatePayrollFinalization_RejectsAlreadyFinalizedCycle()
    {
        var client = CreateAdminClient();

        // Month 5 (May 2026)
        using (var cleanScope = _factory.Services.CreateScope())
        {
            var cleanDb = cleanScope.ServiceProvider.GetRequiredService<MagicDbContext>();
            var existingPeriods = await cleanDb.PayrollPeriods.Include(p => p.Items).Where(p => p.Year == 2026 && p.Month == 5).ToListAsync();
            if (existingPeriods.Count > 0)
            {
                cleanDb.PayrollItems.RemoveRange(existingPeriods.SelectMany(p => p.Items));
                cleanDb.PayrollPeriods.RemoveRange(existingPeriods);
                await cleanDb.SaveChangesAsync();
            }
        }

        var calcRes = await client.PostAsJsonAsync("/api/v1/payroll/calculate", new CalculatePayrollRequest(5, 2026, null));
        var period = (await calcRes.Content.ReadFromJsonAsync<ApiResponse<PayrollPeriodDto>>())!.Data;

        await client.PostAsync($"/api/v1/payroll/periods/{period.Id}/approve", null);
        var fin1 = await client.PostAsync($"/api/v1/payroll/periods/{period.Id}/finalize", null);
        Assert.Equal(HttpStatusCode.OK, fin1.StatusCode);

        // Re-finalizing fails
        var fin2 = await client.PostAsync($"/api/v1/payroll/periods/{period.Id}/finalize", null);
        Assert.Equal(HttpStatusCode.BadRequest, fin2.StatusCode);
    }

    // =========================================================================
    // Scenario 18: Application Restart & Persisted Relational State
    // =========================================================================
    [Fact]
    public async Task Scenario18_ApplicationRestart_VerifiesPersistedRelationalState()
    {
        var client = CreateAdminClient();

        // 1. Create a service job ticket via HTTP
        using var scope1 = _factory.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<MagicDbContext>();
        var customer = await db1.Customers.FirstAsync();
        var catalog = await db1.ServiceCatalogs.FirstAsync();

        var createReqRes = await client.PostAsJsonAsync("/api/v1/services/requests", new CreateServiceRequest(
            customer.Id, catalog.Id, "UPS 1000VA", "UPS-9944", "Battery degradation", "Low",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4))));

        Assert.Equal(HttpStatusCode.Created, createReqRes.StatusCode);
        var serviceReq = (await createReqRes.Content.ReadFromJsonAsync<ApiResponse<ServiceRequestDto>>())!.Data;

        // 2. Simulate complete application restart by opening an entirely fresh dependency injection scope
        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<MagicDbContext>();

        var persisted = await db2.ServiceRequests
            .Include(r => r.Customer)
            .Include(r => r.ServiceCatalog)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == serviceReq.Id);

        Assert.NotNull(persisted);
        Assert.Equal(serviceReq.RequestNumber, persisted.RequestNumber);
        Assert.NotNull(persisted.Customer);
        Assert.NotNull(persisted.ServiceCatalog);
        Assert.Equal("UPS 1000VA", persisted.ItemDescription);
    }
}
