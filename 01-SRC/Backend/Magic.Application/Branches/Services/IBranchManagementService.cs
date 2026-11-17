using Magic.Contracts.Branches;

namespace Magic.Application.Branches.Services;

public interface IBranchManagementService
{
    Task<CompanyProfileDto> GetCompanyProfileAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CompanyProfileDto> UpdateCompanyProfileAsync(Guid companyId, UpdateCompanyProfileRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BranchDto>> GetBranchesAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<BranchDto> GetBranchByIdAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default);
    Task<BranchDto> CreateBranchAsync(Guid companyId, CreateBranchRequest request, CancellationToken cancellationToken = default);
    Task<BranchDto> UpdateBranchAsync(Guid companyId, Guid branchId, UpdateBranchRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TerminalDetailsDto>> GetTerminalsAsync(Guid companyId, Guid? branchId, CancellationToken cancellationToken = default);
    Task<TerminalDetailsDto> RegisterTerminalAsync(Guid companyId, RegisterTerminalRequest request, CancellationToken cancellationToken = default);
    Task<TerminalDetailsDto> UpdateTerminalStatusAsync(Guid companyId, Guid terminalId, UpdateTerminalStatusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterBranchTransferRequestDto>> GetInterBranchTransfersAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<InterBranchTransferRequestDto> CreateInterBranchTransferAsync(Guid companyId, Guid userId, CreateInterBranchTransferRequest request, CancellationToken cancellationToken = default);
    Task<InterBranchTransferRequestDto> ApproveInterBranchTransferAsync(Guid companyId, Guid transferId, Guid userId, CancellationToken cancellationToken = default);
    Task<InterBranchTransferRequestDto> DispatchInterBranchTransferAsync(Guid companyId, Guid transferId, Guid userId, CancellationToken cancellationToken = default);
    Task<InterBranchTransferRequestDto> ReceiveInterBranchTransferAsync(Guid companyId, Guid transferId, Guid userId, CancellationToken cancellationToken = default);
}

