namespace Magic.Contracts.SystemManagement;

public sealed record SystemHealthDashboardDto(
    string ApplicationStatus,
    string ApiHealth,
    string DatabaseConnectivity,
    string DatabaseVersion,
    string ApplicationVersion,
    string EnvironmentName,
    int PendingOutboxMessagesCount,
    int FailedOutboxMessagesCount,
    int ActiveBackgroundJobsCount,
    DateTime? LastBackupAtUtc,
    string UptimeFormatted,
    IReadOnlyList<SystemDiagnosticLogDto> RecentErrors,
    DateTime EvaluatedAtUtc);

public sealed record SystemDiagnosticLogDto(
    DateTime TimestampUtc,
    string Level,
    string Source,
    string Message);

public sealed record BackupRecordDto(
    Guid Id,
    string BackupType, // "Manual", "Scheduled"
    string FileName,
    long FileSizeBytes,
    string StoragePath,
    string Status, // "Completed", "Failed", "Verified"
    string InitiatedByUserName,
    DateTime CreatedAtUtc,
    DateTime? VerifiedAtUtc,
    int RetentionDays);

public sealed record CreateBackupRequest(
    string? Notes);

public sealed record RestoreDatabaseRequest(
    Guid BackupId,
    string TargetDatabaseName,
    string ConfirmationPassword);

public sealed record RestoreResultDto(
    bool Success,
    string TargetDatabase,
    string Message,
    DateTime CompletedAtUtc);

public sealed record DatabaseMigrationSummaryDto(
    string CurrentMigration,
    int AppliedMigrationsCount,
    IReadOnlyList<string> AppliedMigrations,
    int PendingMigrationsCount,
    string DatabaseHealth);

public sealed record BackgroundJobStatusDto(
    string JobName,
    string Status,
    DateTime? LastExecutionUtc,
    DateTime? NextExecutionUtc,
    int ConsecutiveFailuresCount,
    string? LastErrorMessage);

public sealed record SystemConfigurationItemDto(
    string ConfigKey,
    string ConfigValue,
    string Category,
    string Description,
    DateTime UpdatedAtUtc);

public sealed record UpdateSystemConfigurationRequest(
    string ConfigKey,
    string ConfigValue);

