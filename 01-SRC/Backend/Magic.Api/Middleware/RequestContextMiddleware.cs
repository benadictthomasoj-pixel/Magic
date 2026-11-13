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
    public async Task InvokeAsync(
        HttpContext context,
        IRequestContextSetter requestContext,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(configuration);

        var correlationId = ResolveOrCreateCorrelationId(context);
        context.Items[MagicHttpHeaders.CorrelationId] = correlationId;
        context.Response.Headers[MagicHttpHeaders.CorrelationId] = correlationId;

        var branchId = ParseOptionalGuidHeader(context.Request.Headers, MagicHttpHeaders.BranchId);
        var terminalId = ParseOptionalGuidHeader(context.Request.Headers, MagicHttpHeaders.TerminalId);

        // Security Protocol: If authenticated, server-side claims take absolute precedence over unverified caller headers
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var branchClaim = context.User.FindFirst("branch_id")?.Value;
            if (Guid.TryParse(branchClaim, out var claimBranch))
            {
                branchId = claimBranch;
            }

            var terminalClaim = context.User.FindFirst("terminal_id")?.Value;
            if (Guid.TryParse(terminalClaim, out var claimTerminal))
            {
                terminalId = claimTerminal;
            }

            var companyClaim = context.User.FindFirst("company_id")?.Value;
            var companyId = Guid.TryParse(companyClaim, out var claimCompany) ? claimCompany : Guid.Empty;

            var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            var userId = Guid.TryParse(userIdClaim, out var parsedUid) ? parsedUid : Guid.Empty;
            var username = context.User.Identity.Name
                ?? context.User.FindFirst("unique_name")?.Value
                ?? "authenticated_user";
            var roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray();
            var permissions = context.User.FindAll("permission").Select(c => c.Value).ToArray();

            if (userId != Guid.Empty && companyId != Guid.Empty)
            {
                requestContext.SetAuthenticatedCaller(userId, username, companyId, roles, permissions);
            }
        }

        var requireTerminal = configuration.GetValue<bool>("Security:RequireApprovedTerminalHeader");
        if (requireTerminal && !terminalId.HasValue && !IsHealthOrOpenApiEndpoint(context.Request.Path))
        {
            throw new DomainException(
                "API_TERMINAL_HEADER_REQUIRED",
                "Terminal ID header 'X-Terminal-Id' is required by system security policy.",
                StatusCodes.Status400BadRequest);
        }

        requestContext.SetBranchAndTerminal(branchId, terminalId);

        context.Items[MagicHttpHeaders.BranchId] = branchId;
        context.Items[MagicHttpHeaders.TerminalId] = terminalId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("BranchId", branchId))
        using (LogContext.PushProperty("TerminalId", terminalId))
        {
            await next(context);
        }
    }

    private static bool IsHealthOrOpenApiEndpoint(PathString path)
    {
        return path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase);
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

