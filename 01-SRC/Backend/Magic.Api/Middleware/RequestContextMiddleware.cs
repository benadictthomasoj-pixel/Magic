using Magic.Contracts.Headers;
using Magic.Domain.Common;
using Magic.Infrastructure.Security;
using Serilog.Context;

namespace Magic.Api.Middleware;

/// <summary>
/// Resolves X-Correlation-Id, X-Branch-Id, and X-Terminal-Id headers per request
/// and enriches Serilog diagnostic scope without duplicating header parsing in controllers.
/// </summary>
public sealed class RequestContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, MutableRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestContext);

        var correlationId = ResolveOrCreateCorrelationId(context);
        context.Items[MagicHttpHeaders.CorrelationId] = correlationId;
        context.Response.Headers[MagicHttpHeaders.CorrelationId] = correlationId;

        var branchId = ParseOptionalGuidHeader(context.Request.Headers, MagicHttpHeaders.BranchId);
        var terminalId = ParseOptionalGuidHeader(context.Request.Headers, MagicHttpHeaders.TerminalId);

        requestContext.SetBranchAndTerminal(branchId, terminalId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("BranchId", branchId))
        using (LogContext.PushProperty("TerminalId", terminalId))
        {
            await next(context);
        }
    }

    private static string ResolveOrCreateCorrelationId(HttpContext context)
    {
        var raw = context.Request.Headers[MagicHttpHeaders.CorrelationId].FirstOrDefault();
        return Guid.TryParse(raw, out var parsed)
            ? parsed.ToString()
            : Guid.CreateVersion7().ToString();
    }

    private static Guid? ParseOptionalGuidHeader(IHeaderDictionary headers, string headerName)
    {
        if (!headers.TryGetValue(headerName, out var values) || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return null;
        }

        if (!Guid.TryParse(values.FirstOrDefault(), out var parsed) || parsed == Guid.Empty)
        {
            throw new DomainException(
                "API_INVALID_CONTEXT_HEADER",
                $"Header '{headerName}' must be a valid non-empty UUID.",
                StatusCodes.Status400BadRequest);
        }

        return parsed;
    }
}

