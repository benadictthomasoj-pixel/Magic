using System.Globalization;
using Magic.Application.Inventory.Services;
using Magic.Contracts.Inventory;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Inventory.Services;

// ==========================================
// 1. Warehouse Service
// ==========================================

public sealed class WarehouseService(MagicDbContext dbContext) : IWarehouseService
{
    public async Task<(IReadOnlyList<WarehouseDto> Items, int TotalCount)> GetWarehousesAsync(
        Guid companyId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Warehouses
            .Include(w => w.BranchAssignments)
            .Include(w => w.StorageLocations)
            .Where(w => w.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(w => EF.Functions.ILike(w.Code, $"%{term}%") ||
                                     EF.Functions.ILike(w.Name, $"%{term}%") ||
                                     EF.Functions.ILike(w.City, $"%{term}%"));
        }

        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderBy(w => w.Code)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(w => new WarehouseDto
            {
                Id = w.Id,
                CompanyId = w.CompanyId,
                Code = w.Code,
                Name = w.Name,
                Description = w.Description,
                AddressLine1 = w.AddressLine1,
                AddressLine2 = w.AddressLine2,
                City = w.City,
                State = w.State,
                StateCode = w.StateCode,
                PinCode = w.PinCode,
                Country = w.Country,
                Phone = w.Phone,
                Email = w.Email,
                ManagerName = w.ManagerName,
                CapacitySqFt = w.CapacitySqFt,
                IsActive = w.IsActive,
                AssignedBranchIds = w.BranchAssignments.Select(b => b.BranchId).ToList(),
                StorageLocationsCount = w.StorageLocations.Count,
                CreatedAtUtc = w.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<WarehouseDto> GetWarehouseByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var w = await dbContext.Warehouses
            .Include(w => w.BranchAssignments)
            .Include(w => w.StorageLocations)
            .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse with ID '{id}' was not found.");

        return new WarehouseDto
        {
            Id = w.Id,
            CompanyId = w.CompanyId,
            Code = w.Code,
            Name = w.Name,
            Description = w.Description,
            AddressLine1 = w.AddressLine1,
            AddressLine2 = w.AddressLine2,
            City = w.City,
            State = w.State,
            StateCode = w.StateCode,
            PinCode = w.PinCode,
            Country = w.Country,
            Phone = w.Phone,
            Email = w.Email,
            ManagerName = w.ManagerName,
            CapacitySqFt = w.CapacitySqFt,
            IsActive = w.IsActive,
            AssignedBranchIds = w.BranchAssignments.Select(b => b.BranchId).ToList(),
            StorageLocationsCount = w.StorageLocations.Count,
            CreatedAtUtc = w.CreatedAtUtc
        };
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(Guid companyId, Guid userId, CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await dbContext.Warehouses.AnyAsync(w => w.CompanyId == companyId && w.Code == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"A warehouse with code '{code}' already exists.");
        }

        var warehouseId = Guid.CreateVersion7();
        var warehouse = new WarehouseRecord
        {
            Id = warehouseId,
            CompanyId = companyId,
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = request.AddressLine2?.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            StateCode = request.StateCode.Trim().ToUpperInvariant(),
            PinCode = request.PinCode.Trim(),
            Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            ManagerName = request.ManagerName?.Trim(),
            CapacitySqFt = request.CapacitySqFt,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        if (request.AssignedBranchIds is { Count: > 0 })
        {
            foreach (var bId in request.AssignedBranchIds)
            {
                warehouse.BranchAssignments.Add(new WarehouseBranchAssignmentRecord
                {
                    Id = Guid.CreateVersion7(),
                    CompanyId = companyId,
                    WarehouseId = warehouseId,
                    BranchId = bId,
                    IsPrimary = false,
                    AssignedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        dbContext.Warehouses.Add(warehouse);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetWarehouseByIdAsync(companyId, warehouseId, cancellationToken);
    }

    public async Task<WarehouseDto> UpdateWarehouseAsync(Guid companyId, Guid id, Guid userId, UpdateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var w = await dbContext.Warehouses
            .Include(w => w.BranchAssignments)
            .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse with ID '{id}' was not found.");

        w.Name = request.Name.Trim();
        w.Description = request.Description?.Trim();
        w.AddressLine1 = request.AddressLine1.Trim();
        w.AddressLine2 = request.AddressLine2?.Trim();
        w.City = request.City.Trim();
        w.State = request.State.Trim();
        w.StateCode = request.StateCode.Trim().ToUpperInvariant();
        w.PinCode = request.PinCode.Trim();
        w.Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country.Trim();
        w.Phone = request.Phone?.Trim();
        w.Email = request.Email?.Trim();
        w.ManagerName = request.ManagerName?.Trim();
        w.CapacitySqFt = request.CapacitySqFt;
        w.IsActive = request.IsActive;
        w.UpdatedAtUtc = DateTimeOffset.UtcNow;
        w.UpdatedByUserId = userId;

        if (request.AssignedBranchIds is not null)
        {
            dbContext.WarehouseBranchAssignments.RemoveRange(w.BranchAssignments);
            foreach (var bId in request.AssignedBranchIds)
            {
                w.BranchAssignments.Add(new WarehouseBranchAssignmentRecord
                {
                    Id = Guid.CreateVersion7(),
                    CompanyId = companyId,
                    WarehouseId = id,
                    BranchId = bId,
                    IsPrimary = false,
                    AssignedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetWarehouseByIdAsync(companyId, id, cancellationToken);
    }

    public async Task ArchiveWarehouseAsync(Guid companyId, Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var w = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse with ID '{id}' was not found.");

        w.IsActive = false;
        w.UpdatedAtUtc = DateTimeOffset.UtcNow;
        w.UpdatedByUserId = userId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StorageLocationDto>> GetStorageLocationsAsync(Guid companyId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        return await dbContext.StorageLocations
            .Where(l => l.CompanyId == companyId && l.WarehouseId == warehouseId)
            .OrderBy(l => l.Code)
            .Select(l => new StorageLocationDto
            {
                Id = l.Id,
                WarehouseId = l.WarehouseId,
                Code = l.Code,
                Name = l.Name,
                LocationType = l.LocationType,
                ParentLocationId = l.ParentLocationId,
                Barcode = l.Barcode,
                IsActive = l.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StorageLocationDto> CreateStorageLocationAsync(Guid companyId, Guid warehouseId, CreateStorageLocationRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Warehouses.AnyAsync(w => w.CompanyId == companyId && w.Id == warehouseId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException($"Warehouse with ID '{warehouseId}' was not found.");
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var codeExists = await dbContext.StorageLocations.AnyAsync(l => l.WarehouseId == warehouseId && l.Code == code, cancellationToken);
        if (codeExists)
        {
            throw new InvalidOperationException($"Storage location code '{code}' already exists in this warehouse.");
        }

        var loc = new StorageLocationRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            WarehouseId = warehouseId,
            Code = code,
            Name = request.Name.Trim(),
            LocationType = request.LocationType.Trim(),
            ParentLocationId = request.ParentLocationId,
            Barcode = request.Barcode?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.StorageLocations.Add(loc);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new StorageLocationDto
        {
            Id = loc.Id,
            WarehouseId = loc.WarehouseId,
            Code = loc.Code,
            Name = loc.Name,
            LocationType = loc.LocationType,
            ParentLocationId = loc.ParentLocationId,
            Barcode = loc.Barcode,
            IsActive = loc.IsActive
        };
    }
}

// ==========================================
// 2. Inventory Ledger Service
// ==========================================

public sealed class InventoryLedgerService(MagicDbContext dbContext) : IInventoryLedgerService
{
    public async Task<(IReadOnlyList<StockBalanceDto> Items, int TotalCount)> GetStockBalancesAsync(
        Guid companyId,
        Guid? branchId,
        Guid? warehouseId,
        Guid? productId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockBalances
            .Include(b => b.Product)
            .Include(b => b.Warehouse)
            .Include(b => b.StorageLocation)
            .Include(b => b.Variant)
            .Where(b => b.CompanyId == companyId);

        if (branchId.HasValue) query = query.Where(b => b.BranchId == branchId.Value);
        if (warehouseId.HasValue) query = query.Where(b => b.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(b => b.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b => EF.Functions.ILike(b.Product!.Name, $"%{term}%") ||
                                     EF.Functions.ILike(b.Product!.ProductCode, $"%{term}%") ||
                                     EF.Functions.ILike(b.Product!.Sku, $"%{term}%") ||
                                     (b.BatchNumber != null && EF.Functions.ILike(b.BatchNumber, $"%{term}%")));
        }

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderBy(b => b.Product!.Name)
            .ThenBy(b => b.Warehouse!.Name)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(b => new StockBalanceDto
            {
                Id = b.Id,
                CompanyId = b.CompanyId,
                BranchId = b.BranchId,
                WarehouseId = b.WarehouseId,
                WarehouseName = b.Warehouse!.Name,
                ProductId = b.ProductId,
                ProductName = b.Product!.Name,
                ProductCode = b.Product!.ProductCode,
                Sku = b.Variant != null ? b.Variant.Sku : b.Product!.Sku,
                VariantId = b.VariantId,
                VariantName = b.Variant != null ? b.Variant.VariantName : null,
                StorageLocationId = b.StorageLocationId,
                LocationCode = b.StorageLocation != null ? b.StorageLocation.Code : null,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate,
                QuantityOnHand = b.QuantityOnHand,
                QuantityReserved = b.QuantityReserved,
                QuantityAvailable = b.QuantityAvailable,
                QuantityDamaged = b.QuantityDamaged,
                QuantityExpired = b.QuantityExpired,
                QuantityBlocked = b.QuantityBlocked,
                UnitCostPaise = b.UnitCostPaise,
                LastMovementAtUtc = b.LastMovementAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<StockMovementDto> Items, int TotalCount)> GetStockMovementsAsync(
        Guid companyId,
        Guid? warehouseId,
        Guid? productId,
        string? movementType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Where(m => m.CompanyId == companyId);

        if (warehouseId.HasValue) query = query.Where(m => m.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
        if (!string.IsNullOrWhiteSpace(movementType)) query = query.Where(m => m.MovementType == movementType.Trim());

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(m => m.MovementDateUtc)
            .ThenByDescending(m => m.CreatedAtUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(m => new StockMovementDto
            {
                Id = m.Id,
                CompanyId = m.CompanyId,
                BranchId = m.BranchId,
                WarehouseId = m.WarehouseId,
                WarehouseName = m.Warehouse!.Name,
                ProductId = m.ProductId,
                ProductName = m.Product!.Name,
                Sku = m.Variant != null ? m.Variant.Sku : m.Product!.Sku,
                VariantId = m.VariantId,
                StorageLocationId = m.StorageLocationId,
                MovementType = m.MovementType,
                Direction = m.Direction,
                Quantity = m.Quantity,
                BatchNumber = m.BatchNumber,
                ExpiryDate = m.ExpiryDate,
                UnitCostPaise = m.UnitCostPaise,
                TotalCostPaise = m.TotalCostPaise,
                ReferenceType = m.ReferenceType,
                ReferenceId = m.ReferenceId,
                ReferenceNumber = m.ReferenceNumber,
                Reason = m.Reason,
                BalanceAfterOnHand = m.BalanceAfterOnHand,
                PerformedByUserId = m.PerformedByUserId,
                MovementDateUtc = m.MovementDateUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task PostOpeningStockAsync(Guid companyId, Guid userId, PostOpeningStockRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0) return;

        _ = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == request.WarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse '{request.WarehouseId}' was not found.");

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0) continue;

            _ = await dbContext.Products.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == item.ProductId, cancellationToken)
                ?? throw new KeyNotFoundException($"Product '{item.ProductId}' was not found.");

            var balance = await dbContext.StockBalances.FirstOrDefaultAsync(b =>
                b.CompanyId == companyId &&
                b.WarehouseId == request.WarehouseId &&
                b.ProductId == item.ProductId &&
                b.VariantId == item.VariantId &&
                b.StorageLocationId == item.StorageLocationId &&
                b.BatchNumber == item.BatchNumber, cancellationToken);

            if (balance is null)
            {
                balance = new StockBalanceRecord
                {
                    Id = Guid.CreateVersion7(),
                    CompanyId = companyId,
                    BranchId = request.BranchId,
                    WarehouseId = request.WarehouseId,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    StorageLocationId = item.StorageLocationId,
                    BatchNumber = item.BatchNumber,
                    ExpiryDate = item.ExpiryDate,
                    QuantityOnHand = item.Quantity,
                    QuantityReserved = 0,
                    QuantityAvailable = item.Quantity,
                    QuantityDamaged = 0,
                    QuantityExpired = 0,
                    QuantityBlocked = 0,
                    UnitCostPaise = item.UnitCostPaise,
                    LastMovementAtUtc = DateTimeOffset.UtcNow
                };
                dbContext.StockBalances.Add(balance);
            }
            else
            {
                balance.QuantityOnHand += item.Quantity;
                balance.QuantityAvailable += item.Quantity;
                balance.UnitCostPaise = item.UnitCostPaise;
                balance.LastMovementAtUtc = DateTimeOffset.UtcNow;
            }

            var movement = new StockMovementRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                BranchId = request.BranchId,
                WarehouseId = request.WarehouseId,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                StorageLocationId = item.StorageLocationId,
                MovementType = "OpeningStock",
                Direction = "In",
                Quantity = item.Quantity,
                BatchNumber = item.BatchNumber,
                ExpiryDate = item.ExpiryDate,
                UnitCostPaise = item.UnitCostPaise,
                TotalCostPaise = (long)(item.Quantity * item.UnitCostPaise),
                ReferenceType = "OpeningStock",
                ReferenceId = null,
                ReferenceNumber = "OPENING",
                Reason = item.Reason ?? "Initial stock upload",
                BalanceAfterOnHand = balance.QuantityOnHand,
                PerformedByUserId = userId,
                MovementDateUtc = DateTimeOffset.UtcNow,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.StockMovements.Add(movement);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task ApplyStockMovementAsync(
        MagicDbContext db,
        Guid companyId,
        Guid branchId,
        Guid warehouseId,
        Guid productId,
        Guid? variantId,
        Guid? storageLocationId,
        string movementType,
        string direction,
        decimal quantity,
        long unitCostPaise,
        string referenceType,
        Guid? referenceId,
        string? referenceNumber,
        string? reason,
        Guid userId,
        string? batchNumber = null,
        DateOnly? expiryDate = null,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        var balance = await db.StockBalances.FirstOrDefaultAsync(b =>
            b.CompanyId == companyId &&
            b.WarehouseId == warehouseId &&
            b.ProductId == productId &&
            b.VariantId == variantId &&
            b.StorageLocationId == storageLocationId &&
            b.BatchNumber == batchNumber, cancellationToken);

        if (balance is null)
        {
            if (direction == "Out")
            {
                throw new InvalidOperationException($"Cannot issue stock for product '{productId}': No available balance in warehouse '{warehouseId}'.");
            }

            balance = new StockBalanceRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                BranchId = branchId,
                WarehouseId = warehouseId,
                ProductId = productId,
                VariantId = variantId,
                StorageLocationId = storageLocationId,
                BatchNumber = batchNumber,
                ExpiryDate = expiryDate,
                QuantityOnHand = quantity,
                QuantityReserved = 0,
                QuantityAvailable = quantity,
                QuantityDamaged = 0,
                QuantityExpired = 0,
                QuantityBlocked = 0,
                UnitCostPaise = unitCostPaise,
                LastMovementAtUtc = DateTimeOffset.UtcNow
            };
            db.StockBalances.Add(balance);
        }
        else
        {
            if (direction == "In")
            {
                balance.QuantityOnHand += quantity;
                balance.QuantityAvailable += quantity;
                if (unitCostPaise > 0) balance.UnitCostPaise = unitCostPaise;
            }
            else
            {
                if (balance.QuantityAvailable < quantity)
                {
                    throw new InvalidOperationException($"Insufficient available stock. Available: {balance.QuantityAvailable.ToString(CultureInfo.InvariantCulture)}, Requested: {quantity.ToString(CultureInfo.InvariantCulture)}.");
                }
                balance.QuantityOnHand -= quantity;
                balance.QuantityAvailable -= quantity;
            }
            balance.LastMovementAtUtc = DateTimeOffset.UtcNow;
        }

        var movement = new StockMovementRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            ProductId = productId,
            VariantId = variantId,
            StorageLocationId = storageLocationId,
            MovementType = movementType,
            Direction = direction,
            Quantity = quantity,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            UnitCostPaise = unitCostPaise,
            TotalCostPaise = (long)(quantity * unitCostPaise),
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber,
            Reason = reason,
            BalanceAfterOnHand = balance.QuantityOnHand,
            PerformedByUserId = userId,
            MovementDateUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.StockMovements.Add(movement);
    }
}

// ==========================================
// 3. Stock Adjustment Service
// ==========================================

public sealed class StockAdjustmentService(MagicDbContext dbContext) : IStockAdjustmentService
{
    public async Task<(IReadOnlyList<StockAdjustmentDto> Items, int TotalCount)> GetAdjustmentsAsync(
        Guid companyId,
        Guid? warehouseId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockAdjustments
            .Include(a => a.Warehouse)
            .Include(a => a.Lines)
            .Where(a => a.CompanyId == companyId);

        if (warehouseId.HasValue) query = query.Where(a => a.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status.Trim());

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(a => a.AdjustmentDateUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(a => new StockAdjustmentDto
            {
                Id = a.Id,
                CompanyId = a.CompanyId,
                BranchId = a.BranchId,
                WarehouseId = a.WarehouseId,
                WarehouseName = a.Warehouse!.Name,
                AdjustmentNumber = a.AdjustmentNumber,
                AdjustmentDateUtc = a.AdjustmentDateUtc,
                ReasonCode = a.ReasonCode,
                Description = a.Description,
                Status = a.Status,
                RequiresApproval = a.RequiresApproval,
                TotalItemsCount = a.TotalItemsCount,
                CreatedByUserId = a.CreatedByUserId,
                ApprovedByUserId = a.ApprovedByUserId,
                ApprovedAtUtc = a.ApprovedAtUtc,
                RejectionReason = a.RejectionReason,
                CreatedAtUtc = a.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<StockAdjustmentDto> GetAdjustmentByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var a = await dbContext.StockAdjustments
            .Include(a => a.Warehouse)
            .Include(a => a.Lines).ThenInclude(l => l.Product)
            .Include(a => a.Lines).ThenInclude(l => l.StorageLocation)
            .Include(a => a.Lines).ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock adjustment '{id}' was not found.");

        return new StockAdjustmentDto
        {
            Id = a.Id,
            CompanyId = a.CompanyId,
            BranchId = a.BranchId,
            WarehouseId = a.WarehouseId,
            WarehouseName = a.Warehouse!.Name,
            AdjustmentNumber = a.AdjustmentNumber,
            AdjustmentDateUtc = a.AdjustmentDateUtc,
            ReasonCode = a.ReasonCode,
            Description = a.Description,
            Status = a.Status,
            RequiresApproval = a.RequiresApproval,
            TotalItemsCount = a.TotalItemsCount,
            CreatedByUserId = a.CreatedByUserId,
            ApprovedByUserId = a.ApprovedByUserId,
            ApprovedAtUtc = a.ApprovedAtUtc,
            RejectionReason = a.RejectionReason,
            CreatedAtUtc = a.CreatedAtUtc,
            Lines = a.Lines.Select(l => new StockAdjustmentLineDto
            {
                Id = l.Id,
                StockAdjustmentId = l.StockAdjustmentId,
                ProductId = l.ProductId,
                ProductName = l.Product!.Name,
                Sku = l.Variant != null ? l.Variant.Sku : l.Product!.Sku,
                VariantId = l.VariantId,
                StorageLocationId = l.StorageLocationId,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate,
                SystemQuantityOnHand = l.SystemQuantityOnHand,
                AdjustmentQuantity = l.AdjustmentQuantity,
                NewQuantityOnHand = l.NewQuantityOnHand,
                UnitCostPaise = l.UnitCostPaise,
                LineReason = l.LineReason
            }).ToList()
        };
    }

    public async Task<StockAdjustmentDto> CreateAdjustmentAsync(Guid companyId, Guid userId, CreateStockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var count = await dbContext.StockAdjustments.CountAsync(a => a.CompanyId == companyId, cancellationToken);
        var adjNum = string.Format(CultureInfo.InvariantCulture, "ADJ-{0:yyyyMMdd}-{1:D4}", DateTime.UtcNow, count + 1);

        var adjId = Guid.CreateVersion7();
        var adj = new StockAdjustmentRecord
        {
            Id = adjId,
            CompanyId = companyId,
            BranchId = request.BranchId,
            WarehouseId = request.WarehouseId,
            AdjustmentNumber = adjNum,
            AdjustmentDateUtc = DateTimeOffset.UtcNow,
            ReasonCode = request.ReasonCode.Trim(),
            Description = request.Description?.Trim(),
            Status = "Draft",
            RequiresApproval = true,
            TotalItemsCount = request.Lines.Count,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var l in request.Lines)
        {
            var bal = await dbContext.StockBalances.FirstOrDefaultAsync(b =>
                b.CompanyId == companyId &&
                b.WarehouseId == request.WarehouseId &&
                b.ProductId == l.ProductId &&
                b.VariantId == l.VariantId &&
                b.StorageLocationId == l.StorageLocationId &&
                b.BatchNumber == l.BatchNumber, cancellationToken);

            var currentQty = bal?.QuantityOnHand ?? 0m;
            var newQty = currentQty + l.AdjustmentQuantity;
            if (newQty < 0)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Negative stock resulting from adjustment is disallowed. Product: {0}, Current: {1}, Adjustment: {2}",
                    l.ProductId, currentQty, l.AdjustmentQuantity));
            }

            adj.Lines.Add(new StockAdjustmentLineRecord
            {
                Id = Guid.CreateVersion7(),
                StockAdjustmentId = adjId,
                ProductId = l.ProductId,
                VariantId = l.VariantId,
                StorageLocationId = l.StorageLocationId,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate,
                SystemQuantityOnHand = currentQty,
                AdjustmentQuantity = l.AdjustmentQuantity,
                NewQuantityOnHand = newQty,
                UnitCostPaise = l.UnitCostPaise,
                LineReason = l.LineReason?.Trim()
            });
        }

        dbContext.StockAdjustments.Add(adj);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetAdjustmentByIdAsync(companyId, adjId, cancellationToken);
    }

    public async Task<StockAdjustmentDto> ApproveAdjustmentAsync(Guid companyId, Guid id, Guid approverId, CancellationToken cancellationToken = default)
    {
        var adj = await dbContext.StockAdjustments
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock adjustment '{id}' was not found.");

        if (adj.Status == "Approved")
        {
            return await GetAdjustmentByIdAsync(companyId, id, cancellationToken);
        }

        if (adj.Status != "Draft" && adj.Status != "PendingApproval")
        {
            throw new InvalidOperationException($"Cannot approve adjustment in status '{adj.Status}'.");
        }

        foreach (var line in adj.Lines)
        {
            if (line.AdjustmentQuantity == 0) continue;

            var direction = line.AdjustmentQuantity > 0 ? "In" : "Out";
            var mType = line.AdjustmentQuantity > 0 ? "AdjustmentPositive" : "AdjustmentNegative";
            var absQty = Math.Abs(line.AdjustmentQuantity);

            await InventoryLedgerService.ApplyStockMovementAsync(
                dbContext,
                companyId,
                adj.BranchId,
                adj.WarehouseId,
                line.ProductId,
                line.VariantId,
                line.StorageLocationId,
                mType,
                direction,
                absQty,
                line.UnitCostPaise,
                "StockAdjustment",
                adj.Id,
                adj.AdjustmentNumber,
                line.LineReason ?? adj.ReasonCode,
                approverId,
                line.BatchNumber,
                line.ExpiryDate,
                cancellationToken);
        }

        adj.Status = "Approved";
        adj.ApprovedByUserId = approverId;
        adj.ApprovedAtUtc = DateTimeOffset.UtcNow;
        adj.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(companyId, id, cancellationToken);
    }

    public async Task<StockAdjustmentDto> RejectAdjustmentAsync(Guid companyId, Guid id, Guid rejectorId, string? reason, CancellationToken cancellationToken = default)
    {
        var adj = await dbContext.StockAdjustments.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock adjustment '{id}' was not found.");

        if (adj.Status == "Approved")
        {
            throw new InvalidOperationException("Cannot reject an already approved adjustment.");
        }

        adj.Status = "Rejected";
        adj.RejectionReason = reason;
        adj.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(companyId, id, cancellationToken);
    }
}

// ==========================================
// 4. Stock Count Service
// ==========================================

public sealed class StockCountService(MagicDbContext dbContext) : IStockCountService
{
    public async Task<(IReadOnlyList<StockCountDto> Items, int TotalCount)> GetCountsAsync(
        Guid companyId,
        Guid? warehouseId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockCounts
            .Include(c => c.Warehouse)
            .Include(c => c.Lines)
            .Where(c => c.CompanyId == companyId);

        if (warehouseId.HasValue) query = query.Where(c => c.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status.Trim());

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(c => c.CountDateUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(c => new StockCountDto
            {
                Id = c.Id,
                CompanyId = c.CompanyId,
                BranchId = c.BranchId,
                WarehouseId = c.WarehouseId,
                WarehouseName = c.Warehouse!.Name,
                CountNumber = c.CountNumber,
                CountDateUtc = c.CountDateUtc,
                Status = c.Status,
                Notes = c.Notes,
                CreatedByUserId = c.CreatedByUserId,
                CountedByUserId = c.CountedByUserId,
                ApprovedByUserId = c.ApprovedByUserId,
                ApprovedAtUtc = c.ApprovedAtUtc,
                ReconciledAtUtc = c.ReconciledAtUtc,
                CreatedAtUtc = c.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<StockCountDto> GetCountByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var c = await dbContext.StockCounts
            .Include(c => c.Warehouse)
            .Include(c => c.Lines).ThenInclude(l => l.Product)
            .Include(c => c.Lines).ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock count session '{id}' was not found.");

        return new StockCountDto
        {
            Id = c.Id,
            CompanyId = c.CompanyId,
            BranchId = c.BranchId,
            WarehouseId = c.WarehouseId,
            WarehouseName = c.Warehouse!.Name,
            CountNumber = c.CountNumber,
            CountDateUtc = c.CountDateUtc,
            Status = c.Status,
            Notes = c.Notes,
            CreatedByUserId = c.CreatedByUserId,
            CountedByUserId = c.CountedByUserId,
            ApprovedByUserId = c.ApprovedByUserId,
            ApprovedAtUtc = c.ApprovedAtUtc,
            ReconciledAtUtc = c.ReconciledAtUtc,
            CreatedAtUtc = c.CreatedAtUtc,
            Lines = c.Lines.Select(l => new StockCountLineDto
            {
                Id = l.Id,
                StockCountId = l.StockCountId,
                ProductId = l.ProductId,
                ProductName = l.Product!.Name,
                Sku = l.Variant != null ? l.Variant.Sku : l.Product!.Sku,
                VariantId = l.VariantId,
                StorageLocationId = l.StorageLocationId,
                SystemQuantityOnHand = l.SystemQuantityOnHand,
                CountedQuantity = l.CountedQuantity,
                DiscrepancyQuantity = l.DiscrepancyQuantity,
                UnitCostPaise = l.UnitCostPaise,
                DiscrepancyValuePaise = l.DiscrepancyValuePaise,
                Notes = l.Notes
            }).ToList()
        };
    }

    public async Task<StockCountDto> CreateCountAsync(Guid companyId, Guid userId, CreateStockCountRequest request, CancellationToken cancellationToken = default)
    {
        var total = await dbContext.StockCounts.CountAsync(c => c.CompanyId == companyId, cancellationToken);
        var num = string.Format(CultureInfo.InvariantCulture, "CNT-{0:yyyyMMdd}-{1:D4}", DateTime.UtcNow, total + 1);

        var countId = Guid.CreateVersion7();
        var sc = new StockCountRecord
        {
            Id = countId,
            CompanyId = companyId,
            BranchId = request.BranchId,
            WarehouseId = request.WarehouseId,
            CountNumber = num,
            CountDateUtc = DateTimeOffset.UtcNow,
            Status = "InProgress",
            Notes = request.Notes?.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var balQuery = dbContext.StockBalances
            .Where(b => b.CompanyId == companyId && b.WarehouseId == request.WarehouseId);

        if (request.ProductIds is { Count: > 0 })
        {
            balQuery = balQuery.Where(b => request.ProductIds.Contains(b.ProductId));
        }

        var balances = await balQuery.ToListAsync(cancellationToken);
        foreach (var b in balances)
        {
            sc.Lines.Add(new StockCountLineRecord
            {
                Id = Guid.CreateVersion7(),
                StockCountId = countId,
                ProductId = b.ProductId,
                VariantId = b.VariantId,
                StorageLocationId = b.StorageLocationId,
                SystemQuantityOnHand = b.QuantityOnHand,
                CountedQuantity = b.QuantityOnHand,
                DiscrepancyQuantity = 0,
                UnitCostPaise = b.UnitCostPaise,
                DiscrepancyValuePaise = 0,
                Notes = null
            });
        }

        dbContext.StockCounts.Add(sc);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetCountByIdAsync(companyId, countId, cancellationToken);
    }

    public async Task<StockCountDto> RecordCountLinesAsync(Guid companyId, Guid id, Guid counterId, RecordStockCountLinesRequest request, CancellationToken cancellationToken = default)
    {
        var sc = await dbContext.StockCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock count session '{id}' was not found.");

        if (sc.Status == "Reconciled")
        {
            throw new InvalidOperationException("Cannot update lines of an already reconciled stock count.");
        }

        foreach (var entry in request.Lines)
        {
            var line = sc.Lines.FirstOrDefault(l =>
                l.ProductId == entry.ProductId &&
                l.VariantId == entry.VariantId &&
                l.StorageLocationId == entry.StorageLocationId);

            if (line is not null)
            {
                line.CountedQuantity = entry.CountedQuantity;
                line.DiscrepancyQuantity = entry.CountedQuantity - line.SystemQuantityOnHand;
                line.DiscrepancyValuePaise = (long)(line.DiscrepancyQuantity * line.UnitCostPaise);
                line.Notes = entry.Notes?.Trim();
            }
        }

        sc.CountedByUserId = counterId;
        sc.Status = "Submitted";
        sc.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCountByIdAsync(companyId, id, cancellationToken);
    }

    public async Task<StockCountDto> ReconcileCountAsync(Guid companyId, Guid id, Guid approverId, CancellationToken cancellationToken = default)
    {
        var sc = await dbContext.StockCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock count session '{id}' was not found.");

        if (sc.Status == "Reconciled")
        {
            return await GetCountByIdAsync(companyId, id, cancellationToken);
        }

        foreach (var line in sc.Lines)
        {
            if (line.DiscrepancyQuantity == 0) continue;

            var direction = line.DiscrepancyQuantity > 0 ? "In" : "Out";
            var mType = "CountReconcile";
            var absQty = Math.Abs(line.DiscrepancyQuantity);

            await InventoryLedgerService.ApplyStockMovementAsync(
                dbContext,
                companyId,
                sc.BranchId,
                sc.WarehouseId,
                line.ProductId,
                line.VariantId,
                line.StorageLocationId,
                mType,
                direction,
                absQty,
                line.UnitCostPaise,
                "StockCount",
                sc.Id,
                sc.CountNumber,
                $"Reconciliation discrepancy for line {line.Id}: {line.Notes}",
                approverId,
                null,
                null,
                cancellationToken);
        }

        sc.Status = "Reconciled";
        sc.ApprovedByUserId = approverId;
        sc.ApprovedAtUtc = DateTimeOffset.UtcNow;
        sc.ReconciledAtUtc = DateTimeOffset.UtcNow;
        sc.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCountByIdAsync(companyId, id, cancellationToken);
    }
}

// ==========================================
// 5. Purchase Order Service
// ==========================================

public sealed class PurchaseOrderService(MagicDbContext dbContext) : IPurchaseOrderService
{
    public async Task<(IReadOnlyList<PurchaseOrderDto> Items, int TotalCount)> GetPurchaseOrdersAsync(
        Guid companyId,
        Guid? supplierId,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Warehouse)
            .Where(po => po.CompanyId == companyId);

        if (supplierId.HasValue) query = query.Where(po => po.SupplierId == supplierId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(po => po.Status == status.Trim());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(po => EF.Functions.ILike(po.OrderNumber, $"%{term}%") ||
                                      EF.Functions.ILike(po.Supplier!.Name, $"%{term}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(po => po.OrderDateUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(po => new PurchaseOrderDto
            {
                Id = po.Id,
                CompanyId = po.CompanyId,
                BranchId = po.BranchId,
                WarehouseId = po.WarehouseId,
                WarehouseName = po.Warehouse!.Name,
                OrderNumber = po.OrderNumber,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier!.Name,
                OrderDateUtc = po.OrderDateUtc,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Status = po.Status,
                SubtotalPaise = po.SubtotalPaise,
                DiscountPaise = po.DiscountPaise,
                TaxableAmountPaise = po.TaxableAmountPaise,
                CgstPaise = po.CgstPaise,
                SgstPaise = po.SgstPaise,
                IgstPaise = po.IgstPaise,
                CessPaise = po.CessPaise,
                RoundOffPaise = po.RoundOffPaise,
                TotalAmountPaise = po.TotalAmountPaise,
                PaymentTerms = po.PaymentTerms,
                Notes = po.Notes,
                CreatedByUserId = po.CreatedByUserId,
                ApprovedByUserId = po.ApprovedByUserId,
                ApprovedAtUtc = po.ApprovedAtUtc,
                CreatedAtUtc = po.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<PurchaseOrderDto> GetPurchaseOrderByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var po = await dbContext.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Warehouse)
            .Include(po => po.Lines).ThenInclude(l => l.Product)
            .Include(po => po.Lines).ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(po => po.CompanyId == companyId && po.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{id}' was not found.");

        return new PurchaseOrderDto
        {
            Id = po.Id,
            CompanyId = po.CompanyId,
            BranchId = po.BranchId,
            WarehouseId = po.WarehouseId,
            WarehouseName = po.Warehouse!.Name,
            OrderNumber = po.OrderNumber,
            SupplierId = po.SupplierId,
            SupplierName = po.Supplier!.Name,
            OrderDateUtc = po.OrderDateUtc,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            Status = po.Status,
            SubtotalPaise = po.SubtotalPaise,
            DiscountPaise = po.DiscountPaise,
            TaxableAmountPaise = po.TaxableAmountPaise,
            CgstPaise = po.CgstPaise,
            SgstPaise = po.SgstPaise,
            IgstPaise = po.IgstPaise,
            CessPaise = po.CessPaise,
            RoundOffPaise = po.RoundOffPaise,
            TotalAmountPaise = po.TotalAmountPaise,
            PaymentTerms = po.PaymentTerms,
            Notes = po.Notes,
            CreatedByUserId = po.CreatedByUserId,
            ApprovedByUserId = po.ApprovedByUserId,
            ApprovedAtUtc = po.ApprovedAtUtc,
            CreatedAtUtc = po.CreatedAtUtc,
            Lines = po.Lines.Select(l => new PurchaseOrderLineDto
            {
                Id = l.Id,
                PurchaseOrderId = l.PurchaseOrderId,
                ProductId = l.ProductId,
                ProductName = l.Product!.Name,
                Sku = l.Variant != null ? l.Variant.Sku : l.Product!.Sku,
                VariantId = l.VariantId,
                OrderedQuantity = l.OrderedQuantity,
                ReceivedQuantity = l.ReceivedQuantity,
                CancelledQuantity = l.CancelledQuantity,
                UnitPricePaise = l.UnitPricePaise,
                DiscountPercentage = l.DiscountPercentage,
                DiscountPaise = l.DiscountPaise,
                TaxCodeId = l.TaxCodeId,
                GstRatePercentage = l.GstRatePercentage,
                TaxableAmountPaise = l.TaxableAmountPaise,
                CgstPaise = l.CgstPaise,
                SgstPaise = l.SgstPaise,
                IgstPaise = l.IgstPaise,
                TotalLineAmountPaise = l.TotalLineAmountPaise
            }).ToList()
        };
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(Guid companyId, Guid userId, CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        _ = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == request.SupplierId, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier '{request.SupplierId}' was not found.");

        var count = await dbContext.PurchaseOrders.CountAsync(po => po.CompanyId == companyId, cancellationToken);
        var orderNum = string.Format(CultureInfo.InvariantCulture, "PO-{0:yyyyMMdd}-{1:D4}", DateTime.UtcNow, count + 1);

        var poId = Guid.CreateVersion7();
        var po = new PurchaseOrderRecord
        {
            Id = poId,
            CompanyId = companyId,
            BranchId = request.BranchId,
            WarehouseId = request.WarehouseId,
            OrderNumber = orderNum,
            SupplierId = request.SupplierId,
            OrderDateUtc = DateTimeOffset.UtcNow,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Status = "Draft",
            PaymentTerms = string.IsNullOrWhiteSpace(request.PaymentTerms) ? "Net 30" : request.PaymentTerms.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        long subtotal = 0;
        long totalDiscount = 0;
        long totalTaxable = 0;
        long totalCgst = 0;
        long totalSgst = 0;
        long totalIgst = 0;

        foreach (var l in request.Lines)
        {
            var lineGross = (long)(l.Quantity * l.UnitPricePaise);
            var lineDisc = (long)(lineGross * (l.DiscountPercentage / 100m));
            var lineTaxable = lineGross - lineDisc;

            long cgst = 0, sgst = 0, igst = 0;
            var gstAmount = (long)(lineTaxable * (l.GstRatePercentage / 100m));
            cgst = gstAmount / 2;
            sgst = gstAmount - cgst;

            var lineTotal = lineTaxable + cgst + sgst + igst;

            subtotal += lineGross;
            totalDiscount += lineDisc;
            totalTaxable += lineTaxable;
            totalCgst += cgst;
            totalSgst += sgst;
            totalIgst += igst;

            po.Lines.Add(new PurchaseOrderLineRecord
            {
                Id = Guid.CreateVersion7(),
                PurchaseOrderId = poId,
                ProductId = l.ProductId,
                VariantId = l.VariantId,
                OrderedQuantity = l.Quantity,
                ReceivedQuantity = 0,
                CancelledQuantity = 0,
                UnitPricePaise = l.UnitPricePaise,
                DiscountPercentage = l.DiscountPercentage,
                DiscountPaise = lineDisc,
                TaxCodeId = l.TaxCodeId,
                GstRatePercentage = l.GstRatePercentage,
                TaxableAmountPaise = lineTaxable,
                CgstPaise = cgst,
                SgstPaise = sgst,
                IgstPaise = igst,
                TotalLineAmountPaise = lineTotal
            });
        }

        po.SubtotalPaise = subtotal;
        po.DiscountPaise = totalDiscount;
        po.TaxableAmountPaise = totalTaxable;
        po.CgstPaise = totalCgst;
        po.SgstPaise = totalSgst;
        po.IgstPaise = totalIgst;
        po.RoundOffPaise = 0;
        po.TotalAmountPaise = totalTaxable + totalCgst + totalSgst + totalIgst;

        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetPurchaseOrderByIdAsync(companyId, poId, cancellationToken);
    }

    public async Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(Guid companyId, Guid id, Guid approverId, CancellationToken cancellationToken = default)
    {
        var po = await dbContext.PurchaseOrders.FirstOrDefaultAsync(po => po.CompanyId == companyId && po.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{id}' was not found.");

        if (po.Status != "Draft" && po.Status != "Submitted")
        {
            throw new InvalidOperationException($"Cannot approve purchase order in status '{po.Status}'.");
        }

        po.Status = "Approved";
        po.ApprovedByUserId = approverId;
        po.ApprovedAtUtc = DateTimeOffset.UtcNow;
        po.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPurchaseOrderByIdAsync(companyId, id, cancellationToken);
    }

    public async Task<PurchaseOrderDto> CancelPurchaseOrderAsync(Guid companyId, Guid id, Guid userId, string? reason, CancellationToken cancellationToken = default)
    {
        var po = await dbContext.PurchaseOrders.FirstOrDefaultAsync(po => po.CompanyId == companyId && po.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{id}' was not found.");

        if (po.Status == "Completed" || po.Status == "Cancelled")
        {
            throw new InvalidOperationException($"Cannot cancel purchase order in status '{po.Status}'.");
        }

        po.Status = "Cancelled";
        po.CancelledByUserId = userId;
        po.CancelledAtUtc = DateTimeOffset.UtcNow;
        po.CancellationReason = reason?.Trim();
        po.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPurchaseOrderByIdAsync(companyId, id, cancellationToken);
    }
}

// ==========================================
// 6. Goods Receipt Service
// ==========================================

public sealed class GoodsReceiptService(MagicDbContext dbContext) : IGoodsReceiptService
{
    public async Task<(IReadOnlyList<GoodsReceiptNoteDto> Items, int TotalCount)> GetGoodsReceiptsAsync(
        Guid companyId,
        Guid? supplierId,
        Guid? purchaseOrderId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.GoodsReceiptNotes
            .Include(grn => grn.Supplier)
            .Include(grn => grn.Warehouse)
            .Include(grn => grn.PurchaseOrder)
            .Where(grn => grn.CompanyId == companyId);

        if (supplierId.HasValue) query = query.Where(grn => grn.SupplierId == supplierId.Value);
        if (purchaseOrderId.HasValue) query = query.Where(grn => grn.PurchaseOrderId == purchaseOrderId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(grn => grn.Status == status.Trim());

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(grn => grn.ReceiptDateUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(grn => new GoodsReceiptNoteDto
            {
                Id = grn.Id,
                CompanyId = grn.CompanyId,
                BranchId = grn.BranchId,
                WarehouseId = grn.WarehouseId,
                WarehouseName = grn.Warehouse!.Name,
                GrnNumber = grn.GrnNumber,
                PurchaseOrderId = grn.PurchaseOrderId,
                PurchaseOrderNumber = grn.PurchaseOrder != null ? grn.PurchaseOrder.OrderNumber : null,
                SupplierId = grn.SupplierId,
                SupplierName = grn.Supplier!.Name,
                SupplierInvoiceNumber = grn.SupplierInvoiceNumber,
                SupplierInvoiceDate = grn.SupplierInvoiceDate,
                ReceiptDateUtc = grn.ReceiptDateUtc,
                Status = grn.Status,
                Notes = grn.Notes,
                CreatedByUserId = grn.CreatedByUserId,
                ConfirmedByUserId = grn.ConfirmedByUserId,
                ConfirmedAtUtc = grn.ConfirmedAtUtc,
                CreatedAtUtc = grn.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<GoodsReceiptNoteDto> GetGoodsReceiptByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var grn = await dbContext.GoodsReceiptNotes
            .Include(g => g.Supplier)
            .Include(g => g.Warehouse)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Lines).ThenInclude(l => l.Product)
            .Include(g => g.Lines).ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(g => g.CompanyId == companyId && g.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods receipt note '{id}' was not found.");

        return new GoodsReceiptNoteDto
        {
            Id = grn.Id,
            CompanyId = grn.CompanyId,
            BranchId = grn.BranchId,
            WarehouseId = grn.WarehouseId,
            WarehouseName = grn.Warehouse!.Name,
            GrnNumber = grn.GrnNumber,
            PurchaseOrderId = grn.PurchaseOrderId,
            PurchaseOrderNumber = grn.PurchaseOrder?.OrderNumber,
            SupplierId = grn.SupplierId,
            SupplierName = grn.Supplier!.Name,
            SupplierInvoiceNumber = grn.SupplierInvoiceNumber,
            SupplierInvoiceDate = grn.SupplierInvoiceDate,
            ReceiptDateUtc = grn.ReceiptDateUtc,
            Status = grn.Status,
            Notes = grn.Notes,
            CreatedByUserId = grn.CreatedByUserId,
            ConfirmedByUserId = grn.ConfirmedByUserId,
            ConfirmedAtUtc = grn.ConfirmedAtUtc,
            CreatedAtUtc = grn.CreatedAtUtc,
            Lines = grn.Lines.Select(l => new GoodsReceiptLineDto
            {
                Id = l.Id,
                GoodsReceiptNoteId = l.GoodsReceiptNoteId,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                ProductId = l.ProductId,
                ProductName = l.Product!.Name,
                Sku = l.Variant != null ? l.Variant.Sku : l.Product!.Sku,
                VariantId = l.VariantId,
                StorageLocationId = l.StorageLocationId,
                ReceivedQuantity = l.ReceivedQuantity,
                AcceptedQuantity = l.AcceptedQuantity,
                RejectedQuantity = l.RejectedQuantity,
                DamagedQuantity = l.DamagedQuantity,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate,
                UnitPricePaise = l.UnitPricePaise,
                TaxableAmountPaise = l.TaxableAmountPaise,
                CgstPaise = l.CgstPaise,
                SgstPaise = l.SgstPaise,
                IgstPaise = l.IgstPaise,
                TotalLineAmountPaise = l.TotalLineAmountPaise,
                RejectionReason = l.RejectionReason
            }).ToList()
        };
    }

    public async Task<GoodsReceiptNoteDto> CreateGoodsReceiptAsync(Guid companyId, Guid userId, CreateGoodsReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var count = await dbContext.GoodsReceiptNotes.CountAsync(g => g.CompanyId == companyId, cancellationToken);
        var grnNum = string.Format(CultureInfo.InvariantCulture, "GRN-{0:yyyyMMdd}-{1:D4}", DateTime.UtcNow, count + 1);

        var grnId = Guid.CreateVersion7();
        var grn = new GoodsReceiptNoteRecord
        {
            Id = grnId,
            CompanyId = companyId,
            BranchId = request.BranchId,
            WarehouseId = request.WarehouseId,
            GrnNumber = grnNum,
            PurchaseOrderId = request.PurchaseOrderId,
            SupplierId = request.SupplierId,
            SupplierInvoiceNumber = request.SupplierInvoiceNumber?.Trim(),
            SupplierInvoiceDate = request.SupplierInvoiceDate,
            ReceiptDateUtc = DateTimeOffset.UtcNow,
            Status = "Draft",
            Notes = request.Notes?.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var l in request.Lines)
        {
            var lineTaxable = (long)(l.AcceptedQuantity * l.UnitPricePaise);
            var gstAmount = (long)(lineTaxable * (l.GstRatePercentage / 100m));
            var cgst = gstAmount / 2;
            var sgst = gstAmount - cgst;

            grn.Lines.Add(new GoodsReceiptLineRecord
            {
                Id = Guid.CreateVersion7(),
                GoodsReceiptNoteId = grnId,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                ProductId = l.ProductId,
                VariantId = l.VariantId,
                StorageLocationId = l.StorageLocationId,
                ReceivedQuantity = l.ReceivedQuantity,
                AcceptedQuantity = l.AcceptedQuantity,
                RejectedQuantity = l.RejectedQuantity,
                DamagedQuantity = l.DamagedQuantity,
                BatchNumber = l.BatchNumber?.Trim(),
                ExpiryDate = l.ExpiryDate,
                UnitPricePaise = l.UnitPricePaise,
                TaxableAmountPaise = lineTaxable,
                CgstPaise = cgst,
                SgstPaise = sgst,
                IgstPaise = 0,
                TotalLineAmountPaise = lineTaxable + cgst + sgst,
                RejectionReason = l.RejectionReason?.Trim()
            });
        }

        dbContext.GoodsReceiptNotes.Add(grn);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetGoodsReceiptByIdAsync(companyId, grnId, cancellationToken);
    }

    public async Task<GoodsReceiptNoteDto> ConfirmGoodsReceiptAsync(Guid companyId, Guid id, Guid confirmerId, CancellationToken cancellationToken = default)
    {
        var grn = await dbContext.GoodsReceiptNotes
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.CompanyId == companyId && g.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods receipt note '{id}' was not found.");

        if (grn.Status == "Confirmed")
        {
            return await GetGoodsReceiptByIdAsync(companyId, id, cancellationToken);
        }

        if (grn.Status != "Draft")
        {
            throw new InvalidOperationException($"Cannot confirm GRN in status '{grn.Status}'.");
        }

        foreach (var line in grn.Lines)
        {
            if (line.AcceptedQuantity > 0)
            {
                await InventoryLedgerService.ApplyStockMovementAsync(
                    dbContext,
                    companyId,
                    grn.BranchId,
                    grn.WarehouseId,
                    line.ProductId,
                    line.VariantId,
                    line.StorageLocationId,
                    "PurchaseReceipt",
                    "In",
                    line.AcceptedQuantity,
                    line.UnitPricePaise,
                    "GoodsReceipt",
                    grn.Id,
                    grn.GrnNumber,
                    $"Receipt against supplier invoice {grn.SupplierInvoiceNumber}",
                    confirmerId,
                    line.BatchNumber,
                    line.ExpiryDate,
                    cancellationToken);
            }

            if (line.PurchaseOrderLineId.HasValue)
            {
                var pol = await dbContext.PurchaseOrderLines.FirstOrDefaultAsync(p => p.Id == line.PurchaseOrderLineId.Value, cancellationToken);
                if (pol is not null)
                {
                    pol.ReceivedQuantity += line.AcceptedQuantity;
                }
            }
        }

        if (grn.PurchaseOrderId.HasValue)
        {
            var po = await dbContext.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == grn.PurchaseOrderId.Value, cancellationToken);

            if (po is not null)
            {
                var allComplete = po.Lines.All(l => l.ReceivedQuantity + l.CancelledQuantity >= l.OrderedQuantity);
                po.Status = allComplete ? "Completed" : "PartiallyReceived";
                po.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        grn.Status = "Confirmed";
        grn.ConfirmedByUserId = confirmerId;
        grn.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        grn.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetGoodsReceiptByIdAsync(companyId, id, cancellationToken);
    }
}

// ==========================================
// 7. Purchase Return Service
// ==========================================

public sealed class PurchaseReturnService(MagicDbContext dbContext) : IPurchaseReturnService
{
    public async Task<(IReadOnlyList<PurchaseReturnDto> Items, int TotalCount)> GetPurchaseReturnsAsync(
        Guid companyId,
        Guid? supplierId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PurchaseReturns
            .Include(pr => pr.Supplier)
            .Include(pr => pr.Warehouse)
            .Include(pr => pr.GoodsReceiptNote)
            .Where(pr => pr.CompanyId == companyId);

        if (supplierId.HasValue) query = query.Where(pr => pr.SupplierId == supplierId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(pr => pr.Status == status.Trim());

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(pr => pr.ReturnDateUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(pr => new PurchaseReturnDto
            {
                Id = pr.Id,
                CompanyId = pr.CompanyId,
                BranchId = pr.BranchId,
                WarehouseId = pr.WarehouseId,
                WarehouseName = pr.Warehouse!.Name,
                ReturnNumber = pr.ReturnNumber,
                GoodsReceiptNoteId = pr.GoodsReceiptNoteId,
                GrnNumber = pr.GoodsReceiptNote != null ? pr.GoodsReceiptNote.GrnNumber : null,
                SupplierId = pr.SupplierId,
                SupplierName = pr.Supplier!.Name,
                SupplierCreditNoteNumber = pr.SupplierCreditNoteNumber,
                ReturnDateUtc = pr.ReturnDateUtc,
                Status = pr.Status,
                TotalAmountPaise = pr.TotalAmountPaise,
                ReturnReason = pr.ReturnReason,
                Notes = pr.Notes,
                CreatedByUserId = pr.CreatedByUserId,
                ConfirmedByUserId = pr.ConfirmedByUserId,
                ConfirmedAtUtc = pr.ConfirmedAtUtc,
                CreatedAtUtc = pr.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<PurchaseReturnDto> GetPurchaseReturnByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseReturns
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.GoodsReceiptNote)
            .Include(p => p.Lines).ThenInclude(l => l.Product)
            .Include(p => p.Lines).ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase return '{id}' was not found.");

        return new PurchaseReturnDto
        {
            Id = pr.Id,
            CompanyId = pr.CompanyId,
            BranchId = pr.BranchId,
            WarehouseId = pr.WarehouseId,
            WarehouseName = pr.Warehouse!.Name,
            ReturnNumber = pr.ReturnNumber,
            GoodsReceiptNoteId = pr.GoodsReceiptNoteId,
            GrnNumber = pr.GoodsReceiptNote?.GrnNumber,
            SupplierId = pr.SupplierId,
            SupplierName = pr.Supplier!.Name,
            SupplierCreditNoteNumber = pr.SupplierCreditNoteNumber,
            ReturnDateUtc = pr.ReturnDateUtc,
            Status = pr.Status,
            TotalAmountPaise = pr.TotalAmountPaise,
            ReturnReason = pr.ReturnReason,
            Notes = pr.Notes,
            CreatedByUserId = pr.CreatedByUserId,
            ConfirmedByUserId = pr.ConfirmedByUserId,
            ConfirmedAtUtc = pr.ConfirmedAtUtc,
            CreatedAtUtc = pr.CreatedAtUtc,
            Lines = pr.Lines.Select(l => new PurchaseReturnLineDto
            {
                Id = l.Id,
                PurchaseReturnId = l.PurchaseReturnId,
                GoodsReceiptLineId = l.GoodsReceiptLineId,
                ProductId = l.ProductId,
                ProductName = l.Product!.Name,
                Sku = l.Variant != null ? l.Variant.Sku : l.Product!.Sku,
                VariantId = l.VariantId,
                StorageLocationId = l.StorageLocationId,
                ReturnQuantity = l.ReturnQuantity,
                UnitCostPaise = l.UnitCostPaise,
                TotalLineAmountPaise = l.TotalLineAmountPaise,
                Reason = l.Reason
            }).ToList()
        };
    }

    public async Task<PurchaseReturnDto> CreatePurchaseReturnAsync(Guid companyId, Guid userId, CreatePurchaseReturnRequest request, CancellationToken cancellationToken = default)
    {
        var count = await dbContext.PurchaseReturns.CountAsync(pr => pr.CompanyId == companyId, cancellationToken);
        var retNum = string.Format(CultureInfo.InvariantCulture, "PR-{0:yyyyMMdd}-{1:D4}", DateTime.UtcNow, count + 1);

        var prId = Guid.CreateVersion7();
        var pr = new PurchaseReturnRecord
        {
            Id = prId,
            CompanyId = companyId,
            BranchId = request.BranchId,
            WarehouseId = request.WarehouseId,
            ReturnNumber = retNum,
            GoodsReceiptNoteId = request.GoodsReceiptNoteId,
            SupplierId = request.SupplierId,
            SupplierCreditNoteNumber = request.SupplierCreditNoteNumber?.Trim(),
            ReturnDateUtc = DateTimeOffset.UtcNow,
            Status = "Draft",
            ReturnReason = request.ReturnReason?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        long totalAmount = 0;
        foreach (var l in request.Lines)
        {
            var lineTotal = (long)(l.ReturnQuantity * l.UnitCostPaise);
            totalAmount += lineTotal;

            pr.Lines.Add(new PurchaseReturnLineRecord
            {
                Id = Guid.CreateVersion7(),
                PurchaseReturnId = prId,
                GoodsReceiptLineId = l.GoodsReceiptLineId,
                ProductId = l.ProductId,
                VariantId = l.VariantId,
                StorageLocationId = l.StorageLocationId,
                ReturnQuantity = l.ReturnQuantity,
                UnitCostPaise = l.UnitCostPaise,
                TotalLineAmountPaise = lineTotal,
                Reason = l.Reason?.Trim()
            });
        }

        pr.TotalAmountPaise = totalAmount;
        dbContext.PurchaseReturns.Add(pr);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetPurchaseReturnByIdAsync(companyId, prId, cancellationToken);
    }

    public async Task<PurchaseReturnDto> ConfirmPurchaseReturnAsync(Guid companyId, Guid id, Guid confirmerId, CancellationToken cancellationToken = default)
    {
        var pr = await dbContext.PurchaseReturns
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase return '{id}' was not found.");

        if (pr.Status == "Confirmed")
        {
            return await GetPurchaseReturnByIdAsync(companyId, id, cancellationToken);
        }

        if (pr.Status != "Draft" && pr.Status != "Approved")
        {
            throw new InvalidOperationException($"Cannot confirm purchase return in status '{pr.Status}'.");
        }

        foreach (var line in pr.Lines)
        {
            await InventoryLedgerService.ApplyStockMovementAsync(
                dbContext,
                companyId,
                pr.BranchId,
                pr.WarehouseId,
                line.ProductId,
                line.VariantId,
                line.StorageLocationId,
                "PurchaseReturn",
                "Out",
                line.ReturnQuantity,
                line.UnitCostPaise,
                "PurchaseReturn",
                pr.Id,
                pr.ReturnNumber,
                line.Reason ?? pr.ReturnReason,
                confirmerId,
                null,
                null,
                cancellationToken);
        }

        pr.Status = "Confirmed";
        pr.ConfirmedByUserId = confirmerId;
        pr.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        pr.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPurchaseReturnByIdAsync(companyId, id, cancellationToken);
    }
}

// ==========================================
// 8. Stock Transfer Service
// ==========================================

public sealed class StockTransferService(MagicDbContext dbContext) : IStockTransferService
{
    public async Task<(IReadOnlyList<StockTransferDto> Items, int TotalCount)> GetTransfersAsync(
        Guid companyId,
        Guid? sourceWarehouseId,
        Guid? destinationWarehouseId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockTransfers
            .Include(st => st.SourceWarehouse)
            .Include(st => st.DestinationWarehouse)
            .Where(st => st.CompanyId == companyId);

        if (sourceWarehouseId.HasValue) query = query.Where(st => st.SourceWarehouseId == sourceWarehouseId.Value);
        if (destinationWarehouseId.HasValue) query = query.Where(st => st.DestinationWarehouseId == destinationWarehouseId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(st => st.Status == status.Trim());

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(st => st.TransferDateUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(st => new StockTransferDto
            {
                Id = st.Id,
                CompanyId = st.CompanyId,
                TransferNumber = st.TransferNumber,
                SourceBranchId = st.SourceBranchId,
                SourceWarehouseId = st.SourceWarehouseId,
                SourceWarehouseName = st.SourceWarehouse!.Name,
                DestinationBranchId = st.DestinationBranchId,
                DestinationWarehouseId = st.DestinationWarehouseId,
                DestinationWarehouseName = st.DestinationWarehouse!.Name,
                Status = st.Status,
                TransferDateUtc = st.TransferDateUtc,
                DispatchedAtUtc = st.DispatchedAtUtc,
                DispatchedByUserId = st.DispatchedByUserId,
                ReceivedAtUtc = st.ReceivedAtUtc,
                ReceivedByUserId = st.ReceivedByUserId,
                TrackingNumber = st.TrackingNumber,
                CarrierName = st.CarrierName,
                Notes = st.Notes,
                CreatedByUserId = st.CreatedByUserId,
                CreatedAtUtc = st.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<StockTransferDto> GetTransferByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        var st = await dbContext.StockTransfers
            .Include(s => s.SourceWarehouse)
            .Include(s => s.DestinationWarehouse)
            .Include(s => s.Lines).ThenInclude(l => l.Product)
            .Include(s => s.Lines).ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock transfer '{id}' was not found.");

        return new StockTransferDto
        {
            Id = st.Id,
            CompanyId = st.CompanyId,
            TransferNumber = st.TransferNumber,
            SourceBranchId = st.SourceBranchId,
            SourceWarehouseId = st.SourceWarehouseId,
            SourceWarehouseName = st.SourceWarehouse!.Name,
            DestinationBranchId = st.DestinationBranchId,
            DestinationWarehouseId = st.DestinationWarehouseId,
            DestinationWarehouseName = st.DestinationWarehouse!.Name,
            Status = st.Status,
            TransferDateUtc = st.TransferDateUtc,
            DispatchedAtUtc = st.DispatchedAtUtc,
            DispatchedByUserId = st.DispatchedByUserId,
            ReceivedAtUtc = st.ReceivedAtUtc,
            ReceivedByUserId = st.ReceivedByUserId,
            TrackingNumber = st.TrackingNumber,
            CarrierName = st.CarrierName,
            Notes = st.Notes,
            CreatedByUserId = st.CreatedByUserId,
            CreatedAtUtc = st.CreatedAtUtc,
            Lines = st.Lines.Select(l => new StockTransferLineDto
            {
                Id = l.Id,
                StockTransferId = l.StockTransferId,
                ProductId = l.ProductId,
                ProductName = l.Product!.Name,
                Sku = l.Variant != null ? l.Variant.Sku : l.Product!.Sku,
                VariantId = l.VariantId,
                SourceStorageLocationId = l.SourceStorageLocationId,
                DestinationStorageLocationId = l.DestinationStorageLocationId,
                RequestedQuantity = l.RequestedQuantity,
                DispatchedQuantity = l.DispatchedQuantity,
                ReceivedQuantity = l.ReceivedQuantity,
                DamagedQuantity = l.DamagedQuantity,
                MissingQuantity = l.MissingQuantity,
                UnitCostPaise = l.UnitCostPaise
            }).ToList()
        };
    }

    public async Task<StockTransferDto> CreateTransferAsync(Guid companyId, Guid userId, CreateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceWarehouseId == request.DestinationWarehouseId)
        {
            throw new InvalidOperationException("Source and destination warehouses cannot be the same.");
        }

        var count = await dbContext.StockTransfers.CountAsync(st => st.CompanyId == companyId, cancellationToken);
        var trnNum = string.Format(CultureInfo.InvariantCulture, "TRN-{0:yyyyMMdd}-{1:D4}", DateTime.UtcNow, count + 1);

        var trnId = Guid.CreateVersion7();
        var st = new StockTransferRecord
        {
            Id = trnId,
            CompanyId = companyId,
            TransferNumber = trnNum,
            SourceBranchId = request.SourceBranchId,
            SourceWarehouseId = request.SourceWarehouseId,
            DestinationBranchId = request.DestinationBranchId,
            DestinationWarehouseId = request.DestinationWarehouseId,
            Status = "Draft",
            TransferDateUtc = DateTimeOffset.UtcNow,
            TrackingNumber = request.TrackingNumber?.Trim(),
            CarrierName = request.CarrierName?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var l in request.Lines)
        {
            var bal = await dbContext.StockBalances.FirstOrDefaultAsync(b =>
                b.CompanyId == companyId &&
                b.WarehouseId == request.SourceWarehouseId &&
                b.ProductId == l.ProductId &&
                b.VariantId == l.VariantId, cancellationToken);

            var cost = bal?.UnitCostPaise ?? 0;

            st.Lines.Add(new StockTransferLineRecord
            {
                Id = Guid.CreateVersion7(),
                StockTransferId = trnId,
                ProductId = l.ProductId,
                VariantId = l.VariantId,
                SourceStorageLocationId = l.SourceStorageLocationId,
                DestinationStorageLocationId = l.DestinationStorageLocationId,
                RequestedQuantity = l.RequestedQuantity,
                DispatchedQuantity = 0,
                ReceivedQuantity = 0,
                DamagedQuantity = 0,
                MissingQuantity = 0,
                UnitCostPaise = cost
            });
        }

        dbContext.StockTransfers.Add(st);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetTransferByIdAsync(companyId, trnId, cancellationToken);
    }

    public async Task<StockTransferDto> DispatchTransferAsync(Guid companyId, Guid id, Guid dispatcherId, CancellationToken cancellationToken = default)
    {
        var st = await dbContext.StockTransfers
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock transfer '{id}' was not found.");

        if (st.Status == "Dispatched" || st.Status == "Received")
        {
            throw new InvalidOperationException($"Transfer is already in '{st.Status}' state.");
        }

        foreach (var line in st.Lines)
        {
            line.DispatchedQuantity = line.RequestedQuantity;

            await InventoryLedgerService.ApplyStockMovementAsync(
                dbContext,
                companyId,
                st.SourceBranchId,
                st.SourceWarehouseId,
                line.ProductId,
                line.VariantId,
                line.SourceStorageLocationId,
                "TransferOut",
                "Out",
                line.DispatchedQuantity,
                line.UnitCostPaise,
                "StockTransfer",
                st.Id,
                st.TransferNumber,
                $"Dispatch transfer to warehouse {st.DestinationWarehouseId}",
                dispatcherId,
                null,
                null,
                cancellationToken);
        }

        st.Status = "Dispatched";
        st.DispatchedByUserId = dispatcherId;
        st.DispatchedAtUtc = DateTimeOffset.UtcNow;
        st.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetTransferByIdAsync(companyId, id, cancellationToken);
    }

    public async Task<StockTransferDto> ReceiveTransferAsync(Guid companyId, Guid id, Guid receiverId, ReceiveStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        var st = await dbContext.StockTransfers
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock transfer '{id}' was not found.");

        if (st.Status != "Dispatched")
        {
            throw new InvalidOperationException($"Cannot receive transfer in status '{st.Status}'. Must be 'Dispatched'.");
        }

        foreach (var entry in request.Lines)
        {
            var line = st.Lines.FirstOrDefault(l => l.Id == entry.LineId)
                ?? throw new KeyNotFoundException($"Transfer line '{entry.LineId}' was not found.");

            line.ReceivedQuantity = entry.ReceivedQuantity;
            line.DamagedQuantity = entry.DamagedQuantity;
            line.MissingQuantity = entry.MissingQuantity;
            if (entry.DestinationStorageLocationId.HasValue)
            {
                line.DestinationStorageLocationId = entry.DestinationStorageLocationId;
            }

            if (line.ReceivedQuantity > 0)
            {
                await InventoryLedgerService.ApplyStockMovementAsync(
                    dbContext,
                    companyId,
                    st.DestinationBranchId,
                    st.DestinationWarehouseId,
                    line.ProductId,
                    line.VariantId,
                    line.DestinationStorageLocationId,
                    "TransferIn",
                    "In",
                    line.ReceivedQuantity,
                    line.UnitCostPaise,
                    "StockTransfer",
                    st.Id,
                    st.TransferNumber,
                    $"Receipt transfer from warehouse {st.SourceWarehouseId}",
                    receiverId,
                    null,
                    null,
                    cancellationToken);
            }
        }

        st.Status = "Received";
        st.ReceivedByUserId = receiverId;
        st.ReceivedAtUtc = DateTimeOffset.UtcNow;
        st.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetTransferByIdAsync(companyId, id, cancellationToken);
    }

    public async Task<StockTransferDto> CancelTransferAsync(Guid companyId, Guid id, Guid userId, string? reason, CancellationToken cancellationToken = default)
    {
        var st = await dbContext.StockTransfers.FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Stock transfer '{id}' was not found.");

        if (st.Status == "Dispatched" || st.Status == "Received")
        {
            throw new InvalidOperationException($"Cannot cancel transfer that has already been {st.Status.ToLower(CultureInfo.InvariantCulture)}.");
        }

        st.Status = "Cancelled";
        st.Notes = (st.Notes + $" [Cancelled: {reason}]").Trim();
        st.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetTransferByIdAsync(companyId, id, cancellationToken);
    }
}

// ==========================================
// 9. Reorder Management Service
// ==========================================

public sealed class ReorderService(MagicDbContext dbContext) : IReorderService
{
    public async Task<(IReadOnlyList<ReorderRuleDto> Items, int TotalCount)> GetReorderRulesAsync(
        Guid companyId,
        Guid? warehouseId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ReorderRules
            .Include(rr => rr.Product)
            .Include(rr => rr.Warehouse)
            .Include(rr => rr.PreferredSupplier)
            .Where(rr => rr.CompanyId == companyId);

        if (warehouseId.HasValue) query = query.Where(rr => rr.WarehouseId == warehouseId.Value);

        var total = await query.CountAsync(cancellationToken);
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderBy(rr => rr.Product!.Name)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(rr => new ReorderRuleDto
            {
                Id = rr.Id,
                CompanyId = rr.CompanyId,
                BranchId = rr.BranchId,
                WarehouseId = rr.WarehouseId,
                WarehouseName = rr.Warehouse != null ? rr.Warehouse.Name : null,
                ProductId = rr.ProductId,
                ProductName = rr.Product!.Name,
                Sku = rr.Product!.Sku,
                VariantId = rr.VariantId,
                PreferredSupplierId = rr.PreferredSupplierId,
                PreferredSupplierName = rr.PreferredSupplier != null ? rr.PreferredSupplier.Name : null,
                MinStockLevel = rr.MinStockLevel,
                MaxStockLevel = rr.MaxStockLevel,
                ReorderPoint = rr.ReorderPoint,
                SafetyStock = rr.SafetyStock,
                LeadTimeDays = rr.LeadTimeDays,
                ReorderQuantity = rr.ReorderQuantity,
                IsActive = rr.IsActive,
                CreatedAtUtc = rr.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<ReorderRuleDto> UpsertReorderRuleAsync(Guid companyId, UpsertReorderRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.ReorderRules
            .Include(r => r.Product)
            .Include(r => r.Warehouse)
            .Include(r => r.PreferredSupplier)
            .FirstOrDefaultAsync(r =>
                r.CompanyId == companyId &&
                r.ProductId == request.ProductId &&
                r.WarehouseId == request.WarehouseId &&
                r.VariantId == request.VariantId, cancellationToken);

        if (rule is null)
        {
            rule = new ReorderRuleRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                BranchId = request.BranchId,
                WarehouseId = request.WarehouseId,
                ProductId = request.ProductId,
                VariantId = request.VariantId,
                PreferredSupplierId = request.PreferredSupplierId,
                MinStockLevel = request.MinStockLevel,
                MaxStockLevel = request.MaxStockLevel,
                ReorderPoint = request.ReorderPoint,
                SafetyStock = request.SafetyStock,
                LeadTimeDays = request.LeadTimeDays,
                ReorderQuantity = request.ReorderQuantity,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.ReorderRules.Add(rule);
        }
        else
        {
            rule.PreferredSupplierId = request.PreferredSupplierId;
            rule.MinStockLevel = request.MinStockLevel;
            rule.MaxStockLevel = request.MaxStockLevel;
            rule.ReorderPoint = request.ReorderPoint;
            rule.SafetyStock = request.SafetyStock;
            rule.LeadTimeDays = request.LeadTimeDays;
            rule.ReorderQuantity = request.ReorderQuantity;
            rule.IsActive = request.IsActive;
            rule.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var p = await dbContext.Products.FirstAsync(p => p.Id == request.ProductId, cancellationToken);
        var w = request.WarehouseId.HasValue ? await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId.Value, cancellationToken) : null;
        var s = request.PreferredSupplierId.HasValue ? await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == request.PreferredSupplierId.Value, cancellationToken) : null;

        return new ReorderRuleDto
        {
            Id = rule.Id,
            CompanyId = rule.CompanyId,
            BranchId = rule.BranchId,
            WarehouseId = rule.WarehouseId,
            WarehouseName = w?.Name,
            ProductId = rule.ProductId,
            ProductName = p.Name,
            Sku = p.Sku,
            VariantId = rule.VariantId,
            PreferredSupplierId = rule.PreferredSupplierId,
            PreferredSupplierName = s?.Name,
            MinStockLevel = rule.MinStockLevel,
            MaxStockLevel = rule.MaxStockLevel,
            ReorderPoint = rule.ReorderPoint,
            SafetyStock = rule.SafetyStock,
            LeadTimeDays = rule.LeadTimeDays,
            ReorderQuantity = rule.ReorderQuantity,
            IsActive = rule.IsActive,
            CreatedAtUtc = rule.CreatedAtUtc
        };
    }

    public async Task<IReadOnlyList<ReorderSuggestionDto>> GetReorderSuggestionsAsync(Guid companyId, Guid? warehouseId, CancellationToken cancellationToken = default)
    {
        var rulesQuery = dbContext.ReorderRules
            .Include(rr => rr.Product)
            .Include(rr => rr.Warehouse)
            .Include(rr => rr.PreferredSupplier)
            .Where(rr => rr.CompanyId == companyId && rr.IsActive);

        if (warehouseId.HasValue)
        {
            rulesQuery = rulesQuery.Where(rr => rr.WarehouseId == warehouseId.Value || rr.WarehouseId == null);
        }

        var rules = await rulesQuery.ToListAsync(cancellationToken);
        var suggestions = new List<ReorderSuggestionDto>();

        foreach (var rule in rules)
        {
            var balQuery = dbContext.StockBalances
                .Where(b => b.CompanyId == companyId && b.ProductId == rule.ProductId);

            if (rule.WarehouseId.HasValue)
            {
                balQuery = balQuery.Where(b => b.WarehouseId == rule.WarehouseId.Value);
            }

            var balances = await balQuery.ToListAsync(cancellationToken);
            var onHand = balances.Sum(b => b.QuantityOnHand);
            var available = balances.Sum(b => b.QuantityAvailable);

            if (available <= rule.ReorderPoint)
            {
                var suggestedQty = rule.ReorderQuantity > 0 ? rule.ReorderQuantity : Math.Max(0, rule.MaxStockLevel - available);
                var urgency = available <= rule.SafetyStock ? "Critical" : "Low";

                suggestions.Add(new ReorderSuggestionDto
                {
                    ProductId = rule.ProductId,
                    ProductName = rule.Product!.Name,
                    Sku = rule.Product!.Sku,
                    WarehouseId = rule.WarehouseId,
                    WarehouseName = rule.Warehouse?.Name,
                    CurrentStockOnHand = onHand,
                    CurrentStockAvailable = available,
                    ReorderPoint = rule.ReorderPoint,
                    SuggestedOrderQuantity = suggestedQty,
                    PreferredSupplierId = rule.PreferredSupplierId,
                    PreferredSupplierName = rule.PreferredSupplier?.Name,
                    LeadTimeDays = rule.LeadTimeDays,
                    UrgencyLevel = urgency
                });
            }
        }

        return suggestions;
    }
}
