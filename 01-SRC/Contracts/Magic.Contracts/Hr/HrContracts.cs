namespace Magic.Contracts.Hr;

public sealed record DesignationDto(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Title,
    Guid? DepartmentId,
    string? DepartmentName,
    string? Description,
    bool IsActive);

public sealed record CreateDesignationRequest(
    string Code,
    string Title,
    Guid? DepartmentId,
    string? Description);

public sealed record EmployeeDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Address,
    DateOnly JoiningDate,
    string EmploymentType,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? DesignationId,
    string? DesignationTitle,
    Guid? ReportingManagerId,
    string? ReportingManagerName,
    string Status,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BankAccountNumber,
    string? BankIfsc,
    string? BankName,
    string? PanNumber,
    Guid? LinkedUserId,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateEmployeeRequest(
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Address,
    DateOnly JoiningDate,
    string EmploymentType,
    Guid? DepartmentId,
    Guid? DesignationId,
    Guid? ReportingManagerId,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BankAccountNumber,
    string? BankIfsc,
    string? BankName,
    string? PanNumber,
    Guid? LinkedUserId);

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Address,
    string EmploymentType,
    Guid? DepartmentId,
    Guid? DesignationId,
    Guid? ReportingManagerId,
    string Status,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BankAccountNumber,
    string? BankIfsc,
    string? BankName,
    string? PanNumber,
    Guid? LinkedUserId);

public sealed record AttendanceDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly AttendanceDate,
    DateTimeOffset? CheckInTimeUtc,
    DateTimeOffset? CheckOutTimeUtc,
    string Status,
    int WorkDurationMinutes,
    bool IsCorrected,
    string? CorrectionNotes,
    Guid? CorrectedByUserId);

public sealed record PunchAttendanceRequest(
    Guid EmployeeId,
    string PunchType, // CheckIn, CheckOut
    DateTimeOffset? PunchTimeUtc,
    string? Notes);

public sealed record CorrectAttendanceRequest(
    Guid EmployeeId,
    DateOnly AttendanceDate,
    string Status,
    DateTimeOffset? CheckInTimeUtc,
    DateTimeOffset? CheckOutTimeUtc,
    string CorrectionNotes);

public sealed record LeaveTypeDto(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Name,
    int AnnualDays,
    bool IsPaid,
    bool IsActive);

public sealed record CreateLeaveTypeRequest(
    string Code,
    string Name,
    int AnnualDays,
    bool IsPaid);

public sealed record LeaveBalanceDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    decimal TotalDays,
    decimal ConsumedDays,
    decimal PendingDays,
    decimal AvailableDays);

public sealed record LeaveApplicationDto(
    Guid Id,
    string ApplicationNumber,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason,
    string Status,
    Guid? DecisionByUserId,
    DateTimeOffset? DecisionAtUtc,
    string? RejectionReason,
    DateTimeOffset CreatedAtUtc);

public sealed record SubmitLeaveApplicationRequest(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason);

public sealed record RejectLeaveApplicationRequest(string RejectionReason);

public sealed record HrSummaryDto(
    int TotalEmployees,
    int ActiveEmployees,
    int TodayPresentCount,
    int TodayAbsentCount,
    int PendingLeaveApplicationsCount);

