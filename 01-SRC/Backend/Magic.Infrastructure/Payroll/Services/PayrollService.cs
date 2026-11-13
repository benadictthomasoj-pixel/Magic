using Magic.Application.Accounting.Services;
using Magic.Application.Payroll.Services;
using Magic.Contracts.Payroll;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Payroll.Services;

public sealed class PayrollService(
    MagicDbContext dbContext,
    IPostingService postingService) : IPayrollService
{
    public async Task<PayrollSummaryDto> GetSummaryAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var periods = await dbContext.PayrollPeriods
            .Where(p => p.CompanyId == companyId && p.BranchId == branchId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync(cancellationToken);

        var lastPeriod = periods.FirstOrDefault(p => p.Status == "Finalized" || p.Status == "Paid");
        var lastMonthNet = lastPeriod?.TotalNetPaise ?? 0;
        var lastMonthEmpCount = lastPeriod != null
            ? await dbContext.PayrollItems.CountAsync(i => i.PayrollPeriodId == lastPeriod.Id, cancellationToken)
            : 0;

        var pendingCount = periods.Count(p => p.Status == "Calculated");

        return new PayrollSummaryDto(periods.Count, lastMonthNet, lastMonthEmpCount, pendingCount);
    }

    public async Task<IReadOnlyList<SalaryStructureDto>> GetSalaryStructuresAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.SalaryStructures
            .Include(s => s.Components)
            .Where(s => s.CompanyId == companyId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SalaryStructureDto(
                s.Id,
                s.CompanyId,
                s.Name,
                s.Description,
                s.IsActive,
                s.Components.Select(c => new SalaryStructureComponentDto(
                    c.Id,
                    c.SalaryStructureId,
                    c.Name,
                    c.ComponentType,
                    c.CalculationType,
                    c.DefaultValue,
                    c.IsDeduction,
                    c.IsEmployerContribution)).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<SalaryStructureDto> CreateSalaryStructureAsync(Guid companyId, CreateSalaryStructureRequest request, CancellationToken cancellationToken = default)
    {
        var structure = new SalaryStructureRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Description = request.Description,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var c in request.Components)
        {
            structure.Components.Add(new SalaryStructureComponentRecord
            {
                Id = Guid.NewGuid(),
                SalaryStructureId = structure.Id,
                Name = c.Name.Trim(),
                ComponentType = c.ComponentType,
                CalculationType = c.CalculationType,
                DefaultValue = c.DefaultValue,
                IsDeduction = c.IsDeduction,
                IsEmployerContribution = c.IsEmployerContribution
            });
        }

        dbContext.SalaryStructures.Add(structure);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetSalaryStructuresAsync(companyId, cancellationToken)).First(s => s.Id == structure.Id);
    }

    public async Task<EmployeeSalaryAssignmentDto> AssignSalaryStructureAsync(Guid companyId, AssignEmployeeSalaryRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", "Employee does not exist.", StatusCodes.Status404NotFound);

        var structure = await dbContext.SalaryStructures.FirstOrDefaultAsync(s => s.Id == request.SalaryStructureId && s.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("STRUCTURE_NOT_FOUND", "Salary structure does not exist.", StatusCodes.Status404NotFound);

        var existing = await dbContext.EmployeeSalaryAssignments
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.IsActive, cancellationToken);

        if (existing != null)
        {
            existing.IsActive = false;
        }

        var assignment = new EmployeeSalaryAssignmentRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            SalaryStructureId = structure.Id,
            MonthlyGrossPaise = request.MonthlyGrossPaise,
            EffectiveFrom = request.EffectiveFrom,
            IsActive = true
        };

        dbContext.EmployeeSalaryAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new EmployeeSalaryAssignmentDto(
            assignment.Id,
            employee.Id,
            employee.EmployeeCode,
            $"{employee.FirstName} {employee.LastName}",
            structure.Id,
            structure.Name,
            assignment.MonthlyGrossPaise,
            assignment.EffectiveFrom,
            assignment.IsActive);
    }

    public async Task<EmployeeSalaryAssignmentDto?> GetEmployeeAssignmentAsync(Guid companyId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.EmployeeSalaryAssignments
            .Include(a => a.Employee)
            .Include(a => a.SalaryStructure)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Employee!.CompanyId == companyId && a.IsActive, cancellationToken);

        if (assignment == null)
            return null;

        return new EmployeeSalaryAssignmentDto(
            assignment.Id,
            assignment.EmployeeId,
            assignment.Employee!.EmployeeCode,
            $"{assignment.Employee.FirstName} {assignment.Employee.LastName}",
            assignment.SalaryStructureId,
            assignment.SalaryStructure?.Name ?? "Standard",
            assignment.MonthlyGrossPaise,
            assignment.EffectiveFrom,
            assignment.IsActive);
    }

    public async Task<(IReadOnlyList<PayrollPeriodDto> Items, int TotalCount)> GetPayrollPeriodsAsync(
        Guid companyId,
        Guid branchId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PayrollPeriods
            .Where(p => p.CompanyId == companyId && p.BranchId == branchId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PayrollPeriodDto(
                p.Id,
                p.CompanyId,
                p.BranchId,
                p.FiscalYearId,
                p.PeriodName,
                p.Month,
                p.Year,
                p.StartDate,
                p.EndDate,
                p.Status,
                p.TotalGrossPaise,
                p.TotalDeductionsPaise,
                p.TotalNetPaise,
                p.TotalEmployerContributionsPaise,
                p.JournalVoucherId,
                p.DisbursementVoucherId,
                p.ProcessedByUserId,
                p.ApprovedByUserId,
                p.FinalizedAtUtc,
                dbContext.PayrollItems.Count(i => i.PayrollPeriodId == p.Id),
                null))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<PayrollPeriodDto> GetPayrollPeriodByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var period = await dbContext.PayrollPeriods
            .Include(p => p.Items)
                .ThenInclude(i => i.Employee)
                    .ThenInclude(e => e!.Designation)
            .Include(p => p.Items)
                .ThenInclude(i => i.Employee)
                    .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "Payroll cycle not found.", StatusCodes.Status404NotFound);

        var itemsDto = period.Items.Select(i => new PayrollItemDto(
            i.Id,
            i.PayrollPeriodId,
            i.EmployeeId,
            i.Employee?.EmployeeCode ?? "UNKNOWN",
            i.Employee != null ? $"{i.Employee.FirstName} {i.Employee.LastName}" : "Unknown",
            i.Employee?.Designation?.Title,
            i.Employee?.Department?.Name,
            i.PresentDays,
            i.PaidLeaveDays,
            i.UnpaidLeaveDays,
            i.BasicPaise,
            i.HraPaise,
            i.AllowancesPaise,
            i.GrossSalaryPaise,
            i.PfDeductionPaise,
            i.EsiDeductionPaise,
            i.PtDeductionPaise,
            i.TdsDeductionPaise,
            i.TotalDeductionsPaise,
            i.NetSalaryPaise,
            i.EmployerPfPaise,
            i.EmployerEsiPaise,
            i.PaymentStatus,
            i.PaymentReference)).ToList();

        return new PayrollPeriodDto(
            period.Id,
            period.CompanyId,
            period.BranchId,
            period.FiscalYearId,
            period.PeriodName,
            period.Month,
            period.Year,
            period.StartDate,
            period.EndDate,
            period.Status,
            period.TotalGrossPaise,
            period.TotalDeductionsPaise,
            period.TotalNetPaise,
            period.TotalEmployerContributionsPaise,
            period.JournalVoucherId,
            period.DisbursementVoucherId,
            period.ProcessedByUserId,
            period.ApprovedByUserId,
            period.FinalizedAtUtc,
            itemsDto.Count,
            itemsDto);
    }

    public async Task<PayrollPeriodDto> CalculatePayrollAsync(Guid companyId, Guid branchId, Guid userId, CalculatePayrollRequest request, CancellationToken cancellationToken = default)
    {
        var existingPeriod = await dbContext.PayrollPeriods
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.BranchId == branchId && p.Year == request.Year && p.Month == request.Month, cancellationToken);

        if (existingPeriod != null && existingPeriod.Status != "Calculated")
        {
            throw new DomainException("PERIOD_ALREADY_EXISTS", $"Payroll cycle {request.Month}/{request.Year} is already in {existingPeriod.Status} state and cannot be recalculated.", StatusCodes.Status400BadRequest);
        }

        var startDate = new DateOnly(request.Year, request.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
        var endDate = new DateOnly(request.Year, request.Month, daysInMonth);

        var fiscalYear = await dbContext.FiscalYears
            .FirstOrDefaultAsync(f => f.CompanyId == companyId && f.StartDate <= startDate && f.EndDate >= endDate, cancellationToken)
            ?? await dbContext.FiscalYears.FirstOrDefaultAsync(f => f.CompanyId == companyId && !f.IsClosed, cancellationToken)
            ?? throw new DomainException("FY_NOT_FOUND", "Active fiscal year not found for this date range.", StatusCodes.Status400BadRequest);

        var activeEmployees = await dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Where(e => e.CompanyId == companyId && e.BranchId == branchId && e.Status == "Active")
            .ToListAsync(cancellationToken);

        var assignments = await dbContext.EmployeeSalaryAssignments
            .Include(a => a.SalaryStructure)
                .ThenInclude(s => s!.Components)
            .Where(a => a.Employee!.CompanyId == companyId && a.IsActive)
            .ToDictionaryAsync(a => a.EmployeeId, cancellationToken);

        // Attendance records for the month
        var attendanceRecords = await dbContext.AttendanceRecords
            .Where(a => a.CompanyId == companyId && a.BranchId == branchId && a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
            .ToListAsync(cancellationToken);

        var period = existingPeriod ?? new PayrollPeriodRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            FiscalYearId = fiscalYear.Id,
            PeriodName = $"{startDate:MMMM yyyy}",
            Month = request.Month,
            Year = request.Year,
            StartDate = startDate,
            EndDate = endDate,
            Status = "Calculated",
            ProcessedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (existingPeriod != null)
        {
            var oldItems = await dbContext.PayrollItems.Where(i => i.PayrollPeriodId == existingPeriod.Id).ToListAsync(cancellationToken);
            dbContext.PayrollItems.RemoveRange(oldItems);
            period.Items.Clear();
        }
        else
        {
            dbContext.PayrollPeriods.Add(period);
        }

        long totalGross = 0;
        long totalDeductions = 0;
        long totalNet = 0;
        long totalEmployerContributions = 0;

        foreach (var emp in activeEmployees)
        {
            if (!assignments.TryGetValue(emp.Id, out var assign))
                continue;

            var empAttendance = attendanceRecords.Where(a => a.EmployeeId == emp.Id).ToList();
            var presentDays = (decimal)empAttendance.Count(a => a.Status == "Present") + (empAttendance.Count(a => a.Status == "HalfDay") * 0.5m);
            var paidLeaves = (decimal)empAttendance.Count(a => a.Status == "OnLeave" || a.Status == "Holiday");

            // If attendance wasn't logged daily, default working days to full month
            var workedDays = presentDays + paidLeaves;
            if (workedDays == 0)
                workedDays = daysInMonth;

            var unpaidLeaves = Math.Max(0, daysInMonth - workedDays);

            // Pro-rate gross salary based on days worked
            var grossMonthly = assign.MonthlyGrossPaise;
            var proRatedGross = (long)Math.Round((double)grossMonthly * (double)workedDays / daysInMonth);

            // Component calculations: Basic (50%), HRA (30%), Allowances (20%)
            var basic = (long)Math.Round(proRatedGross * 0.50);
            var hra = (long)Math.Round(proRatedGross * 0.30);
            var allowance = proRatedGross - basic - hra;

            // Statutory Deductions: Employee PF = 12% of Basic, ESI = 0.75% of Gross, PT = 200 INR (20000 paise), TDS = 0
            var pfEmp = (long)Math.Round(basic * 0.12);
            var esiEmp = proRatedGross < 2100000 ? (long)Math.Round(proRatedGross * 0.0075) : 0; // ESI under 21,000 INR
            var pt = 20000L; // 200 INR
            var tds = 0L;

            var itemDeductions = pfEmp + esiEmp + pt + tds;
            var netSalary = proRatedGross - itemDeductions;

            // Employer Contributions: Employer PF = 12% of Basic, Employer ESI = 3.25% of Gross
            var pfEmployer = (long)Math.Round(basic * 0.12);
            var esiEmployer = proRatedGross < 2100000 ? (long)Math.Round(proRatedGross * 0.0325) : 0;

            var item = new PayrollItemRecord
            {
                Id = Guid.NewGuid(),
                PayrollPeriodId = period.Id,
                EmployeeId = emp.Id,
                PresentDays = presentDays,
                PaidLeaveDays = paidLeaves,
                UnpaidLeaveDays = unpaidLeaves,
                BasicPaise = basic,
                HraPaise = hra,
                AllowancesPaise = allowance,
                GrossSalaryPaise = proRatedGross,
                PfDeductionPaise = pfEmp,
                EsiDeductionPaise = esiEmp,
                PtDeductionPaise = pt,
                TdsDeductionPaise = tds,
                TotalDeductionsPaise = itemDeductions,
                NetSalaryPaise = netSalary,
                EmployerPfPaise = pfEmployer,
                EmployerEsiPaise = esiEmployer,
                PaymentStatus = "Pending"
            };

            period.Items.Add(item);
            dbContext.PayrollItems.Add(item);

            totalGross += proRatedGross;
            totalDeductions += itemDeductions;
            totalNet += netSalary;
            totalEmployerContributions += (pfEmployer + esiEmployer);
        }

        period.TotalGrossPaise = totalGross;
        period.TotalDeductionsPaise = totalDeductions;
        period.TotalNetPaise = totalNet;
        period.TotalEmployerContributionsPaise = totalEmployerContributions;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetPayrollPeriodByIdAsync(companyId, period.Id, cancellationToken);
    }

    public async Task<PayrollPeriodDto> ApprovePayrollAsync(Guid companyId, Guid id, Guid approverUserId, CancellationToken cancellationToken = default)
    {
        var period = await dbContext.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "Payroll cycle not found.", StatusCodes.Status404NotFound);

        if (period.Status != "Calculated")
            throw new DomainException("INVALID_STATE", $"Cannot approve payroll in {period.Status} status.", StatusCodes.Status400BadRequest);

        period.Status = "Approved";
        period.ApprovedByUserId = approverUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPayrollPeriodByIdAsync(companyId, period.Id, cancellationToken);
    }

    public async Task<PayrollPeriodDto> FinalizePayrollAsync(Guid companyId, Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var period = await dbContext.PayrollPeriods
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "Payroll cycle not found.", StatusCodes.Status404NotFound);

        if (period.Status != "Approved")
            throw new DomainException("INVALID_STATE", "Payroll must be approved before finalization.", StatusCodes.Status400BadRequest);

        var fiscalPeriod = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.StartDate <= period.EndDate && p.EndDate >= period.EndDate, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "No active fiscal period found for payroll finalization.", StatusCodes.Status400BadRequest);

        // Fetch Accounts:
        // Expense: 5210 (Salaries & Wages), 5211 (Employer Contributions)
        // Liability: 2130 (Net Salaries Payable), 2131 (Employee PF), 2132 (Employer PF), 2133 (ESI Payable), 2134 (PT Payable), 2135 (TDS Payable)
        var accSalariesExp = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "5210", cancellationToken)
            ?? await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "5201", cancellationToken);

        var accEmployerContribExp = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "5211", cancellationToken)
            ?? accSalariesExp;

        var accNetSalariesPayable = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2130", cancellationToken)
            ?? await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "2111", cancellationToken);

        var accPfEmp = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2131", cancellationToken) ?? accNetSalariesPayable;
        var accPfEmployer = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2132", cancellationToken) ?? accNetSalariesPayable;
        var accEsi = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2133", cancellationToken) ?? accNetSalariesPayable;
        var accPt = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2134", cancellationToken) ?? accNetSalariesPayable;

        var totalPfEmp = period.Items.Sum(i => i.PfDeductionPaise);
        var totalPfEmployer = period.Items.Sum(i => i.EmployerPfPaise);
        var totalEsiTotal = period.Items.Sum(i => i.EsiDeductionPaise + i.EmployerEsiPaise);
        var totalPt = period.Items.Sum(i => i.PtDeductionPaise);

        // Double-Entry Accrual Journal Voucher
        var voucher = new JournalVoucher(
            companyId,
            period.BranchId,
            fiscalPeriod.Id,
            VoucherType.Journal,
            period.EndDate,
            userId,
            $"Payroll Accrual for {period.PeriodName}",
            referenceNumber: null,
            voucherNumber: string.Empty,
            idempotencyKey: Guid.NewGuid(),
            sourceModule: "PAYROLL",
            sourceDocumentId: period.Id);

        // Debits:
        voucher.AddLine(accSalariesExp.Id, Money.FromPaise(period.TotalGrossPaise), Money.Zero, $"Staff Salaries & Wages: {period.PeriodName}");
        if (period.TotalEmployerContributionsPaise > 0)
        {
            voucher.AddLine(accEmployerContribExp.Id, Money.FromPaise(period.TotalEmployerContributionsPaise), Money.Zero, "Employer Statutory Contributions (PF + ESI)");
        }

        // Credits:
        voucher.AddLine(accNetSalariesPayable.Id, Money.Zero, Money.FromPaise(period.TotalNetPaise), "Net Salaries Payable to Staff");
        if (totalPfEmp > 0)
            voucher.AddLine(accPfEmp.Id, Money.Zero, Money.FromPaise(totalPfEmp), "Employee PF Deductions Payable");
        if (totalPfEmployer > 0)
            voucher.AddLine(accPfEmployer.Id, Money.Zero, Money.FromPaise(totalPfEmployer), "Employer PF Contributions Payable");
        if (totalEsiTotal > 0)
            voucher.AddLine(accEsi.Id, Money.Zero, Money.FromPaise(totalEsiTotal), "Total ESI Contributions Payable");
        if (totalPt > 0)
            voucher.AddLine(accPt.Id, Money.Zero, Money.FromPaise(totalPt), "Professional Tax Payable");

        var fyCode = fiscalPeriod.FiscalYear?.Code ?? "FY2026-27";
        var posted = await postingService.PostVoucherAsync(voucher, "MAIN", fyCode, userId, hasSupervisorElevation: true, cancellationToken);

        period.Status = "Finalized";
        period.JournalVoucherId = posted.Id;
        period.FinalizedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPayrollPeriodByIdAsync(companyId, period.Id, cancellationToken);
    }

    public async Task<PayrollPeriodDto> DisbursePayrollAsync(Guid companyId, Guid id, Guid userId, DisbursePayrollRequest request, CancellationToken cancellationToken = default)
    {
        var period = await dbContext.PayrollPeriods
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "Payroll cycle not found.", StatusCodes.Status404NotFound);

        if (period.Status != "Finalized")
            throw new DomainException("INVALID_STATE", "Payroll must be finalized before disbursement.", StatusCodes.Status400BadRequest);

        var bankAccount = await dbContext.BankCashAccounts.FirstOrDefaultAsync(b => b.Id == request.BankAccountId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BANK_NOT_FOUND", "Disbursement bank account not found.", StatusCodes.Status404NotFound);

        var fiscalPeriod = await dbContext.FiscalPeriods
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.StartDate <= request.DisbursementDate && p.EndDate >= request.DisbursementDate, cancellationToken)
            ?? throw new DomainException("PERIOD_NOT_FOUND", "No active fiscal period found for disbursement date.", StatusCodes.Status400BadRequest);

        var accNetSalariesPayable = await dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == "2130", cancellationToken)
            ?? await dbContext.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "2111", cancellationToken);

        // Payment Voucher: Debit 2130 Net Salaries Payable, Credit Bank Account GL
        var voucher = new JournalVoucher(
            companyId,
            period.BranchId,
            fiscalPeriod.Id,
            VoucherType.Payment,
            request.DisbursementDate,
            userId,
            $"Salary Disbursement for {period.PeriodName} (Ref: {request.PaymentReference})",
            referenceNumber: request.PaymentReference,
            voucherNumber: string.Empty,
            idempotencyKey: Guid.NewGuid(),
            sourceModule: "PAYROLL",
            sourceDocumentId: period.Id);

        voucher.AddLine(accNetSalariesPayable.Id, Money.FromPaise(period.TotalNetPaise), Money.Zero, $"Net Salary Settlement: {period.PeriodName}");
        voucher.AddLine(bankAccount.GlAccountId, Money.Zero, Money.FromPaise(period.TotalNetPaise), $"Disbursement from {bankAccount.AccountName}");

        var fyCode = fiscalPeriod.FiscalYear?.Code ?? "FY2026-27";
        var posted = await postingService.PostVoucherAsync(voucher, "MAIN", fyCode, userId, hasSupervisorElevation: true, cancellationToken);

        bankAccount.CurrentBalancePaise -= period.TotalNetPaise;
        period.Status = "Paid";
        period.DisbursementVoucherId = posted.Id;

        foreach (var item in period.Items)
        {
            item.PaymentStatus = "Paid";
            item.PaymentReference = request.PaymentReference;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPayrollPeriodByIdAsync(companyId, period.Id, cancellationToken);
    }

    public async Task<PayrollItemDto> GetPayslipAsync(Guid companyId, Guid payrollItemId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.PayrollItems
            .Include(i => i.Employee)
                .ThenInclude(e => e!.Department)
            .Include(i => i.Employee)
                .ThenInclude(e => e!.Designation)
            .Include(i => i.PayrollPeriod)
            .FirstOrDefaultAsync(i => i.Id == payrollItemId && i.PayrollPeriod!.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("PAYSLIP_NOT_FOUND", "Payslip item not found.", StatusCodes.Status404NotFound);

        return new PayrollItemDto(
            item.Id,
            item.PayrollPeriodId,
            item.EmployeeId,
            item.Employee?.EmployeeCode ?? "UNKNOWN",
            item.Employee != null ? $"{item.Employee.FirstName} {item.Employee.LastName}" : "Unknown",
            item.Employee?.Designation?.Title,
            item.Employee?.Department?.Name,
            item.PresentDays,
            item.PaidLeaveDays,
            item.UnpaidLeaveDays,
            item.BasicPaise,
            item.HraPaise,
            item.AllowancesPaise,
            item.GrossSalaryPaise,
            item.PfDeductionPaise,
            item.EsiDeductionPaise,
            item.PtDeductionPaise,
            item.TdsDeductionPaise,
            item.TotalDeductionsPaise,
            item.NetSalaryPaise,
            item.EmployerPfPaise,
            item.EmployerEsiPaise,
            item.PaymentStatus,
            item.PaymentReference);
    }
}
