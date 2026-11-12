using Magic.Application.Accounting.Persistence;
using Magic.Domain.Accounting.Entities;
using Magic.Domain.Accounting.Enums;
using Magic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class ChartOfAccountsPersistenceTests(MagicTestFixture factory) : IClassFixture<MagicTestFixture>
{
    private readonly MagicTestFixture _factory = factory;

    [Fact]
    public async Task AccountGroup_And_DetailAccount_PersistAndLoadWithHierarchy()
    {
        using var scope = _factory.Services.CreateScope();
        var coaRepo = scope.ServiceProvider.GetRequiredService<IChartOfAccountsRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        // 1. Create parent group (Assets)
        var parentGroup = new AccountGroup(
            companyId,
            branchId,
            $"CURR-ASSETS-{Guid.NewGuid():N}"[..20],
            "Current Assets",
            AccountRootClass.Asset,
            userId,
            sortOrder: 100);

        await coaRepo.AddGroupAsync(parentGroup);
        await dbContext.SaveChangesAsync();

        // 2. Create child group (Bank Accounts)
        var childGroup = new AccountGroup(
            companyId,
            branchId,
            $"BANK-GRP-{Guid.NewGuid():N}"[..20],
            "Bank Accounts",
            AccountRootClass.Asset,
            userId,
            parentGroupId: parentGroup.Id,
            sortOrder: 110);
        childGroup.SetParentGroup(parentGroup, userId);

        await coaRepo.AddGroupAsync(childGroup);
        await dbContext.SaveChangesAsync();

        // 3. Create detail account (Operating Bank Account)
        var accountCode = $"1121-{Guid.NewGuid():N}"[..12];
        var account = new Account(
            companyId,
            branchId,
            childGroup.Id,
            accountCode,
            "Primary Operating Current A/c",
            AccountRootClass.Asset,
            userId,
            isControlAccount: true,
            controlAccountType: "BANK",
            allowDirectPosting: true);

        await coaRepo.AddAccountAsync(account);
        await dbContext.SaveChangesAsync();

        // 4. Verification from database
        var loadedAccount = await coaRepo.GetAccountByCodeAsync(companyId, accountCode);
        Assert.NotNull(loadedAccount);
        Assert.Equal(account.Id, loadedAccount.Id);
        Assert.Equal("Primary Operating Current A/c", loadedAccount.Name);
        Assert.Equal(AccountRootClass.Asset, loadedAccount.RootClass);
        Assert.Equal(AccountNormalBalance.Debit, loadedAccount.NormalBalance);
        Assert.True(loadedAccount.IsControlAccount);
        Assert.Equal("BANK", loadedAccount.ControlAccountType);
        Assert.NotNull(loadedAccount.AccountGroup);
        Assert.Equal(childGroup.Id, loadedAccount.AccountGroup.Id);
    }

    [Fact]
    public async Task DuplicateAccountCode_UnderSameCompany_ViolatesUniqueConstraint()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        var group = new AccountGroup(
            companyId,
            branchId,
            $"GRP-{Guid.NewGuid():N}"[..15],
            "Revenue Group",
            AccountRootClass.Revenue,
            userId);

        dbContext.AccountGroups.Add(group);
        await dbContext.SaveChangesAsync();

        var code = $"4110-{Guid.NewGuid():N}"[..12];
        var acc1 = new Account(companyId, branchId, group.Id, code, "Sales Retail", AccountRootClass.Revenue, userId);
        var acc2 = new Account(companyId, branchId, group.Id, code, "Sales Wholesale", AccountRootClass.Revenue, userId);

        dbContext.Accounts.Add(acc1);
        await dbContext.SaveChangesAsync();

        dbContext.Accounts.Add(acc2);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23505", postgresEx.SqlState); // unique_violation
        Assert.Contains("uq_accounts_company_code", postgresEx.ConstraintName);
    }

    [Fact]
    public async Task CircularAccountGroup_DirectSelfParent_ViolatesCheckConstraint()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var groupId = Guid.CreateVersion7();
        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();

        // Attempting to insert a row where parent_group_id == id via raw SQL to test DB check constraint
        const string sql = """
            INSERT INTO accounting.account_groups
                (id, company_id, branch_id, parent_group_id, group_code, group_name, root_class, hierarchy_depth, is_system, sort_order, is_active, created_at_utc, created_by_user_id, updated_at_utc, concurrency_version)
            VALUES
                (@id, @companyId, @branchId, @id, 'CIRCULAR-TEST', 'Self Parent Group', 'Asset', 1, false, 0, true, NOW(), @userId, NOW(), 1);
            """;

        var conn = dbContext.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("id", groupId));
        cmd.Parameters.Add(new NpgsqlParameter("companyId", companyId));
        cmd.Parameters.Add(new NpgsqlParameter("branchId", branchId));
        cmd.Parameters.Add(new NpgsqlParameter("userId", Guid.CreateVersion7()));

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState); // check_violation
        Assert.Contains("chk_account_groups_hierarchy", ex.ConstraintName);
    }
}
