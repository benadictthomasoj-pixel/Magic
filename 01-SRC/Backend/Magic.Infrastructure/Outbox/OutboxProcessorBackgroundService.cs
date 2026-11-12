using Magic.Contracts.Common;
using Magic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Outbox;

/// <summary>
/// Native .NET 10 BackgroundService processing pending platform.outbox_messages (OPEN-004 approved).
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

        var pending = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null && message.RetryCount < 10)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var now = dateTimeProvider.UtcNow;
        foreach (var message in pending)
        {
            // Phase 1 technical foundation marks queued diagnostic/outbox messages processed;
            // concrete domain subscribers will be registered as modules are implemented in Phase 2+.
            message.ProcessedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogBatchProcessed(logger, pending.Count);
        return pending.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "MAGIC Outbox background processor started.")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "MAGIC Outbox background processor stopped.")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processed {MessageCount} pending outbox message(s).")]
    private static partial void LogBatchProcessed(ILogger logger, int messageCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error polling platform.outbox_messages; retrying after interval.")]
    private static partial void LogBatchFailure(ILogger logger, Exception exception);
}

