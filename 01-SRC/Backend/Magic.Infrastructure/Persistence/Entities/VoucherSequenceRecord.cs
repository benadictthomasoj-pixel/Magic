namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity tracking concurrency-safe sequential voucher allocation per branch, fiscal year, and voucher type.
/// </summary>
public sealed class VoucherSequenceRecord
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid FiscalYearId { get; set; }
    public string VoucherType { get; set; } = string.Empty;
    public long CurrentSequence { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
