using Asp.Versioning;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Headers;
using Magic.Contracts.Security;
using Magic.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
public abstract class OperationsBaseController(
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : MagicApiControllerBase(dateTimeProvider, branchContext, currentUserContext)
{
}

