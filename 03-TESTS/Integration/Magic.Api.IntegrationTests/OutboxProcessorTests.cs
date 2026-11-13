using Magic.Contracts.Common;
using Magic.Contracts.Events;
using Magic.Infrastructure.Outbox;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class OutboxProcessorTests : IClassFixture<MagicTestFixture>
{
    private sealed class TrackingConsumer : IIntegrationEventConsumer
    {
        public List<Guid> HandledEventIds { get; } = new();
        public bool FailOnPurpose { get; set; }

        public bool CanHandle(string eventType) =>
            eventType is "TEST_OUTBOX_SUCCESS" or "TEST_OUTBOX_FAIL";

        public Task HandleAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default)
        {
            if (FailOnPurpose || envelope.EventType == "TEST_OUTBOX_FAIL")
            {
                throw new InvalidOperationException("Deliberate consumer processing failure for testing.");
            }

            HandledEventIds.Add(envelope.EventId);
            return Task.CompletedTask;
        }
    }

    private readonly WebApplicationFactory<Program> _factory;
    private readonly TrackingConsumer _consumer = new();

    public OutboxProcessorTests(MagicTestFixture factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IIntegrationEventConsumer>(_consumer);
            });
        });
    }

    [Fact]
    public async Task ProcessPendingBatchDispatchesToConsumerAndMarksProcessed()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OutboxProcessorBackgroundService>>();
        var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

        var testMessage = new OutboxMessageRecord
        {
            Id = Guid.CreateVersion7(),
            OccurredOnUtc = DateTimeOffset.UtcNow,
            AggregateType = "Order",
            AggregateId = Guid.CreateVersion7(),
            EventType = "TEST_OUTBOX_SUCCESS",
            PayloadJson = "{\"orderId\":\"123\"}",
            TraceCorrelationId = Guid.CreateVersion7(),
            ProcessedAtUtc = null,
            RetryCount = 0
        };

        dbContext.OutboxMessages.Add(testMessage);
        await dbContext.SaveChangesAsync();

        var processor = new OutboxProcessorBackgroundService(scopeFactory, dateTimeProvider, logger);
        var attempts = 0;
        var totalProcessed = 0;
        while (!_consumer.HandledEventIds.Contains(testMessage.Id) && attempts++ < 10)
        {
            var count = await processor.ProcessPendingBatchAsync(CancellationToken.None);
            totalProcessed += count;
            if (count == 0) break;
        }

        Assert.True(totalProcessed >= 1);
        Assert.Contains(testMessage.Id, _consumer.HandledEventIds);

        // Verify database state: row is marked processed and unlocked
        var updated = await dbContext.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == testMessage.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated.ProcessedAtUtc);
        Assert.Null(updated.LockToken);
        Assert.Null(updated.LockExpiresAtUtc);
        Assert.Null(updated.ErrorMessage);

        // Cleanup test message
        dbContext.OutboxMessages.Remove(testMessage);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ProcessPendingBatchIncrementsRetryCountAndRecordsErrorOnHandlerFailure()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OutboxProcessorBackgroundService>>();
        var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

        var testMessage = new OutboxMessageRecord
        {
            Id = Guid.CreateVersion7(),
            OccurredOnUtc = DateTimeOffset.UtcNow,
            AggregateType = "Order",
            AggregateId = Guid.CreateVersion7(),
            EventType = "TEST_OUTBOX_FAIL",
            PayloadJson = "{\"error\":\"expected\"}",
            TraceCorrelationId = Guid.CreateVersion7(),
            ProcessedAtUtc = null,
            RetryCount = 0
        };

        dbContext.OutboxMessages.Add(testMessage);
        await dbContext.SaveChangesAsync();

        var processor = new OutboxProcessorBackgroundService(scopeFactory, dateTimeProvider, logger);
        var attempts = 0;
        OutboxMessageRecord? updated = null;
        while (attempts++ < 10)
        {
            await processor.ProcessPendingBatchAsync(CancellationToken.None);
            updated = await dbContext.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == testMessage.Id);
            if (updated?.RetryCount > 0) break;
        }

        Assert.NotNull(updated);
        Assert.Null(updated.ProcessedAtUtc);
        Assert.Equal(1, updated.RetryCount);
        Assert.NotNull(updated.ErrorMessage);
        Assert.Contains("Deliberate consumer processing failure", updated.ErrorMessage);
        Assert.Null(updated.LockToken);

        // Cleanup test message
        dbContext.OutboxMessages.Remove(testMessage);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ConcurrentWorkersClaimDisjointMessagesUsingForUpdateSkipLocked()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();

        var msg1 = new OutboxMessageRecord
        {
            Id = Guid.CreateVersion7(),
            OccurredOnUtc = DateTimeOffset.UtcNow.AddSeconds(-2),
            AggregateType = "TestAggregate",
            AggregateId = Guid.CreateVersion7(),
            EventType = "TEST_CONCURRENCY",
            PayloadJson = "{}",
            TraceCorrelationId = Guid.CreateVersion7()
        };

        var msg2 = new OutboxMessageRecord
        {
            Id = Guid.CreateVersion7(),
            OccurredOnUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
            AggregateType = "TestAggregate",
            AggregateId = Guid.CreateVersion7(),
            EventType = "TEST_CONCURRENCY",
            PayloadJson = "{}",
            TraceCorrelationId = Guid.CreateVersion7()
        };

        dbContext.OutboxMessages.AddRange(msg1, msg2);
        await dbContext.SaveChangesAsync();

        // Simulate two concurrent worker transactions
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();

        var db1 = scope1.ServiceProvider.GetRequiredService<MagicDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<MagicDbContext>();

        await using var tx1 = await db1.Database.BeginTransactionAsync();
        await using var tx2 = await db2.Database.BeginTransactionAsync();

        var conn1 = db1.Database.GetDbConnection();
        var conn2 = db2.Database.GetDbConnection();

        const string claimSql = """
            SELECT id
            FROM platform.outbox_messages
            WHERE processed_at_utc IS NULL
              AND retry_count < 10
              AND (lock_expires_at_utc IS NULL OR lock_expires_at_utc < NOW())
            ORDER BY occurred_on_utc
            LIMIT 1
            FOR UPDATE SKIP LOCKED;
            """;

        // Worker 1 claims first available row
        Guid worker1ClaimedId = Guid.Empty;
        await using (var cmd1 = conn1.CreateCommand())
        {
            cmd1.Transaction = tx1.GetDbTransaction();
            cmd1.CommandText = claimSql;
            var result = await cmd1.ExecuteScalarAsync();
            if (result is Guid g) worker1ClaimedId = g;
        }

        // Worker 2 runs concurrently and claims with SKIP LOCKED
        Guid worker2ClaimedId = Guid.Empty;
        await using (var cmd2 = conn2.CreateCommand())
        {
            cmd2.Transaction = tx2.GetDbTransaction();
            cmd2.CommandText = claimSql;
            var result = await cmd2.ExecuteScalarAsync();
            if (result is Guid g) worker2ClaimedId = g;
        }

        Assert.NotEqual(Guid.Empty, worker1ClaimedId);
        Assert.NotEqual(Guid.Empty, worker2ClaimedId);
        // Worker 2 must NOT claim the row locked by Worker 1
        Assert.NotEqual(worker1ClaimedId, worker2ClaimedId);

        await tx1.RollbackAsync();
        await tx2.RollbackAsync();

        // Cleanup
        dbContext.OutboxMessages.RemoveRange(msg1, msg2);
        await dbContext.SaveChangesAsync();
    }
}
