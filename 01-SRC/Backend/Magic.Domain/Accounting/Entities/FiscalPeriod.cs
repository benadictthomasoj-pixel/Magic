using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;

namespace Magic.Domain.Accounting.Entities;

/// <summary>
/// Domain entity representing a monthly operational accounting period within a fiscal year.
/// Enforces period governance invariant INV-FIN-05 (Closed Period Posting Rejection).
/// </summary>
public class FiscalPeriod : DomainEntity, IAuditableEntity
{
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid FiscalYearId { get; private set; }
    public int PeriodNumber { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public PeriodState State { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public int ConcurrencyVersion { get; private set; }

    private FiscalPeriod() { } // EF Core

    public FiscalPeriod(
        Guid companyId,
        Guid branchId,
        Guid fiscalYearId,
        int periodNumber,
        string code,
        DateOnly startDate,
        DateOnly endDate,
        Guid createdByUserId)
    {
        if (companyId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_COMPANY", "Company identity is required.");
        if (branchId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_BRANCH", "Branch identity is required.");
        if (fiscalYearId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_FISCAL_YEAR", "Fiscal year identity is required.");
        DomainGuard.NotNullOrWhiteSpace(code, nameof(code));

        if (periodNumber < 1 || periodNumber > 12)
        {
            throw new DomainException(
                "DOMAIN_INVALID_PERIOD_NUMBER",
                $"Period number must be between 1 and 12. Given: {periodNumber}.");
        }

        if (endDate < startDate)
        {
            throw new DomainException(
                "DOMAIN_INVALID_PERIOD_DATES",
                $"Period end date ({endDate}) cannot be before start date ({startDate}).");
        }

        CompanyId = companyId;
        BranchId = branchId;
        FiscalYearId = fiscalYearId;
        PeriodNumber = periodNumber;
        Code = code.Trim();
        StartDate = startDate;
        EndDate = endDate;
        State = PeriodState.Open;

        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        CreatedByUserId = createdByUserId;
        UpdatedAtUtc = now;
        ConcurrencyVersion = 1;
    }

    public void TransitionToClosing(Guid userId)
    {
        if (State != PeriodState.Open)
        {
            throw new DomainException("DOMAIN_INVALID_PERIOD_TRANSITION", $"Period '{Code}' must be Open to transition to Closing. Current state: {State}.");
        }

        State = PeriodState.Closing;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = userId;
    }

    public void Lock(Guid userId)
    {
        if (State == PeriodState.Locked)
        {
            throw new DomainException("DOMAIN_PERIOD_ALREADY_LOCKED", $"Period '{Code}' is already locked.");
        }

        State = PeriodState.Locked;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = userId;
    }

    public void Reopen(Guid supervisorUserId)
    {
        if (supervisorUserId == Guid.Empty)
        {
            throw new DomainException("DOMAIN_SUPERVISOR_REQUIRED", "Reopening a period requires an authorized supervisor user identity.");
        }

        State = PeriodState.Open;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = supervisorUserId;
    }

    /// <summary>
    /// Validates whether a transaction can be posted to this period on the given effective date.
    /// Invariant INV-FIN-05: Closed/Locked periods reject standard postings unconditionally.
    /// </summary>
    public void AssertCanPost(DateOnly effectiveDate, bool hasSupervisorElevation = false)
    {
        if (effectiveDate < StartDate || effectiveDate > EndDate)
        {
            throw new DomainException(
                "DOMAIN_DATE_OUTSIDE_PERIOD",
                $"Effective date {effectiveDate} is outside period '{Code}' date boundaries [{StartDate} to {EndDate}].");
        }

        if (State == PeriodState.Locked && !hasSupervisorElevation)
        {
            throw new DomainException(
                "DOMAIN_POSTING_PERIOD_LOCKED",
                $"Cannot post transaction to period '{Code}' because it is in LOCKED state.");
        }

        if (State == PeriodState.Closing && !hasSupervisorElevation)
        {
            throw new DomainException(
                "DOMAIN_POSTING_PERIOD_CLOSING",
                $"Cannot post transaction to period '{Code}' because it is CLOSING. Supervisor authorization required.");
        }
    }
}

