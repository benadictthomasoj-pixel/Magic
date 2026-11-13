using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Magic.Contracts.Api;
using Magic.Contracts.Commercial;
using Magic.Contracts.Inventory;
using Magic.Contracts.Security;
using Magic.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class InventoryAndProcurementTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;

    public InventoryAndProcurementTests(MagicTestFixture factory)
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

    private static async Task<WarehouseDto> GetOrCreateWarehouseAsync(HttpClient client, string suffix = "")
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

        return await CreateWarehouseAsync(client, suffix);
    }

    private static async Task<WarehouseDto> CreateWarehouseAsync(HttpClient client, string suffix = "")
    {
        var createReq = new CreateWarehouseRequest
        {
            Code = $"WH-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            Name = string.IsNullOrWhiteSpace(suffix) ? "Standard Test Warehouse" : $"Test Warehouse {suffix}",
            AddressLine1 = "123 Auto Lane",
            City = "Chennai",
            State = "Tamil Nadu",
            StateCode = "33",
            PinCode = "600001",
            Country = "India",
            CapacitySqFt = 50000m,
            AssignedBranchIds = [Guid.Parse("22222222-2222-2222-2222-222222222222")]
        };
        var createRes = await client.PostAsJsonAsync("/api/v1/inventory/warehouses", createReq);
        createRes.EnsureSuccessStatusCode();
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        return created!.Data;
    }

    [Fact]
    public async Task Warehouse_Create_Get_Update_And_Locations_Workflow_Succeeds()
    {
        var client = CreateAdminClient();
        var code = $"WH-TEST-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        // 1. Create Warehouse
        var createReq = new CreateWarehouseRequest
        {
            Code = code,
            Name = "Central Logistics Hub",
            AddressLine1 = "100 Industrial Area",
            City = "Chennai",
            State = "Tamil Nadu",
            StateCode = "33",
            PinCode = "600001",
            Country = "India",
            CapacitySqFt = 50000m,
            AssignedBranchIds = [Guid.Parse("22222222-2222-2222-2222-222222222222")]
        };

        var createRes = await client.PostAsJsonAsync("/api/v1/inventory/warehouses", createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var createdEnvelope = await createRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        Assert.NotNull(createdEnvelope?.Data);
        var warehouseId = createdEnvelope.Data.Id;
        Assert.Equal(code, createdEnvelope.Data.Code);

        // 2. Get Warehouse by ID
        var getRes = await client.GetAsync($"/api/v1/inventory/warehouses/{warehouseId}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var getEnvelope = await getRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDto>>();
        Assert.Equal("Central Logistics Hub", getEnvelope?.Data.Name);
        Assert.Single(getEnvelope!.Data.AssignedBranchIds);

        // 3. Create Storage Location
        var locReq = new CreateStorageLocationRequest
        {
            Code = "A1-R1-B1",
            Name = "Aisle 1 Rack 1 Bin 1",
            LocationType = "Bin"
        };
        var locRes = await client.PostAsJsonAsync($"/api/v1/inventory/warehouses/{warehouseId}/locations", locReq);
        Assert.Equal(HttpStatusCode.Created, locRes.StatusCode);

        // 4. List Locations
        var locListRes = await client.GetAsync($"/api/v1/inventory/warehouses/{warehouseId}/locations");
        Assert.Equal(HttpStatusCode.OK, locListRes.StatusCode);
        var locList = await locListRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StorageLocationDto>>>();
        Assert.Contains(locList!.Data, l => l.Code == "A1-R1-B1");

        // 5. Update Warehouse
        var updateReq = new UpdateWarehouseRequest
        {
            Name = "Central Logistics Hub - Updated",
            AddressLine1 = "100 Industrial Area",
            City = "Chennai",
            State = "Tamil Nadu",
            StateCode = "33",
            PinCode = "600001",
            CapacitySqFt = 65000m,
            IsActive = true
        };
        var updateRes = await client.PutAsJsonAsync($"/api/v1/inventory/warehouses/{warehouseId}", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        // 6. Archive Warehouse
        var archiveRes = await client.PostAsync($"/api/v1/inventory/warehouses/{warehouseId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archiveRes.StatusCode);
    }

    [Fact]
    public async Task OpeningStock_And_InventoryLedger_UpdatesBalances_And_RecordsMovements()
    {
        var client = CreateAdminClient();

        // 1. Get an existing product and warehouse
        var prodRes = await client.GetAsync("/api/v1/commercial/products?pageSize=1");
        var prods = await prodRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ProductSummaryDto>>>();
        Assert.NotEmpty(prods!.Data);
        var product = prods.Data[0];

        var warehouse = await GetOrCreateWarehouseAsync(client, "OpeningStock");
        var warehouseId = warehouse.Id;
        var branchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // 2. Post Opening Stock
        var openReq = new PostOpeningStockRequest
        {
            WarehouseId = warehouseId,
            BranchId = branchId,
            Items =
            [
                new OpeningStockItemRequest
                {
                    ProductId = product.Id,
                    Quantity = 50m,
                    UnitCostPaise = 25000, // 250.00 INR
                    Reason = "Warehouse opening stock verification"
                }
            ]
        };

        var openRes = await client.PostAsJsonAsync("/api/v1/inventory/stock/opening-stock", openReq);
        Assert.Equal(HttpStatusCode.OK, openRes.StatusCode);

        // 3. Verify Stock Balance
        var balRes = await client.GetAsync($"/api/v1/inventory/stock/balances?warehouseId={warehouseId}&productId={product.Id}");
        Assert.Equal(HttpStatusCode.OK, balRes.StatusCode);
        var balEnvelope = await balRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StockBalanceDto>>>();
        Assert.NotEmpty(balEnvelope!.Data);
        var balance = balEnvelope.Data[0];
        Assert.True(balance.QuantityOnHand >= 50m);
        Assert.True(balance.QuantityAvailable >= 50m);

        // 4. Verify Movement Ledger Record
        var movRes = await client.GetAsync($"/api/v1/inventory/stock/movements?warehouseId={warehouseId}&productId={product.Id}&movementType=OpeningStock");
        Assert.Equal(HttpStatusCode.OK, movRes.StatusCode);
        var movEnvelope = await movRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StockMovementDto>>>();
        Assert.NotEmpty(movEnvelope!.Data);
        var mov = movEnvelope.Data[0];
        Assert.Equal("In", mov.Direction);
        Assert.Equal(50m, mov.Quantity);
    }

    [Fact]
    public async Task PurchaseOrder_Approval_DoesNotIncrease_PhysicalStock()
    {
        var client = CreateAdminClient();

        var prodRes = await client.GetAsync("/api/v1/commercial/products?pageSize=1");
        var prods = await prodRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ProductSummaryDto>>>();
        var product = prods!.Data[0];

        var suppRes = await client.GetAsync("/api/v1/commercial/suppliers?pageSize=1");
        var supps = await suppRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SupplierSummaryDto>>>();
        var supplier = supps!.Data[0];

        var warehouse = await GetOrCreateWarehouseAsync(client, "PO");

        // 1. Get initial stock
        var initialBalRes = await client.GetAsync($"/api/v1/inventory/stock/balances?warehouseId={warehouse.Id}&productId={product.Id}");
        var initialBals = await initialBalRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StockBalanceDto>>>();
        var initialOnHand = initialBals?.Data != null && initialBals.Data.Count > 0 ? initialBals.Data[0].QuantityOnHand : 0m;

        // 2. Create Purchase Order
        var poReq = new CreatePurchaseOrderRequest
        {
            BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            WarehouseId = warehouse.Id,
            SupplierId = supplier.Id,
            PaymentTerms = "Net 30",
            Lines =
            [
                new CreatePurchaseOrderLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 100m,
                    UnitPricePaise = 30000,
                    GstRatePercentage = 18m
                }
            ]
        };

        var createPoRes = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders", poReq);
        Assert.Equal(HttpStatusCode.Created, createPoRes.StatusCode);
        var po = (await createPoRes.Content.ReadFromJsonAsync<ApiResponse<PurchaseOrderDto>>())!.Data;
        Assert.Equal("Draft", po.Status);

        // 3. Approve Purchase Order
        var appRes = await client.PostAsync($"/api/v1/procurement/purchase-orders/{po.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);
        var approvedPo = (await appRes.Content.ReadFromJsonAsync<ApiResponse<PurchaseOrderDto>>())!.Data;
        Assert.Equal("Approved", approvedPo.Status);

        // 4. Verify Physical Inventory has NOT increased
        var postBalRes = await client.GetAsync($"/api/v1/inventory/stock/balances?warehouseId={warehouse.Id}&productId={product.Id}");
        var postBals = await postBalRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StockBalanceDto>>>();
        var postOnHand = postBals?.Data != null && postBals.Data.Count > 0 ? postBals.Data[0].QuantityOnHand : 0m;
        Assert.Equal(initialOnHand, postOnHand);
    }

    [Fact]
    public async Task GoodsReceiptNote_Confirm_Atomically_IncreasesStock_And_FulfillsPO()
    {
        var client = CreateAdminClient();

        var prodRes = await client.GetAsync("/api/v1/commercial/products?pageSize=1");
        var prods = await prodRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ProductSummaryDto>>>();
        var product = prods!.Data[0];

        var suppRes = await client.GetAsync("/api/v1/commercial/suppliers?pageSize=1");
        var supps = await suppRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SupplierSummaryDto>>>();
        var supplier = supps!.Data[0];

        var warehouse = await GetOrCreateWarehouseAsync(client, "PO");

        // 1. Create and Approve a Purchase Order
        var poReq = new CreatePurchaseOrderRequest
        {
            BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            WarehouseId = warehouse.Id,
            SupplierId = supplier.Id,
            Lines =
            [
                new CreatePurchaseOrderLineRequest
                {
                    ProductId = product.Id,
                    Quantity = 20m,
                    UnitPricePaise = 15000,
                    GstRatePercentage = 18m
                }
            ]
        };
        var createPoRes = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders", poReq);
        var po = (await createPoRes.Content.ReadFromJsonAsync<ApiResponse<PurchaseOrderDto>>())!.Data;
        await client.PostAsync($"/api/v1/procurement/purchase-orders/{po.Id}/approve", null);

        // Get initial stock
        var preBalRes = await client.GetAsync($"/api/v1/inventory/stock/balances?warehouseId={warehouse.Id}&productId={product.Id}");
        var preBals = await preBalRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StockBalanceDto>>>();
        var preOnHand = preBals?.Data != null && preBals.Data.Count > 0 ? preBals.Data[0].QuantityOnHand : 0m;

        // 2. Create GRN
        var grnReq = new CreateGoodsReceiptRequest
        {
            BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            WarehouseId = warehouse.Id,
            PurchaseOrderId = po.Id,
            SupplierId = supplier.Id,
            SupplierInvoiceNumber = "INV-SUP-9988",
            SupplierInvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines =
            [
                new CreateGoodsReceiptLineRequest
                {
                    PurchaseOrderLineId = po.Lines[0].Id,
                    ProductId = product.Id,
                    ReceivedQuantity = 20m,
                    AcceptedQuantity = 20m,
                    UnitPricePaise = 15000,
                    GstRatePercentage = 18m
                }
            ]
        };

        var grnCreateRes = await client.PostAsJsonAsync("/api/v1/procurement/goods-receipts", grnReq);
        Assert.Equal(HttpStatusCode.Created, grnCreateRes.StatusCode);
        var grn = (await grnCreateRes.Content.ReadFromJsonAsync<ApiResponse<GoodsReceiptNoteDto>>())!.Data;
        Assert.Equal("Draft", grn.Status);

        // 3. Confirm GRN
        var confirmRes = await client.PostAsync($"/api/v1/procurement/goods-receipts/{grn.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);
        var confirmedGrn = (await confirmRes.Content.ReadFromJsonAsync<ApiResponse<GoodsReceiptNoteDto>>())!.Data;
        Assert.Equal("Confirmed", confirmedGrn.Status);

        // 4. Verify StockOnHand increased by 20m
        var postBalRes = await client.GetAsync($"/api/v1/inventory/stock/balances?warehouseId={warehouse.Id}&productId={product.Id}");
        var postBals = await postBalRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StockBalanceDto>>>();
        var postOnHand = postBals!.Data[0].QuantityOnHand;
        Assert.Equal(preOnHand + 20m, postOnHand);

        // 5. Verify PO status is Completed
        var postPoRes = await client.GetAsync($"/api/v1/procurement/purchase-orders/{po.Id}");
        var postPo = (await postPoRes.Content.ReadFromJsonAsync<ApiResponse<PurchaseOrderDto>>())!.Data;
        Assert.Equal("Completed", postPo.Status);
    }

    [Fact]
    public async Task StockTransfer_Dispatch_And_Receive_TwoPhase_Workflow_Succeeds()
    {
        var client = CreateAdminClient();

        var prodRes = await client.GetAsync("/api/v1/commercial/products?pageSize=1");
        var prods = await prodRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ProductSummaryDto>>>();
        var product = prods!.Data[0];

        // Ensure 2 dedicated warehouses exist
        var sourceWh = await CreateWarehouseAsync(client, "TransferSource");
        var destWh = await CreateWarehouseAsync(client, "TransferDest");

        // Inward stock into source warehouse
        await client.PostAsJsonAsync("/api/v1/inventory/stock/opening-stock", new PostOpeningStockRequest
        {
            WarehouseId = sourceWh.Id,
            BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Items =
            [
                new OpeningStockItemRequest
                {
                    ProductId = product.Id,
                    Quantity = 30m,
                    UnitCostPaise = 10000
                }
            ]
        });

        // 1. Create Transfer Request
        var trnReq = new CreateStockTransferRequest
        {
            SourceBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            SourceWarehouseId = sourceWh.Id,
            DestinationBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DestinationWarehouseId = destWh.Id,
            Lines =
            [
                new CreateStockTransferLineRequest
                {
                    ProductId = product.Id,
                    RequestedQuantity = 10m
                }
            ]
        };

        var trnRes = await client.PostAsJsonAsync("/api/v1/inventory/transfers", trnReq);
        Assert.Equal(HttpStatusCode.Created, trnRes.StatusCode);
        var trn = (await trnRes.Content.ReadFromJsonAsync<ApiResponse<StockTransferDto>>())!.Data;
        Assert.Equal("Draft", trn.Status);

        // 2. Dispatch Transfer
        var dispRes = await client.PostAsync($"/api/v1/inventory/transfers/{trn.Id}/dispatch", null);
        Assert.Equal(HttpStatusCode.OK, dispRes.StatusCode);
        var dispatchedTrn = (await dispRes.Content.ReadFromJsonAsync<ApiResponse<StockTransferDto>>())!.Data;
        Assert.Equal("Dispatched", dispatchedTrn.Status);

        // 3. Receive Transfer at Destination
        var recReq = new ReceiveStockTransferRequest
        {
            Lines =
            [
                new ReceiveStockTransferLineRequest
                {
                    LineId = dispatchedTrn.Lines[0].Id,
                    ReceivedQuantity = 10m
                }
            ]
        };
        var recRes = await client.PostAsJsonAsync($"/api/v1/inventory/transfers/{trn.Id}/receive", recReq);
        Assert.Equal(HttpStatusCode.OK, recRes.StatusCode);
        var receivedTrn = (await recRes.Content.ReadFromJsonAsync<ApiResponse<StockTransferDto>>())!.Data;
        Assert.Equal("Received", receivedTrn.Status);

        // 4. Verify destination warehouse has received stock
        var destBalRes = await client.GetAsync($"/api/v1/inventory/stock/balances?warehouseId={destWh.Id}&productId={product.Id}");
        var destBals = (await destBalRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StockBalanceDto>>>())!.Data;
        Assert.NotEmpty(destBals);
        Assert.Equal(10m, destBals[0].QuantityOnHand);
    }

    [Fact]
    public async Task StockAdjustment_And_Counting_Reconciliation_Succeeds()
    {
        var client = CreateAdminClient();

        var prodRes = await client.GetAsync("/api/v1/commercial/products?pageSize=1");
        var prods = await prodRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ProductSummaryDto>>>();
        var product = prods!.Data[0];

        var warehouse = await GetOrCreateWarehouseAsync(client, "Reorder");

        // 1. Inward initial 10 units
        await client.PostAsJsonAsync("/api/v1/inventory/stock/opening-stock", new PostOpeningStockRequest
        {
            WarehouseId = warehouse.Id,
            BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Items = [new OpeningStockItemRequest { ProductId = product.Id, Quantity = 10m, UnitCostPaise = 5000 }]
        });

        // 2. Create Stock Adjustment (+5 units)
        var adjReq = new CreateStockAdjustmentRequest
        {
            BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            WarehouseId = warehouse.Id,
            ReasonCode = "Found",
            Description = "Found 5 unrecorded units in storage",
            Lines =
            [
                new CreateStockAdjustmentLineRequest
                {
                    ProductId = product.Id,
                    AdjustmentQuantity = 5m,
                    UnitCostPaise = 5000
                }
            ]
        };
        var adjRes = await client.PostAsJsonAsync("/api/v1/inventory/adjustments", adjReq);
        Assert.Equal(HttpStatusCode.Created, adjRes.StatusCode);
        var adj = (await adjRes.Content.ReadFromJsonAsync<ApiResponse<StockAdjustmentDto>>())!.Data;

        // Approve Adjustment
        var appAdjRes = await client.PostAsync($"/api/v1/inventory/adjustments/{adj.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, appAdjRes.StatusCode);

        // Verify balance is now 15 units
        var balRes = await client.GetAsync($"/api/v1/inventory/stock/balances?warehouseId={warehouse.Id}&productId={product.Id}");
        var bals = (await balRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<StockBalanceDto>>>())!.Data;
        Assert.True(bals[0].QuantityOnHand >= 15m);

        // 3. Create Stock Count session
        var countReq = new CreateStockCountRequest
        {
            BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            WarehouseId = warehouse.Id,
            Notes = "Monthly physical audit",
            ProductIds = [product.Id]
        };
        var countRes = await client.PostAsJsonAsync("/api/v1/inventory/counts", countReq);
        Assert.Equal(HttpStatusCode.Created, countRes.StatusCode);
        var countSession = (await countRes.Content.ReadFromJsonAsync<ApiResponse<StockCountDto>>())!.Data;

        // Record physical count: found 14 units (1 unit missing)
        var recordReq = new RecordStockCountLinesRequest
        {
            Lines =
            [
                new StockCountLineEntryRequest
                {
                    ProductId = product.Id,
                    CountedQuantity = bals[0].QuantityOnHand - 1m,
                    Notes = "1 unit short on shelf"
                }
            ]
        };
        var recordRes = await client.PostAsJsonAsync($"/api/v1/inventory/counts/{countSession.Id}/record-lines", recordReq);
        Assert.Equal(HttpStatusCode.OK, recordRes.StatusCode);

        // Reconcile Count
        var reconRes = await client.PostAsync($"/api/v1/inventory/counts/{countSession.Id}/reconcile", null);
        Assert.Equal(HttpStatusCode.OK, reconRes.StatusCode);
        var reconciled = (await reconRes.Content.ReadFromJsonAsync<ApiResponse<StockCountDto>>())!.Data;
        Assert.Equal("Reconciled", reconciled.Status);
    }

    [Fact]
    public async Task ReorderRules_And_Suggestions_Generation_Succeeds()
    {
        var client = CreateAdminClient();

        var prodRes = await client.GetAsync("/api/v1/commercial/products?pageSize=1");
        var prods = await prodRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ProductSummaryDto>>>();
        var product = prods!.Data[0];

        var warehouse = await GetOrCreateWarehouseAsync(client, "Reorder");

        // Configure Reorder Rule: Min 10, Max 50, ReorderPoint 25, Safety 5
        var ruleReq = new UpsertReorderRuleRequest
        {
            WarehouseId = warehouse.Id,
            ProductId = product.Id,
            MinStockLevel = 10m,
            MaxStockLevel = 50m,
            ReorderPoint = 25m,
            SafetyStock = 5m,
            LeadTimeDays = 7,
            ReorderQuantity = 30m,
            IsActive = true
        };

        var upsertRes = await client.PostAsJsonAsync("/api/v1/inventory/reorder/rules", ruleReq);
        Assert.Equal(HttpStatusCode.OK, upsertRes.StatusCode);
        var rule = (await upsertRes.Content.ReadFromJsonAsync<ApiResponse<ReorderRuleDto>>())!.Data;
        Assert.Equal(25m, rule.ReorderPoint);

        // Query Suggestions
        var sugRes = await client.GetAsync($"/api/v1/inventory/reorder/suggestions?warehouseId={warehouse.Id}");
        Assert.Equal(HttpStatusCode.OK, sugRes.StatusCode);
        var suggestions = (await sugRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ReorderSuggestionDto>>>())!.Data;
        Assert.NotNull(suggestions);
    }
}

