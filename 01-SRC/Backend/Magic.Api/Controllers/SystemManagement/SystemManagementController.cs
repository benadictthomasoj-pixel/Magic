using Asp.Versioning;
using Magic.Application.SystemManagement.Services;
using Magic.Contracts.Api;
using Magic.Contracts.Common;
using Magic.Contracts.Security;
using Magic.Contracts.SystemManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Magic.Api.Controllers.SystemManagement;

[Authorize]
[Route("api/v{version:apiVersion}/system-management")]
public sealed class SystemManagementController(
    ISystemManagementService systemService,
    IDateTimeProvider dateTimeProvider,
    IBranchContext branchContext,
    ICurrentUserContext userContext) : OperationsBaseController(dateTimeProvider, branchContext, userContext)
{
    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<SystemHealthDashboardDto>>> GetHealthDashboard(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("system:status:view");
        var companyId = ResolveCompanyId();
        var item = await systemService.GetHealthDashboardAsync(companyId, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("backups")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BackupRecordDto>>>> GetBackups(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("system:backups:manage");
        var items = await systemService.GetBackupsAsync(cancellationToken);
        return Envelope(items);
    }

    [HttpPost("backups")]
    public async Task<ActionResult<ApiResponse<BackupRecordDto>>> CreateBackup(
        [FromBody] CreateBackupRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("system:backups:manage");
        var userId = ResolveCurrentUserId();
        var userName = CurrentUser.Username ?? "Administrator";
        var item = await systemService.CreateBackupAsync(userId, userName, request, cancellationToken);
        return CreatedEnvelope(item);
    }

    [HttpPost("restore/verify")]
    public async Task<ActionResult<ApiResponse<RestoreResultDto>>> VerifyRestoreSimulation(
        [FromBody] RestoreDatabaseRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("system:backups:manage");
        var userId = ResolveCurrentUserId();
        var item = await systemService.VerifyAndSimulateRestoreAsync(userId, request, cancellationToken);
        return Envelope(item);
    }

    [HttpGet("migrations")]
    public async Task<ActionResult<ApiResponse<DatabaseMigrationSummaryDto>>> GetMigrationSummary(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("system:status:view");
        var item = await systemService.GetMigrationSummaryAsync(cancellationToken);
        return Envelope(item);
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BackgroundJobStatusDto>>>> GetBackgroundJobs(
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("system:status:view");
        var items = await systemService.GetBackgroundJobsStatusAsync(cancellationToken);
        return Envelope(items);
    }

    [HttpGet("configurations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SystemConfigurationItemDto>>>> GetConfigurations(
        [FromQuery] string? category,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("system:config:manage");
        var companyId = ResolveCompanyId();
        var items = await systemService.GetConfigurationsAsync(companyId, category, cancellationToken);
        return Envelope(items);
    }

    [HttpPut("configurations")]
    public async Task<ActionResult<ApiResponse<SystemConfigurationItemDto>>> UpdateConfiguration(
        [FromBody] UpdateSystemConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission("system:config:manage");
        var companyId = ResolveCompanyId();
        var userId = ResolveCurrentUserId();
        var item = await systemService.UpdateConfigurationAsync(companyId, userId, request, cancellationToken);
        return Envelope(item);
    }
}
