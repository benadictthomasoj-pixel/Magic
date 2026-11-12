using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class AuditImmutabilityTests(MagicTestFixture factory)
    : IClassFixture<MagicTestFixture>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task AuditLogTableRejectsUpdateAndThrowsPostgreSqlException55000()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var record = new SystemAuditLogRecord
            {
                EventTimestampUtc = DateTimeOffset.UtcNow,
                UserId = Guid.CreateVersion7(),
                BranchId = Guid.CreateVersion7(),
                TerminalId = Guid.CreateVersion7(),
                ClientIp = "127.0.0.1",
                ActionName = "TEST_SECURITY_IMMUTABILITY",
                EntityName = "SecurityTestEntity",
                EntityId = Guid.CreateVersion7(),
                BeforeStateJson = "{}",
                AfterStateJson = "{\"status\":\"initial\"}",
                CorrelationId = Guid.CreateVersion7()
            };

            dbContext.SystemAuditLogs.Add(record);
            await dbContext.SaveChangesAsync();
            Assert.True(record.Id > 0);

            var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE audit.system_audit_logs SET action_name = 'TAMPERED' WHERE id = {record.Id}");
            });

            var postgresEx = FindPostgresException(ex);
            Assert.NotNull(postgresEx);
            Assert.Equal("55000", postgresEx.SqlState);
            Assert.Contains("immutable", postgresEx.Message, StringComparison.OrdinalIgnoreCase);

            await transaction.RollbackAsync();
        });
    }

    [Fact]
    public async Task AuditLogTableRejectsDeleteAndThrowsPostgreSqlException55000()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var record = new SystemAuditLogRecord
            {
                EventTimestampUtc = DateTimeOffset.UtcNow,
                UserId = Guid.CreateVersion7(),
                BranchId = Guid.CreateVersion7(),
                TerminalId = Guid.CreateVersion7(),
                ClientIp = "127.0.0.1",
                ActionName = "TEST_SECURITY_IMMUTABILITY_DELETE",
                EntityName = "SecurityTestEntity",
                EntityId = Guid.CreateVersion7(),
                BeforeStateJson = "{}",
                AfterStateJson = "{\"status\":\"initial\"}",
                CorrelationId = Guid.CreateVersion7()
            };

            dbContext.SystemAuditLogs.Add(record);
            await dbContext.SaveChangesAsync();
            Assert.True(record.Id > 0);

            var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM audit.system_audit_logs WHERE id = {record.Id}");
            });

            var postgresEx = FindPostgresException(ex);
            Assert.NotNull(postgresEx);
            Assert.Equal("55000", postgresEx.SqlState);
            Assert.Contains("immutable", postgresEx.Message, StringComparison.OrdinalIgnoreCase);

            await transaction.RollbackAsync();
        });
    }

    private static PostgresException? FindPostgresException(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is PostgresException pgEx)
            {
                return pgEx;
            }

            ex = ex.InnerException;
        }

        return null;
    }
}
