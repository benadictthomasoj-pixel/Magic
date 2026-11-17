using System.Text.Json;
using Magic.Contracts.Events;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Outbox;

public sealed class StockAlertEventConsumer(
    MagicDbContext dbContext,
    ILogger<StockAlertEventConsumer> logger) : IIntegrationEventConsumer
{
    public bool CanHandle(string eventType) =>
        eventType is "inventory.stock.alert" or "sales.invoice.created";

    public async Task HandleAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(envelope.PayloadJson);
            var root = doc.RootElement;
            var companyId = root.TryGetProperty("CompanyId", out var cProp) && cProp.TryGetGuid(out var cId) ? cId : Guid.Empty;
            if (companyId == Guid.Empty) return;

            // Check if any product in this company has fallen below reorder level
            var lowStockItems = await dbContext.StockBalances
                .Include(b => b.Product)
                .Where(b => b.CompanyId == companyId && b.Product != null && b.Product.ReorderLevel > 0 && b.QuantityOnHand <= b.Product.ReorderLevel)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var item in lowStockItems)
            {
                var exists = await dbContext.Notifications
                    .AnyAsync(n => n.CompanyId == companyId && n.Category == "Inventory" && !n.IsRead && n.Title.Contains(item.Product!.ProductCode), cancellationToken);

                if (!exists)
                {
                    dbContext.Notifications.Add(new NotificationRecord
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        BranchId = item.BranchId,
                        Category = "Inventory",
                        Priority = "High",
                        Title = $"Low Stock Alert: {item.Product!.ProductCode}",
                        Message = $"Product '{item.Product.Name}' is low on stock ({item.QuantityOnHand:F0} on hand, reorder at {item.Product.ReorderLevel:F0}).",
                        IsRead = false,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            if (dbContext.ChangeTracker.HasChanges())
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Stock alert consumer completed scan for outbox event {EventType}", envelope.EventType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking stock alerts for event {EventType}", envelope.EventType);
            throw;
        }
    }
}

