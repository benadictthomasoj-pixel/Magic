using Magic.Application.Automation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Automation;

/// <summary>
/// Background hosted service that periodically evaluates active automation rules (DEF-ARC-003).
/// Ensures that scheduled alerts, low-stock triggers, and notifications execute autonomously
/// without requiring manual invocation of the trigger endpoint.
/// </summary>
public sealed class AutomationRuleSchedulerBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<AutomationRuleSchedulerBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MAGIC Automation Rule Scheduler Background Service started.");

        // Initial delay to allow DB migrations and seeders to settle
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledRulesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in Automation Rule Scheduler iteration.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("MAGIC Automation Rule Scheduler Background Service stopped.");
    }

    internal async Task<int> ProcessScheduledRulesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var automationService = scope.ServiceProvider.GetRequiredService<IAutomationService>();
        var executedCount = await automationService.EvaluateAllActiveRulesAsync(cancellationToken);
        if (executedCount > 0)
        {
            logger.LogInformation("Automation scheduler evaluated {ExecutedCount} active rule(s).", executedCount);
        }
        return executedCount;
    }
}

