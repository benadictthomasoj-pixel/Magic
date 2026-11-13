using Magic.Application.Hr.Services;
using Magic.Contracts.Hr;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Hr.Services;

public sealed class HrService(MagicDbContext dbContext) : IHrService
{
    public async Task<HrSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var total = await dbContext.Employees.CountAsync(e => e.CompanyId == companyId && e.BranchId == branchId, cancellationToken);
        var active = await dbContext.Employees.CountAsync(e => e.CompanyId == companyId && e.BranchId == branchId && e.Status == "Active", cancellationToken);
        var present = await dbContext.AttendanceRecords.CountAsync(a => a.CompanyId == companyId && a.BranchId == branchId && a.AttendanceDate == today && a.Status == "Present", cancellationToken);
        var absent = await dbContext.AttendanceRecords.CountAsync(a => a.CompanyId == companyId && a.BranchId == branchId && a.AttendanceDate == today && a.Status == "Absent", cancellationToken);
        var pendingLeaves = await dbContext.LeaveApplications.CountAsync(l => l.Employee!.CompanyId == companyId && l.Employee.BranchId == branchId && l.Status == "Applied", cancellationToken);

        return new HrSummaryDto(total, active, present, absent, pendingLeaves);
    }

    public async Task<IReadOnlyList<DesignationDto>> GetDesignationsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Designations
            .Include(d => d.Department)
            .Where(d => d.CompanyId == companyId && d.IsActive)
            .OrderBy(d => d.Title)
            .Select(d => new DesignationDto(
                d.Id,
                d.CompanyId,
                d.Code,
                d.Title,
                d.DepartmentId,
                d.Department != null ? d.Department.Name : null,
                d.Description,
                d.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<DesignationDto> CreateDesignationAsync(Guid companyId, CreateDesignationRequest request, CancellationToken cancellationToken = default)
    {
        var designation = new DesignationRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Title = request.Title.Trim(),
            DepartmentId = request.DepartmentId,
            Description = request.Description,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Designations.Add(designation);
        await dbContext.SaveChangesAsync(cancellationToken);

        string? deptName = null;
        if (designation.DepartmentId.HasValue)
        {
            deptName = (await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == designation.DepartmentId.Value, cancellationToken))?.Name;
        }

        return new DesignationDto(
            designation.Id,
            designation.CompanyId,
            designation.Code,
            designation.Title,
            designation.DepartmentId,
            deptName,
            designation.Description,
            designation.IsActive);
    }

    public async Task<(IReadOnlyList<EmployeeDto> Items, int TotalCount)> GetEmployeesAsync(
        Guid companyId,
        Guid branchId,
        Guid? departmentId,
        Guid? designationId,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.ReportingManager)
            .Where(e => e.CompanyId == companyId && e.BranchId == branchId);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (designationId.HasValue)
            query = query.Where(e => e.DesignationId == designationId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(e => EF.Functions.ILike(e.FirstName, pattern) ||
                                     EF.Functions.ILike(e.LastName, pattern) ||
                                     EF.Functions.ILike(e.EmployeeCode, pattern) ||
                                     EF.Functions.ILike(e.Email, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapEmployee(e))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<EmployeeDto> GetEmployeeByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.ReportingManager)
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Employee does not exist.", StatusCodes.Status404NotFound);

        return MapEmployee(employee);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(Guid companyId, Guid branchId, CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.EmployeeCode.Trim().ToUpperInvariant();
        var exists = await dbContext.Employees.AnyAsync(e => e.CompanyId == companyId && e.EmployeeCode == code, cancellationToken);
        if (exists)
            throw new DomainException("DUPLICATE_CODE", $"Employee code {code} is already registered.", StatusCodes.Status400BadRequest);

        var employee = new EmployeeRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            EmployeeCode = code,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            Address = request.Address,
            JoiningDate = request.JoiningDate,
            EmploymentType = request.EmploymentType,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            ReportingManagerId = request.ReportingManagerId,
            Status = "Active",
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            BankAccountNumber = request.BankAccountNumber,
            BankIfsc = request.BankIfsc,
            BankName = request.BankName,
            PanNumber = request.PanNumber,
            LinkedUserId = request.LinkedUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Employees.Add(employee);

        // Seed initial leave balances for active leave types
        var leaveTypes = await dbContext.LeaveTypes.Where(l => l.CompanyId == companyId && l.IsActive).ToListAsync(cancellationToken);
        var currentFy = await dbContext.FiscalYears.FirstOrDefaultAsync(f => f.CompanyId == companyId && !f.IsClosed, cancellationToken);
        if (currentFy != null)
        {
            foreach (var lt in leaveTypes)
            {
                dbContext.LeaveBalances.Add(new LeaveBalanceRecord
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    LeaveTypeId = lt.Id,
                    FiscalYearId = currentFy.Id,
                    TotalDays = lt.AnnualDays,
                    ConsumedDays = 0,
                    PendingDays = 0
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetEmployeeByIdAsync(companyId, employee.Id, cancellationToken);
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(Guid companyId, Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Employee does not exist.", StatusCodes.Status404NotFound);

        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = request.Email.Trim().ToLowerInvariant();
        employee.Phone = request.Phone.Trim();
        employee.Address = request.Address;
        employee.EmploymentType = request.EmploymentType;
        employee.DepartmentId = request.DepartmentId;
        employee.DesignationId = request.DesignationId;
        employee.ReportingManagerId = request.ReportingManagerId;
        employee.Status = request.Status;
        employee.EmergencyContactName = request.EmergencyContactName;
        employee.EmergencyContactPhone = request.EmergencyContactPhone;
        employee.BankAccountNumber = request.BankAccountNumber;
        employee.BankIfsc = request.BankIfsc;
        employee.BankName = request.BankName;
        employee.PanNumber = request.PanNumber;
        employee.LinkedUserId = request.LinkedUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetEmployeeByIdAsync(companyId, employee.Id, cancellationToken);
    }

    public async Task<(IReadOnlyList<AttendanceDto> Items, int TotalCount)> GetAttendanceAsync(
        Guid companyId,
        Guid branchId,
        DateOnly attendanceDate,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AttendanceRecords
            .Include(a => a.Employee)
            .Where(a => a.CompanyId == companyId && a.BranchId == branchId && a.AttendanceDate == attendanceDate);

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(a => a.Employee != null ? a.Employee.FirstName : string.Empty)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AttendanceDto(
                a.Id,
                a.EmployeeId,
                a.Employee != null ? a.Employee.EmployeeCode : "UNKNOWN",
                a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}" : "Unknown",
                a.AttendanceDate,
                a.CheckInTimeUtc,
                a.CheckOutTimeUtc,
                a.Status,
                a.WorkDurationMinutes,
                a.IsCorrected,
                a.CorrectionNotes,
                a.CorrectedByUserId))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<AttendanceDto> PunchAttendanceAsync(Guid companyId, Guid branchId, PunchAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Employee does not exist.", StatusCodes.Status404NotFound);

        var now = request.PunchTimeUtc ?? DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        var record = await dbContext.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == today, cancellationToken);

        if (record == null)
        {
            record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                CompanyId = companyId,
                BranchId = branchId,
                AttendanceDate = today,
                CheckInTimeUtc = now,
                Status = "Present",
                WorkDurationMinutes = 0,
                IsCorrected = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.AttendanceRecords.Add(record);
        }
        else
        {
            if (request.PunchType == "CheckOut")
            {
                record.CheckOutTimeUtc = now;
                if (record.CheckInTimeUtc.HasValue)
                {
                    record.WorkDurationMinutes = (int)(now - record.CheckInTimeUtc.Value).TotalMinutes;
                }
            }
            else
            {
                record.CheckInTimeUtc = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AttendanceDto(
            record.Id,
            record.EmployeeId,
            employee.EmployeeCode,
            $"{employee.FirstName} {employee.LastName}",
            record.AttendanceDate,
            record.CheckInTimeUtc,
            record.CheckOutTimeUtc,
            record.Status,
            record.WorkDurationMinutes,
            record.IsCorrected,
            record.CorrectionNotes,
            record.CorrectedByUserId);
    }

    public async Task<AttendanceDto> CorrectAttendanceAsync(Guid companyId, Guid branchId, Guid supervisorUserId, CorrectAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Employee does not exist.", StatusCodes.Status404NotFound);

        var record = await dbContext.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == request.AttendanceDate, cancellationToken);

        if (record == null)
        {
            record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                CompanyId = companyId,
                BranchId = branchId,
                AttendanceDate = request.AttendanceDate,
                CheckInTimeUtc = request.CheckInTimeUtc,
                CheckOutTimeUtc = request.CheckOutTimeUtc,
                Status = request.Status,
                IsCorrected = true,
                CorrectionNotes = request.CorrectionNotes,
                CorrectedByUserId = supervisorUserId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            if (request.CheckInTimeUtc.HasValue && request.CheckOutTimeUtc.HasValue)
            {
                record.WorkDurationMinutes = (int)(request.CheckOutTimeUtc.Value - request.CheckInTimeUtc.Value).TotalMinutes;
            }
            dbContext.AttendanceRecords.Add(record);
        }
        else
        {
            record.Status = request.Status;
            record.CheckInTimeUtc = request.CheckInTimeUtc;
            record.CheckOutTimeUtc = request.CheckOutTimeUtc;
            if (request.CheckInTimeUtc.HasValue && request.CheckOutTimeUtc.HasValue)
            {
                record.WorkDurationMinutes = (int)(request.CheckOutTimeUtc.Value - request.CheckInTimeUtc.Value).TotalMinutes;
            }
            record.IsCorrected = true;
            record.CorrectionNotes = request.CorrectionNotes;
            record.CorrectedByUserId = supervisorUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AttendanceDto(
            record.Id,
            record.EmployeeId,
            employee.EmployeeCode,
            $"{employee.FirstName} {employee.LastName}",
            record.AttendanceDate,
            record.CheckInTimeUtc,
            record.CheckOutTimeUtc,
            record.Status,
            record.WorkDurationMinutes,
            record.IsCorrected,
            record.CorrectionNotes,
            record.CorrectedByUserId);
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveTypes
            .Where(l => l.CompanyId == companyId && l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LeaveTypeDto(l.Id, l.CompanyId, l.Code, l.Name, l.AnnualDays, l.IsPaid, l.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveTypeDto> CreateLeaveTypeAsync(Guid companyId, CreateLeaveTypeRequest request, CancellationToken cancellationToken = default)
    {
        var lt = new LeaveTypeRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            AnnualDays = request.AnnualDays,
            IsPaid = request.IsPaid,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.LeaveTypes.Add(lt);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LeaveTypeDto(lt.Id, lt.CompanyId, lt.Code, lt.Name, lt.AnnualDays, lt.IsPaid, lt.IsActive);
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetLeaveBalancesAsync(Guid companyId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveBalances
            .Include(b => b.Employee)
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Employee!.CompanyId == companyId)
            .Select(b => new LeaveBalanceDto(
                b.Id,
                b.EmployeeId,
                b.Employee != null ? $"{b.Employee.FirstName} {b.Employee.LastName}" : "Unknown",
                b.LeaveTypeId,
                b.LeaveType != null ? b.LeaveType.Code : "LEAVE",
                b.LeaveType != null ? b.LeaveType.Name : "Leave",
                b.TotalDays,
                b.ConsumedDays,
                b.PendingDays,
                b.TotalDays - b.ConsumedDays - b.PendingDays))
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<LeaveApplicationDto> Items, int TotalCount)> GetLeaveApplicationsAsync(
        Guid companyId,
        Guid? employeeId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.LeaveApplications
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .Where(l => l.Employee!.CompanyId == companyId);

        if (employeeId.HasValue)
            query = query.Where(l => l.EmployeeId == employeeId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(l => l.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeaveApplicationDto(
                l.Id,
                l.ApplicationNumber,
                l.EmployeeId,
                l.Employee != null ? l.Employee.EmployeeCode : "UNKNOWN",
                l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}" : "Unknown",
                l.LeaveTypeId,
                l.LeaveType != null ? l.LeaveType.Name : "Leave",
                l.StartDate,
                l.EndDate,
                l.TotalDays,
                l.Reason,
                l.Status,
                l.DecisionByUserId,
                l.DecisionAtUtc,
                l.RejectionReason,
                l.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<LeaveApplicationDto> SubmitLeaveApplicationAsync(Guid companyId, SubmitLeaveApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Employee does not exist.", StatusCodes.Status404NotFound);

        var balance = await dbContext.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == employee.Id && b.LeaveTypeId == request.LeaveTypeId, cancellationToken);
        if (balance != null)
        {
            var available = balance.TotalDays - balance.ConsumedDays - balance.PendingDays;
            if (available < request.TotalDays)
                throw new DomainException("INSUFFICIENT_LEAVE_BALANCE", $"Insufficient leave quota. Available: {available} days, Requested: {request.TotalDays} days.", StatusCodes.Status400BadRequest);

            balance.PendingDays += request.TotalDays;
        }

        var appCount = await dbContext.LeaveApplications.CountAsync(cancellationToken);
        var appNumber = $"LEV-{DateTime.UtcNow:yyyyMM}-{appCount + 1:D4}";

        var app = new LeaveApplicationRecord
        {
            Id = Guid.NewGuid(),
            ApplicationNumber = appNumber,
            EmployeeId = employee.Id,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = request.TotalDays,
            Reason = request.Reason,
            Status = "Applied",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.LeaveApplications.Add(app);
        await dbContext.SaveChangesAsync(cancellationToken);

        var leaveType = await dbContext.LeaveTypes.FirstOrDefaultAsync(l => l.Id == app.LeaveTypeId, cancellationToken);

        return new LeaveApplicationDto(
            app.Id,
            app.ApplicationNumber,
            app.EmployeeId,
            employee.EmployeeCode,
            $"{employee.FirstName} {employee.LastName}",
            app.LeaveTypeId,
            leaveType?.Name ?? "Leave",
            app.StartDate,
            app.EndDate,
            app.TotalDays,
            app.Reason,
            app.Status,
            app.DecisionByUserId,
            app.DecisionAtUtc,
            app.RejectionReason,
            app.CreatedAtUtc);
    }

    public async Task<LeaveApplicationDto> ApproveLeaveApplicationAsync(Guid companyId, Guid id, Guid approverUserId, CancellationToken cancellationToken = default)
    {
        var app = await dbContext.LeaveApplications
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .FirstOrDefaultAsync(l => l.Id == id && l.Employee!.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("APPLICATION_NOT_FOUND", "Leave application does not exist.", StatusCodes.Status404NotFound);

        if (app.Status != "Applied")
            throw new DomainException("INVALID_STATE", $"Cannot approve leave in {app.Status} status.", StatusCodes.Status400BadRequest);

        app.Status = "Approved";
        app.DecisionByUserId = approverUserId;
        app.DecisionAtUtc = DateTimeOffset.UtcNow;

        var balance = await dbContext.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == app.EmployeeId && b.LeaveTypeId == app.LeaveTypeId, cancellationToken);
        if (balance != null)
        {
            balance.PendingDays = Math.Max(0, balance.PendingDays - app.TotalDays);
            balance.ConsumedDays += app.TotalDays;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new LeaveApplicationDto(
            app.Id,
            app.ApplicationNumber,
            app.EmployeeId,
            app.Employee!.EmployeeCode,
            $"{app.Employee.FirstName} {app.Employee.LastName}",
            app.LeaveTypeId,
            app.LeaveType?.Name ?? "Leave",
            app.StartDate,
            app.EndDate,
            app.TotalDays,
            app.Reason,
            app.Status,
            app.DecisionByUserId,
            app.DecisionAtUtc,
            app.RejectionReason,
            app.CreatedAtUtc);
    }

    public async Task<LeaveApplicationDto> RejectLeaveApplicationAsync(Guid companyId, Guid id, Guid approverUserId, RejectLeaveApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var app = await dbContext.LeaveApplications
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .FirstOrDefaultAsync(l => l.Id == id && l.Employee!.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("APPLICATION_NOT_FOUND", "Leave application does not exist.", StatusCodes.Status404NotFound);

        if (app.Status != "Applied")
            throw new DomainException("INVALID_STATE", $"Cannot reject leave in {app.Status} status.", StatusCodes.Status400BadRequest);

        app.Status = "Rejected";
        app.DecisionByUserId = approverUserId;
        app.DecisionAtUtc = DateTimeOffset.UtcNow;
        app.RejectionReason = request.RejectionReason;

        var balance = await dbContext.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == app.EmployeeId && b.LeaveTypeId == app.LeaveTypeId, cancellationToken);
        if (balance != null)
        {
            balance.PendingDays = Math.Max(0, balance.PendingDays - app.TotalDays);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new LeaveApplicationDto(
            app.Id,
            app.ApplicationNumber,
            app.EmployeeId,
            app.Employee!.EmployeeCode,
            $"{app.Employee.FirstName} {app.Employee.LastName}",
            app.LeaveTypeId,
            app.LeaveType?.Name ?? "Leave",
            app.StartDate,
            app.EndDate,
            app.TotalDays,
            app.Reason,
            app.Status,
            app.DecisionByUserId,
            app.DecisionAtUtc,
            app.RejectionReason,
            app.CreatedAtUtc);
    }

    private static EmployeeDto MapEmployee(EmployeeRecord e) => new(
        e.Id,
        e.CompanyId,
        e.BranchId,
        e.EmployeeCode,
        e.FirstName,
        e.LastName,
        e.Email,
        e.Phone,
        e.Address,
        e.JoiningDate,
        e.EmploymentType,
        e.DepartmentId,
        e.Department != null ? e.Department.Name : null,
        e.DesignationId,
        e.Designation != null ? e.Designation.Title : null,
        e.ReportingManagerId,
        e.ReportingManager != null ? $"{e.ReportingManager.FirstName} {e.ReportingManager.LastName}" : null,
        e.Status,
        e.EmergencyContactName,
        e.EmergencyContactPhone,
        e.BankAccountNumber,
        e.BankIfsc,
        e.BankName,
        e.PanNumber,
        e.LinkedUserId,
        e.CreatedAtUtc);
}
