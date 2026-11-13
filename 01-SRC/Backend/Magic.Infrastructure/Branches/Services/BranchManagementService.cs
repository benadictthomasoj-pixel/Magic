using Magic.Application.Branches.Services;
using Magic.Contracts.Branches;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Magic.Infrastructure.Branches.Services;

public sealed class BranchManagementService(MagicDbContext dbContext) : IBranchManagementService
{
    public async Task<CompanyProfileDto> GetCompanyProfileAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company == null)
        {
            // Auto-provision if missing
            company = new CompanyRecord
            {
                Id = companyId,
                LegalName = "MAGIC Universal Enterprises Ltd",
                TradeName = "MAGIC Retail & Solutions",
                Gstin = "29AAAAA0000A1Z5",
                Pan = "AAAAA0000A",
                Address = "100 Innovation Boulevard, Tech Park, Bangalore 560001",
                Phone = "+91 80 4000 1000",
                Email = "admin@magic-erp.local",
                CurrencyCode = "INR",
                FiscalYearStartMonth = 4,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.Companies.Add(company);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new CompanyProfileDto(
            company.Id,
            company.LegalName,
            company.TradeName,
            company.Gstin,
            company.Pan,
            company.Address,
            company.Phone,
            company.Email,
            company.CurrencyCode,
            company.FiscalYearStartMonth,
            company.IsActive,
            company.CreatedAtUtc);
    }

    public async Task<CompanyProfileDto> UpdateCompanyProfileAsync(Guid companyId, UpdateCompanyProfileRequest request, CancellationToken cancellationToken = default)
    {
        var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new DomainException("COMPANY_NOT_FOUND", "Company not found.", StatusCodes.Status404NotFound);

        company.LegalName = request.LegalName;
        company.TradeName = request.TradeName;
        company.Gstin = request.Gstin;
        company.Pan = request.Pan;
        company.Address = request.Address;
        company.Phone = request.Phone;
        company.Email = request.Email;
        company.FiscalYearStartMonth = request.FiscalYearStartMonth;
        company.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCompanyProfileAsync(companyId, cancellationToken);
    }

    public async Task<IReadOnlyList<BranchDto>> GetBranchesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var branches = await dbContext.Branches.AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.BranchCode)
            .ToListAsync(cancellationToken);

        var list = new List<BranchDto>();
        foreach (var b in branches)
        {
            var whCount = await dbContext.WarehouseBranchAssignments.CountAsync(w => w.CompanyId == companyId && w.BranchId == b.Id, cancellationToken);
            var termCount = await dbContext.TerminalRegistrations.CountAsync(t => t.CompanyId == companyId && t.BranchId == b.Id, cancellationToken);

            list.Add(new BranchDto(
                b.Id,
                b.CompanyId,
                b.BranchCode,
                b.BranchName,
                b.Address,
                b.Phone,
                b.Email,
                b.Gstin,
                b.BranchManager,
                b.IsActive,
                whCount,
                termCount,
                b.CreatedAtUtc));
        }

        return list;
    }

    public async Task<BranchDto> GetBranchByIdAsync(Guid companyId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var b = await dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == branchId && x.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BRANCH_NOT_FOUND", "Branch not found or belongs to another company.", StatusCodes.Status404NotFound);

        var whCount = await dbContext.WarehouseBranchAssignments.CountAsync(w => w.CompanyId == companyId && w.BranchId == b.Id, cancellationToken);
        var termCount = await dbContext.TerminalRegistrations.CountAsync(t => t.CompanyId == companyId && t.BranchId == b.Id, cancellationToken);

        return new BranchDto(
            b.Id,
            b.CompanyId,
            b.BranchCode,
            b.BranchName,
            b.Address,
            b.Phone,
            b.Email,
            b.Gstin,
            b.BranchManager,
            b.IsActive,
            whCount,
            termCount,
            b.CreatedAtUtc);
    }

    public async Task<BranchDto> CreateBranchAsync(Guid companyId, CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Branches.AnyAsync(b => b.CompanyId == companyId && b.BranchCode == request.BranchCode, cancellationToken);
        if (exists)
            throw new DomainException("DUPLICATE_BRANCH_CODE", $"Branch with code {request.BranchCode} already exists.", StatusCodes.Status400BadRequest);

        var branch = new BranchRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchCode = request.BranchCode.Trim().ToUpperInvariant(),
            BranchName = request.BranchName.Trim(),
            Address = request.Address.Trim(),
            Phone = request.Phone,
            Email = request.Email,
            Gstin = request.Gstin,
            BranchManager = request.BranchManager,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Branches.Add(branch);

        if (request.WarehouseIds != null)
        {
            foreach (var whId in request.WarehouseIds)
            {
                dbContext.WarehouseBranchAssignments.Add(new WarehouseBranchAssignmentRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BranchId = branch.Id,
                    WarehouseId = whId,
                    IsPrimary = false,
                    AssignedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetBranchByIdAsync(companyId, branch.Id, cancellationToken);
    }

    public async Task<BranchDto> UpdateBranchAsync(Guid companyId, Guid branchId, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == branchId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BRANCH_NOT_FOUND", "Branch not found.", StatusCodes.Status404NotFound);

        branch.BranchName = request.BranchName.Trim();
        branch.Address = request.Address.Trim();
        branch.Phone = request.Phone;
        branch.Email = request.Email;
        branch.Gstin = request.Gstin;
        branch.BranchManager = request.BranchManager;
        branch.IsActive = request.IsActive;
        branch.UpdatedAtUtc = DateTime.UtcNow;

        if (request.WarehouseIds != null)
        {
            var existingAssignments = await dbContext.WarehouseBranchAssignments
                .Where(w => w.CompanyId == companyId && w.BranchId == branchId)
                .ToListAsync(cancellationToken);
            dbContext.WarehouseBranchAssignments.RemoveRange(existingAssignments);

            foreach (var whId in request.WarehouseIds)
            {
                dbContext.WarehouseBranchAssignments.Add(new WarehouseBranchAssignmentRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BranchId = branch.Id,
                    WarehouseId = whId,
                    IsPrimary = false,
                    AssignedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetBranchByIdAsync(companyId, branch.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<TerminalDetailsDto>> GetTerminalsAsync(Guid companyId, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.TerminalRegistrations.AsNoTracking().Where(t => t.CompanyId == companyId);
        if (branchId.HasValue) query = query.Where(t => t.BranchId == branchId.Value);

        var terminals = await query.ToListAsync(cancellationToken);
        var list = new List<TerminalDetailsDto>();

        foreach (var t in terminals)
        {
            var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == t.BranchId, cancellationToken);
            string? cashierName = null;
            if (t.AssignedCashierUserId.HasValue)
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == t.AssignedCashierUserId.Value, cancellationToken);
                cashierName = user?.DisplayName;
            }

            list.Add(new TerminalDetailsDto(
                t.Id,
                t.CompanyId,
                t.BranchId,
                branch?.BranchName ?? "Central Store",
                t.TerminalCode,
                t.MachineName,
                t.OperatingSystem,
                cashierName,
                t.IsAuthorized,
                t.RegisteredAtUtc,
                t.LastHeartbeatAtUtc,
                t.PosPrinterModel,
                t.ReceiptWidthMm));
        }

        return list;
    }

    public async Task<TerminalDetailsDto> RegisterTerminalAsync(Guid companyId, RegisterTerminalRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId && b.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("BRANCH_NOT_FOUND", "Branch not found.", StatusCodes.Status404NotFound);

        var exists = await dbContext.TerminalRegistrations.AnyAsync(t => t.CompanyId == companyId && t.TerminalCode == request.TerminalCode, cancellationToken);
        if (exists)
            throw new DomainException("DUPLICATE_TERMINAL_CODE", $"Terminal {request.TerminalCode} already registered.", StatusCodes.Status400BadRequest);

        var terminal = new TerminalRegistrationRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branch.Id,
            TerminalCode = request.TerminalCode.Trim().ToUpperInvariant(),
            MachineName = request.MachineName.Trim(),
            OperatingSystem = request.OperatingSystem,
            PosPrinterModel = request.PosPrinterModel,
            ReceiptWidthMm = request.ReceiptWidthMm > 0 ? request.ReceiptWidthMm : 80,
            IsAuthorized = true,
            RegisteredAtUtc = DateTime.UtcNow,
            LastHeartbeatAtUtc = DateTime.UtcNow
        };

        dbContext.TerminalRegistrations.Add(terminal);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TerminalDetailsDto(
            terminal.Id,
            terminal.CompanyId,
            terminal.BranchId,
            branch.BranchName,
            terminal.TerminalCode,
            terminal.MachineName,
            terminal.OperatingSystem,
            null,
            terminal.IsAuthorized,
            terminal.RegisteredAtUtc,
            terminal.LastHeartbeatAtUtc,
            terminal.PosPrinterModel,
            terminal.ReceiptWidthMm);
    }

    public async Task<TerminalDetailsDto> UpdateTerminalStatusAsync(Guid companyId, Guid terminalId, UpdateTerminalStatusRequest request, CancellationToken cancellationToken = default)
    {
        var terminal = await dbContext.TerminalRegistrations.FirstOrDefaultAsync(t => t.Id == terminalId && t.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("TERMINAL_NOT_FOUND", "Terminal not found.", StatusCodes.Status404NotFound);

        terminal.IsAuthorized = request.IsAuthorized;
        terminal.AssignedCashierUserId = request.AssignedCashierUserId;
        terminal.LastHeartbeatAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == terminal.BranchId, cancellationToken);
        string? cashierName = null;
        if (terminal.AssignedCashierUserId.HasValue)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == terminal.AssignedCashierUserId.Value, cancellationToken);
            cashierName = user?.DisplayName;
        }

        return new TerminalDetailsDto(
            terminal.Id,
            terminal.CompanyId,
            terminal.BranchId,
            branch?.BranchName ?? "Store",
            terminal.TerminalCode,
            terminal.MachineName,
            terminal.OperatingSystem,
            cashierName,
            terminal.IsAuthorized,
            terminal.RegisteredAtUtc,
            terminal.LastHeartbeatAtUtc,
            terminal.PosPrinterModel,
            terminal.ReceiptWidthMm);
    }

    public async Task<IReadOnlyList<InterBranchTransferRequestDto>> GetInterBranchTransfersAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var transfers = await dbContext.InterBranchTransfers.AsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var list = new List<InterBranchTransferRequestDto>();
        foreach (var t in transfers)
        {
            var srcBranch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == t.SourceBranchId, cancellationToken);
            var dstBranch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == t.DestinationBranchId, cancellationToken);
            var srcWh = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == t.SourceWarehouseId, cancellationToken);
            var dstWh = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == t.DestinationWarehouseId, cancellationToken);

            list.Add(new InterBranchTransferRequestDto(
                t.Id,
                t.CompanyId,
                t.TransferNumber,
                t.SourceBranchId,
                srcBranch?.BranchName ?? "Source Branch",
                t.SourceWarehouseId,
                srcWh?.Name ?? "Source Warehouse",
                t.DestinationBranchId,
                dstBranch?.BranchName ?? "Dest Branch",
                t.DestinationWarehouseId,
                dstWh?.Name ?? "Dest Warehouse",
                t.Status,
                t.Notes,
                t.CreatedAtUtc));
        }

        return list;
    }

    public async Task<InterBranchTransferRequestDto> CreateInterBranchTransferAsync(Guid companyId, Guid userId, CreateInterBranchTransferRequest request, CancellationToken cancellationToken = default)
    {
        var count = await dbContext.InterBranchTransfers.CountAsync(t => t.CompanyId == companyId, cancellationToken);
        var transferNumber = $"IBT-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        var transfer = new InterBranchTransferRequestRecord
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            TransferNumber = transferNumber,
            SourceBranchId = request.SourceBranchId,
            SourceWarehouseId = request.SourceWarehouseId,
            DestinationBranchId = request.DestinationBranchId,
            DestinationWarehouseId = request.DestinationWarehouseId,
            Status = "Requested",
            Notes = request.Notes,
            RequestedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var line in request.Lines)
        {
            transfer.Lines.Add(new InterBranchTransferLineRecord
            {
                Id = Guid.NewGuid(),
                TransferRequestId = transfer.Id,
                ProductId = line.ProductId,
                VariantId = line.VariantId,
                Quantity = line.Quantity,
                DispatchedQuantity = 0,
                ReceivedQuantity = 0
            });
        }

        dbContext.InterBranchTransfers.Add(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);

        var srcBranch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == transfer.SourceBranchId, cancellationToken);
        var dstBranch = await dbContext.Branches.FirstOrDefaultAsync(b => b.Id == transfer.DestinationBranchId, cancellationToken);
        var srcWh = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == transfer.SourceWarehouseId, cancellationToken);
        var dstWh = await dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == transfer.DestinationWarehouseId, cancellationToken);

        return new InterBranchTransferRequestDto(
            transfer.Id,
            transfer.CompanyId,
            transfer.TransferNumber,
            transfer.SourceBranchId,
            srcBranch?.BranchName ?? "Source",
            transfer.SourceWarehouseId,
            srcWh?.Name ?? "Source WH",
            transfer.DestinationBranchId,
            dstBranch?.BranchName ?? "Dest",
            transfer.DestinationWarehouseId,
            dstWh?.Name ?? "Dest WH",
            transfer.Status,
            transfer.Notes,
            transfer.CreatedAtUtc);
    }

    public async Task<InterBranchTransferRequestDto> ApproveInterBranchTransferAsync(Guid companyId, Guid transferId, Guid userId, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.InterBranchTransfers.FirstOrDefaultAsync(t => t.Id == transferId && t.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("TRANSFER_NOT_FOUND", "Transfer request not found.", StatusCodes.Status404NotFound);

        if (transfer.Status != "Requested")
            throw new DomainException("INVALID_STATE", $"Cannot approve transfer in {transfer.Status} status.", StatusCodes.Status400BadRequest);

        transfer.Status = "Approved";
        transfer.ApprovedByUserId = userId;
        transfer.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetInterBranchTransfersAsync(companyId, cancellationToken)).First(t => t.Id == transferId);
    }

    public async Task<InterBranchTransferRequestDto> DispatchInterBranchTransferAsync(Guid companyId, Guid transferId, Guid userId, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.InterBranchTransfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("TRANSFER_NOT_FOUND", "Transfer request not found.", StatusCodes.Status404NotFound);

        if (transfer.Status != "Approved")
            throw new DomainException("INVALID_STATE", $"Cannot dispatch transfer in {transfer.Status} status.", StatusCodes.Status400BadRequest);

        // Deduct from Source Warehouse
        foreach (var line in transfer.Lines)
        {
            var stock = await dbContext.StockBalances.FirstOrDefaultAsync(s => s.WarehouseId == transfer.SourceWarehouseId && s.ProductId == line.ProductId, cancellationToken);
            if (stock == null || stock.QuantityOnHand < line.Quantity)
            {
                throw new DomainException("INSUFFICIENT_STOCK", $"Insufficient stock in source warehouse for product {line.ProductId}.", StatusCodes.Status400BadRequest);
            }

            stock.QuantityOnHand -= line.Quantity;
            stock.QuantityAvailable = stock.QuantityOnHand - stock.QuantityReserved;
            stock.LastMovementAtUtc = DateTimeOffset.UtcNow;
            line.DispatchedQuantity = line.Quantity;

            dbContext.StockMovements.Add(new StockMovementRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = transfer.SourceBranchId,
                WarehouseId = transfer.SourceWarehouseId,
                ProductId = line.ProductId,
                MovementType = "TransferOut",
                Direction = "Out",
                ReferenceType = "InterBranchTransfer",
                ReferenceId = transfer.Id,
                ReferenceNumber = transfer.TransferNumber,
                Quantity = line.Quantity,
                BalanceAfterOnHand = stock.QuantityOnHand,
                PerformedByUserId = userId,
                MovementDateUtc = DateTimeOffset.UtcNow,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Reason = $"Inter-branch transfer to {transfer.DestinationBranchId}"
            });
        }

        transfer.Status = "Dispatched";
        transfer.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetInterBranchTransfersAsync(companyId, cancellationToken)).First(t => t.Id == transferId);
    }

    public async Task<InterBranchTransferRequestDto> ReceiveInterBranchTransferAsync(Guid companyId, Guid transferId, Guid userId, CancellationToken cancellationToken = default)
    {
        var transfer = await dbContext.InterBranchTransfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == transferId && t.CompanyId == companyId, cancellationToken)
            ?? throw new DomainException("TRANSFER_NOT_FOUND", "Transfer request not found.", StatusCodes.Status404NotFound);

        if (transfer.Status != "Dispatched")
            throw new DomainException("INVALID_STATE", $"Cannot receive transfer in {transfer.Status} status.", StatusCodes.Status400BadRequest);

        // Add to Destination Warehouse
        foreach (var line in transfer.Lines)
        {
            var stock = await dbContext.StockBalances.FirstOrDefaultAsync(s => s.WarehouseId == transfer.DestinationWarehouseId && s.ProductId == line.ProductId, cancellationToken);
            if (stock == null)
            {
                stock = new StockBalanceRecord
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BranchId = transfer.DestinationBranchId,
                    WarehouseId = transfer.DestinationWarehouseId,
                    ProductId = line.ProductId,
                    QuantityOnHand = line.DispatchedQuantity,
                    QuantityAvailable = line.DispatchedQuantity,
                    QuantityReserved = 0,
                    QuantityDamaged = 0,
                    QuantityExpired = 0,
                    QuantityBlocked = 0,
                    UnitCostPaise = 10000,
                    LastMovementAtUtc = DateTimeOffset.UtcNow
                };
                dbContext.StockBalances.Add(stock);
            }
            else
            {
                stock.QuantityOnHand += line.DispatchedQuantity;
                stock.QuantityAvailable = stock.QuantityOnHand - stock.QuantityReserved;
                stock.LastMovementAtUtc = DateTimeOffset.UtcNow;
            }

            line.ReceivedQuantity = line.DispatchedQuantity;

            dbContext.StockMovements.Add(new StockMovementRecord
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = transfer.DestinationBranchId,
                WarehouseId = transfer.DestinationWarehouseId,
                ProductId = line.ProductId,
                MovementType = "TransferIn",
                Direction = "In",
                ReferenceType = "InterBranchTransfer",
                ReferenceId = transfer.Id,
                ReferenceNumber = transfer.TransferNumber,
                Quantity = line.DispatchedQuantity,
                BalanceAfterOnHand = stock.QuantityOnHand,
                PerformedByUserId = userId,
                MovementDateUtc = DateTimeOffset.UtcNow,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Reason = $"Inter-branch receipt from {transfer.SourceBranchId}"
            });
        }

        transfer.Status = "Received";
        transfer.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetInterBranchTransfersAsync(companyId, cancellationToken)).First(t => t.Id == transferId);
    }
}
