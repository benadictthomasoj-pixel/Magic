using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Magic.Contracts.Api;
using Magic.Contracts.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class IdempotencyTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory.WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
    });

    [Fact]
    public async Task IdempotentPostReplaysCachedResponseOnIdenticalDuplicate()
    {
        using var client = _factory.CreateClient();
        var idempotencyKey = Guid.CreateVersion7().ToString();
        var payload = new { message = "First attempt", value = 42 };
        var jsonPayload = JsonSerializer.Serialize(payload);

        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/system/echo");
        request1.Headers.Add(MagicHttpHeaders.IdempotencyKey, idempotencyKey);
        request1.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response1 = await client.SendAsync(request1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var body1 = await response1.Content.ReadAsStringAsync();

        // Second request with exact same Idempotency-Key and payload
        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/system/echo");
        request2.Headers.Add(MagicHttpHeaders.IdempotencyKey, idempotencyKey);
        request2.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response2 = await client.SendAsync(request2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.True(response2.Headers.TryGetValues("X-Cache-Lookup", out var cacheHeader));
        Assert.Contains("HIT", cacheHeader);

        var body2 = await response2.Content.ReadAsStringAsync();
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(body1), JsonNode.Parse(body2)));
    }

    [Fact]
    public async Task IdempotentPostRejectsPayloadTamperingWith422UnprocessableEntity()
    {
        using var client = _factory.CreateClient();
        var idempotencyKey = Guid.CreateVersion7().ToString();

        // First request with payload A
        var payloadA = new { action = "PAY", amount = 500 };
        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/system/echo");
        request1.Headers.Add(MagicHttpHeaders.IdempotencyKey, idempotencyKey);
        request1.Content = new StringContent(JsonSerializer.Serialize(payloadA), Encoding.UTF8, "application/json");

        using var response1 = await client.SendAsync(request1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        // Second request with SAME Idempotency-Key but modified payload B
        var payloadB = new { action = "PAY", amount = 10000 };
        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/system/echo");
        request2.Headers.Add(MagicHttpHeaders.IdempotencyKey, idempotencyKey);
        request2.Content = new StringContent(JsonSerializer.Serialize(payloadB), Encoding.UTF8, "application/json");

        using var response2 = await client.SendAsync(request2);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response2.StatusCode);

        var error = await response2.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Equal("IDEMPOTENCY_PAYLOAD_MISMATCH", error.Error.ErrorCode);
    }

    [Fact]
    public async Task IdempotentPostRejectsNonUuidKeyWith400BadRequest()
    {
        using var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/system/echo");
        request.Headers.Add(MagicHttpHeaders.IdempotencyKey, "invalid-not-a-guid");
        request.Content = new StringContent("{\"data\":1}", Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("API_INVALID_IDEMPOTENCY_KEY", error.Error.ErrorCode);
    }
}
