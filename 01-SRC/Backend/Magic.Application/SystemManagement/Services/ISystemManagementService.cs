using Magic.Contracts.SystemManagement;

namespace Magic.Application.SystemManagement.Services;

public interface ISystemManagementService
{
    Task<SystemHealthDashboardDto> GetHealthDashboardAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupRecordDto>> GetBackupsAsync(CancellationToken cancellationToken = default);
    Task<BackupRecordDto> CreateBackupAsync(Guid userId, string userName, CreateBackupRequest request, CancellationToken cancellationToken = default);
    Task<RestoreResultDto> VerifyAndSimulateRestoreAsync(Guid userId, RestoreDatabaseRequest request, CancellationToken cancellationToken = default);
    Task<DatabaseMigrationSummaryDto> GetMigrationSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackgroundJobStatusDto>> GetBackgroundJobsStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemConfigurationItemDto>> GetConfigurationsAsync(Guid companyId, string? category, CancellationToken cancellationToken = default);
    Task<SystemConfigurationItemDto> UpdateConfigurationAsync(Guid companyId, Guid userId, UpdateSystemConfigurationRequest request, CancellationToken cancellationToken = default);
}

