using Asp.Versioning;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Headers;
using Magic.Contracts.Security;
using Magic.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Commercial;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/commercial")]
public abstract class CommercialBaseController(
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : MagicApiControllerBase(dateTimeProvider, branchContext, currentUserContext)
{
    protected Guid ResolveAuthorizedUserId() => ResolveUserId();
}
