using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Magic.Contracts.Accounting;
using Magic.Contracts.Api;
using Magic.Contracts.Headers;
using Magic.Contracts.Security;
using Magic.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class AuthenticationAndSecurityTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;

    public AuthenticationAndSecurityTests(MagicTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<SecurityDatabaseSeeder>();
        await seeder.SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsAccessTokenAndUserSummary()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "admin",
            Password = "AdminPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));
        Assert.Equal("admin", result.Data.User.Username);
        Assert.Contains("Administrator", result.Data.User.Roles);
        Assert.Contains("accounting:vouchers:create", result.Data.User.Permissions);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401Unauthorized()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "admin",
            Password = "WrongPassword999!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_Returns401Unauthorized()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "non_existent_user_99999",
            Password = "SomePassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesTokenAndIssuesNewPair()
    {
        // 1. Initial Login
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "admin",
            Password = "AdminPassword123!"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var firstRefreshToken = loginResult!.Data!.RefreshToken;

        // 2. Rotate token
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = firstRefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(refreshResult);
        Assert.True(refreshResult.Success);
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.Data!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshResult.Data.RefreshToken));
        Assert.NotEqual(firstRefreshToken, refreshResult.Data.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_ReuseDetection_RevokesEntireTokenFamily()
    {
        // 1. Initial Login
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "accountant1",
            Password = "AccountantPassword123!"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var originalRefreshToken = loginResult!.Data!.RefreshToken;

        // 2. Legitimate rotation: original is now revoked and replaced by rotatedToken
        var rotateResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = originalRefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        var rotateResult = await rotateResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var legitRotatedToken = rotateResult!.Data!.RefreshToken;

        // 3. MALICIOUS ATTACK / REUSE: Attacker attempts to replay the original (already revoked) token!
        var reuseAttackResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = originalRefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseAttackResponse.StatusCode);

        // 4. Verify Entire Family Revocation: The legitimate rotated token MUST now be revoked too!
        var victimFollowUpResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = legitRotatedToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, victimFollowUpResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "admin",
            Password = "AdminPassword123!"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var refreshToken = loginResult!.Data!.RefreshToken;

        // Logout
        var logoutResponse = await _client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        });
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Attempting refresh should fail
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task ProtectedAccountingEndpoints_RejectUnauthenticatedWith401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounting/vouchers/draft");
        request.Content = JsonContent.Create(new CreateJournalDraftRequest
        {
            VoucherDate = DateOnly.FromDateTime(DateTime.UtcNow),
            VoucherType = "JV",
            Narration = "Unauthenticated attempt",
            Lines = []
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedAccountingEndpoints_RejectUserLackingRequiredPermissionWith403()
    {
        // 1. Login as Cashier (Cashier has billing:pos permissions, NOT accounting:vouchers:create)
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "cashier1",
            Password = "CashierPassword123!"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token = loginResult!.Data!.AccessToken;

        // 2. Cashier attempts to create an accounting journal voucher draft
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounting/vouchers/draft");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new CreateJournalDraftRequest
        {
            VoucherDate = DateOnly.FromDateTime(DateTime.UtcNow),
            VoucherType = "JV",
            Narration = "Unauthorized cashier attempt",
            Lines = []
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedAccountingEndpoints_AllowAuthorizedAccountant()
    {
        // 1. Login as Accountant
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "accountant1",
            Password = "AccountantPassword123!"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var token = loginResult!.Data!.AccessToken;

        // 2. Fetch Accounts
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/accounting/accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RegisterTerminal_WithValidBranch_ReturnsAuthorizedTerminal()
    {
        var request = new RegisterTerminalRequest
        {
            TerminalCode = $"POS-TEST-{Guid.NewGuid().ToString("N")[..4]}",
            MachineName = "DESKTOP-TEST-01",
            OperatingSystem = "Windows 11 Pro",
            BranchId = SecurityDatabaseSeeder.DefaultBranchId
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/terminals/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TerminalRegistrationResponse>>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Data!.IsAuthorized);
        Assert.Equal(request.TerminalCode.ToUpperInvariant(), result.Data.TerminalCode);
    }
}
