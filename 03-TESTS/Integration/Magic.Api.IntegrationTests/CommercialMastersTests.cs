using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Magic.Contracts.Api;
using Magic.Contracts.Commercial;
using Magic.Contracts.Security;
using Magic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class CommercialMastersTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;
    private string _cashierToken = string.Empty;

    public CommercialMastersTests(MagicTestFixture factory)
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

        // Obtain admin bearer token
        var adminLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "ben",
            Password = "123123"
        });
        var adminAuth = await adminLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        _adminToken = adminAuth!.Data.AccessToken;

        // Obtain cashier bearer token (Cashier role lacks bank-details and delete permission)
        var cashierLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "cashier1",
            Password = "123123"
        });
        var cashierAuth = await cashierLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        _cashierToken = cashierAuth!.Data.AccessToken;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        return client;
    }

    private HttpClient CreateCashierClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cashierToken);
        return client;
    }

    [Fact]
    public async Task Products_GetProducts_ReturnsSuccessAndSeedProducts()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/commercial/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductSummaryDto>>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotEmpty(envelope.Data);
        Assert.Contains(envelope.Data, p => p.ProductCode == "PRD-001");
    }

    [Fact]
    public async Task Products_CreateProduct_ValidInput_Persists64BitIntegerPaise()
    {
        var client = CreateAdminClient();
        var uniqueCode = $"PRD-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var uniqueSku = $"SKU-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        // Fetch category & UOM
        var catsRes = await client.GetFromJsonAsync<ApiResponse<List<CategoryDto>>>("/api/v1/commercial/categories");
        var unitsRes = await client.GetFromJsonAsync<ApiResponse<List<UnitOfMeasureDto>>>("/api/v1/commercial/units");
        var categoryId = catsRes!.Data[0].Id;
        var unitId = unitsRes!.Data[0].Id;

        var request = new CreateProductRequest
        {
            ProductCode = uniqueCode,
            Name = "Organic Rolled Oats 500g",
            ProductType = ProductType.Goods,
            CategoryId = categoryId,
            UnitOfMeasureId = unitId,
            HsnSacCode = "1104",
            GstRatePercentage = 5.0m,
            Sku = uniqueSku,
            PrimaryBarcode = null,
            SellingPricePaise = 18500, // ₹185.00
            MrpPaise = 21000,          // ₹210.00
            PurchasePricePaise = 14000,
            CostPricePaise = 14500,
            MinSellingPricePaise = 17500,
            WholesalePricePaise = 16500,
            ReorderLevel = 10,
            MinStockLevel = 5,
            MaxStockLevel = 100
        };

        var response = await client.PostAsJsonAsync("/api/v1/commercial/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDetailDto>>();
        Assert.NotNull(created);
        Assert.Equal(uniqueCode, created.Data.ProductCode);
        Assert.Equal(18500, created.Data.SellingPricePaise);
        Assert.Equal(21000, created.Data.MrpPaise);
        Assert.NotNull(created.Data.Sku);
        Assert.NotNull(created.Data.PrimaryBarcode); // Auto-generated EAN-13
    }

    [Fact]
    public async Task Products_CreateProduct_DuplicateSku_FailsValidation()
    {
        var client = CreateAdminClient();
        var catsRes = await client.GetFromJsonAsync<ApiResponse<List<CategoryDto>>>("/api/v1/commercial/categories");
        var unitsRes = await client.GetFromJsonAsync<ApiResponse<List<UnitOfMeasureDto>>>("/api/v1/commercial/units");

        var duplicateSku = "GROC-RICE-001"; // Already belongs to PRD-001

        var request = new CreateProductRequest
        {
            ProductCode = $"PRD-DUP-{Guid.NewGuid().ToString("N")[..4]}",
            Name = "Duplicate Rice Attempt",
            CategoryId = catsRes!.Data[0].Id,
            UnitOfMeasureId = unitsRes!.Data[0].Id,
            HsnSacCode = "1006",
            Sku = duplicateSku,
            SellingPricePaise = 10000,
            MrpPaise = 12000
        };

        var response = await client.PostAsJsonAsync("/api/v1/commercial/products", request);
        Assert.True(response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.Conflict or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Categories_TreeEndpoint_ReturnsHierarchicalStructure()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/commercial/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();
        Assert.NotNull(envelope);
        Assert.NotEmpty(envelope.Data);
        Assert.Contains(envelope.Data, c => c.Code == "GROC");
    }

    [Fact]
    public async Task Categories_DeleteCategoryWithProducts_Returns422()
    {
        var client = CreateAdminClient();
        var catsRes = await client.GetFromJsonAsync<ApiResponse<List<CategoryDto>>>("/api/v1/commercial/categories");
        var groc = catsRes!.Data.First(c => c.Code == "GROC");

        var response = await client.DeleteAsync($"/api/v1/commercial/categories/{groc.Id}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(err);
        Assert.Contains("products", err.Error.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Customers_CreateCustomer_WithValidGstin_Succeeds()
    {
        var client = CreateAdminClient();
        var code = $"CUST-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var uniqueDigits = Random.Shared.Next(1000, 9999).ToString(CultureInfo.InvariantCulture);
        var uniqueGstin = $"33AAAAA{uniqueDigits}A1Z5";

        var request = new CreateCustomerRequest
        {
            CustomerCode = code,
            Name = "Vasantham Supermarket Coimbatore",
            CustomerType = CustomerType.Wholesale,
            GstType = GstRegistrationType.Registered,
            Gstin = uniqueGstin, // Valid 15-char Indian GSTIN
            Phone = "+91 9842100001",
            CreditLimitPaise = 5000000, // ₹50,000.00
            CreditPeriodDays = 30
        };

        var response = await client.PostAsJsonAsync("/api/v1/commercial/customers", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerDetailDto>>();
        Assert.NotNull(created);
        Assert.Equal(code, created.Data.CustomerCode);
        Assert.Equal(uniqueGstin, created.Data.Gstin);
    }

    [Fact]
    public async Task Customers_CreateCustomer_WithInvalidGstin_FailsValidation()
    {
        var client = CreateAdminClient();

        var request = new CreateCustomerRequest
        {
            CustomerCode = $"CUST-INV-{Guid.NewGuid().ToString("N")[..4]}",
            Name = "Invalid GSTIN Customer",
            CustomerType = CustomerType.Business,
            GstType = GstRegistrationType.Registered,
            Gstin = "INVALID_GST_NUMBER", // Malformed GSTIN
            Phone = "+91 9842100002"
        };

        var response = await client.PostAsJsonAsync("/api/v1/commercial/customers", request);
        Assert.True(response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Customers_UpdateCreditLimit_RecordsTamperEvidentAuditEvent()
    {
        var client = CreateAdminClient();

        // 1. Create a customer
        var code = $"CUST-CR-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var createRes = await client.PostAsJsonAsync("/api/v1/commercial/customers", new CreateCustomerRequest
        {
            CustomerCode = code,
            Name = "Credit Audit Test Customer",
            CustomerType = CustomerType.Wholesale,
            Phone = "+91 9999988888",
            CreditLimitPaise = 1000000, // ₹10,000
            CreditPeriodDays = 15
        });
        var customer = (await createRes.Content.ReadFromJsonAsync<ApiResponse<CustomerDetailDto>>())!.Data;

        // 2. Update credit limit with authorized reason
        var updateLimitRes = await client.PutAsJsonAsync(
            $"/api/v1/commercial/customers/{customer.Id}/credit-limit",
            new UpdateCreditLimitRequest
            {
                NewCreditLimitPaise = 2500000, // ₹25,000
                CreditPeriodDays = 30,
                IsCreditHold = false,
                Reason = "Approved after reviewing quarterly audited balance sheet."
            });

        Assert.Equal(HttpStatusCode.OK, updateLimitRes.StatusCode);

        // 3. Verify audit record in test database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var audit = await db.SecurityAuditEvents
            .Where(a => a.EventType == "CUSTOMER_CREDIT_LIMIT_MODIFIED")
            .OrderByDescending(a => a.TimestampUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(audit);
        Assert.Contains(code, audit.DetailsJson);
    }

    [Fact]
    public async Task Suppliers_CreateSupplier_EncryptsBankAtRestAndMasksDisplay()
    {
        var client = CreateAdminClient();
        var code = $"SUPP-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var uniqueDigits = Random.Shared.Next(1000, 9999).ToString(CultureInfo.InvariantCulture);
        var uniqueGstin = $"33BBBBB{uniqueDigits}B1Z2";
        var sensitiveAccount = "9876543210123456";

        var request = new CreateSupplierRequest
        {
            SupplierCode = code,
            Name = "Southern Mills Supply Ltd",
            Category = "Raw Materials",
            GstType = GstRegistrationType.Registered,
            Gstin = uniqueGstin,
            Phone = "+91 9843011111",
            PaymentTerms = "Net 30",
            CreditPeriodDays = 30,
            BankAccounts =
            [
                new CreateSupplierBankAccountDto
                {
                    BankName = "State Bank of India",
                    BranchName = "Coimbatore Main",
                    AccountNumber = sensitiveAccount,
                    IfscCode = "SBIN0001234",
                    AccountType = "Current",
                    IsPrimary = true
                }
            ]
        };

        var response = await client.PostAsJsonAsync("/api/v1/commercial/suppliers", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ApiResponse<SupplierDetailDto>>();
        Assert.NotNull(created);
        Assert.NotEmpty(created.Data.BankAccounts);

        // Account number is masked in DTO
        var bankDto = created.Data.BankAccounts[0];
        Assert.Equal("•••• •••• 3456", bankDto.AccountNumberMasked);

        // Raw database inspection: verify ciphertext is NOT plaintext
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var dbBank = await db.SupplierBankAccounts.FirstOrDefaultAsync(b => b.SupplierId == created.Data.Id);
        Assert.NotNull(dbBank);
        Assert.NotEqual(sensitiveAccount, dbBank.AccountNumberEncrypted);
        Assert.DoesNotContain(sensitiveAccount, dbBank.AccountNumberEncrypted);
    }

    [Fact]
    public async Task Suppliers_GetUnmaskedBankAccount_AuthorizedUser_DecryptsAndLogsAudit()
    {
        var client = CreateAdminClient();
        var code = $"SUPP-UNM-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var sensitiveAccount = "1122334455667788";

        var createRes = await client.PostAsJsonAsync("/api/v1/commercial/suppliers", new CreateSupplierRequest
        {
            SupplierCode = code,
            Name = "Secure Bank Decrypt Test Supplier",
            Category = "Packaging",
            Phone = "+91 9843022222",
            BankAccounts =
            [
                new CreateSupplierBankAccountDto
                {
                    BankName = "HDFC Bank",
                    BranchName = "RS Puram",
                    AccountNumber = sensitiveAccount,
                    IfscCode = "HDFC0000123",
                    AccountType = "Current",
                    IsPrimary = true
                }
            ]
        });
        var supp = (await createRes.Content.ReadFromJsonAsync<ApiResponse<SupplierDetailDto>>())!.Data;
        var bankId = supp.BankAccounts[0].Id;

        // Admin has 'commercial:suppliers:bank-details' permission
        var unmaskRes = await client.GetAsync($"/api/v1/commercial/suppliers/{supp.Id}/bank-accounts/{bankId}/unmasked");
        Assert.Equal(HttpStatusCode.OK, unmaskRes.StatusCode);

        var unmasked = await unmaskRes.Content.ReadFromJsonAsync<ApiResponse<UnmaskedBankAccountDto>>();
        Assert.NotNull(unmasked);
        Assert.Equal(sensitiveAccount, unmasked.Data.PlaintextAccountNumber);

        // Verify tamper-evident audit record
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var audit = await db.SecurityAuditEvents
            .Where(a => a.EventType == "SUPPLIER_BANK_UNMASKED")
            .OrderByDescending(a => a.TimestampUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(audit);
        Assert.Contains(code, audit.DetailsJson);
    }

    [Fact]
    public async Task Suppliers_GetUnmaskedBankAccount_UnauthorizedRole_ReturnsForbidden()
    {
        // Cashier lacks 'commercial:suppliers:bank-details' permission
        var adminClient = CreateAdminClient();
        var cashierClient = CreateCashierClient();

        var code = $"SUPP-FORB-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
        var createRes = await adminClient.PostAsJsonAsync("/api/v1/commercial/suppliers", new CreateSupplierRequest
        {
            SupplierCode = code,
            Name = "Forbidden Test Supplier",
            Category = "Logistics",
            Phone = "+91 9843033333",
            BankAccounts =
            [
                new CreateSupplierBankAccountDto
                {
                    BankName = "ICICI Bank",
                    BranchName = "Town Hall",
                    AccountNumber = "5555666677778888",
                    IfscCode = "ICIC0000001",
                    IsPrimary = true
                }
            ]
        });
        var supp = (await createRes.Content.ReadFromJsonAsync<ApiResponse<SupplierDetailDto>>())!.Data;
        var bankId = supp.BankAccounts[0].Id;

        // Cashier attempts unmasking -> 403 Forbidden
        var response = await cashierClient.GetAsync($"/api/v1/commercial/suppliers/{supp.Id}/bank-accounts/{bankId}/unmasked");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
