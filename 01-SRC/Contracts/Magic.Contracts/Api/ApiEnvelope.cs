namespace Magic.Contracts.Api;

/// <summary>
/// Standard JSON envelope for successful MAGIC API responses.
/// </summary>
public sealed record ApiResponse<T>
{
    public bool Success { get; init; } = true;
    public required T Data { get; init; }
    public PaginationMeta? Meta { get; init; }
    public required string CorrelationId { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Pagination metadata included on list endpoints.
/// </summary>
public sealed record PaginationMeta(int Page, int PageSize, long TotalCount);

/// <summary>
/// Standard RFC 7807 compatible error envelope returned on failures.
/// </summary>
public sealed record ApiErrorResponse
{
    public bool Success { get; init; }
    public required ApiErrorDetail Error { get; init; }
    public required string CorrelationId { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Structured error details compliant with RFC 7807 Problem Details.
/// </summary>
public sealed record ApiErrorDetail
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required int Status { get; init; }
    public required string Detail { get; init; }
    public required string ErrorCode { get; init; }
    public IReadOnlyList<FieldValidationError> InvalidFields { get; init; } = [];
}

/// <summary>
/// Field-level validation error reported to UI clients.
/// </summary>
public sealed record FieldValidationError(string Field, string Message);

