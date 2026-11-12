using System.Net;
using System.Net.Http.Json;
using Magic.Contracts.Accounting;
using Magic.Contracts.Api;
using Magic.Contracts.Headers;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class AccountingApiEndpointsTests(MagicTestFixture factory) : IClassFixture<MagicTestFixture>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly MagicTestFixture _factory = factory;

    [Fact]
    public async Task GetDashboard_ReturnsSuccessEnvelope_WithRealAggregateCounts()
    {
        var response = await _client.GetAsync("/api/v1/accounting/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AccountingDashboardDto>>();
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.NotNull(payload.Data);
        Assert.True(payload.Data.PostedVouchersCount >= 0);
        Assert.NotNull(payload.Data.SystemValidationStatus);
    }

    [Fact]
    public async Task GetAccountGroups_AndAccounts_ReturnSuccessEnvelopes()
    {
        var groupsRes = await _client.GetAsync("/api/v1/accounting/account-groups");
        Assert.Equal(HttpStatusCode.OK, groupsRes.StatusCode);

        var groupsPayload = await groupsRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AccountGroupDto>>>();
        Assert.NotNull(groupsPayload);
        Assert.True(groupsPayload.Success);

        var accountsRes = await _client.GetAsync("/api/v1/accounting/accounts");
        Assert.Equal(HttpStatusCode.OK, accountsRes.StatusCode);

        var accountsPayload = await accountsRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AccountDto>>>();
        Assert.NotNull(accountsPayload);
        Assert.True(accountsPayload.Success);
    }

    [Fact]
    public async Task ValidateJournal_EvaluatesDoubleEntryWithoutPosting_AndReportsUnbalancedDifference()
    {
        var request = new CreateJournalDraftRequest
        {
            FiscalPeriodId = Guid.NewGuid(),
            VoucherType = "Journal",
            VoucherDate = new DateOnly(2026, 4, 15),
            Narration = "Draft entry for validation check",
            Lines =
            [
                new JournalLineInputDto
                {
                    AccountId = Guid.NewGuid(),
                    DebitPaise = 150_000,
                    CreditPaise = 0,
                    Narration = "Debit cash"
                },
                new JournalLineInputDto
                {
                    AccountId = Guid.NewGuid(),
                    DebitPaise = 0,
                    CreditPaise = 100_000,
                    Narration = "Credit sales (unbalanced)"
                }
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/v1/accounting/vouchers/validate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<JournalValidationResultDto>>();
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.False(payload.Data.IsBalanced);
        Assert.False(payload.Data.IsValid);
        Assert.Equal(50_000L, payload.Data.DifferencePaise);
        Assert.NotEmpty(payload.Data.Errors);
    }

    [Fact]
    public async Task FinancialWriteEndpoints_EnforceFailClosed_WhenUnauthenticated()
    {
        // 1. Unauthenticated Create Account
        var createAccReq = new CreateAccountRequest
        {
            GroupId = Guid.NewGuid(),
            Code = "1999",
            Name = "Unauthenticated Asset",
            RootClass = "Asset"
        };
        var accRes = await _client.PostAsJsonAsync("/api/v1/accounting/accounts", createAccReq);
        Assert.Equal(HttpStatusCode.Unauthorized, accRes.StatusCode);

        // 2. Unauthenticated Create Journal Draft
        var draftReq = new CreateJournalDraftRequest
        {
            FiscalPeriodId = Guid.NewGuid(),
            VoucherType = "Journal",
            VoucherDate = new DateOnly(2026, 4, 15),
            Narration = "Test",
            Lines = []
        };
        var draftRes = await _client.PostAsJsonAsync("/api/v1/accounting/vouchers/draft", draftReq);
        Assert.Equal(HttpStatusCode.Unauthorized, draftRes.StatusCode);

        // 3. Unauthenticated Post Voucher
        var postReq = new PostVoucherRequest { BranchCode = "MAIN", FyCode = "FY26" };
        var postRes = await _client.PostAsJsonAsync($"/api/v1/accounting/vouchers/{Guid.NewGuid()}/post", postReq);
        Assert.Equal(HttpStatusCode.Unauthorized, postRes.StatusCode);
    }

    [Fact]
    public async Task GetTrialBalance_AndGeneralLedger_ReturnSuccessEnvelopes()
    {
        var tbRes = await _client.GetAsync("/api/v1/accounting/trial-balance");
        Assert.Equal(HttpStatusCode.OK, tbRes.StatusCode);

        var tbPayload = await tbRes.Content.ReadFromJsonAsync<ApiResponse<TrialBalanceReportDto>>();
        Assert.NotNull(tbPayload);
        Assert.True(tbPayload.Success);
        Assert.NotNull(tbPayload.Data.Accounts);

        // Subledgers
        var arRes = await _client.GetAsync("/api/v1/accounting/subledgers/ar");
        Assert.Equal(HttpStatusCode.OK, arRes.StatusCode);
        var arPayload = await arRes.Content.ReadFromJsonAsync<ApiResponse<SubledgerReportDto>>();
        Assert.NotNull(arPayload);
        Assert.True(arPayload.Success);
        Assert.Equal("AR", arPayload.Data.SubledgerType);

        var apRes = await _client.GetAsync("/api/v1/accounting/subledgers/ap");
        Assert.Equal(HttpStatusCode.OK, apRes.StatusCode);
        var apPayload = await apRes.Content.ReadFromJsonAsync<ApiResponse<SubledgerReportDto>>();
        Assert.NotNull(apPayload);
        Assert.True(apPayload.Success);
        Assert.Equal("AP", apPayload.Data.SubledgerType);
    }
}
