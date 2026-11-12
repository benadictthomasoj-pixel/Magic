using Asp.Versioning;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Headers;
using Magic.Contracts.Security;
using Magic.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Accounting;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/accounting")]
public abstract class AccountingBaseController(
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : ControllerBase
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    protected Guid ResolveCompanyId()
    {
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

    protected Guid ResolveAuthorizedUserId()
    {
        if (currentUserContext.IsAuthenticated && currentUserContext.UserId.HasValue && currentUserContext.UserId.Value != Guid.Empty)
        {
            return currentUserContext.UserId.Value;
        }

        throw new DomainException(
            "AUTH_REQUIRED_OPEN_003",
            "Financial write operations require authenticated identity. Concrete authentication is deferred under OPEN-003.",
            StatusCodes.Status401Unauthorized);
    }

    protected ApiResponse<T> Wrap<T>(T data, PaginationMeta? meta = null)
    {
        var correlationId = HttpContext.Items[MagicHttpHeaders.CorrelationId]?.ToString()
            ?? Guid.NewGuid().ToString("N");

        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Meta = meta,
            CorrelationId = correlationId,
            TimestampUtc = dateTimeProvider.UtcNow
        };
    }
}
