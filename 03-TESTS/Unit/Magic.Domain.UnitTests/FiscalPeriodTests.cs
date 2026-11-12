using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Xunit;

namespace Magic.Domain.UnitTests;

public class FiscalPeriodTests
{
    private readonly Guid _companyId = Guid.CreateVersion7();
    private readonly Guid _branchId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();

    [Fact]
    public void FiscalYear_EndBeforeStart_ThrowsDomainException()
    {
        var start = new DateOnly(2026, 4, 1);
        var invalidEnd = new DateOnly(2026, 3, 31);

        var ex = Assert.Throws<DomainException>(() => new FiscalYear(
            _companyId,
            _branchId,
            "FY2026-27",
            start,
            invalidEnd,
            _userId));

        Assert.Equal("DOMAIN_INVALID_FISCAL_DATES", ex.ErrorCode);
    }

    [Fact]
    public void FiscalPeriod_PostingToOpenPeriodWithinDates_Succeeds()
    {
        var start = new DateOnly(2026, 4, 1);
        var end = new DateOnly(2026, 4, 30);
        var period = new FiscalPeriod(
            _companyId,
            _branchId,
            Guid.CreateVersion7(),
            1,
            "2026-04",
            start,
            end,
            _userId);

        // Date in period should succeed
        var effectiveDate = new DateOnly(2026, 4, 15);
        period.AssertCanPost(effectiveDate);
        Assert.Equal(PeriodState.Open, period.State);
    }

    [Fact]
    public void FiscalPeriod_PostingOutsidePeriodDates_ThrowsDomainException()
    {
        var start = new DateOnly(2026, 4, 1);
        var end = new DateOnly(2026, 4, 30);
        var period = new FiscalPeriod(
            _companyId,
            _branchId,
            Guid.CreateVersion7(),
            1,
            "2026-04",
            start,
            end,
            _userId);

        var outsideDate = new DateOnly(2026, 5, 1);
        var ex = Assert.Throws<DomainException>(() => period.AssertCanPost(outsideDate));
        Assert.Equal("DOMAIN_DATE_OUTSIDE_PERIOD", ex.ErrorCode);
    }

    [Fact]
    public void FiscalPeriod_PostingToLockedPeriod_RejectsStandardPosting()
    {
        var start = new DateOnly(2026, 4, 1);
        var end = new DateOnly(2026, 4, 30);
        var period = new FiscalPeriod(
            _companyId,
            _branchId,
            Guid.CreateVersion7(),
            1,
            "2026-04",
            start,
            end,
            _userId);

        period.Lock(_userId);
        Assert.Equal(PeriodState.Locked, period.State);

        var effectiveDate = new DateOnly(2026, 4, 15);

        // Standard clerk posting rejected
        var ex = Assert.Throws<DomainException>(() => period.AssertCanPost(effectiveDate, hasSupervisorElevation: false));
        Assert.Equal("DOMAIN_POSTING_PERIOD_LOCKED", ex.ErrorCode);

        // Supervisor elevation succeeds
        period.AssertCanPost(effectiveDate, hasSupervisorElevation: true);
    }
}

