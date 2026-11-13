using Magic.Contracts.Services;

namespace Magic.Application.Services.Services;

public interface IServiceDeskService
{
    Task<ServiceSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCatalogDto>> GetCatalogAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<ServiceCatalogDto> CreateCatalogEntryAsync(Guid companyId, CreateServiceCatalogRequest request, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ServiceRequestDto> Items, int TotalCount)> GetRequestsAsync(Guid companyId, Guid branchId, Guid? customerId, string? status, string? priority, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceRequestDto> GetRequestByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<ServiceRequestDto> CreateRequestAsync(Guid companyId, Guid branchId, CreateServiceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceRequestDto> UpdateRequestStatusAsync(Guid companyId, Guid id, UpdateServiceRequestStatusRequest request, CancellationToken cancellationToken = default);

    Task<ServiceQuotationDto> CreateQuotationAsync(Guid companyId, Guid requestId, CreateServiceQuotationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceQuotationDto> ApproveQuotationAsync(Guid companyId, Guid quotationId, CancellationToken cancellationToken = default);

    Task<ServicePartConsumptionDto> ConsumePartAsync(Guid companyId, Guid requestId, ConsumeServicePartRequest request, CancellationToken cancellationToken = default);
    Task<Guid> InvoiceServiceRequestAsync(Guid companyId, Guid requestId, Guid cashierUserId, InvoiceServiceRequest request, CancellationToken cancellationToken = default);
}

