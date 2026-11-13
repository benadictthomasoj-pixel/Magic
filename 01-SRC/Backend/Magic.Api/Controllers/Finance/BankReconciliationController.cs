using Asp.Versioning;
using Magic.Application.Finance.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Finance;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Finance;

[Route("api/v{version:apiVersion}/accounting/bank-reconciliation")]
[Route("api/v{version:apiVersion}/finance/bank-reconciliation")]
public sealed class BankReconciliationController(
    IBankReconciliationService reconciliationService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : FinanceBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("statements")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BankStatementSummaryDto>>>> GetStatements(
        [FromQuery] Guid? bankAccountId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:reports:view");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var list = await reconciliationService.GetStatementsAsync(companyId, branchId, bankAccountId, cancellationToken);
        return Ok(Wrap(list));
    }

    [HttpPost("import-csv")]
    public async Task<ActionResult<ApiResponse<BankStatementDetailDto>>> ImportCsv(
        [FromBody] ImportBankStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:reconciliation:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var detail = await reconciliationService.ImportStatementCsvAsync(companyId, branchId, userId, request, cancellationToken);
        return Ok(Wrap(detail));
    }

    [HttpGet("workspace/{statementId:guid}")]
    public async Task<ActionResult<ApiResponse<ReconciliationWorkspaceDto>>> GetWorkspace(
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:reconciliation:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var workspace = await reconciliationService.GetReconciliationWorkspaceAsync(companyId, branchId, statementId, cancellationToken);
        return Ok(Wrap(workspace));
    }

    [HttpPost("match")]
    public async Task<ActionResult<ApiResponse<ReconciliationWorkspaceDto>>> MatchLine(
        [FromQuery] Guid statementId,
        [FromBody] MatchStatementLineRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:reconciliation:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var workspace = await reconciliationService.MatchStatementLineAsync(companyId, branchId, statementId, request, cancellationToken);
        return Ok(Wrap(workspace));
    }

    [HttpPost("unmatch")]
    public async Task<ActionResult<ApiResponse<ReconciliationWorkspaceDto>>> UnmatchLine(
        [FromQuery] Guid statementId,
        [FromBody] UnmatchStatementLineRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:reconciliation:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();

        var workspace = await reconciliationService.UnmatchStatementLineAsync(companyId, branchId, statementId, request, cancellationToken);
        return Ok(Wrap(workspace));
    }

    [HttpPost("complete/{statementId:guid}")]
    public async Task<ActionResult<ApiResponse<BankStatementSummaryDto>>> Complete(
        Guid statementId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("finance:reconciliation:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();

        var summary = await reconciliationService.CompleteReconciliationAsync(companyId, branchId, statementId, userId, cancellationToken);
        return Ok(Wrap(summary));
    }
}
