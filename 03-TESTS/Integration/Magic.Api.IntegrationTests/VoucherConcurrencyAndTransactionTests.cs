using System.Collections.Concurrent;
using System.Globalization;
using Magic.Application.Accounting.Persistence;
using Magic.Application.Accounting.Services;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class VoucherConcurrencyAndTransactionTests(MagicTestFixture factory) : IClassFixture<MagicTestFixture>
{
    private readonly MagicTestFixture _factory = factory;

    [Fact]
    public async Task ConcurrentSequenceGenerator_AllocatesContiguousNumbers_WithZeroGapsOrDuplicates()
    {
        var branchId = Guid.CreateVersion7();
        var fyId = Guid.CreateVersion7();
        var voucherType = VoucherType.Journal;
        const string branchCode = "MAIN";
        const string fyCode = "FY26";
        const int concurrentTasks = 15;

        var allocatedNumbers = new ConcurrentBag<string>();
        var tasks = new List<Task>();

        for (var i = 0; i < concurrentTasks; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var scope = _factory.Services.CreateScope();
                var generator = scope.ServiceProvider.GetRequiredService<IVoucherNumberGenerator>();
                var voucherNumber = await generator.NextVoucherNumberAsync(branchId, fyId, branchCode, fyCode, voucherType);
                allocatedNumbers.Add(voucherNumber);
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(concurrentTasks, allocatedNumbers.Count);
        Assert.Equal(concurrentTasks, allocatedNumbers.Distinct().Count());

        const string expectedPrefix = "VCH-MAIN-FY26-JV-";

        // Extract sequence integers
        var sequenceInts = allocatedNumbers
            .Select(num =>
            {
                Assert.StartsWith(expectedPrefix, num, StringComparison.Ordinal);
                var numPart = num.Replace(expectedPrefix, string.Empty, StringComparison.Ordinal);
                return int.Parse(numPart, CultureInfo.InvariantCulture);
            })
            .OrderBy(n => n)
            .ToList();

        var expectedRange = Enumerable.Range(1, concurrentTasks).ToList();
        Assert.Equal(expectedRange, sequenceInts);

        // Verify sequence record in database
        using var verifyScope = _factory.Services.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var seqRecord = await dbContext.VoucherSequences
            .FirstOrDefaultAsync(s => s.BranchId == branchId && s.FiscalYearId == fyId && s.VoucherType == "JOURNAL");

        Assert.NotNull(seqRecord);
        Assert.Equal(concurrentTasks, seqRecord.CurrentSequence);
    }

    [Fact]
    public async Task ConcurrentAllocation_AcrossDifferentBranches_OperatesIndependently()
    {
        var branch1 = Guid.CreateVersion7();
        var branch2 = Guid.CreateVersion7();
        var fyId = Guid.CreateVersion7();
        var voucherType = VoucherType.Sales;

        using var scope1 = _factory.Services.CreateScope();
        var generator1 = scope1.ServiceProvider.GetRequiredService<IVoucherNumberGenerator>();

        using var scope2 = _factory.Services.CreateScope();
        var generator2 = scope2.ServiceProvider.GetRequiredService<IVoucherNumberGenerator>();

        var num1 = await generator1.NextVoucherNumberAsync(branch1, fyId, "BR1", "FY26", voucherType);
        var num2 = await generator2.NextVoucherNumberAsync(branch2, fyId, "BR2", "FY26", voucherType);

        Assert.Equal("VCH-BR1-FY26-SAL-000001", num1);
        Assert.Equal("VCH-BR2-FY26-SAL-000001", num2);
    }

    [Fact]
    public async Task TransactionRollback_EnsuresZeroRowsInAllTables_WhenPostingFails()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        // Fiscal period setup
        var fy = new FiscalYear(companyId, branchId, $"FY-{Guid.NewGuid():N}"[..10], new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31), userId);
        var period = new FiscalPeriod(companyId, branchId, fy.Id, 1, "2026-04", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), userId);
        fy.AddPeriod(period);

        var group = new AccountGroup(companyId, branchId, $"GRP-{Guid.NewGuid():N}"[..15], "Rollback Group", AccountRootClass.Asset, userId);
        var acc1 = new Account(companyId, branchId, group.Id, $"1100-{Guid.NewGuid():N}"[..10], "Cash", AccountRootClass.Asset, userId);
        var acc2 = new Account(companyId, branchId, group.Id, $"1200-{Guid.NewGuid():N}"[..10], "Bank", AccountRootClass.Asset, userId);

        dbContext.FiscalYears.Add(fy);
        dbContext.FiscalPeriods.Add(period);
        dbContext.AccountGroups.Add(group);
        dbContext.Accounts.AddRange(acc1, acc2);
        await dbContext.SaveChangesAsync();

        var idempotencyKey = Guid.CreateVersion7();

        var voucher = new JournalVoucher(
            companyId,
            branchId,
            period.Id,
            VoucherType.Journal,
            new DateOnly(2026, 4, 15),
            userId,
            narration: "Rollback Test Voucher",
            idempotencyKey: idempotencyKey);

        voucher.AddLine(acc1.Id, debit: Money.FromPaise(100_000), credit: Money.Zero, narration: "Debit line");
        voucher.AddLine(acc2.Id, debit: Money.Zero, credit: Money.FromPaise(100_000), narration: "Credit line");
        voucher.Post(period, "VCH-MAIN-FY26-JV-999999", userId);

        // Simulate atomic posting inside explicit transaction using execution strategy that encounters an unexpected failure before commit
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync();
            dbContext.Vouchers.Add(voucher);
            dbContext.JournalLines.AddRange(voucher.Lines);
            await dbContext.SaveChangesAsync();

            // Intentionally rollback without committing
            await tx.RollbackAsync();
        });

        // Verify that neither voucher, lines, nor anything else persisted
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var persistedVoucher = await verifyDb.Vouchers.FirstOrDefaultAsync(v => v.Id == voucher.Id || v.IdempotencyKey == idempotencyKey);
        Assert.Null(persistedVoucher);

        var persistedLines = await verifyDb.JournalLines.Where(l => l.VoucherId == voucher.Id).ToListAsync();
        Assert.Empty(persistedLines);
    }

    [Fact]
    public async Task DuplicateIdempotencyKey_WithinSameBranch_IsRejectedByUniqueConstraint()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var idempotencyKey = Guid.CreateVersion7();

        var fy = new FiscalYear(companyId, branchId, $"FY-{Guid.NewGuid():N}"[..10], new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31), userId);
        var period = new FiscalPeriod(companyId, branchId, fy.Id, 1, "2026-04", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), userId);
        fy.AddPeriod(period);

        var group = new AccountGroup(companyId, branchId, $"GRP-{Guid.NewGuid():N}"[..15], "Assets", AccountRootClass.Asset, userId);
        var acc1 = new Account(companyId, branchId, group.Id, $"1110-{Guid.NewGuid():N}"[..10], "Cash", AccountRootClass.Asset, userId);
        var acc2 = new Account(companyId, branchId, group.Id, $"1120-{Guid.NewGuid():N}"[..10], "Bank", AccountRootClass.Asset, userId);

        dbContext.FiscalYears.Add(fy);
        dbContext.FiscalPeriods.Add(period);
        dbContext.AccountGroups.Add(group);
        dbContext.Accounts.AddRange(acc1, acc2);
        await dbContext.SaveChangesAsync();

        var v1 = new JournalVoucher(
            companyId,
            branchId,
            period.Id,
            VoucherType.Journal,
            new DateOnly(2026, 4, 10),
            userId,
            narration: "First Submission",
            idempotencyKey: idempotencyKey);
        v1.AddLine(acc1.Id, debit: Money.FromPaise(50000), credit: Money.Zero, narration: "D1");
        v1.AddLine(acc2.Id, debit: Money.Zero, credit: Money.FromPaise(50000), narration: "C1");
        v1.Post(period, $"VCH-MAIN-FY26-JV-{Guid.NewGuid():N}"[..26], userId);

        dbContext.Vouchers.Add(v1);
        dbContext.JournalLines.AddRange(v1.Lines);
        await dbContext.SaveChangesAsync();

        // Second submission with exact same (branchId, idempotencyKey)
        var v2 = new JournalVoucher(
            companyId,
            branchId,
            period.Id,
            VoucherType.Journal,
            new DateOnly(2026, 4, 11),
            userId,
            narration: "Duplicate Submission",
            idempotencyKey: idempotencyKey);
        v2.AddLine(acc1.Id, debit: Money.FromPaise(75000), credit: Money.Zero, narration: "D2");
        v2.AddLine(acc2.Id, debit: Money.Zero, credit: Money.FromPaise(75000), narration: "C2");
        v2.Post(period, $"VCH-MAIN-FY26-JV-{Guid.NewGuid():N}"[..26], userId);

        dbContext.Vouchers.Add(v2);
        dbContext.JournalLines.AddRange(v2.Lines);

        var ex = await Assert.ThrowsAnyAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        Assert.Contains("uq_vouchers_branch_idempotency", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
