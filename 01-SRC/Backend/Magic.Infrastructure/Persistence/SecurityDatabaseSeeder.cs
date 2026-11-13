using Magic.Infrastructure.Persistence.Entities;
using Magic.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Persistence;

public sealed class SecurityDatabaseSeeder(
    MagicDbContext dbContext,
    IPasswordHasher passwordHasher,
    IHostEnvironment hostEnvironment,
    ILogger<SecurityDatabaseSeeder> logger)
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            throw new InvalidOperationException("CRITICAL: SecurityDatabaseSeeder is restricted to Development environments and cannot execute in Production.");
        }
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

            // Commercial Masters (Phase B)
            ("commercial:products:view", "Commercial", "View products, pricing, barcodes, and catalog"),
            ("commercial:products:create", "Commercial", "Register new products, variants, and barcodes"),
            ("commercial:products:edit", "Commercial", "Update product specifications and prices"),
            ("commercial:products:archive", "Commercial", "Deactivate or archive products"),
            ("commercial:categories:manage", "Commercial", "Manage categories, brands, and departments"),
            ("commercial:pricing:manage", "Commercial", "Configure pricing tiers and price lists"),
            ("commercial:customers:view", "Commercial", "View customer directory and credit terms"),
            ("commercial:customers:manage", "Commercial", "Create and edit customer records"),
            ("commercial:credit:approve", "Commercial", "Approve or modify customer credit limits"),
            ("commercial:suppliers:view", "Commercial", "View supplier directory and payment terms"),
            ("commercial:suppliers:manage", "Commercial", "Create and edit supplier records"),
            ("commercial:suppliers:bank-details", "Commercial", "View unmasked supplier bank account details"),

            // Inventory & Procurement (Phase C)
            ("inventory:warehouse:view", "Inventory", "View warehouses and storage locations"),
            ("inventory:warehouse:manage", "Inventory", "Create, edit, and configure warehouses"),
            ("inventory:stock:view", "Inventory", "View stock balances, levels, and movements"),
            ("inventory:stock:manage", "Inventory", "Post opening stock and manage inventory movements"),
            ("inventory:adjust:create", "Inventory", "Create draft stock adjustments"),
            ("inventory:adjust:approve", "Inventory", "Approve and post stock adjustments"),
            ("inventory:count:view", "Inventory", "View physical stock counting sessions"),
            ("inventory:count:manage", "Inventory", "Perform stock counting, recording, and reconciliation"),
            ("procurement:po:view", "Procurement", "View purchase orders and vendor orders"),
            ("procurement:po:create", "Procurement", "Create and edit purchase orders"),
            ("procurement:po:approve", "Procurement", "Approve and authorize purchase orders"),
            ("procurement:grn:view", "Procurement", "View goods receipt notes"),
            ("procurement:grn:create", "Procurement", "Create goods receipt notes and record inspections"),
            ("procurement:grn:confirm", "Procurement", "Confirm goods receipts and atomically update inventory"),
            ("procurement:return:manage", "Procurement", "Create and confirm purchase returns to suppliers"),
            ("inventory:transfer:create", "Inventory", "Create warehouse transfer requests"),
            ("inventory:transfer:dispatch", "Inventory", "Dispatch stock for warehouse transfers"),
            ("inventory:transfer:receive", "Inventory", "Confirm receipt of transferred stock"),
            ("inventory:reorder:view", "Inventory", "View reorder rules and stock replenishment suggestions"),
            ("inventory:reorder:manage", "Inventory", "Configure reorder points, safety stock, and lead times"),

            // Sales & POS (Phase D)
            ("pos:terminal:operate", "Sales", "Operate POS cashier terminal, barcode scanning and cart"),
            ("pos:terminal:open-close-shift", "Sales", "Open and close cashier shifts, declare closing float"),
            ("pos:cash:movement", "Sales", "Record cashier cash drops, pay-ins, and pay-outs"),
            ("sales:invoice:create", "Sales", "Create, finalize, and tender sales invoices"),
            ("sales:invoice:view", "Sales", "View sales invoices, line items, and tender breakdowns"),
            ("sales:invoice:cancel", "Sales", "Cancel sales invoices and restore physical inventory"),
            ("sales:returns:create", "Sales", "Process sales returns, credit notes, and restock items"),
            ("sales:returns:view", "Sales", "View sales returns history and return lines"),
            ("sales:credit-note:view", "Sales", "View and print customer credit notes"),
            ("sales:reports:view", "Sales", "View sales metrics, payment method breakdowns, and hourly analytics"),

            // Financial Management & Treasury (Phase E)
            ("finance:receivables:view", "Finance", "View customer receivables, aging, and customer statements"),
            ("finance:receipts:create", "Finance", "Create customer receipts and allocate against open invoices"),
            ("finance:receipts:cancel", "Finance", "Reverse customer receipts with companion reversal vouchers"),
            ("finance:payables:view", "Finance", "View supplier payables register, AP aging, and statements"),
            ("finance:bills:create", "Finance", "Register and approve supplier commercial bills and purchase liability"),
            ("finance:payments:create", "Finance", "Create supplier payments and allocate against open bills"),
            ("finance:payments:cancel", "Finance", "Reverse supplier payments with companion reversal vouchers"),
            ("finance:bank-accounts:view", "Finance", "View bank and cash accounts with masked numbers"),
            ("finance:bank-accounts:manage", "Finance", "Create, configure, and manage bank and cash account masters"),
            ("finance:bank-accounts:reveal", "Finance", "Reveal unmasked bank account numbers (Audited in platform log)"),
            ("finance:treasury:create", "Finance", "Record Contra cash/bank transfers, deposits, and bank adjustments"),
            ("finance:reconciliation:manage", "Finance", "Import bank statements, match transactions, post adjustments, and approve"),
            ("finance:reports:view", "Finance", "View and export financial reports, Cash Book, Bank Book, and statements"),

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
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Seeded {Count} security permissions.", newPermissions.Count);
            }
            catch (DbUpdateException)
            {
                dbContext.ChangeTracker.Clear();
            }
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
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
        }

        // Re-read permissions and roles to get latest IDs
        allPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        permMap = allPermissions.ToDictionary(p => p.Code, p => p.Id);
        var refreshedRoles = await dbContext.Roles.ToListAsync(cancellationToken);
        roleMap = refreshedRoles.ToDictionary(r => r.Code, r => r);

        // 3. Seed Role-Permissions
        var existingRolePerms = await dbContext.RolePermissions
            .Select(rp => $"{rp.RoleId}_{rp.PermissionId}")
            .ToHashSetAsync(cancellationToken);

        var roleAssignments = new Dictionary<string, string[]>
        {
            ["Administrator"] = permissionsToSeed.Select(p => p.Code).ToArray(),
            ["StoreManager"] = permissionsToSeed.Where(p => p.Module != "Admin").Select(p => p.Code).ToArray(),
            ["Accountant"] = permissionsToSeed.Where(p => p.Module == "Accounting" || p.Module == "Finance" || p.Code == "commercial:products:view" || p.Code == "commercial:customers:view" || p.Code == "commercial:suppliers:view" || p.Code == "commercial:credit:approve" || p.Code == "commercial:suppliers:bank-details" || p.Code == "inventory:stock:view" || p.Code == "procurement:po:view" || p.Code == "procurement:po:approve" || p.Code == "procurement:grn:view" || p.Code == "inventory:reorder:view").Select(p => p.Code).ToArray(),
            ["Cashier"] = ["billing:dashboard:view", "billing:pos:access", "billing:sales:view", "billing:sales:create", "commercial:products:view", "commercial:customers:view", "pos:terminal:operate", "pos:terminal:open-close-shift", "pos:cash:movement", "sales:invoice:create", "sales:invoice:view", "sales:returns:create", "sales:returns:view", "sales:credit-note:view"]
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
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                dbContext.ChangeTracker.Clear();
            }
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

