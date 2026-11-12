using Asp.Versioning;
using Magic.Application.Diagnostics;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Diagnostics;
using Magic.Contracts.Headers;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers;

/// <summary>
/// Versioned v1 platform diagnostics and integration boundary endpoints.
/// Contains zero business module endpoints (reserved for Phase 2+).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system")]
public sealed class SystemController(
    ISystemDiagnosticsService diagnosticsService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ITerminalContext terminalContext,
    ITokenService tokenService) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<ApiResponse<SystemHealthStatusDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SystemHealthStatusDto>>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await diagnosticsService.GetStatusAsync(cancellationToken);
        return Ok(Wrap(status));
    }

    [HttpGet("context")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetContextBoundary()
    {
        var payload = new
        {
            branchId = branchContext.BranchId,
            terminalId = terminalContext.TerminalId,
            authenticationConfigured = tokenService.IsConfigured,
            openDecisions = new[]
            {
                "OPEN-003: Concrete authentication and token signing engine deferred until user approval."
            }
        };

        return Ok(Wrap<object>(payload));
    }

    [HttpPost("echo")]
    [ProducesResponseType<ApiResponse<System.Text.Json.JsonElement>>(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<System.Text.Json.JsonElement>> PostEcho([FromBody] System.Text.Json.JsonElement body)
    {
        return Ok(Wrap(body));
    }

    private ApiResponse<T> Wrap<T>(T data)
    {
        var correlationId = HttpContext.Items[MagicHttpHeaders.CorrelationId]?.ToString()
            ?? HttpContext.TraceIdentifier;

        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            CorrelationId = correlationId,
            TimestampUtc = dateTimeProvider.UtcNow
        };
    }
}

