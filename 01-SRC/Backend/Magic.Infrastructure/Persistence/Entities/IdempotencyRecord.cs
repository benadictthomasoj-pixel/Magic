namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Persists HTTP Idempotency-Key records to guarantee single execution of critical requests (platform.idempotency_records).
/// </summary>
public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public Guid BranchId { get; set; }
    public string EndpointPath { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string? ResponseBodyJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

