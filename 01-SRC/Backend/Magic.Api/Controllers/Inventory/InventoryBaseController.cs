using Asp.Versioning;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Headers;
using Magic.Contracts.Security;
using Magic.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Inventory;

[ApiController]
[ApiVersion("1.0")]
public abstract class InventoryBaseController(
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : ControllerBase
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    protected Guid ResolveCompanyId()
    {
        if (currentUserContext.IsAuthenticated && currentUserContext.CompanyId.HasValue && currentUserContext.CompanyId.Value != Guid.Empty)
        {
            var authCompanyId = currentUserContext.CompanyId.Value;

            if (Request.Headers.TryGetValue(MagicHttpHeaders.CompanyId, out var headerVal) &&
                Guid.TryParse(headerVal, out var clientSuppliedCompanyId) && clientSuppliedCompanyId != Guid.Empty)
            {
                if (clientSuppliedCompanyId != authCompanyId)
                {
                    throw new DomainException(
                        "TENANT_MISMATCH",
                        $"Header X-Company-Id '{clientSuppliedCompanyId}' does not match authorized tenant '{authCompanyId}'.",
                        StatusCodes.Status403Forbidden);
                }
            }

            return authCompanyId;
        }

        if (currentUserContext.IsAuthenticated)
        {
            throw new DomainException(
                "TENANT_REQUIRED",
                "Authenticated request lacks valid company tenancy context.",
                StatusCodes.Status401Unauthorized);
        }

        if (Request.Headers.TryGetValue(MagicHttpHeaders.CompanyId, out var val) &&
            Guid.TryParse(val, out var parsed) && parsed != Guid.Empty)
        {
            return parsed;
        }

        return DefaultCompanyId;
    }

    protected Guid ResolveBranchId()
    {
        if (branchContext.BranchId.HasValue && branchContext.BranchId.Value != Guid.Empty)
        {
            return branchContext.BranchId.Value;
        }

        if (Request.Headers.TryGetValue(MagicHttpHeaders.BranchId, out var val) &&
            Guid.TryParse(val, out var parsed) && parsed != Guid.Empty)
        {
            return parsed;
        }

        return DefaultBranchId;
    }

    protected Guid ResolveUserId()
    {
        if (currentUserContext.IsAuthenticated && currentUserContext.UserId.HasValue && currentUserContext.UserId.Value != Guid.Empty)
        {
            return currentUserContext.UserId.Value;
        }

        return Guid.Parse("6df27c15-6264-45b4-b8b4-e8c7fafb1299");
    }

    protected void EnsurePermission(string permissionCode)
    {
        if (!currentUserContext.IsAuthenticated)
        {
            throw new DomainException(
                "AUTH_UNAUTHORIZED",
                "Authentication is required to perform this inventory operation.",
                StatusCodes.Status401Unauthorized);
        }

        if (currentUserContext.Roles.Contains("Administrator", StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (!currentUserContext.Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainException(
                "AUTH_FORBIDDEN",
                $"User lacks required permission '{permissionCode}' for this inventory operation.",
                StatusCodes.Status403Forbidden);
        }
    }

    protected string CorrelationId =>
        Request.Headers.TryGetValue(MagicHttpHeaders.CorrelationId, out var val) ? val.ToString() : Guid.NewGuid().ToString("N");

    protected ApiResponse<T> Wrap<T>(T data, PaginationMeta? meta = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Meta = meta,
            CorrelationId = CorrelationId,
            TimestampUtc = dateTimeProvider.UtcNow
        };
    }

    protected ActionResult<ApiResponse<T>> Envelope<T>(T data, PaginationMeta? meta = null)
    {
        return Ok(Wrap(data, meta));
    }

    protected ActionResult<ApiResponse<T>> CreatedEnvelope<T>(T data)
    {
        return StatusCode(StatusCodes.Status201Created, Wrap(data));
    }
}
