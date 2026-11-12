using System.Net;
using System.Net.Http.Json;
using Magic.Contracts.Api;
using Magic.Contracts.Diagnostics;
using Magic.Contracts.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class SystemEndpointsTests(MagicTestFixture factory) : IClassFixture<MagicTestFixture>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task LivenessHealthReturnsOkHealthy()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task ReadinessHealthVerifiesPostgreSqlConnectivity()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task SystemStatusEndpointReturnsEnvelopeWithDatabaseConnectedAndCorrelationId()
    {
        using var client = _factory.CreateClient();
        var correlationId = Guid.CreateVersion7().ToString();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/system/status");
        request.Headers.Add(MagicHttpHeaders.CorrelationId, correlationId);

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(MagicHttpHeaders.CorrelationId, out var returnedCorrelation));
        Assert.Contains(correlationId, returnedCorrelation);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<SystemHealthStatusDto>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.Equal(correlationId, envelope.CorrelationId);
        Assert.NotNull(envelope.Data);
        Assert.True(envelope.Data.DatabaseConnected);
        Assert.Equal("StrictOnlineLan", envelope.Data.NetworkPolicy);
        Assert.Equal("Configured", envelope.Data.AuthenticationStatus);
        Assert.Contains(envelope.Data.Components, c => c.Name == "OfflineBuffering" && c.Status == "Disabled");
    }

    [Fact]
    public async Task SystemContextBoundaryEchoesHeadersAndReportsConfiguredAuth()
    {
        using var client = _factory.CreateClient();
        var branchId = Guid.CreateVersion7();
        var terminalId = Guid.CreateVersion7();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/system/context");
        request.Headers.Add(MagicHttpHeaders.BranchId, branchId.ToString());
        request.Headers.Add(MagicHttpHeaders.TerminalId, terminalId.ToString());

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(branchId.ToString(), body, StringComparison.Ordinal);
        Assert.Contains(terminalId.ToString(), body, StringComparison.Ordinal);
        Assert.Contains("\"authenticationConfigured\":true", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidBranchHeaderProducesConsistentProblemDetailsErrorEnvelope()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/system/context");
        request.Headers.Add(MagicHttpHeaders.BranchId, "not-a-uuid");

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorEnvelope = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errorEnvelope);
        Assert.False(errorEnvelope.Success);
        Assert.Equal("API_INVALID_CONTEXT_HEADER", errorEnvelope.Error.ErrorCode);
        Assert.Equal(400, errorEnvelope.Error.Status);
    }

    [Fact]
    public async Task OpenApiDocumentIsExposedInDevelopment()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/v1/system/status", json, StringComparison.Ordinal);
    }
}

