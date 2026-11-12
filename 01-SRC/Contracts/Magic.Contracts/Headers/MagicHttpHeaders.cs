namespace Magic.Contracts.Headers;

/// <summary>
/// Canonical HTTP request and response header names required across MAGIC endpoints.
/// </summary>
public static class MagicHttpHeaders
{
    public const string BranchId = "X-Branch-Id";
    public const string TerminalId = "X-Terminal-Id";
    public const string IdempotencyKey = "Idempotency-Key";
    public const string CorrelationId = "X-Correlation-Id";
    public const string SupervisorAuthToken = "X-Supervisor-Auth-Token";
}

