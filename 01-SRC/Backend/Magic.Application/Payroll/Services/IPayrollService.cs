using Magic.Contracts.Payroll;

namespace Magic.Application.Payroll.Services;

public interface IPayrollService
{
    Task<PayrollSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalaryStructureDto>> GetSalaryStructuresAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<SalaryStructureDto> CreateSalaryStructureAsync(Guid companyId, CreateSalaryStructureRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeSalaryAssignmentDto> AssignSalaryStructureAsync(Guid companyId, AssignEmployeeSalaryRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeSalaryAssignmentDto?> GetEmployeeAssignmentAsync(Guid companyId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PayrollPeriodDto> Items, int TotalCount)> GetPayrollPeriodsAsync(Guid companyId, Guid branchId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PayrollPeriodDto> GetPayrollPeriodByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<PayrollPeriodDto> CalculatePayrollAsync(Guid companyId, Guid branchId, Guid userId, CalculatePayrollRequest request, CancellationToken cancellationToken = default);
    Task<PayrollPeriodDto> ApprovePayrollAsync(Guid companyId, Guid id, Guid approverUserId, CancellationToken cancellationToken = default);
    Task<PayrollPeriodDto> FinalizePayrollAsync(Guid companyId, Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<PayrollPeriodDto> DisbursePayrollAsync(Guid companyId, Guid id, Guid userId, DisbursePayrollRequest request, CancellationToken cancellationToken = default);
    Task<PayrollItemDto> GetPayslipAsync(Guid companyId, Guid payrollItemId, CancellationToken cancellationToken = default);
}

