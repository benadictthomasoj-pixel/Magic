using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Accounting.Events;
using Magic.Domain.Common;
using Magic.Domain.ValueObjects;
using Xunit;

namespace Magic.Domain.UnitTests;

public class JournalVoucherTests
{
    private readonly Guid _companyId = Guid.CreateVersion7();
    private readonly Guid _branchId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _bankAccountId = Guid.CreateVersion7();
    private readonly Guid _revenueAccountId = Guid.CreateVersion7();
    private readonly FiscalPeriod _openPeriod;

    public JournalVoucherTests()
    {
        var start = new DateOnly(2026, 4, 1);
        var end = new DateOnly(2026, 4, 30);
        _openPeriod = new FiscalPeriod(
            _companyId,
            _branchId,
            Guid.CreateVersion7(),
            1,
            "2026-04",
            start,
            end,
            _userId);
    }

    [Fact]
    public void Post_BalancedVoucher_SucceedsAndRaisesDomainEvent()
    {
        var voucherDate = new DateOnly(2026, 4, 15);
        var voucher = new JournalVoucher(
            _companyId,
            _branchId,
            _openPeriod.Id,
            VoucherType.Journal,
            voucherDate,
            _userId,
            narration: "April Bank Deposit");

        voucher.AddLine(_bankAccountId, debit: Money.FromPaise(100_000), credit: Money.Zero, narration: "Bank debit");
        voucher.AddLine(_revenueAccountId, debit: Money.Zero, credit: Money.FromPaise(100_000), narration: "Revenue credit");

        Assert.True(voucher.IsBalanced);
        Assert.Equal(Money.FromPaise(100_000), voucher.TotalDebit);
        Assert.Equal(Money.FromPaise(100_000), voucher.TotalCredit);

        voucher.Post(_openPeriod, "VCH-HO-2026-JV-000001", _userId);

        Assert.Equal(VoucherStatus.Posted, voucher.Status);
        Assert.Equal("VCH-HO-2026-JV-000001", voucher.VoucherNumber);

        var domainEvents = voucher.DomainEvents.ToList();
        Assert.Single(domainEvents);
        var postedEvent = Assert.IsType<VoucherPostedDomainEvent>(domainEvents[0]);
        Assert.Equal(voucher.Id, postedEvent.VoucherId);
        Assert.Equal(Money.FromPaise(100_000), postedEvent.TotalAmount);
    }

    [Fact]
    public void Post_UnbalancedVoucher_ViolatesInvariantINVFIN02()
    {
        var voucher = new JournalVoucher(
            _companyId,
            _branchId,
            _openPeriod.Id,
            VoucherType.Journal,
            new DateOnly(2026, 4, 15),
            _userId);

        // Debits: 100,000 paise, Credits: 95,000 paise (variance = 5,000 paise)
        voucher.AddLine(_bankAccountId, debit: Money.FromPaise(100_000), credit: Money.Zero);
        voucher.AddLine(_revenueAccountId, debit: Money.Zero, credit: Money.FromPaise(95_000));

        var ex = Assert.Throws<DomainException>(() =>
            voucher.Post(_openPeriod, "VCH-HO-2026-JV-000002", _userId));

        Assert.Equal("DOMAIN_DOUBLE_ENTRY_UNBALANCED", ex.ErrorCode);
        Assert.Contains("5000 paise", ex.Message);
    }

    [Fact]
    public void Post_FewerThanTwoLines_ThrowsDomainException()
    {
        var voucher = new JournalVoucher(
            _companyId,
            _branchId,
            _openPeriod.Id,
            VoucherType.Journal,
            new DateOnly(2026, 4, 15),
            _userId);

        voucher.AddLine(_bankAccountId, debit: Money.FromPaise(50_000), credit: Money.Zero);

        var ex = Assert.Throws<DomainException>(() =>
            voucher.Post(_openPeriod, "VCH-HO-2026-JV-000003", _userId));

        Assert.Equal("DOMAIN_DOUBLE_ENTRY_MINIMUM_LINES", ex.ErrorCode);
    }

    [Fact]
    public void JournalLine_BothDebitAndCredit_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            new JournalLine(
                Guid.CreateVersion7(),
                _bankAccountId,
                debit: Money.FromPaise(1000),
                credit: Money.FromPaise(1000),
                lineNumber: 1));

        Assert.Equal("DOMAIN_INVALID_JOURNAL_LINE_AMOUNTS", ex.ErrorCode);
    }

    [Fact]
    public void AddLine_AfterPosting_ThrowsDomainException()
    {
        var voucher = new JournalVoucher(
            _companyId,
            _branchId,
            _openPeriod.Id,
            VoucherType.Journal,
            new DateOnly(2026, 4, 15),
            _userId);

        voucher.AddLine(_bankAccountId, debit: Money.FromPaise(10_000), credit: Money.Zero);
        voucher.AddLine(_revenueAccountId, debit: Money.Zero, credit: Money.FromPaise(10_000));
        voucher.Post(_openPeriod, "VCH-HO-2026-JV-000004", _userId);

        var ex = Assert.Throws<DomainException>(() =>
            voucher.AddLine(_bankAccountId, debit: Money.FromPaise(5_000), credit: Money.Zero));

        Assert.Equal("DOMAIN_POSTED_VOUCHER_IMMUTABLE", ex.ErrorCode);
    }

    [Fact]
    public void CreateReversal_MirrorsAllLegsAndCreatesBalancedCompanionVoucher()
    {
        var voucher = new JournalVoucher(
            _companyId,
            _branchId,
            _openPeriod.Id,
            VoucherType.Journal,
            new DateOnly(2026, 4, 15),
            _userId,
            narration: "Original Transaction");

        voucher.AddLine(_bankAccountId, debit: Money.FromPaise(75_000), credit: Money.Zero, narration: "Bank leg");
        voucher.AddLine(_revenueAccountId, debit: Money.Zero, credit: Money.FromPaise(75_000), narration: "Revenue leg");
        voucher.Post(_openPeriod, "VCH-HO-2026-JV-000005", _userId);

        var reversalDate = new DateOnly(2026, 4, 18);
        var supervisorId = Guid.CreateVersion7();

        var reversal = voucher.CreateReversal(
            "VCH-HO-2026-REV-000001",
            reversalDate,
            _openPeriod,
            supervisorId,
            reason: "Incorrect revenue classification");

        // Assert original voucher status
        Assert.Equal(VoucherStatus.Reversed, voucher.Status);
        Assert.True(voucher.IsReversed);
        Assert.Equal(reversal.Id, voucher.ReversalVoucherId);

        // Assert reversal voucher status & invariants
        Assert.Equal(VoucherType.Reversal, reversal.VoucherType);
        Assert.Equal(VoucherStatus.Posted, reversal.Status);
        Assert.Equal("VCH-HO-2026-REV-000001", reversal.VoucherNumber);
        Assert.True(reversal.IsBalanced);
        Assert.Equal(Money.FromPaise(75_000), reversal.TotalDebit);
        Assert.Equal(Money.FromPaise(75_000), reversal.TotalCredit);

        // Assert inverted legs: original line 1 had Debit=75,000; reversal line 1 must have Credit=75,000
        var revLine1 = reversal.Lines[0];
        Assert.Equal(_bankAccountId, revLine1.AccountId);
        Assert.Equal(Money.Zero, revLine1.Debit);
        Assert.Equal(Money.FromPaise(75_000), revLine1.Credit);

        // Original line 2 had Credit=75,000; reversal line 2 must have Debit=75,000
        var revLine2 = reversal.Lines[1];
        Assert.Equal(_revenueAccountId, revLine2.AccountId);
        Assert.Equal(Money.FromPaise(75_000), revLine2.Debit);
        Assert.Equal(Money.Zero, revLine2.Credit);

        // Double reversal prevention
        var ex = Assert.Throws<DomainException>(() =>
            voucher.CreateReversal(
                "VCH-HO-2026-REV-000002",
                reversalDate,
                _openPeriod,
                supervisorId,
                "Attempt duplicate reversal"));
        Assert.Equal("DOMAIN_VOUCHER_ALREADY_REVERSED", ex.ErrorCode);
    }
}
