using Magic.Domain.Common;

namespace Magic.Domain.Accounting.Entities;

/// <summary>
/// Domain entity representing a 12-month accounting fiscal year cycle (e.g. 1 April to 31 March).
/// </summary>
public class FiscalYear : DomainEntity, IAuditableEntity
{
    private readonly List<FiscalPeriod> _periods = [];

    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsClosed { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public int ConcurrencyVersion { get; private set; }

    public IReadOnlyCollection<FiscalPeriod> Periods => _periods.AsReadOnly();

    private FiscalYear() { } // EF Core

    public FiscalYear(
        Guid companyId,
        Guid branchId,
        string code,
        DateOnly startDate,
        DateOnly endDate,
        Guid createdByUserId)
    {
        if (companyId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_COMPANY", "Company identity is required.");
        if (branchId == Guid.Empty) throw new DomainException("DOMAIN_EMPTY_BRANCH", "Branch identity is required.");
        DomainGuard.NotNullOrWhiteSpace(code, nameof(code));

        if (endDate <= startDate)
        {
            throw new DomainException(
                "DOMAIN_INVALID_FISCAL_DATES",
                $"Fiscal year end date ({endDate}) must be strictly after start date ({startDate}).");
        }

        CompanyId = companyId;
        BranchId = branchId;
        Code = code.Trim();
        StartDate = startDate;
        EndDate = endDate;
        IsClosed = false;

        var now = DateTimeOffset.UtcNow;
        CreatedAtUtc = now;
        CreatedByUserId = createdByUserId;
        UpdatedAtUtc = now;
        ConcurrencyVersion = 1;
    }

    public void AddPeriod(FiscalPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);

        if (period.StartDate < StartDate || period.EndDate > EndDate)
        {
            throw new DomainException(
                "DOMAIN_PERIOD_OUTSIDE_FISCAL_YEAR",
                $"Period [{period.StartDate} to {period.EndDate}] falls outside Fiscal Year [{StartDate} to {EndDate}].");
        }

        _periods.Add(period);
    }

    public void Close(Guid userId, DateTimeOffset closedAtUtc)
    {
        if (IsClosed)
        {
            throw new DomainException("DOMAIN_FISCAL_YEAR_ALREADY_CLOSED", $"Fiscal year '{Code}' is already closed.");
        }

        IsClosed = true;
        ClosedAtUtc = closedAtUtc;
        UpdatedAtUtc = closedAtUtc;
        UpdatedByUserId = userId;
    }

    public void Reopen(Guid userId)
    {
        if (!IsClosed)
        {
            throw new DomainException("DOMAIN_FISCAL_YEAR_NOT_CLOSED", $"Fiscal year '{Code}' is not closed.");
        }

        IsClosed = false;
        ClosedAtUtc = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByUserId = userId;
    }

    public void AssertDateInFiscalYear(DateOnly date)
    {
        if (date < StartDate || date > EndDate)
        {
            throw new DomainException(
                "DOMAIN_DATE_OUTSIDE_FISCAL_YEAR",
                $"Date {date} is outside fiscal year '{Code}' ({StartDate} to {EndDate}).");
        }
    }
}

