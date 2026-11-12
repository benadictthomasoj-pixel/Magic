using System.Security.Cryptography;
using System.Text;
using Magic.Contracts.Common;
using Magic.Contracts.Headers;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Magic.Api.Middleware;

/// <summary>
/// Enforces HTTP request idempotency using platform.idempotency_records.
/// Absorbs duplicate network retries safely and rejects payload tampering with 422 Unprocessable Entity.
/// </summary>
public sealed partial class IdempotencyMiddleware(RequestDelegate next)
{
    private static readonly TimeSpan DefaultRetention = TimeSpan.FromHours(24);

    public async Task InvokeAsync(
        HttpContext context,
        MagicDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<IdempotencyMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        if (!HttpMethods.IsPost(context.Request.Method) &&
            !HttpMethods.IsPut(context.Request.Method) &&
            !HttpMethods.IsPatch(context.Request.Method) &&
            !HttpMethods.IsDelete(context.Request.Method))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(MagicHttpHeaders.IdempotencyKey, out var headerValues) ||
            string.IsNullOrWhiteSpace(headerValues.FirstOrDefault()))
        {
            await next(context);
            return;
        }

        var rawKey = headerValues.FirstOrDefault();
        if (!Guid.TryParse(rawKey, out var idempotencyKey) || idempotencyKey == Guid.Empty)
        {
            throw new DomainException(
                "API_INVALID_IDEMPOTENCY_KEY",
                $"Header '{MagicHttpHeaders.IdempotencyKey}' must be a valid non-empty UUID.",
                StatusCodes.Status400BadRequest);
        }

        var branchId = (context.Items[MagicHttpHeaders.BranchId] as Guid?) ?? Guid.Empty;
        var endpointPath = context.Request.Path.Value?.ToLowerInvariant() ?? "/";
        var now = dateTimeProvider.UtcNow;

        // Compute request hash including method, path, and request body content
        context.Request.EnableBuffering();
        var requestHash = await ComputeRequestHashAsync(context.Request);

        // Check for existing idempotency record
        var existing = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.BranchId == branchId &&
                r.IdempotencyKey == idempotencyKey &&
                r.EndpointPath == endpointPath &&
                r.ExpiresAtUtc > now,
                context.RequestAborted);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                LogPayloadMismatch(logger, idempotencyKey, endpointPath);

                throw new DomainException(
                    "IDEMPOTENCY_PAYLOAD_MISMATCH",
                    "An idempotency record exists for this key with a different request payload.",
                    StatusCodes.Status422UnprocessableEntity);
            }

            LogCacheHit(logger, existing.ResponseStatusCode, idempotencyKey, endpointPath);

            context.Response.StatusCode = existing.ResponseStatusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.Headers["X-Cache-Lookup"] = "HIT";
            context.Response.Headers[MagicHttpHeaders.IdempotencyKey] = idempotencyKey.ToString();

            if (!string.IsNullOrEmpty(existing.ResponseBodyJson))
            {
                await context.Response.WriteAsync(existing.ResponseBodyJson, Encoding.UTF8, context.RequestAborted);
            }

            return;
        }

        // Capture response and persist idempotency record upon completion
        var originalBodyStream = context.Response.Body;
        await using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await next(context);

            memoryStream.Position = 0;
            var responseBodyText = await new StreamReader(memoryStream, Encoding.UTF8).ReadToEndAsync(context.RequestAborted);
            memoryStream.Position = 0;

            // Only cache successful or intentional client error responses (200-499), skipping transient 5xx server errors
            if (context.Response.StatusCode is >= 200 and < 500)
            {
                var record = new IdempotencyRecord
                {
                    Id = Guid.CreateVersion7(),
                    IdempotencyKey = idempotencyKey,
                    BranchId = branchId,
                    EndpointPath = endpointPath,
                    RequestHash = requestHash,
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseBodyJson = string.IsNullOrWhiteSpace(responseBodyText) ? null : responseBodyText,
                    CreatedAtUtc = now,
                    ExpiresAtUtc = now.Add(DefaultRetention)
                };

                try
                {
                    dbContext.IdempotencyRecords.Add(record);
                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
                catch (DbUpdateException ex)
                {
                    // Concurrency safety: if another thread committed the same idempotency key concurrently, log debug
                    LogConcurrentDuplicate(logger, ex, idempotencyKey);
                }
            }

            await memoryStream.CopyToAsync(originalBodyStream, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static async Task<string> ComputeRequestHashAsync(HttpRequest request)
    {
        using var sha256 = SHA256.Create();
        var prefix = $"{request.Method.ToUpperInvariant()}:{request.Path.Value?.ToLowerInvariant()}|";
        var prefixBytes = Encoding.UTF8.GetBytes(prefix);

        sha256.TransformBlock(prefixBytes, 0, prefixBytes.Length, null, 0);

        request.Body.Position = 0;
        var buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await request.Body.ReadAsync(buffer)) > 0)
        {
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock([], 0, 0);
        request.Body.Position = 0;

        return Convert.ToHexString(sha256.Hash ?? []);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency violation: key {IdempotencyKey} on {EndpointPath} presented with mismatching payload hash.")]
    private static partial void LogPayloadMismatch(ILogger logger, Guid idempotencyKey, string endpointPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Idempotency cache hit: replaying cached response ({StatusCode}) for key {IdempotencyKey} on {EndpointPath}.")]
    private static partial void LogCacheHit(ILogger logger, int statusCode, Guid idempotencyKey, string endpointPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Concurrent duplicate idempotency record for key {IdempotencyKey} absorbed.")]
    private static partial void LogConcurrentDuplicate(ILogger logger, Exception exception, Guid idempotencyKey);
}
