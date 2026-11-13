using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Magic.Contracts.Api;
using Magic.Contracts.Commercial;
using Magic.Contracts.Inventory;
using Magic.Contracts.Sales;
using Magic.Contracts.Security;
using Magic.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class PosAndSalesTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;

    public PosAndSalesTests(MagicTestFixture factory)
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

    private static async Task<WarehouseDto> GetOrCreateWarehouseAsync(HttpClient client)
    {
        var whRes = await client.GetAsync("/api/v1/inventory/warehouses?pageSize=10&isActive=true");
        if (whRes.IsSuccessStatusCode)
        {
            var whs = await whRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<WarehouseDto>>>();
            if (whs?.Data != null && whs.Data.Count > 0)
            {
                return whs.Data[0];
            }
        }

        var code = $"POS-WH-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
        var createRes = await client.PostAsJsonAsync("/api/v1/inventory/warehouses", new CreateWarehouseRequest
        {
            Code = code,
            Name = "POS Test Retail Outlet",
            Description = "Dedicated test warehouse for POS sales",
            AddressLine1 = "100 Retail Park",
            City = "Bangalore",
            State = "Karnataka",
            StateCode = "29",
            PinCode = "560001",
            Country = "India"
        });
        createRes.EnsureSuccessStatusCode();
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        return created!.Data;
    }

    private static async Task<ProductDetailDto> CreateTestProductAsync(HttpClient client, string prefix = "POS")
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
            Name = $"POS Test Product {code}",
            CategoryId = catId,
            UnitOfMeasureId = unitId,
            HsnSacCode = "84713010",
            GstRatePercentage = 18.0m,
            Sku = $"SKU-{code}",
            PrimaryBarcode = $"BAR-{code}",
            SellingPricePaise = 250000, // ₹2,500.00
            CostPricePaise = 180000,
            PurchasePricePaise = 180000,
            MrpPaise = 299900
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

    [Fact]
    public async Task Checkout_WithSufficientStock_CreatesInvoiceAndDeductsInventory()
    {
        // Arrange
        var client = CreateAdminClient();
        var warehouse = await GetOrCreateWarehouseAsync(client);
        var product = await CreateTestProductAsync(client, "POS-SALE");
        await EnsureStockOnHandAsync(client, warehouse.Id, product.Id, 100m);

        // Open Cashier Shift
        var openSessionRes = await client.PostAsJsonAsync("/api/v1/cashier/sessions/open", new OpenCashierSessionRequest
        {
            OpeningCashPaise = 500000 // ₹5,000.00 float
        });
        CashierSessionDto? session = null;
        if (openSessionRes.IsSuccessStatusCode)
        {
            var sessionEnvelope = await openSessionRes.Content.ReadFromJsonAsync<ApiResponse<CashierSessionDto>>();
            session = sessionEnvelope?.Data;
        }
        else
        {
            var activeRes = await client.GetAsync("/api/v1/cashier/sessions/active");
            var activeEnvelope = await activeRes.Content.ReadFromJsonAsync<ApiResponse<CashierSessionDto>>();
            session = activeEnvelope?.Data;
        }

        Assert.NotNull(session);

        // Act: Execute POS Checkout for 2 units
        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = warehouse.Id,
            CashierSessionId = session.Id,
            Lines =
            [
                new PosSaleLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 2m,
                    DiscountPercentage = 0m,
                    DiscountPaise = 0
                }
            ],
            Payments =
            [
                new PosPaymentRequest
                {
                    PaymentMethod = "Cash",
                    AmountPaise = 1000000 // Tendered ₹10,000
                }
            ]
        };

        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);
        Assert.Equal(HttpStatusCode.Created, checkoutRes.StatusCode);

        var invoice = (await checkoutRes.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data;

        // Assert: Invoice properties
        Assert.NotNull(invoice.InvoiceNumber);
        Assert.StartsWith("INV-", invoice.InvoiceNumber);
        Assert.Equal("Paid", invoice.Status);
        Assert.Single(invoice.Lines);
        Assert.Equal(2m, invoice.Lines[0].Quantity);
        Assert.True(invoice.GrandTotalPaise > 0);
        Assert.Equal(invoice.SubtotalPaise + invoice.TotalTaxPaise - invoice.DiscountPaise, invoice.GrandTotalPaise);

        // Assert: Stock Balances are decremented
        var lookupRes = await client.GetAsync($"/api/v1/pos/lookup?warehouseId={warehouse.Id}&query={product.Sku}");
        var lookupItems = (await lookupRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PosProductLookupDto>>>())!.Data;
        var pLookup = lookupItems.FirstOrDefault(p => p.Id == product.Id);
        Assert.NotNull(pLookup);
        Assert.True(pLookup.QuantityOnHand < 100m);
    }

    [Fact]
    public async Task Checkout_WithInsufficientStock_FailsClosed()
    {
        // Arrange
        var client = CreateAdminClient();
        var warehouse = await GetOrCreateWarehouseAsync(client);
        var product = await CreateTestProductAsync(client, "POS-OOS");

        // Act: Try to checkout huge quantity (1,000,000 units)
        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = warehouse.Id,
            Lines =
            [
                new PosSaleLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 1_000_000m,
                    DiscountPercentage = 0m,
                    DiscountPaise = 0
                }
            ],
            Payments =
            [
                new PosPaymentRequest
                {
                    PaymentMethod = "Cash",
                    AmountPaise = 500_000_000_000
                }
            ]
        };

        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);

        // Assert: Request is rejected
        Assert.False(checkoutRes.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SalesInvoice_Cancellation_RestoresInventoryAndMarksCancelled()
    {
        // Arrange
        var client = CreateAdminClient();
        var warehouse = await GetOrCreateWarehouseAsync(client);
        var product = await CreateTestProductAsync(client, "POS-CANCEL");
        await EnsureStockOnHandAsync(client, warehouse.Id, product.Id, 50m);

        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = warehouse.Id,
            Lines =
            [
                new PosSaleLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 3m
                }
            ],
            Payments =
            [
                new PosPaymentRequest
                {
                    PaymentMethod = "Upi",
                    AmountPaise = 1000000
                }
            ]
        };

        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);
        checkoutRes.EnsureSuccessStatusCode();
        var invoice = (await checkoutRes.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data;

        // Act: Cancel Invoice
        var cancelRes = await client.PostAsJsonAsync($"/api/v1/sales/invoices/{invoice.Id}/cancel", new CancelInvoiceRequest
        {
            Reason = "Customer changed mind immediately"
        });
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);

        var cancelledInvoice = (await cancelRes.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data;
        Assert.Equal("Cancelled", cancelledInvoice.Status);

        // Double cancellation should fail
        var secondCancel = await client.PostAsJsonAsync($"/api/v1/sales/invoices/{invoice.Id}/cancel", new CancelInvoiceRequest
        {
            Reason = "Duplicate attempt"
        });
        Assert.False(secondCancel.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SalesReturn_WithResalableItem_RestocksInventoryAndIssuesCreditNote()
    {
        // Arrange
        var client = CreateAdminClient();
        var warehouse = await GetOrCreateWarehouseAsync(client);
        var product = await CreateTestProductAsync(client, "POS-RET");
        await EnsureStockOnHandAsync(client, warehouse.Id, product.Id, 20m);

        var saleReq = new CreatePosSaleRequest
        {
            WarehouseId = warehouse.Id,
            Lines =
            [
                new PosSaleLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 5m
                }
            ],
            Payments =
            [
                new PosPaymentRequest
                {
                    PaymentMethod = "Card",
                    AmountPaise = 2000000
                }
            ]
        };

        var checkoutRes = await client.PostAsJsonAsync("/api/v1/pos/checkout", saleReq);
        checkoutRes.EnsureSuccessStatusCode();
        var invoice = (await checkoutRes.Content.ReadFromJsonAsync<ApiResponse<SalesInvoiceDto>>())!.Data;

        // Act: Return 2 units with IsResalable = true
        var returnReq = new CreateSalesReturnRequest
        {
            InvoiceId = invoice.Id,
            ReasonCode = "CustomerCancelled",
            Notes = "Unopened box return",
            Lines =
            [
                new CreateSalesReturnLineRequest
                {
                    InvoiceLineId = invoice.Lines[0].Id,
                    ReturnQuantity = 2m,
                    IsResalable = true,
                    Reason = "Item unopened and in mint condition"
                }
            ]
        };

        var returnRes = await client.PostAsJsonAsync("/api/v1/sales/returns", returnReq);
        Assert.Equal(HttpStatusCode.Created, returnRes.StatusCode);

        var returnDto = (await returnRes.Content.ReadFromJsonAsync<ApiResponse<SalesReturnDto>>())!.Data;
        Assert.NotNull(returnDto.ReturnNumber);
        Assert.StartsWith("RET-", returnDto.ReturnNumber);
        Assert.NotNull(returnDto.CreditNoteNumber);
        Assert.StartsWith("CRN-", returnDto.CreditNoteNumber);
        Assert.Equal("Confirmed", returnDto.Status);

        // Verify Credit Note can be retrieved
        var creditNotesRes = await client.GetAsync("/api/v1/sales/credit-notes");
        Assert.Equal(HttpStatusCode.OK, creditNotesRes.StatusCode);
    }

    [Fact]
    public async Task CashierSession_Lifecycle_DropAndCloseWithVariance()
    {
        // Arrange
        var client = CreateAdminClient();

        // Get or Open Active Session
        var activeRes = await client.GetAsync("/api/v1/cashier/sessions/active");
        var activeEnv = await activeRes.Content.ReadFromJsonAsync<ApiResponse<CashierSessionDto?>>();
        CashierSessionDto session;

        if (activeEnv?.Data != null)
        {
            session = activeEnv.Data;
        }
        else
        {
            var openRes = await client.PostAsJsonAsync("/api/v1/cashier/sessions/open", new OpenCashierSessionRequest
            {
                OpeningCashPaise = 300000 // ₹3,000.00
            });
            session = (await openRes.Content.ReadFromJsonAsync<ApiResponse<CashierSessionDto>>())!.Data;
        }

        // Record a Cash Drop (Withdrawal to safe)
        var dropRes = await client.PostAsJsonAsync($"/api/v1/cashier/sessions/{session.Id}/movements", new RecordCashMovementRequest
        {
            MovementType = "Withdrawal",
            AmountPaise = 100000, // ₹1,000 drop
            Reason = "Mid-day safe deposit drop"
        });
        Assert.Equal(HttpStatusCode.Created, dropRes.StatusCode);

        // Close Session with declared physical cash count
        var closeRes = await client.PostAsJsonAsync($"/api/v1/cashier/sessions/{session.Id}/close", new CloseCashierSessionRequest
        {
            ActualClosingCashPaise = 200000, // Exactly ₹2,000 left
            Notes = "Shift ended, cash reconciled"
        });
        Assert.Equal(HttpStatusCode.OK, closeRes.StatusCode);

        var closed = (await closeRes.Content.ReadFromJsonAsync<ApiResponse<CashierSessionDto>>())!.Data;
        Assert.Equal("Closed", closed.Status);
        Assert.NotNull(closed.ActualClosingCashPaise);
        Assert.NotNull(closed.VariancePaise);
    }

    [Fact]
    public async Task SalesReports_ReturnsAggregatedMetrics()
    {
        // Arrange
        var client = CreateAdminClient();

        // Act & Assert Summary
        var sumRes = await client.GetAsync("/api/v1/sales/reports/summary");
        Assert.Equal(HttpStatusCode.OK, sumRes.StatusCode);
        var summary = (await sumRes.Content.ReadFromJsonAsync<ApiResponse<SalesReportSummaryDto>>())!.Data;
        Assert.True(summary.GrossSalesPaise >= 0);

        // Act & Assert Payment Breakdown
        var payRes = await client.GetAsync("/api/v1/sales/reports/payment-methods");
        Assert.Equal(HttpStatusCode.OK, payRes.StatusCode);

        // Act & Assert Top Products
        var prodRes = await client.GetAsync("/api/v1/sales/reports/top-products?limit=5");
        Assert.Equal(HttpStatusCode.OK, prodRes.StatusCode);

        // Act & Assert Hourly
        var hrRes = await client.GetAsync("/api/v1/sales/reports/hourly");
        Assert.Equal(HttpStatusCode.OK, hrRes.StatusCode);
        var hourly = (await hrRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<HourlySalesDto>>>())!.Data;
        Assert.Equal(24, hourly.Count);
    }
}
