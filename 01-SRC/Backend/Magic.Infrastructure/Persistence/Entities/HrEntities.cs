namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Organizational designation / job role.
/// </summary>
public sealed class DesignationRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public DepartmentRecord? Department { get; set; }
}

/// <summary>
/// Staff member directory record linked to branch and optional platform user.
/// </summary>
public sealed class EmployeeRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateOnly JoiningDate { get; set; }
    public string EmploymentType { get; set; } = "FullTime"; // FullTime, PartTime, Contract, Probation, Intern
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public string Status { get; set; } = "Active"; // Active, OnLeave, Resigned, Terminated, Deactivated
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIfsc { get; set; }
    public string? BankName { get; set; }
    public string? PanNumber { get; set; }
    public Guid? LinkedUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public DepartmentRecord? Department { get; set; }
    public DesignationRecord? Designation { get; set; }
    public EmployeeRecord? ReportingManager { get; set; }
    public UserRecord? LinkedUser { get; set; }
}

/// <summary>
/// Daily attendance logging with check-in, check-out, duration, and supervisor correction notes.
/// </summary>
public sealed class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTimeOffset? CheckInTimeUtc { get; set; }
    public DateTimeOffset? CheckOutTimeUtc { get; set; }
    public string Status { get; set; } = "Present"; // Present, Absent, HalfDay, OnLeave, Holiday
    public int WorkDurationMinutes { get; set; }
    public bool IsCorrected { get; set; }
    public string? CorrectionNotes { get; set; }
    public Guid? CorrectedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public EmployeeRecord? Employee { get; set; }
    public UserRecord? CorrectedByUser { get; set; }
}

/// <summary>
/// Leave type definition (Casual, Sick, Earned).
/// </summary>
public sealed class LeaveTypeRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AnnualDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Real-time leave balance quota for an employee per fiscal year.
/// </summary>
public sealed class LeaveBalanceRecord
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public Guid FiscalYearId { get; set; }
    public decimal TotalDays { get; set; }
    public decimal ConsumedDays { get; set; }
    public decimal PendingDays { get; set; }

    public EmployeeRecord? Employee { get; set; }
    public LeaveTypeRecord? LeaveType { get; set; }
}

/// <summary>
/// Leave application workflow with multi-state approval.
/// </summary>
public sealed class LeaveApplicationRecord
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Applied"; // Applied, Approved, Rejected, Cancelled
    public Guid? DecisionByUserId { get; set; }
    public DateTimeOffset? DecisionAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public EmployeeRecord? Employee { get; set; }
    public LeaveTypeRecord? LeaveType { get; set; }
    public UserRecord? DecisionByUser { get; set; }
}

