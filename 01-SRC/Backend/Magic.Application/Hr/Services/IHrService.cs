using Magic.Contracts.Hr;

namespace Magic.Application.Hr.Services;

public interface IHrService
{
    Task<HrSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DesignationDto>> GetDesignationsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<DesignationDto> CreateDesignationAsync(Guid companyId, CreateDesignationRequest request, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<EmployeeDto> Items, int TotalCount)> GetEmployeesAsync(Guid companyId, Guid branchId, Guid? departmentId, Guid? designationId, string? status, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<EmployeeDto> GetEmployeeByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateEmployeeAsync(Guid companyId, Guid branchId, CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateEmployeeAsync(Guid companyId, Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AttendanceDto> Items, int TotalCount)> GetAttendanceAsync(Guid companyId, Guid branchId, DateOnly attendanceDate, Guid? employeeId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AttendanceDto> PunchAttendanceAsync(Guid companyId, Guid branchId, PunchAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceDto> CorrectAttendanceAsync(Guid companyId, Guid branchId, Guid supervisorUserId, CorrectAttendanceRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<LeaveTypeDto> CreateLeaveTypeAsync(Guid companyId, CreateLeaveTypeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveBalanceDto>> GetLeaveBalancesAsync(Guid companyId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<LeaveApplicationDto> Items, int TotalCount)> GetLeaveApplicationsAsync(Guid companyId, Guid? employeeId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<LeaveApplicationDto> SubmitLeaveApplicationAsync(Guid companyId, SubmitLeaveApplicationRequest request, CancellationToken cancellationToken = default);
    Task<LeaveApplicationDto> ApproveLeaveApplicationAsync(Guid companyId, Guid id, Guid approverUserId, CancellationToken cancellationToken = default);
    Task<LeaveApplicationDto> RejectLeaveApplicationAsync(Guid companyId, Guid id, Guid approverUserId, RejectLeaveApplicationRequest request, CancellationToken cancellationToken = default);
}
