using Magic.Contracts.Common;
using Magic.Contracts.Events;
using Magic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Outbox;

internal sealed record ClaimedOutboxMessage(
    Guid Id,
    DateTimeOffset OccurredOnUtc,
    string AggregateType,
    Guid AggregateId,
    string EventType,
    string PayloadJson,
    Guid TraceCorrelationId,
    int RetryCount);

/// <summary>
/// Native .NET 10 BackgroundService reliably processing pending platform.outbox_messages (OPEN-004 approved).
/// Enforces PostgreSQL row locking (FOR UPDATE SKIP LOCKED), worker lease acquisition, and event handler dispatch.
/// </summary>
public sealed partial class OutboxProcessorBackgroundService(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider dateTimeProvider,
    ILogger<OutboxProcessorBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogBatchFailure(logger, ex);
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        LogStopped(logger);
    }

    internal async Task<int> ProcessPendingBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MagicDbContext>();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventConsumer>().ToArray();

        var workerId = Guid.CreateVersion7();
        var now = dateTimeProvider.UtcNow;
        var lockExpiry = now.AddMinutes(2);

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var claimed = new List<ClaimedOutboxMessage>();
        await using (var claimCmd = connection.CreateCommand())
        {
            claimCmd.CommandText = """
                UPDATE platform.outbox_messages
                SET lock_token = @workerId,
                    lock_expires_at_utc = @lockExpiry
                WHERE id IN (
                    SELECT id
                    FROM platform.outbox_messages
                    WHERE processed_at_utc IS NULL
                      AND retry_count < 10
                      AND (lock_expires_at_utc IS NULL OR lock_expires_at_utc < @now)
                    ORDER BY occurred_on_utc
                    LIMIT 25
                    FOR UPDATE SKIP LOCKED
                )
                RETURNING id, occurred_on_utc, aggregate_type, aggregate_id, event_type, payload_json, trace_correlation_id, retry_count;
                """;

            var pWorker = claimCmd.CreateParameter();
            pWorker.ParameterName = "workerId";
            pWorker.Value = workerId;
            claimCmd.Parameters.Add(pWorker);

            var pExpiry = claimCmd.CreateParameter();
            pExpiry.ParameterName = "lockExpiry";
            pExpiry.Value = lockExpiry;
            claimCmd.Parameters.Add(pExpiry);

            var pNow = claimCmd.CreateParameter();
            pNow.ParameterName = "now";
            pNow.Value = now;
            claimCmd.Parameters.Add(pNow);

            await using var reader = await claimCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                claimed.Add(new ClaimedOutboxMessage(
                    reader.GetGuid(0),
                    reader.GetFieldValue<DateTimeOffset>(1),
                    reader.GetString(2),
                    reader.GetGuid(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetGuid(6),
                    reader.GetInt32(7)));
            }
        }

        if (claimed.Count == 0)
        {
            return 0;
        }

        var processedCount = 0;
        foreach (var message in claimed)
        {
            var envelope = new IntegrationEventEnvelope
            {
                EventId = message.Id,
                OccurredOnUtc = message.OccurredOnUtc,
                AggregateType = message.AggregateType,
                AggregateId = message.AggregateId,
                EventType = message.EventType,
                PayloadJson = message.PayloadJson,
                CorrelationId = message.TraceCorrelationId
            };

            var matchingHandlers = handlers.Where(h => h.CanHandle(message.EventType)).ToArray();

            try
            {
                foreach (var handler in matchingHandlers)
                {
                    await handler.HandleAsync(envelope, cancellationToken);
                }

                await using var successCmd = connection.CreateCommand();
                successCmd.CommandText = """
                    UPDATE platform.outbox_messages
                    SET processed_at_utc = @processedAt,
                        lock_token = NULL,
                        lock_expires_at_utc = NULL,
                        error_message = NULL
                    WHERE id = @id AND lock_token = @workerId;
                    """;

                var pProcAt = successCmd.CreateParameter();
                pProcAt.ParameterName = "processedAt";
                pProcAt.Value = dateTimeProvider.UtcNow;
                successCmd.Parameters.Add(pProcAt);

                var pId = successCmd.CreateParameter();
                pId.ParameterName = "id";
                pId.Value = message.Id;
                successCmd.Parameters.Add(pId);

                var pWork = successCmd.CreateParameter();
                pWork.ParameterName = "workerId";
                pWork.Value = workerId;
                successCmd.Parameters.Add(pWork);

                await successCmd.ExecuteNonQueryAsync(cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                LogMessageDispatchFailure(logger, ex, message.Id, message.EventType);

                await using var failCmd = connection.CreateCommand();
                failCmd.CommandText = """
                    UPDATE platform.outbox_messages
                    SET retry_count = retry_count + 1,
                        error_message = @errorMessage,
                        lock_token = NULL,
                        lock_expires_at_utc = NULL
                    WHERE id = @id AND lock_token = @workerId;
                    """;

                var pErrMsg = failCmd.CreateParameter();
                pErrMsg.ParameterName = "errorMessage";
                pErrMsg.Value = ex.ToString();
                failCmd.Parameters.Add(pErrMsg);

                var pId = failCmd.CreateParameter();
                pId.ParameterName = "id";
                pId.Value = message.Id;
                failCmd.Parameters.Add(pId);

                var pWork = failCmd.CreateParameter();
                pWork.ParameterName = "workerId";
                pWork.Value = workerId;
                failCmd.Parameters.Add(pWork);

                await failCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        LogBatchProcessed(logger, processedCount);
        return processedCount;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "MAGIC Outbox background processor started.")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "MAGIC Outbox background processor stopped.")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processed {MessageCount} pending outbox message(s).")]
    private static partial void LogBatchProcessed(ILogger logger, int messageCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error polling platform.outbox_messages; retrying after interval.")]
    private static partial void LogBatchFailure(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to dispatch outbox message {MessageId} of event type {EventType}. Incrementing retry count.")]
    private static partial void LogMessageDispatchFailure(ILogger logger, Exception exception, Guid messageId, string eventType);
}

