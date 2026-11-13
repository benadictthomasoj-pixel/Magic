using Magic.Contracts.Orders;

namespace Magic.Application.Orders.Services;

public interface IOrderService
{
    Task<OrderSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SalesOrderDto> Items, int TotalCount)> GetOrdersAsync(Guid companyId, Guid branchId, Guid? customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> GetOrderByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> CreateOrderAsync(Guid companyId, Guid branchId, CreateSalesOrderRequest request, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> ConfirmOrderAsync(Guid companyId, Guid id, Guid confirmedByUserId, CancellationToken cancellationToken = default);
    Task<OrderFulfillmentDto> FulfillOrderAsync(Guid companyId, Guid id, FulfillOrderRequest request, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> CancelOrderAsync(Guid companyId, Guid id, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackorderDto>> GetBackordersAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default);
    Task<Guid> ConvertOrderToInvoiceAsync(Guid companyId, Guid id, Guid cashierUserId, string paymentMethod, CancellationToken cancellationToken = default);
}

