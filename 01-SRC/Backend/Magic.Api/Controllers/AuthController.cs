using Asp.Versioning;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Headers;
using Magic.Contracts.Security;
using Magic.Domain.Common;
using Magic.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(
    IAuthenticationApplicationService authService,
    ICurrentUserContext currentUser,
    IDateTimeProvider dateTimeProvider) : ControllerBase
{
    private static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var response = await authService.LoginAsync(request, ipAddress, cancellationToken);
        return Ok(Wrap(response));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var response = await authService.RefreshTokenAsync(request, ipAddress, cancellationToken);
        return Ok(Wrap(response));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        await authService.LogoutAsync(request.RefreshToken, ipAddress, cancellationToken);
        return Ok(Wrap("Successfully logged out."));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException("UNAUTHORIZED", "User identity not found in token.", StatusCodes.Status401Unauthorized);
        }

        var ipAddress = GetClientIpAddress();
        await authService.ChangePasswordAsync(currentUser.UserId.Value, request, ipAddress, cancellationToken);
        return Ok(Wrap("Password changed successfully."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> GetMe(CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException("UNAUTHORIZED", "User identity not found in token.", StatusCodes.Status401Unauthorized);
        }

        var user = await authService.GetCurrentUserAsync(currentUser.UserId.Value, cancellationToken);
        return Ok(Wrap(user));
    }

    [HttpPost("terminals/register")]
    public async Task<ActionResult<ApiResponse<TerminalRegistrationResponse>>> RegisterTerminal(
        [FromBody] RegisterTerminalRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = ResolveCompanyId();
        var ipAddress = GetClientIpAddress();
        var result = await authService.RegisterTerminalAsync(request, companyId, ipAddress, cancellationToken);
        return Ok(Wrap(result));
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString()
            ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? "127.0.0.1";
    }

    private Guid ResolveCompanyId()
    {
        if (Request.Headers.TryGetValue(MagicHttpHeaders.CompanyId, out var val) &&
            Guid.TryParse(val, out var parsed) && parsed != Guid.Empty)
        {
            return parsed;
        }
        return DefaultCompanyId;
    }

    private ApiResponse<T> Wrap<T>(T data)
    {
        var correlationId = HttpContext.Items[MagicHttpHeaders.CorrelationId]?.ToString()
            ?? Guid.NewGuid().ToString("N");

        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            CorrelationId = correlationId,
            TimestampUtc = dateTimeProvider.UtcNow
        };
    }
}

