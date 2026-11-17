using System.Text.Json;
using Magic.Contracts.Events;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Outbox;

public sealed class SalesNotificationEventConsumer(
    MagicDbContext dbContext,
    ILogger<SalesNotificationEventConsumer> logger) : IIntegrationEventConsumer
{
    public bool CanHandle(string eventType) =>
        eventType is "sales.invoice.created" or "sales.return.created";

    public async Task HandleAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(envelope.PayloadJson);
            var root = doc.RootElement;
            var companyId = root.TryGetProperty("CompanyId", out var cProp) && cProp.TryGetGuid(out var cId) ? cId : Guid.Empty;
            var branchId = root.TryGetProperty("BranchId", out var bProp) && bProp.TryGetGuid(out var bId) ? (Guid?)bId : null;

            if (envelope.EventType == "sales.invoice.created")
            {
                var invoiceNumber = root.TryGetProperty("InvoiceNumber", out var numProp) ? numProp.GetString() : "Unknown";
                var grandTotalPaise = root.TryGetProperty("GrandTotalPaise", out var totProp) ? totProp.GetInt64() : 0L;

                dbContext.Notifications.Add(new NotificationRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BranchId = branchId,
                    Category = "Finance",
                    Priority = "Low",
                    Title = $"POS Sale Completed: {invoiceNumber}",
                    Message = $"Invoice {invoiceNumber} generated for ₹{grandTotalPaise / 100.0:F2}.",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else if (envelope.EventType == "sales.return.created")
            {
                var returnNumber = root.TryGetProperty("ReturnNumber", out var rNumProp) ? rNumProp.GetString() : "Unknown";
                var refundPaise = root.TryGetProperty("TotalRefundPaise", out var refProp) ? refProp.GetInt64() : 0L;

                dbContext.Notifications.Add(new NotificationRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BranchId = branchId,
                    Category = "Finance",
                    Priority = "Medium",
                    Title = $"Sales Return Processed: {returnNumber}",
                    Message = $"Sales Return {returnNumber} processed with refund of ₹{refundPaise / 100.0:F2}.",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Outbox consumer successfully processed {EventType} for AggregateId {AggregateId}",
                envelope.EventType, envelope.AggregateId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling integration event {EventType} for AggregateId {AggregateId}",
                envelope.EventType, envelope.AggregateId);
            throw;
        }
    }
}

