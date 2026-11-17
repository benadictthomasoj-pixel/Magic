namespace Magic.Infrastructure.Persistence.Entities;

public sealed class BackupRecord
{
    public Guid Id { get; set; }
    public required string BackupType { get; set; } // "Manual", "Scheduled"
    public required string FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public required string StoragePath { get; set; }
    public string? ChecksumSha256 { get; set; }
    public string Status { get; set; } = "Completed"; // "Completed", "Failed", "Verified"
    public Guid InitiatedByUserId { get; set; }
    public required string InitiatedByUserName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAtUtc { get; set; }
    public int RetentionDays { get; set; } = 30;
}

public sealed class SystemConfigurationRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string ConfigKey { get; set; }
    public required string ConfigValue { get; set; }
    public required string Category { get; set; } // "Company", "Branch", "Invoice", "Tax", "Backup", "Notifications"
    public string? Description { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

