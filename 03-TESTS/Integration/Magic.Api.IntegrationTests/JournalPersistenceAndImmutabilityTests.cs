using Magic.Application.Accounting.Persistence;
using Magic.Application.Accounting.Services;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.ValueObjects;
using Magic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class JournalPersistenceAndImmutabilityTests(MagicTestFixture factory) : IClassFixture<MagicTestFixture>
{
    private readonly MagicTestFixture _factory = factory;

    [Fact]
    public async Task BalancedJournal_PersistsWithExactIntegerPaise_AndEnforcesAuditOutboxAtomicity()
    {
        using var scope = _factory.Services.CreateScope();
        var postingService = scope.ServiceProvider.GetRequiredService<IPostingService>();
        var glRepo = scope.ServiceProvider.GetRequiredService<IGeneralLedgerRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        // Setup Chart of Accounts & Fiscal Governance
        var fy = new FiscalYear(companyId, branchId, $"FY-{Guid.NewGuid():N}"[..10], new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31), userId);
        var period = new FiscalPeriod(companyId, branchId, fy.Id, 1, "2026-04", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), userId);
        fy.AddPeriod(period);

        var group = new AccountGroup(companyId, branchId, $"GRP-{Guid.NewGuid():N}"[..15], "Assets", AccountRootClass.Asset, userId);
        var bankAcc = new Account(companyId, branchId, group.Id, $"1120-{Guid.NewGuid():N}"[..10], "Bank", AccountRootClass.Asset, userId);
        var revGroup = new AccountGroup(companyId, branchId, $"REV-{Guid.NewGuid():N}"[..15], "Revenues", AccountRootClass.Revenue, userId);
        var revAcc = new Account(companyId, branchId, revGroup.Id, $"4110-{Guid.NewGuid():N}"[..10], "Sales", AccountRootClass.Revenue, userId);

        dbContext.FiscalYears.Add(fy);
        dbContext.FiscalPeriods.Add(period);
        dbContext.AccountGroups.AddRange(group, revGroup);
        dbContext.Accounts.AddRange(bankAcc, revAcc);
        await dbContext.SaveChangesAsync();

        // Create Voucher
        var voucherDate = new DateOnly(2026, 4, 15);
        var voucher = new JournalVoucher(
            companyId,
            branchId,
            period.Id,
            VoucherType.Journal,
            voucherDate,
            userId,
            narration: "Cash receipt from customer",
            idempotencyKey: Guid.CreateVersion7());

        voucher.AddLine(bankAcc.Id, debit: Money.FromPaise(125_000), credit: Money.Zero, narration: "Debit Bank Account");
        voucher.AddLine(revAcc.Id, debit: Money.Zero, credit: Money.FromPaise(125_000), narration: "Credit Revenue Account");

        // Execute Posting
        var posted = await postingService.PostVoucherAsync(voucher, "MAIN", "FY26", userId);

        Assert.Equal(VoucherStatus.Posted, posted.Status);
        Assert.StartsWith("VCH-MAIN-FY26-JV-", posted.VoucherNumber, StringComparison.Ordinal);
        Assert.Equal(125_000L, posted.TotalDebitPaise);
        Assert.Equal(125_000L, posted.TotalCreditPaise);

        // Verify from database directly
        var fromDb = await glRepo.GetVoucherByIdAsync(posted.Id);
        Assert.NotNull(fromDb);
        Assert.Equal(posted.VoucherNumber, fromDb.VoucherNumber);
        Assert.Equal(2, fromDb.Lines.Count);
        Assert.Equal(125_000L, fromDb.TotalDebitPaise);
        Assert.Equal(125_000L, fromDb.TotalCreditPaise);

        // Verify Outbox Event created atomically
        var outbox = await dbContext.OutboxMessages.FirstOrDefaultAsync(m => m.AggregateId == posted.Id && m.EventType == "VOUCHER_POSTED");
        Assert.NotNull(outbox);
        Assert.Equal(posted.TraceCorrelationId, outbox.TraceCorrelationId);

        // Verify System Audit Log created atomically
        var audit = await dbContext.SystemAuditLogs.FirstOrDefaultAsync(a => a.EntityId == posted.Id && a.ActionName == "POST_JOURNAL_VOUCHER");
        Assert.NotNull(audit);
        Assert.Equal(userId, audit.UserId);
    }

    [Fact]
    public async Task DatabaseTrigger_RejectsUnbalancedPostedVoucher_AtDatabaseTransactionBoundary()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        var fy = new FiscalYear(companyId, branchId, $"FY-{Guid.NewGuid():N}"[..10], new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31), userId);
        var period = new FiscalPeriod(companyId, branchId, fy.Id, 1, "2026-04", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), userId);
        var group = new AccountGroup(companyId, branchId, $"GRP-{Guid.NewGuid():N}"[..15], "Assets", AccountRootClass.Asset, userId);
        var acc1 = new Account(companyId, branchId, group.Id, $"1111-{Guid.NewGuid():N}"[..10], "Acc1", AccountRootClass.Asset, userId);
        var acc2 = new Account(companyId, branchId, group.Id, $"1112-{Guid.NewGuid():N}"[..10], "Acc2", AccountRootClass.Asset, userId);

        dbContext.FiscalYears.Add(fy);
        dbContext.FiscalPeriods.Add(period);
        dbContext.AccountGroups.Add(group);
        dbContext.Accounts.AddRange(acc1, acc2);
        await dbContext.SaveChangesAsync();

        var voucherId = Guid.CreateVersion7();
        var line1Id = Guid.CreateVersion7();
        var line2Id = Guid.CreateVersion7();

        // Attempt direct SQL insertion of an unbalanced voucher marked POSTED
        // Line 1: Debit = 10,000 paise
        // Line 2: Credit = 9,999 paise (Unbalanced by 1 paise!)
        const string insertSql = """
            BEGIN;
            INSERT INTO accounting.vouchers
                (id, company_id, branch_id, fiscal_period_id, voucher_number, posting_date, voucher_type, status, total_debit_paise, total_credit_paise, idempotency_key, narration, posted_at_utc, posted_by_user_id, trace_correlation_id, created_at_utc, created_by_user_id, updated_at_utc, concurrency_version)
            VALUES
                (@vId, @cId, @bId, @pId, 'VCH-UNBALANCED-TEST', '2026-04-10', 'Journal', 'Posted', 10000, 10000, @idemp, 'Unbalanced Test', NOW(), @uId, @corr, NOW(), @uId, NOW(), 1);

            INSERT INTO accounting.journal_lines
                (id, voucher_id, account_id, branch_id, line_number, debit_paise, credit_paise, description)
            VALUES
                (@l1, @vId, @acc1, @bId, 1, 10000, 0, 'Line 1'),
                (@l2, @vId, @acc2, @bId, 2, 0, 9999, 'Line 2');

            COMMIT;
            """;

        var conn = dbContext.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = insertSql;
        cmd.Parameters.Add(new NpgsqlParameter("vId", voucherId));
        cmd.Parameters.Add(new NpgsqlParameter("cId", companyId));
        cmd.Parameters.Add(new NpgsqlParameter("bId", branchId));
        cmd.Parameters.Add(new NpgsqlParameter("pId", period.Id));
        cmd.Parameters.Add(new NpgsqlParameter("idemp", Guid.CreateVersion7()));
        cmd.Parameters.Add(new NpgsqlParameter("uId", userId));
        cmd.Parameters.Add(new NpgsqlParameter("corr", Guid.CreateVersion7()));
        cmd.Parameters.Add(new NpgsqlParameter("l1", line1Id));
        cmd.Parameters.Add(new NpgsqlParameter("l2", line2Id));
        cmd.Parameters.Add(new NpgsqlParameter("acc1", acc1.Id));
        cmd.Parameters.Add(new NpgsqlParameter("acc2", acc2.Id));

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
        Assert.Contains("DATABASE_DOUBLE_ENTRY_VIOLATION", ex.Message);
    }

    [Fact]
    public async Task DatabaseTrigger_RejectsDirectUpdateOrDelete_OnPostedJournalLines()
    {
        using var scope = _factory.Services.CreateScope();
        var postingService = scope.ServiceProvider.GetRequiredService<IPostingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        var fy = new FiscalYear(companyId, branchId, $"FY-{Guid.NewGuid():N}"[..10], new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31), userId);
        var period = new FiscalPeriod(companyId, branchId, fy.Id, 1, "2026-04", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), userId);
        var group = new AccountGroup(companyId, branchId, $"GRP-{Guid.NewGuid():N}"[..15], "Assets", AccountRootClass.Asset, userId);
        var acc1 = new Account(companyId, branchId, group.Id, $"1131-{Guid.NewGuid():N}"[..10], "AR", AccountRootClass.Asset, userId);
        var acc2 = new Account(companyId, branchId, group.Id, $"1132-{Guid.NewGuid():N}"[..10], "AP", AccountRootClass.Asset, userId);

        dbContext.FiscalYears.Add(fy);
        dbContext.FiscalPeriods.Add(period);
        dbContext.AccountGroups.Add(group);
        dbContext.Accounts.AddRange(acc1, acc2);
        await dbContext.SaveChangesAsync();

        var voucher = new JournalVoucher(companyId, branchId, period.Id, VoucherType.Journal, new DateOnly(2026, 4, 10), userId, narration: "Protect Test");
        voucher.AddLine(acc1.Id, Money.FromPaise(50_000), Money.Zero, "Line 1");
        voucher.AddLine(acc2.Id, Money.Zero, Money.FromPaise(50_000), "Line 2");

        var posted = await postingService.PostVoucherAsync(voucher, "HO", "FY26", userId);
        var targetLineId = posted.Lines[0].Id;

        // Attempt direct UPDATE of debit on posted journal line via raw SQL
        var conn = dbContext.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using (var updateCmd = conn.CreateCommand())
        {
            updateCmd.CommandText = "UPDATE accounting.journal_lines SET debit_paise = 60000 WHERE id = @lineId;";
            updateCmd.Parameters.Add(new NpgsqlParameter("lineId", targetLineId));

            var exUpdate = await Assert.ThrowsAsync<PostgresException>(() => updateCmd.ExecuteNonQueryAsync());
            Assert.Equal("55000", exUpdate.SqlState);
            Assert.Contains("DATABASE_FINANCIAL_INVARIANT_VIOLATION", exUpdate.Message);
        }

        // Attempt direct DELETE of a posted journal line via raw SQL
        await using (var deleteCmd = conn.CreateCommand())
        {
            deleteCmd.CommandText = "DELETE FROM accounting.journal_lines WHERE id = @lineId;";
            deleteCmd.Parameters.Add(new NpgsqlParameter("lineId", targetLineId));

            var exDelete = await Assert.ThrowsAsync<PostgresException>(() => deleteCmd.ExecuteNonQueryAsync());
            Assert.Equal("55000", exDelete.SqlState);
            Assert.Contains("DATABASE_FINANCIAL_INVARIANT_VIOLATION", exDelete.Message);
        }
    }

    [Fact]
    public async Task DatabaseTrigger_RejectsDirectDelete_OnPostedVoucherHeader()
    {
        using var scope = _factory.Services.CreateScope();
        var postingService = scope.ServiceProvider.GetRequiredService<IPostingService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        var fy = new FiscalYear(companyId, branchId, $"FY-{Guid.NewGuid():N}"[..10], new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31), userId);
        var period = new FiscalPeriod(companyId, branchId, fy.Id, 1, "2026-04", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), userId);
        var group = new AccountGroup(companyId, branchId, $"GRP-{Guid.NewGuid():N}"[..15], "Assets", AccountRootClass.Asset, userId);
        var acc1 = new Account(companyId, branchId, group.Id, $"1141-{Guid.NewGuid():N}"[..10], "Cash", AccountRootClass.Asset, userId);
        var acc2 = new Account(companyId, branchId, group.Id, $"1142-{Guid.NewGuid():N}"[..10], "Income", AccountRootClass.Asset, userId);

        dbContext.FiscalYears.Add(fy);
        dbContext.FiscalPeriods.Add(period);
        dbContext.AccountGroups.Add(group);
        dbContext.Accounts.AddRange(acc1, acc2);
        await dbContext.SaveChangesAsync();

        var voucher = new JournalVoucher(companyId, branchId, period.Id, VoucherType.Journal, new DateOnly(2026, 4, 12), userId, narration: "Delete Test");
        voucher.AddLine(acc1.Id, Money.FromPaise(30_000), Money.Zero, "L1");
        voucher.AddLine(acc2.Id, Money.Zero, Money.FromPaise(30_000), "L2");

        var posted = await postingService.PostVoucherAsync(voucher, "HO", "FY26", userId);

        // Attempt direct DELETE of the posted voucher header
        var conn = dbContext.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM accounting.vouchers WHERE id = @id;";
        cmd.Parameters.Add(new NpgsqlParameter("id", posted.Id));

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("55000", ex.SqlState);
        Assert.Contains("DATABASE_FINANCIAL_INVARIANT_VIOLATION", ex.Message);
    }

    [Fact]
    public async Task NonDestructiveReversal_PostsCompanionVoucher_AndPreventsDuplicateReversal()
    {
        using var scope = _factory.Services.CreateScope();
        var postingService = scope.ServiceProvider.GetRequiredService<IPostingService>();
        var glRepo = scope.ServiceProvider.GetRequiredService<IGeneralLedgerRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var supervisorId = Guid.CreateVersion7();

        var fy = new FiscalYear(companyId, branchId, $"FY-{Guid.NewGuid():N}"[..10], new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31), userId);
        var period = new FiscalPeriod(companyId, branchId, fy.Id, 1, "2026-04", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), userId);
        var group = new AccountGroup(companyId, branchId, $"GRP-{Guid.NewGuid():N}"[..15], "Assets", AccountRootClass.Asset, userId);
        var acc1 = new Account(companyId, branchId, group.Id, $"1151-{Guid.NewGuid():N}"[..10], "A1", AccountRootClass.Asset, userId);
        var acc2 = new Account(companyId, branchId, group.Id, $"1152-{Guid.NewGuid():N}"[..10], "A2", AccountRootClass.Asset, userId);

        dbContext.FiscalYears.Add(fy);
        dbContext.FiscalPeriods.Add(period);
        dbContext.AccountGroups.Add(group);
        dbContext.Accounts.AddRange(acc1, acc2);
        await dbContext.SaveChangesAsync();

        var voucher = new JournalVoucher(companyId, branchId, period.Id, VoucherType.Journal, new DateOnly(2026, 4, 14), userId, narration: "Original To Reverse");
        voucher.AddLine(acc1.Id, Money.FromPaise(40_000), Money.Zero, "Orig Leg 1");
        voucher.AddLine(acc2.Id, Money.Zero, Money.FromPaise(40_000), "Orig Leg 2");

        var posted = await postingService.PostVoucherAsync(voucher, "HO", "FY26", userId);

        // Perform Reversal
        var reversal = await postingService.ReverseVoucherAsync(
            posted.Id,
            new DateOnly(2026, 4, 15),
            "HO",
            "FY26",
            supervisorId,
            "Posting error in original voucher");

        Assert.NotNull(reversal);
        Assert.Equal(VoucherType.Reversal, reversal.VoucherType);
        Assert.Equal(VoucherStatus.Posted, reversal.Status);
        Assert.StartsWith("VCH-HO-FY26-REV-", reversal.VoucherNumber, StringComparison.Ordinal);

        // Verify original voucher state in DB
        var updatedOriginal = await glRepo.GetVoucherByIdAsync(posted.Id);
        Assert.NotNull(updatedOriginal);
        Assert.True(updatedOriginal.IsReversed);
        Assert.Equal(reversal.Id, updatedOriginal.ReversalVoucherId);

        // Verify reversal lines are inverted: Line 1 of original was Debit=40,000; in reversal it must be Credit=40,000
        Assert.Equal(Money.Zero, reversal.Lines[0].Debit);
        Assert.Equal(Money.FromPaise(40_000), reversal.Lines[0].Credit);

        // Attempting duplicate reversal must fail
        var ex = await Assert.ThrowsAsync<Magic.Domain.Common.DomainException>(() =>
            postingService.ReverseVoucherAsync(
                posted.Id,
                new DateOnly(2026, 4, 15),
                "HO",
                "FY26",
                supervisorId,
                "Attempt second reversal"));

        Assert.Equal("DOMAIN_VOUCHER_ALREADY_REVERSED", ex.ErrorCode);
    }
}
