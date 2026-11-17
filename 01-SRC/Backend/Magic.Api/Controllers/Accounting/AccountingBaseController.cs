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
    ICurrentUserContext currentUserContext) : MagicApiControllerBase(dateTimeProvider, branchContext, currentUserContext)
{
    protected Guid ResolveAuthorizedUserId()
    {
        if (CurrentUser.IsAuthenticated && CurrentUser.UserId.HasValue && CurrentUser.UserId.Value != Guid.Empty)
        {
            return CurrentUser.UserId.Value;
        }

        throw new DomainException(
            "AUTH_REQUIRED_OPEN_003",
            "Financial write operations require authenticated identity. Please sign in with valid credentials.",
            StatusCodes.Status401Unauthorized);
    }
}
