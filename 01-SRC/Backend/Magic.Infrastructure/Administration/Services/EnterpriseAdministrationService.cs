using Magic.Application.Administration.Services;
using Magic.Contracts.Administration;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Magic.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Administration.Services;

public sealed class EnterpriseAdministrationService(
    MagicDbContext dbContext,
    IPasswordHasher passwordHasher) : IEnterpriseAdministrationService
{
    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users.AsNoTracking()
            .Where(u => u.CompanyId == companyId)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.BranchAssignments)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        return users.Select(u => new AdminUserDto(
            u.Id,
            u.CompanyId,
            u.Username,
            u.Email,
            u.DisplayName,
            u.IsActive,
            u.IsLockedOut,
            u.FailedLoginAttempts,
            u.LastLoginAtUtc,
            u.UserRoles.Select(r => r.Role?.Code ?? "User").ToList(),
            u.BranchAssignments.Select(b => b.BranchId).ToList(),
            u.CreatedAtUtc)).ToList();
    }

    public async Task<AdminUserDto> GetUserByIdAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var u = await dbContext.Users.AsNoTracking()
            .Include(x => x.UserRoles).ThenInclude(ur => ur.Role)
            .Include(x => x.BranchAssignments)
            .FirstOrDefaultAsync(x => x.Id == userId && x.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("USER_NOT_FOUND", "User not found or belongs to another company.", StatusCodes.Status404NotFound);

        return new AdminUserDto(
            u.Id,
            u.CompanyId,
            u.Username,
            u.Email,
            u.DisplayName,
            u.IsActive,
            u.IsLockedOut,
            u.FailedLoginAttempts,
            u.LastLoginAtUtc,
            u.UserRoles.Select(r => r.Role?.Code ?? "User").ToList(),
            u.BranchAssignments.Select(b => b.BranchId).ToList(),
            u.CreatedAtUtc);
    }

    public async Task<AdminUserDto> CreateUserAsync(Guid companyId, CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedUser = request.Username.Trim().ToUpperInvariant();
        var exists = await dbContext.Users.AnyAsync(u => u.CompanyId == companyId && u.NormalizedUsername == normalizedUser, cancellationToken);
        if (exists)
            throw new DomainException("DUPLICATE_USERNAME", "A user with this username already exists.", StatusCodes.Status400BadRequest);

        var (hash, salt) = passwordHasher.HashPassword(request.Password);

        var user = new UserRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Username = request.Username.Trim(),
            NormalizedUsername = normalizedUser,
            Email = request.Email.Trim(),
            NormalizedEmail = request.Email.Trim().ToUpperInvariant(),
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            IsLockedOut = false,
            FailedLoginAttempts = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);

        // Assign Roles
        if (request.Roles != null)
        {
            var roles = await dbContext.Roles.Where(r => r.CompanyId == companyId && request.Roles.Contains(r.Code)).ToListAsync(cancellationToken);
            foreach (var r in roles)
            {
                dbContext.UserRoles.Add(new UserRoleRecord
                {
                    UserId = user.Id,
                    RoleId = r.Id,
                    AssignedAtUtc = DateTime.UtcNow
                });
            }
        }

        // Assign Branches
        if (request.BranchIds != null)
        {
            bool isFirst = true;
            foreach (var bId in request.BranchIds)
            {
                dbContext.UserBranchAssignments.Add(new UserBranchAssignmentRecord
                {
                    UserId = user.Id,
                    BranchId = bId,
                    CompanyId = companyId,
                    IsPrimary = isFirst,
                    AssignedAtUtc = DateTime.UtcNow
                });
                isFirst = false;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserByIdAsync(companyId, user.Id, cancellationToken);
    }

    public async Task<AdminUserDto> UpdateUserAsync(Guid companyId, Guid userId, UpdateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.BranchAssignments)
            .FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("USER_NOT_FOUND", "User not found.", StatusCodes.Status404NotFound);

        user.DisplayName = request.DisplayName.Trim();
        user.Email = request.Email.Trim();
        user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
        user.IsActive = request.IsActive;

        if (request.UnlockAccount)
        {
            user.IsLockedOut = false;
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        // Sync Roles
        if (request.Roles != null)
        {
            dbContext.UserRoles.RemoveRange(user.UserRoles);
            var roles = await dbContext.Roles.Where(r => r.CompanyId == companyId && request.Roles.Contains(r.Code)).ToListAsync(cancellationToken);
            foreach (var r in roles)
            {
                dbContext.UserRoles.Add(new UserRoleRecord
                {
                    UserId = user.Id,
                    RoleId = r.Id,
                    AssignedAtUtc = DateTime.UtcNow
                });
            }
        }

        // Sync Branches
        if (request.BranchIds != null)
        {
            dbContext.UserBranchAssignments.RemoveRange(user.BranchAssignments);
            bool isFirst = true;
            foreach (var bId in request.BranchIds)
            {
                dbContext.UserBranchAssignments.Add(new UserBranchAssignmentRecord
                {
                    UserId = user.Id,
                    BranchId = bId,
                    CompanyId = companyId,
                    IsPrimary = isFirst,
                    AssignedAtUtc = DateTime.UtcNow
                });
                isFirst = false;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserByIdAsync(companyId, user.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<RoleWithPermissionsDto>> GetRolesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var roles = await dbContext.Roles.AsNoTracking()
            .Where(r => r.CompanyId == companyId)
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(r => new RoleWithPermissionsDto(
            r.Id,
            r.Code,
            r.Name,
            r.Description,
            r.IsSystem,
            r.RolePermissions.Select(rp => rp.Permission?.Code ?? "").Where(c => !string.IsNullOrEmpty(c)).ToList())).ToList();
    }

    public async Task<RoleWithPermissionsDto> UpdateRolePermissionsAsync(Guid companyId, Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("ROLE_NOT_FOUND", "Role not found.", StatusCodes.Status404NotFound);

        dbContext.RolePermissions.RemoveRange(role.RolePermissions);

        var permissions = await dbContext.Permissions
            .Where(p => request.PermissionCodes.Contains(p.Code))
            .ToListAsync(cancellationToken);

        foreach (var p in permissions)
        {
            dbContext.RolePermissions.Add(new RolePermissionRecord
            {
                RoleId = role.Id,
                PermissionId = p.Id,
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetRolesAsync(companyId, cancellationToken)).First(r => r.Id == roleId);
    }

    public async Task<IReadOnlyList<ApprovalWorkflowDto>> GetWorkflowsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var workflows = await dbContext.ApprovalWorkflows.AsNoTracking()
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.Module)
            .ToListAsync(cancellationToken);

        return workflows.Select(w => new ApprovalWorkflowDto(
            w.Id,
            w.CompanyId,
            w.Module,
            w.TransactionType,
            w.MinimumAmountPaise,
            w.RequiredApprovalsCount,
            w.IsActive,
            w.CreatedAtUtc)).ToList();
    }

    public async Task<ApprovalWorkflowDto> CreateWorkflowAsync(Guid companyId, CreateApprovalWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var workflow = new ApprovalWorkflowRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Module = request.Module.Trim(),
            TransactionType = request.TransactionType.Trim(),
            MinimumAmountPaise = request.MinimumAmountPaise,
            RequiredApprovalsCount = request.RequiredApprovalsCount > 0 ? request.RequiredApprovalsCount : 1,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.ApprovalWorkflows.Add(workflow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ApprovalWorkflowDto(
            workflow.Id,
            workflow.CompanyId,
            workflow.Module,
            workflow.TransactionType,
            workflow.MinimumAmountPaise,
            workflow.RequiredApprovalsCount,
            workflow.IsActive,
            workflow.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<ApprovalRequestDto>> GetApprovalRequestsAsync(Guid companyId, string? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ApprovalRequests.AsNoTracking().Where(r => r.CompanyId == companyId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);

        var requests = await query
            .Include(r => r.Actions)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var list = new List<ApprovalRequestDto>();
        foreach (var r in requests)
        {
            var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == r.BranchId, cancellationToken);
            list.Add(new ApprovalRequestDto(
                r.Id,
                r.CompanyId,
                r.BranchId,
                branch?.BranchName ?? "Store",
                r.WorkflowId,
                r.SourceModule,
                r.SourceDocumentId,
                r.ReferenceNumber,
                r.AmountPaise,
                r.Status,
                r.RequestedByUserName,
                r.Notes,
                r.CreatedAtUtc,
                r.Actions.Select(a => new ApprovalActionDto(a.ApproverUserId, a.ApproverName, a.Action, a.Reason, a.ActedAtUtc)).ToList()));
        }

        return list;
    }

    public async Task<ApprovalRequestDto> SubmitApprovalActionAsync(Guid companyId, Guid requestId, Guid userId, string userName, SubmitApprovalActionRequest request, CancellationToken cancellationToken = default)
    {
        var req = await dbContext.ApprovalRequests
            .Include(r => r.Actions)
            .Include(r => r.Workflow)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("REQUEST_NOT_FOUND", "Approval request not found.", StatusCodes.Status404NotFound);

        if (req.Status != "Pending")
            throw new DomainException("INVALID_STATE", $"Request is already {req.Status}.", StatusCodes.Status400BadRequest);

        // Separation of duties: requester cannot approve their own request
        if (req.RequestedByUserId == userId && request.Action == "Approved")
        {
            throw new DomainException("SELF_APPROVAL_FORBIDDEN", "Separation of duties: You cannot approve your own request.", StatusCodes.Status400BadRequest);
        }

        var action = new ApprovalActionRecord
        {
            Id = Guid.NewGuid(),
            ApprovalRequestId = req.Id,
            ApproverUserId = userId,
            ApproverName = userName,
            Action = request.Action,
            Reason = request.Reason,
            ActedAtUtc = DateTime.UtcNow
        };

        dbContext.ApprovalActions.Add(action);

        if (request.Action == "Rejected")
        {
            req.Status = "Rejected";
            req.DecidedAtUtc = DateTime.UtcNow;
        }
        else
        {
            var requiredCount = req.Workflow?.RequiredApprovalsCount ?? 1;
            var currentApprovals = req.Actions.Count(a => a.Action == "Approved") + 1;
            if (currentApprovals >= requiredCount)
            {
                req.Status = "Approved";
                req.DecidedAtUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetApprovalRequestsAsync(companyId, null, cancellationToken)).First(r => r.Id == requestId);
    }

    public async Task<IReadOnlyList<AuditLogEntryDto>> QueryAuditLogsAsync(Guid companyId, AuditLogQueryRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SecurityAuditEvents.AsNoTracking().Where(a => a.CompanyId == companyId);

        if (request.StartDate.HasValue)
        {
            var startUtc = request.StartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.TimestampUtc >= startUtc);
        }

        if (request.EndDate.HasValue)
        {
            var endUtc = request.EndDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.TimestampUtc <= endUtc);
        }

        if (!string.IsNullOrEmpty(request.EventType))
        {
            query = query.Where(a => a.EventType.Contains(request.EventType));
        }

        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var logs = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return logs.Select(l => new AuditLogEntryDto(
            l.Id,
            l.CompanyId,
            l.Username,
            l.EventType,
            l.DetailsJson,
            l.IpAddress,
            l.TimestampUtc)).ToList();
    }

    public async Task<SecurityMetricsDto> GetSecurityMetricsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var activeUsers = await dbContext.Users.AsNoTracking().CountAsync(u => u.CompanyId == companyId && u.IsActive, cancellationToken);
        var lockedUsers = await dbContext.Users.AsNoTracking().CountAsync(u => u.CompanyId == companyId && u.IsLockedOut, cancellationToken);

        var last24Hours = DateTime.UtcNow.AddHours(-24);
        var failedLogins = await dbContext.SecurityAuditEvents.AsNoTracking()
            .CountAsync(a => a.CompanyId == companyId && a.EventType == "AUTH_LOGIN_FAILED" && a.TimestampUtc >= last24Hours, cancellationToken);

        var activeSessions = await dbContext.RefreshTokens.AsNoTracking()
            .CountAsync(r => r.CompanyId == companyId && !r.IsRevoked && r.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);

        var revokedTokens = await dbContext.RefreshTokens.AsNoTracking()
            .CountAsync(r => r.CompanyId == companyId && r.IsRevoked, cancellationToken);

        return new SecurityMetricsDto(
            activeUsers,
            lockedUsers,
            failedLogins,
            activeSessions,
            revokedTokens,
            DateTime.UtcNow);
    }
}
