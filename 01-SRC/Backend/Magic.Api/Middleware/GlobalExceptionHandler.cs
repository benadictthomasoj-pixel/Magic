using Magic.Application.Validation;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Headers;
using Magic.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace Magic.Api.Middleware;

/// <summary>
/// Centralized exception handler converting domain, validation, and unhandled errors
/// into MAGIC's standard RFC 7807 ApiErrorResponse envelope.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IDateTimeProvider dateTimeProvider) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var correlationId = httpContext.Items[MagicHttpHeaders.CorrelationId]?.ToString()
            ?? httpContext.TraceIdentifier;

        var (statusCode, errorCode, title, detail, invalidFields) = MapException(exception);

        if (statusCode >= 500)
        {
            LogUnhandledException(logger, correlationId, exception);
        }
        else
        {
            LogHandledDomainException(logger, correlationId, errorCode, statusCode);
        }

        var response = new ApiErrorResponse
        {
            Success = false,
            CorrelationId = correlationId,
            TimestampUtc = dateTimeProvider.UtcNow,
            Error = new ApiErrorDetail
            {
                Type = $"https://magic.local/errors/{errorCode.ToLowerInvariant().Replace('_', '-')}",
                Title = title,
                Status = statusCode,
                Detail = detail,
                ErrorCode = errorCode,
                InvalidFields = invalidFields
            }
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static (int StatusCode, string ErrorCode, string Title, string Detail, IReadOnlyList<FieldValidationError> Fields)
        MapException(Exception exception) =>
        exception switch
        {
            ApplicationValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                validationEx.ErrorCode,
                "Request Validation Failed",
                validationEx.Message,
                validationEx.Errors),
            DomainException domainEx => (
                domainEx.HttpStatusCode,
                domainEx.ErrorCode,
                "Business Rule Violation",
                domainEx.Message,
                []),
            _ => (
                StatusCodes.Status500InternalServerError,
                "SYS_UNHANDLED_ERROR",
                "Internal Server Error",
                "An unexpected internal error occurred. Use the correlationId when reviewing server logs.",
                [])
        };

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception processing request {CorrelationId}.")]
    private static partial void LogUnhandledException(ILogger logger, string correlationId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handled error {ErrorCode} (HTTP {StatusCode}) for request {CorrelationId}.")]
    private static partial void LogHandledDomainException(ILogger logger, string correlationId, string errorCode, int statusCode);
}

