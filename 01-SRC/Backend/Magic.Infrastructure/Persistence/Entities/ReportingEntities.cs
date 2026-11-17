namespace Magic.Infrastructure.Persistence.Entities;

public sealed class ReportDefinitionRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string ReportCode { get; set; }
    public required string Title { get; set; }
    public required string Category { get; set; } // "Sales", "Purchases", "Inventory", "Accounting", "Finance", "HR", "Operations"
    public string? Description { get; set; }
    public string FilterSchemaJson { get; set; } = "{}";
    public bool IsSystem { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ReportExportLogRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string ReportCode { get; set; }
    public required string ExportFormat { get; set; } // "PDF", "XLSX", "CSV"
    public string FilterParametersJson { get; set; } = "{}";
    public Guid ExportedByUserId { get; set; }
    public required string ExportedByUserName { get; set; }
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
}

