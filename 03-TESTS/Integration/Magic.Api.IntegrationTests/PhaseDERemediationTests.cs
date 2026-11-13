using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Magic.Contracts.Api;
using Magic.Contracts.Commercial;
using Magic.Contracts.Finance;
using Magic.Contracts.Inventory;
using Magic.Contracts.Sales;
using Magic.Contracts.Security;
using Magic.Domain.Accounting.Enums;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class PhaseDERemediationTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;

    public PhaseDERemediationTests(MagicTestFixture factory)
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

    private static async Task<WarehouseDto> CreateWarehouseAsync(HttpClient client, string prefix = "REM-WH")
    {
        var code = $"{prefix}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
        var createRes = await client.PostAsJsonAsync("/api/v1/inventory/warehouses", new CreateWarehouseRequest
        {
            Code = code,
            Name = $"Warehouse {code}",
            AddressLine1 = "Industrial Area",
            City = "Bengaluru",
            State = "Karnataka",
            StateCode = "29",
            PinCode = "560001",
            Country = "India"
        });
        createRes.EnsureSuccessStatusCode();
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        return created!.Data;
    }

    private static async Task<ProductDetailDto> CreateTestProductAsync(HttpClient client, string prefix = "REM-PROD", long pricePaise = 250000, decimal gstRate = 18.0m)
    {
        var uomRes = await client.GetAsync("/api/v1/commercial/categories");
        var cats = await uomRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CategoryDto>>>();
        var catId = cats!.Data[0].Id;

        var unitsRes = await client.GetAsync("/api/v1/commercial/units");
        var units = await unitsRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<UnitOfMeasureDto>>>();
        var unitId = units!.Data[0].Id;

        var code = $"{prefix}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var req = new CreateProductRequest
        {
            ProductCode = code,
            Name = $"Test Product {code}",
            CategoryId = catId,
            UnitOfMeasureId = unitId,
            HsnSacCode = "84713010",
            GstRatePercentage = gstRate,
            Sku = $"SKU-{code}",
            PrimaryBarcode = $"BAR-{code}",
            SellingPricePaise = pricePaise,
            CostPricePaise = 180000,
            PurchasePricePaise = 180000,
            MrpPaise = pricePaise + 50000
        };

        var postRes = await client.PostAsJsonAsync("/api/v1/commercial/products", req);
        postRes.EnsureSuccessStatusCode();
        var result = await postRes.Content.ReadFromJsonAsync<ApiResponse<ProductDetailDto>>();
        return result!.Data;
    }

    private static async Task EnsureStockOnHandAsync(HttpClient client, Guid warehouseId, Guid productId, decimal qty)
    {
        var stockReq = new PostOpeningStockRequest
        {
            WarehouseId = warehouseId,
            BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Items =
            [
                new OpeningStockItemRequest
                {
                    ProductId = productId,
                    Quantity = qty,
                    UnitCostPaise = 180000
                }
            ]
        };

        var res = await client.PostAsJsonAsync("/api/v1/inventory/stock/opening-stock", stockReq);
        res.EnsureSuccessStatusCode();
    }

    private static async Task<CashierSessionDto> GetOrOpenSessionAsync(HttpClient client, long openingCashPaise = 500000)
    {
        var activeRes = await client.GetAsync("/api/v1/cashier/sessions/active");
        if (activeRes.IsSuccessStatusCode)
        {
            var activeEnv = await activeRes.Content.ReadFromJsonAsync<ApiResponse<CashierSessionDto?>>();
            if (activeEnv?.Data != null)
            {
                return activeEnv.Data;
            }
        }

        var openRes = await client.PostAsJsonAsync("/api/v1/cashier/sessions/open", new OpenCashierSessionRequest
        {
            OpeningCashPaise = openingCashPaise
        });
        openRes.EnsureSuccessStatusCode();
        var opened = await openRes.Content.ReadFromJsonAsync<ApiResponse<CashierSessionDto>>();
        return opened!.Data;
    }

    private static async Task<CustomerDetailDto> CreateTestCustomerAsync(HttpClient client, long creditLimitPaise = 5000000, int creditPeriodDays = 30)
    {
        var code = $"CUST-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var uniqueDigits = Random.Shared.Next(1000, 9999);
        var uniqueGstin = $"29AAAAA{uniqueDigits}A1Z5";
        var req = new CreateCustomerRequest
        {
            CustomerCode = code,
            Name = $"Commercial Customer {code}",
            CustomerType = CustomerType.Business,
            GstType = GstRegistrationType.Registered,
            Gstin = uniqueGstin,
            Phone = "+91 9876543210",
            Email = $"{code.ToLowerInvariant()}@test.com",
            CreditLimitPaise = creditLimitPaise,
            CreditPeriodDays = creditPeriodDays
        };

        var res = await client.PostAsJsonAsync("/api/v1/commercial/customers", req);
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<ApiResponse<CustomerDetailDto>>();
        return created!.Data;
    }

    // ==========================================
    // R1: POS Checkout Posting to General Ledger
    // ==========================================

    [Fact]
    public async Task R1_POS_Checkout_Posts_Balanced_Journal_Voucher_To_General_Ledger()
    {
        // Arrange
        var client = CreateAdminClient();
        var warehouse = await CreateWarehouseAsync(client, "R1-WH");
        var product = await CreateTestProductAsync(client, "R1-PROD", pricePaise: 250000, gstRate: 18.0m);
        await EnsureStockOnHandAsync(client, warehouse.Id, product.Id, 20m);

        // 1 unit = 250,000 + 18% GST (45,000) = 295,000 paise (₹2,950.00)
        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = warehouse.Id,
            Lines =
            [
                new PosSaleLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 1m
                }
            ],
            Payments =
            [
                new PosPaymentRequest
                {
                    PaymentMethod = "Cash",
                    AmountPaise = 295000
                }
            ]
        };

        // Act
        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);
        checkoutRes.EnsureSuccessStatusCode();
        var invoice = (await checkoutRes.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data;

        // Assert Invoice
        Assert.NotNull(invoice);
        Assert.Equal("Paid", invoice.Status);
        Assert.NotNull(invoice.AccountingVoucherId);

        // Assert General Ledger Voucher
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var voucher = await db.Vouchers
            .Include(v => v.Lines)
            .FirstOrDefaultAsync(v => v.Id == invoice.AccountingVoucherId.Value);

        Assert.NotNull(voucher);
        Assert.Equal(VoucherType.Sales, voucher.VoucherType);
        Assert.Equal(invoice.InvoiceNumber, voucher.ReferenceNumber);
        Assert.True(voucher.IsBalanced);
        Assert.Equal(295000, voucher.TotalDebitPaise);
        Assert.Equal(295000, voucher.TotalCreditPaise);

        // Verify Cash Account debited
        var accCash = await db.Accounts.FirstAsync(a => a.Code == "1111");
        var debitLine = voucher.Lines.FirstOrDefault(l => l.AccountId == accCash.Id);
        Assert.NotNull(debitLine);
        Assert.Equal(295000, debitLine.Debit.Paise);

        // Verify Sales Revenue credited
        var accSales = await db.Accounts.FirstAsync(a => a.Code == "4101");
        var creditSalesLine = voucher.Lines.FirstOrDefault(l => l.AccountId == accSales.Id);
        Assert.NotNull(creditSalesLine);
        Assert.Equal(250000, creditSalesLine.Credit.Paise);

        // Verify Output Taxes credited (CGST 22,500 + SGST 22,500)
        var accCgst = await db.Accounts.FirstAsync(a => a.Code == "2121");
        var accSgst = await db.Accounts.FirstAsync(a => a.Code == "2122");
        Assert.Contains(voucher.Lines, l => l.AccountId == accCgst.Id && l.Credit.Paise == 22500);
        Assert.Contains(voucher.Lines, l => l.AccountId == accSgst.Id && l.Credit.Paise == 22500);
    }

    // ==========================================
    // R2: Customer Credit Sales & AR Subledger
    // ==========================================

    [Fact]
    public async Task R2_POS_Credit_Sale_Generates_Ar_Subledger_Item_And_Debits_Trade_Debtors()
    {
        // Arrange
        var client = CreateAdminClient();
        var warehouse = await CreateWarehouseAsync(client, "R2-WH");
        var customer = await CreateTestCustomerAsync(client, creditLimitPaise: 5000000, creditPeriodDays: 30);
        var product = await CreateTestProductAsync(client, "R2-PROD", pricePaise: 250000, gstRate: 18.0m);
        await EnsureStockOnHandAsync(client, warehouse.Id, product.Id, 20m);

        // Total: 295,000 paise. Tender ₹1,000 cash (100,000 paise), leaving 195,000 paise on credit.
        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = warehouse.Id,
            CustomerId = customer.Id,
            Lines =
            [
                new PosSaleLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 1m
                }
            ],
            Payments =
            [
                new PosPaymentRequest
                {
                    PaymentMethod = "Cash",
                    AmountPaise = 100000
                }
            ]
        };

        // Act
        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);
        checkoutRes.EnsureSuccessStatusCode();
        var invoice = (await checkoutRes.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data;

        // Assert Invoice
        Assert.Equal("PartiallyPaid", invoice.Status);
        Assert.Equal(100000, invoice.AmountPaidPaise);
        Assert.Equal(195000, invoice.OutstandingBalancePaise);

        // Assert AR Subledger Record
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var arItem = await db.ArSubledgerItems
            .FirstOrDefaultAsync(a => a.InvoiceId == invoice.Id);

        Assert.NotNull(arItem);
        Assert.Equal(customer.Id, arItem.CustomerId);
        Assert.Equal(295000, arItem.OriginalAmountPaise);
        Assert.Equal(100000, arItem.SettledAmountPaise);
        Assert.Equal(195000, arItem.OutstandingAmountPaise);
        Assert.Equal("PARTIALLY_PAID", arItem.Status);

        // Assert GL Journal Voucher debits Trade Debtors for 195,000 paise
        var voucher = await db.Vouchers
            .Include(v => v.Lines)
            .FirstOrDefaultAsync(v => v.Id == invoice.AccountingVoucherId!.Value);

        Assert.NotNull(voucher);
        Assert.True(voucher.IsBalanced);
        var accDebtors = await db.Accounts.FirstAsync(a => a.Code == "1201");
        var arLine = voucher.Lines.FirstOrDefault(l => l.AccountId == accDebtors.Id);
        Assert.NotNull(arLine);
        Assert.Equal(195000, arLine.Debit.Paise);
    }

    [Fact]
    public async Task R2_POS_Credit_Sale_Exceeding_Credit_Limit_Is_Rejected()
    {
        // Arrange
        var client = CreateAdminClient();
        var warehouse = await CreateWarehouseAsync(client, "R2-OOS");
        var customer = await CreateTestCustomerAsync(client, creditLimitPaise: 100000); // Only ₹1,000 limit
        var product = await CreateTestProductAsync(client, "R2-LIMIT", pricePaise: 250000, gstRate: 18.0m);
        await EnsureStockOnHandAsync(client, warehouse.Id, product.Id, 20m);

        // Total: 295,000 paise. 0 paid -> requests 295,000 credit against 100,000 limit
        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = warehouse.Id,
            CustomerId = customer.Id,
            Lines =
            [
                new PosSaleLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 1m
                }
            ],
            Payments = []
        };

        // Act
        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);

        // Assert
        Assert.False(checkoutRes.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, checkoutRes.StatusCode);
    }

    // ==========================================
    // R3: Sales Return Discount Math
    // ==========================================

    [Fact]
    public async Task R3_Sales_Return_With_Line_Discount_Calculates_Net_Refund()
    {
        // Arrange
        var client = CreateAdminClient();
        var warehouse = await CreateWarehouseAsync(client, "R3-WH");
        var product = await CreateTestProductAsync(client, "R3-DISC", pricePaise: 250000, gstRate: 18.0m);
        await EnsureStockOnHandAsync(client, warehouse.Id, product.Id, 20m);

        // Line discount of ₹500 (50,000 paise).
        // Taxable = 250,000 - 50,000 = 200,000 paise.
        // 18% GST = 36,000 paise.
        // Line total paid = 236,000 paise.
        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = warehouse.Id,
            Lines =
            [
                new PosSaleLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 1m,
                    DiscountPaise = 50000
                }
            ],
            Payments =
            [
                new PosPaymentRequest
                {
                    PaymentMethod = "Cash",
                    AmountPaise = 236000
                }
            ]
        };

        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);
        checkoutRes.EnsureSuccessStatusCode();
        var invoice = (await checkoutRes.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data;

        // Act: Return 1 unit
        var returnReq = new CreateSalesReturnRequest
        {
            InvoiceId = invoice.Id,
            ReasonCode = "CustomerCancelled",
            Notes = "Discounted return test",
            RefundMethod = "CreditNote",
            Lines =
            [
                new CreateSalesReturnLineRequest
                {
                    InvoiceLineId = invoice.Lines[0].Id,
                    ReturnQuantity = 1m,
                    IsResalable = true,
                    Reason = "Customer request"
                }
            ]
        };

        var returnRes = await client.PostAsJsonAsync("/api/v1/sales/returns", returnReq);
        returnRes.EnsureSuccessStatusCode();
        var ret = (await returnRes.Content.ReadFromJsonAsync<ApiResponse<SalesReturnDto>>())!.Data;

        // Assert: Refund is exactly net paid amount (236,000 paise), not gross price (295,000 paise)
        Assert.Equal(236000, ret.TotalRefundPaise);
        Assert.Equal(200000, ret.Lines[0].UnitPricePaise);
        Assert.Equal(36000, ret.Lines[0].TaxAmountPaise);
    }

    // ==========================================
    // R4: Non-Cash Returns & Drawer Isolation
    // ==========================================

    [Fact]
    public async Task R4_Sales_Return_Non_Cash_Does_Not_Reduce_Physical_Cash_Drawer()
    {
        // Arrange
        var client = CreateAdminClient();
        var warehouse = await CreateWarehouseAsync(client, "R4-WH");
        var product = await CreateTestProductAsync(client, "R4-DRAWER", pricePaise: 250000, gstRate: 18.0m);
        await EnsureStockOnHandAsync(client, warehouse.Id, product.Id, 20m);

        // Open Cashier Shift with float (or reuse active)
        var session = await GetOrOpenSessionAsync(client);
        var initialExpectedCash = session.ExpectedCashPaise;
        var initialRefunds = session.CashRefundsPaise;

        // Bill ₹2,950 via Cash
        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = warehouse.Id,
            CashierSessionId = session.Id,
            Lines = [new PosSaleLineRequest { ProductId = product.Id, Quantity = 1m }],
            Payments = [new PosPaymentRequest { PaymentMethod = "Cash", AmountPaise = 295000 }]
        };
        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);
        checkoutRes.EnsureSuccessStatusCode();
        var invoice = (await checkoutRes.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data;

        // Process Return via CreditNote (non-cash)
        var returnReq = new CreateSalesReturnRequest
        {
            InvoiceId = invoice.Id,
            ReasonCode = "CustomerCancelled",
            RefundMethod = "CreditNote",
            Lines =
            [
                new CreateSalesReturnLineRequest
                {
                    InvoiceLineId = invoice.Lines[0].Id,
                    ReturnQuantity = 1m,
                    IsResalable = true,
                    Reason = "Store credit issued"
                }
            ]
        };
        var returnRes = await client.PostAsJsonAsync("/api/v1/sales/returns", returnReq);
        returnRes.EnsureSuccessStatusCode();

        // Assert: Cashier drawer expected cash is NOT reduced by the credit note
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var updatedSession = await db.CashierSessions.FirstAsync(s => s.Id == session.Id);

        Assert.Equal(initialExpectedCash + 295000, updatedSession.ExpectedCashPaise);
        Assert.Equal(initialRefunds, updatedSession.CashRefundsPaise);
    }

    // ==========================================
    // R5: Warehouse Restoration on Cancellation
    // ==========================================

    [Fact]
    public async Task R5_Invoice_Cancellation_Restores_Stock_To_Originating_Warehouse()
    {
        // Arrange: Create two separate warehouses
        var client = CreateAdminClient();
        var wh1 = await CreateWarehouseAsync(client, "WH1");
        var wh2 = await CreateWarehouseAsync(client, "WH2");

        var product = await CreateTestProductAsync(client, "R5-RESTORE", pricePaise: 250000, gstRate: 18.0m);
        await EnsureStockOnHandAsync(client, wh1.Id, product.Id, 50m);
        await EnsureStockOnHandAsync(client, wh2.Id, product.Id, 50m);

        // Bill 5 units from WH2
        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = wh2.Id,
            Lines = [new PosSaleLineRequest { ProductId = product.Id, Quantity = 5m }],
            Payments = [new PosPaymentRequest { PaymentMethod = "Cash", AmountPaise = 1475000 }]
        };
        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);
        checkoutRes.EnsureSuccessStatusCode();
        var invoice = (await checkoutRes.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data;

        // Cancel Invoice
        var cancelRes = await client.PostAsJsonAsync($"/api/v1/sales/invoices/{invoice.Id}/cancel", new CancelInvoiceRequest
        {
            Reason = "Test multi-warehouse cancellation"
        });
        cancelRes.EnsureSuccessStatusCode();

        // Assert: Stock on WH2 restored to 50m, WH1 stock remained exactly 50m
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var balanceWh1 = await db.StockBalances.FirstAsync(b => b.WarehouseId == wh1.Id && b.ProductId == product.Id);
        var balanceWh2 = await db.StockBalances.FirstAsync(b => b.WarehouseId == wh2.Id && b.ProductId == product.Id);

        Assert.Equal(50m, balanceWh1.QuantityOnHand);
        Assert.Equal(50m, balanceWh2.QuantityOnHand);
    }

    // ==========================================
    // R6: Bank Reconciliation Safeguards
    // ==========================================

    [Fact]
    public async Task R6_Bank_Reconciliation_Rejects_Invalid_Gl_Account_Match()
    {
        // Arrange
        var client = CreateAdminClient();

        // 1. Resolve Bank GL Account (1112)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var bankGlAcc = await db.Accounts.FirstAsync(a => a.Code == "1112");

        var bankReq = new CreateBankCashAccountRequest
        {
            AccountName = $"Operating Current Bank {Guid.NewGuid().ToString("N")[..4]}",
            AccountType = "Bank",
            AccountNumber = $"502000{Random.Shared.Next(10000000, 99999999)}",
            BankName = "HDFC Bank",
            BranchName = "Koramangala",
            IfscCode = "HDFC0001234",
            GlAccountId = bankGlAcc.Id,
            OpeningBalancePaise = 10000000
        };
        var bankRes = await client.PostAsJsonAsync("/api/v1/finance/bank-accounts", bankReq);
        bankRes.EnsureSuccessStatusCode();
        var bankAccount = (await bankRes.Content.ReadFromJsonAsync<ApiResponse<BankCashAccountDto>>())!.Data;

        // 2. Import CSV Statement
        var csvContent = "Date,Description,Reference,Debit,Credit,Balance\n2026-11-01,Deposit,REF123,0,50000,10050000\n";
        var importReq = new ImportBankStatementRequest
        {
            BankAccountId = bankAccount.Id,
            StatementStartDate = new DateOnly(2026, 11, 1),
            StatementEndDate = new DateOnly(2026, 11, 30),
            OpeningBalancePaise = 10000000,
            ClosingBalancePaise = 10050000,
            Filename = "HDFC_Nov_2026.csv",
            CsvContent = csvContent
        };
        var uploadRes = await client.PostAsJsonAsync("/api/v1/finance/bank-reconciliation/import-csv", importReq);
        uploadRes.EnsureSuccessStatusCode();
        var stmtDetail = (await uploadRes.Content.ReadFromJsonAsync<ApiResponse<BankStatementDetailDto>>())!.Data;

        // 3. Attempt to match statement line with a non-bank GL line (Revenue line 4101)
        var salesLine = (await db.JournalLines.Take(20).ToListAsync()).First(l => l.Credit.Paise > 0);

        var matchReq = new MatchStatementLineRequest
        {
            StatementLineId = stmtDetail.Lines[0].Id,
            JournalLineId = salesLine.Id,
            MatchType = "MANUAL"
        };

        var matchRes = await client.PostAsJsonAsync(
            $"/api/v1/finance/bank-reconciliation/match?statementId={stmtDetail.Id}",
            matchReq);

        // Assert: Rejection due to GL Account mismatch (400 Bad Request)
        Assert.Equal(HttpStatusCode.BadRequest, matchRes.StatusCode);
    }

    // ==========================================
    // R7: Inter-State Procurement IGST Accounting
    // ==========================================

    [Fact]
    public async Task R7_Supplier_Bill_InterState_Posts_To_Input_IGST()
    {
        // Arrange
        var client = CreateAdminClient();

        // 1. Create Out-of-State Supplier (Maharashtra)
        var uniqueDigits = Random.Shared.Next(1000, 9999);
        var suppReq = new CreateSupplierRequest
        {
            SupplierCode = $"SUPP-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
            Name = "Interstate Tech Supplies Mumbai",
            Category = "Manufacturer",
            GstType = GstRegistrationType.Registered,
            Gstin = $"27BBBBB{uniqueDigits}B1Z8",
            Phone = "+91 9123456789",
            Email = "accounts@mumbaitech.com",
            PaymentTerms = "Net 30",
            CreditPeriodDays = 30
        };
        var suppRes = await client.PostAsJsonAsync("/api/v1/commercial/suppliers", suppReq);
        suppRes.EnsureSuccessStatusCode();
        var supplier = (await suppRes.Content.ReadFromJsonAsync<ApiResponse<SupplierDetailDto>>())!.Data;

        // 2. Create Inter-State Supplier Bill: ₹10,000 subtotal + 18% IGST (₹1,800) = ₹11,800
        var billReq = new CreateSupplierBillRequest
        {
            SupplierId = supplier.Id,
            InvoiceNumber = $"VEND-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            IsInterState = true,
            Lines =
            [
                new CreateSupplierBillLineRequest
                {
                    Description = "Server Rack Hardware",
                    Quantity = 1m,
                    UnitPricePaise = 1000000,
                    TaxRatePercentage = 18.0m
                }
            ]
        };

        var billRes = await client.PostAsJsonAsync("/api/v1/accounting/payables/bills", billReq);
        billRes.EnsureSuccessStatusCode();
        var bill = (await billRes.Content.ReadFromJsonAsync<ApiResponse<SupplierBillDto>>())!.Data;

        // Assert: General Ledger voucher posted strictly to 1133 (Input IGST)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var voucher = await db.Vouchers
            .Include(v => v.Lines)
            .FirstOrDefaultAsync(v => v.Id == bill.VoucherId);

        Assert.NotNull(voucher);
        Assert.True(voucher.IsBalanced);

        var accIgst = await db.Accounts.FirstAsync(a => a.Code == "1133");
        var igstLine = voucher.Lines.FirstOrDefault(l => l.AccountId == accIgst.Id);
        Assert.NotNull(igstLine);
        Assert.Equal(180000, igstLine.Debit.Paise);

        // CGST and SGST must have zero lines for inter-state procurement
        var accCgst = await db.Accounts.FirstAsync(a => a.Code == "1131");
        var accSgst = await db.Accounts.FirstAsync(a => a.Code == "1132");
        Assert.DoesNotContain(voucher.Lines, l => l.AccountId == accCgst.Id);
        Assert.DoesNotContain(voucher.Lines, l => l.AccountId == accSgst.Id);
    }
}
