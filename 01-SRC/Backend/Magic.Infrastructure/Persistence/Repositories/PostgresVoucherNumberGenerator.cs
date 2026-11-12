using Magic.Application.Accounting.Persistence;
using Magic.Contracts.Common;
using Magic.Domain.Accounting.Enums;
using Magic.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Magic.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL implementation of IVoucherNumberGenerator using atomic row locking via ON CONFLICT DO UPDATE.
/// Guarantees concurrency-safe sequential allocation isolated by branch, fiscal year, and voucher type.
/// </summary>
public sealed class PostgresVoucherNumberGenerator(
    MagicDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IVoucherNumberGenerator
{
    public async Task<string> NextVoucherNumberAsync(
        Guid branchId,
        Guid fiscalYearId,
        string branchCode,
        string fyCode,
        VoucherType voucherType,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.NotEmpty(branchId, nameof(branchId));
        DomainGuard.NotEmpty(fiscalYearId, nameof(fiscalYearId));
        DomainGuard.NotNullOrWhiteSpace(branchCode, nameof(branchCode));
        DomainGuard.NotNullOrWhiteSpace(fyCode, nameof(fyCode));

        var typeString = voucherType.ToString().ToUpperInvariant();
        var now = dateTimeProvider.UtcNow;
        var newId = Guid.CreateVersion7();

        const string sql = """
            INSERT INTO accounting.voucher_sequences (id, branch_id, fiscal_year_id, voucher_type, current_sequence, updated_at_utc)
            VALUES (@pId, @pBranchId, @pFiscalYearId, @pVoucherType, 1, @pNow)
            ON CONFLICT (branch_id, fiscal_year_id, voucher_type)
            DO UPDATE SET current_sequence = accounting.voucher_sequences.current_sequence + 1, updated_at_utc = @pNow
            RETURNING current_sequence;
            """;

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();

        cmd.Parameters.Add(new NpgsqlParameter("pId", newId));
        cmd.Parameters.Add(new NpgsqlParameter("pBranchId", branchId));
        cmd.Parameters.Add(new NpgsqlParameter("pFiscalYearId", fiscalYearId));
        cmd.Parameters.Add(new NpgsqlParameter("pVoucherType", typeString));
        cmd.Parameters.Add(new NpgsqlParameter("pNow", now));

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        var sequenceNumber = Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);

        var prefix = GetVoucherTypePrefix(voucherType);
        var cleanBranch = branchCode.Trim().ToUpperInvariant();
        var cleanFy = fyCode.Trim().ToUpperInvariant();

        return $"VCH-{cleanBranch}-{cleanFy}-{prefix}-{sequenceNumber:D6}";
    }

    private static string GetVoucherTypePrefix(VoucherType voucherType) =>
        voucherType switch
        {
            VoucherType.Journal => "JV",
            VoucherType.Sales => "SAL",
            VoucherType.Purchase => "PUR",
            VoucherType.Payment => "PAY",
            VoucherType.Receipt => "REC",
            VoucherType.Contra => "CON",
            VoucherType.Reversal => "REV",
            _ => "VCH"
        };
}
