using Magic.Infrastructure.Persistence.Entities;
using Magic.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Persistence;

public sealed class SecurityDatabaseSeeder(
    MagicDbContext dbContext,
    IPasswordHasher passwordHasher,
    ILogger<SecurityDatabaseSeeder> logger)
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // 1. Seed Permissions
        var existingPermCodes = await dbContext.Permissions
            .Select(p => p.Code)
            .ToHashSetAsync(cancellationToken);

        var permissionsToSeed = new (string Code, string Module, string Description)[]
        {
            // Accounting
            ("accounting:dashboard:view", "Accounting", "View accounting health dashboard and KPIs"),
            ("accounting:accounts:view", "Accounting", "View hierarchical Chart of Accounts"),
            ("accounting:accounts:manage", "Accounting", "Create and update ledger accounts"),
            ("accounting:vouchers:view", "Accounting", "View journal voucher registry and details"),
            ("accounting:vouchers:create", "Accounting", "Create draft journal vouchers"),
            ("accounting:vouchers:post", "Accounting", "Post journal vouchers to General Ledger"),
            ("accounting:vouchers:reverse", "Accounting", "Create counter-reversal journal vouchers"),
            ("accounting:ledger:view", "Accounting", "View General Ledger account statements and Trial Balance"),
            ("accounting:periods:view", "Accounting", "View fiscal periods and locking status"),
            ("accounting:periods:manage", "Accounting", "Lock, close, and reopen fiscal periods"),
            ("accounting:subledgers:view", "Accounting", "View Accounts Receivable and Accounts Payable subledgers"),

            // Billing & POS
            ("billing:dashboard:view", "Billing", "View billing overview and commercial metrics"),
            ("billing:pos:access", "Billing", "Access retail POS cashier terminal"),
            ("billing:sales:view", "Billing", "View commercial sales orders and invoices"),
            ("billing:sales:create", "Billing", "Create and post sales tax invoices"),
            ("billing:purchases:view", "Billing", "View purchase orders and vendor bills"),
            ("billing:inventory:view", "Billing", "View inventory stock levels and movements"),

            // Administration
            ("admin:users:manage", "Admin", "Create, edit, and assign roles to users"),
            ("admin:roles:manage", "Admin", "Manage custom roles and permission assignments"),
            ("admin:terminals:manage", "Admin", "Register and authorize client hardware terminals"),
            ("admin:audit:view", "Admin", "View tamper-evident security audit trail")
        };

        var newPermissions = new List<PermissionRecord>();
        foreach (var (code, module, desc) in permissionsToSeed)
        {
            if (!existingPermCodes.Contains(code))
            {
                newPermissions.Add(new PermissionRecord
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Module = module,
                    Description = desc,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        if (newPermissions.Count > 0)
        {
            dbContext.Permissions.AddRange(newPermissions);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} security permissions.", newPermissions.Count);
        }

        var allPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        var permMap = allPermissions.ToDictionary(p => p.Code, p => p.Id);

        // 2. Seed Roles
        var existingRoles = await dbContext.Roles
            .Where(r => r.CompanyId == DefaultCompanyId)
            .ToListAsync(cancellationToken);
        var roleMap = existingRoles.ToDictionary(r => r.Code, r => r);

        var rolesDef = new (string Code, string Name, string Desc)[]
        {
            ("Administrator", "System Administrator", "Full unconstrained administrative access"),
            ("StoreManager", "Store Manager", "Operational supervision, sales, purchasing, inventory"),
            ("Accountant", "Senior Accountant", "Full Chart of Accounts, Vouchers, GL, Fiscal governance"),
            ("Cashier", "Front Desk Cashier", "Retail POS checkout and receipt printing")
        };

        foreach (var (code, name, desc) in rolesDef)
        {
            if (!roleMap.ContainsKey(code))
            {
                var role = new RoleRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = DefaultCompanyId,
                    Code = code,
                    Name = name,
                    Description = desc,
                    IsSystem = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                dbContext.Roles.Add(role);
                roleMap[code] = role;
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        // 3. Seed Role-Permissions
        var existingRolePerms = await dbContext.RolePermissions
            .Select(rp => $"{rp.RoleId}_{rp.PermissionId}")
            .ToHashSetAsync(cancellationToken);

        var roleAssignments = new Dictionary<string, string[]>
        {
            ["Administrator"] = permissionsToSeed.Select(p => p.Code).ToArray(),
            ["StoreManager"] = permissionsToSeed.Where(p => p.Module != "Admin").Select(p => p.Code).ToArray(),
            ["Accountant"] = permissionsToSeed.Where(p => p.Module == "Accounting").Select(p => p.Code).ToArray(),
            ["Cashier"] = ["billing:dashboard:view", "billing:pos:access", "billing:sales:view", "billing:sales:create"]
        };

        var newRolePerms = new List<RolePermissionRecord>();
        foreach (var (roleCode, permCodes) in roleAssignments)
        {
            if (roleMap.TryGetValue(roleCode, out var role))
            {
                foreach (var pCode in permCodes)
                {
                    if (permMap.TryGetValue(pCode, out var pId))
                    {
                        var key = $"{role.Id}_{pId}";
                        if (!existingRolePerms.Contains(key))
                        {
                            newRolePerms.Add(new RolePermissionRecord
                            {
                                RoleId = role.Id,
                                PermissionId = pId,
                                AssignedAtUtc = DateTime.UtcNow
                            });
                            existingRolePerms.Add(key);
                        }
                    }
                }
            }
        }

        if (newRolePerms.Count > 0)
        {
            dbContext.RolePermissions.AddRange(newRolePerms);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 4. Seed Users
        var usersDef = new (string Username, string Email, string DisplayName, string Password, string RoleCode)[]
        {
            ("ben", "ben@magic.local", "Benadict (Administrator)", "123123", "Administrator"),
            ("admin", "admin@magic.local", "System Administrator", "123123", "Administrator"),
            ("accountant1", "accountant@magic.local", "Senior Accountant", "123123", "Accountant"),
            ("cashier1", "cashier1@magic.local", "Front Desk Cashier", "123123", "Cashier")
        };

        foreach (var (username, email, displayName, password, roleCode) in usersDef)
        {
            var normalizedUser = username.ToUpperInvariant();
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(
                u => u.CompanyId == DefaultCompanyId && u.NormalizedUsername == normalizedUser,
                cancellationToken);

            var (hash, salt) = passwordHasher.HashPassword(password);

            if (existingUser is null)
            {
                var user = new UserRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = DefaultCompanyId,
                    Username = username,
                    NormalizedUsername = normalizedUser,
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    DisplayName = displayName,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    IsActive = true,
                    IsLockedOut = false,
                    DefaultBranchId = DefaultBranchId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                dbContext.Users.Add(user);

                if (roleMap.TryGetValue(roleCode, out var role))
                {
                    dbContext.UserRoles.Add(new UserRoleRecord
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedAtUtc = DateTime.UtcNow
                    });
                }

                dbContext.UserBranchAssignments.Add(new UserBranchAssignmentRecord
                {
                    UserId = user.Id,
                    BranchId = DefaultBranchId,
                    CompanyId = DefaultCompanyId,
                    IsPrimary = true,
                    AssignedAtUtc = DateTime.UtcNow
                });

                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Seeded default user {Username} with role {RoleCode}.", username, roleCode);
            }
            else
            {
                existingUser.PasswordHash = hash;
                existingUser.PasswordSalt = salt;
                existingUser.IsActive = true;
                existingUser.IsLockedOut = false;
                existingUser.FailedLoginAttempts = 0;
                existingUser.LockoutEndUtc = null;
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Updated credentials for user {Username}.", username);
            }
        }

        // 5. Seed Default POS Terminal
        var terminalExists = await dbContext.Terminals.AnyAsync(
            t => t.CompanyId == DefaultCompanyId && t.BranchId == DefaultBranchId && t.TerminalCode == "POS-01",
            cancellationToken);

        if (!terminalExists)
        {
            dbContext.Terminals.Add(new TerminalRecord
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CompanyId = DefaultCompanyId,
                BranchId = DefaultBranchId,
                TerminalCode = "POS-01",
                MachineName = "COUNTER-TERM-01",
                OperatingSystem = "Windows 11 Enterprise",
                IsAuthorized = true,
                RegisteredAtUtc = DateTime.UtcNow,
                LastHeartbeatAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded default terminal POS-01.");
        }
    }
}

