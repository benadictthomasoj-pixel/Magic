using Magic.Contracts.Expenses;

namespace Magic.Application.Expenses.Services;

public interface IExpenseService
{
    Task<ExpenseSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<ExpenseCategoryDto> CreateCategoryAsync(Guid companyId, CreateExpenseCategoryRequest request, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ExpenseDto> Items, int TotalCount)> GetExpensesAsync(Guid companyId, Guid branchId, Guid? categoryId, string? status, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ExpenseDto> GetExpenseByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseDto> CreateExpenseAsync(Guid companyId, Guid branchId, Guid userId, CreateExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseDto> ApproveExpenseAsync(Guid companyId, Guid id, Guid approverUserId, CancellationToken cancellationToken = default);
    Task<ExpenseDto> RejectExpenseAsync(Guid companyId, Guid id, Guid approverUserId, RejectExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseDto> PayExpenseAsync(Guid companyId, Guid id, Guid userId, PayExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseDto> CancelExpenseAsync(Guid companyId, Guid id, Guid userId, string reason, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PettyCashAccountDto>> GetPettyCashAccountsAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default);
    Task<PettyCashAccountDto> CreatePettyCashAccountAsync(Guid companyId, Guid branchId, CreatePettyCashAccountRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PettyCashTransactionDto>> GetPettyCashTransactionsAsync(Guid companyId, Guid accountId, CancellationToken cancellationToken = default);
    Task<PettyCashTransactionDto> ReplenishPettyCashAsync(Guid companyId, Guid accountId, Guid userId, ReplenishPettyCashRequest request, CancellationToken cancellationToken = default);
    Task<PettyCashTransactionDto> ReconcilePettyCashAsync(Guid companyId, Guid accountId, Guid userId, ReconcilePettyCashRequest request, CancellationToken cancellationToken = default);
}

