using Asp.Versioning;
using Magic.Application.Expenses.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Expenses;
using Magic.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.Expenses;

[Authorize]
[Route("api/v{version:apiVersion}/expenses")]
public sealed class ExpensesController(
    IExpenseService expenseService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext currentUserContext) : OperationsBaseController(dateTimeProvider, branchContext, currentUserContext)
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ExpenseSummaryDto>>> GetSummary(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var item = await expenseService.GetSummaryAsync(companyId, bId, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExpenseCategoryDto>>>> GetCategories(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:view");
        var companyId = ResolveCompanyId();
        var items = await expenseService.GetCategoriesAsync(companyId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryDto>>> CreateCategory(
        [FromBody] CreateExpenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:create", "expenses:approve");
        var companyId = ResolveCompanyId();
        var item = await expenseService.CreateCategoryAsync(companyId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExpenseDto>>>> GetExpenses(
        [FromQuery] Guid? branchId,
        [FromQuery] string? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var (items, total) = await expenseService.GetExpensesAsync(companyId, bId, categoryId, status, fromDate, toDate, page, pageSize, cancellationToken);
        return Envelope(items, new PaginationMeta(page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> GetExpenseById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:view");
        var companyId = ResolveCompanyId();
        var item = await expenseService.GetExpenseByIdAsync(companyId, id, cancellationToken);
        return Envelope(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> CreateExpense(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:create");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var userId = ResolveCurrentUserId();
        var item = await expenseService.CreateExpenseAsync(companyId, branchId, userId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> ApproveExpense(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await expenseService.ApproveExpenseAsync(companyId, id, userId, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> RejectExpense(
        Guid id,
        [FromBody] RejectExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:approve");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await expenseService.RejectExpenseAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> PayExpense(
        Guid id,
        [FromBody] PayExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:pay");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await expenseService.PayExpenseAsync(companyId, id, userId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> CancelExpense(
        Guid id,
        [FromBody] CancelExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:approve", "expenses:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await expenseService.CancelExpenseAsync(companyId, id, userId, request.Reason, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("petty-cash")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PettyCashAccountDto>>>> GetPettyCashAccounts(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:pettycash:manage", "expenses:view");
        var companyId = ResolveCompanyId();
        var bId = branchId ?? ResolveBranchId();
        var items = await expenseService.GetPettyCashAccountsAsync(companyId, bId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("petty-cash")]
    public async Task<ActionResult<ApiResponse<PettyCashAccountDto>>> CreatePettyCashAccount(
        [FromBody] CreatePettyCashAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:pettycash:manage");
        var companyId = ResolveCompanyId();
        var branchId = ResolveBranchId();
        var item = await expenseService.CreatePettyCashAccountAsync(companyId, branchId, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpGet("petty-cash/{accountId:guid}/transactions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PettyCashTransactionDto>>>> GetPettyCashTransactions(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:pettycash:manage", "expenses:view");
        var companyId = ResolveCompanyId();
        var items = await expenseService.GetPettyCashTransactionsAsync(companyId, accountId, cancellationToken);
        return Envelope(items);
    }

    [HttpPost("petty-cash/{accountId:guid}/replenish")]
    public async Task<ActionResult<ApiResponse<PettyCashTransactionDto>>> ReplenishPettyCash(
        Guid accountId,
        [FromBody] ReplenishPettyCashRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:pettycash:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await expenseService.ReplenishPettyCashAsync(companyId, accountId, userId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpPost("petty-cash/{accountId:guid}/reconcile")]
    public async Task<ActionResult<ApiResponse<PettyCashTransactionDto>>> ReconcilePettyCash(
        Guid accountId,
        [FromBody] ReconcilePettyCashRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("expenses:pettycash:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await expenseService.ReconcilePettyCashAsync(companyId, accountId, userId, request, cancellationToken);
        return Envelope(item);
    }
}
