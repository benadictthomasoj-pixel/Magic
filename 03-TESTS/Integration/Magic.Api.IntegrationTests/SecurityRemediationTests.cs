using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Magic.Contracts.Api;
using Magic.Contracts.Commercial;
using Magic.Contracts.Headers;
using Magic.Contracts.Security;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class SecurityRemediationTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;

    public SecurityRemediationTests(MagicTestFixture factory)
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

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "ben",
            Password = "123123"
        });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        _adminToken = auth!.Data.AccessToken;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // =========================================================================
    // SEC-B01 Regression Tests: Remove password fallback
    // =========================================================================

    [Fact]
    public async Task SEC_B01_Login_WithValidPassword_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "ben",
            Password = "123123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotEmpty(envelope.Data.AccessToken);
    }

    [Fact]
    public async Task SEC_B01_Login_WithIncorrectPassword_Fails()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "ben",
            Password = "IncorrectPassword999!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SEC_B01_Login_WithRemovedHardcodedPasswordFallback_Fails()
    {
        // "admin" has password "123123" stored in DB.
        // The old fallback "AdminPassword123!" must NO LONGER authenticate.
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "admin",
            Password = "AdminPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =========================================================================
    // SEC-B02 Regression Tests: Secure company identity
    // =========================================================================

    [Fact]
    public async Task SEC_B02_AuthenticatedUser_AccessesOwnCompanyRecords_Succeeds()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await client.GetAsync("/api/v1/commercial/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SEC_B02_TamperedCompanyHeader_MismatchAgainstJwt_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        // Inject an unauthorized foreign company ID header
        client.DefaultRequestHeaders.Add(MagicHttpHeaders.CompanyId, "99999999-9999-9999-9999-999999999999");

        var response = await client.GetAsync("/api/v1/commercial/products");

        // Must reject with 403 Forbidden due to TENANT_MISMATCH
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SEC_B02_UnauthenticatedRequest_OnProtectedEndpoint_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        // No Authorization header

        var response = await client.GetAsync("/api/v1/commercial/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =========================================================================
    // SEC-B03 Regression Tests: Secure encryption key configuration
    // =========================================================================

    [Fact]
    public void SEC_B03_MissingEncryptionKey_ThrowsInvalidOperationException()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new AesGcmBankDataEncryptionService(emptyConfig));
        Assert.Contains("Security:BankEncryptionKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SEC_B03_InvalidEncryptionKeyLength_ThrowsInvalidOperationException()
    {
        var inMemory = new Dictionary<string, string?>
        {
            ["Security:BankEncryptionKey"] = "SHORT_INVALID_KEY_12345"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new AesGcmBankDataEncryptionService(config));
        Assert.Contains("must be exactly 64 hex characters", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SEC_B03_ValidKey_EncryptsAndDecryptsSuccessfully()
    {
        var validKeyHex = "3BE52C1C5E44B692983FDD2EB74372EEB5C5BC820D737189B83A5839134F5711";
        var inMemory = new Dictionary<string, string?>
        {
            ["Security:BankEncryptionKey"] = validKeyHex
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        var service = new AesGcmBankDataEncryptionService(config);

        var secret = "9876543210123456";
        var encrypted = service.Encrypt(secret);
        var decrypted = service.Decrypt(encrypted);

        Assert.NotEqual(secret, encrypted);
        Assert.Equal(secret, decrypted);
    }

    [Fact]
    public void SEC_B03_TamperedCiphertext_ThrowsCryptographicException()
    {
        var validKeyHex = "3BE52C1C5E44B692983FDD2EB74372EEB5C5BC820D737189B83A5839134F5711";
        var inMemory = new Dictionary<string, string?>
        {
            ["Security:BankEncryptionKey"] = validKeyHex
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        var service = new AesGcmBankDataEncryptionService(config);

        var secret = "9876543210123456";
        var encrypted = service.Encrypt(secret);

        // Tamper with the base64 ciphertext
        var raw = Convert.FromBase64String(encrypted);
        raw[^1] ^= 0xFF; // Flip bits in authentication tag/ciphertext
        var tampered = Convert.ToBase64String(raw);

        Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(tampered));
    }
}
