namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity representing an open accounts payable subledger item (supplier bill or debit note).
/// </summary>
public sealed class ApSubledgerItemRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid VoucherId { get; set; }
    public Guid? BillId { get; set; }
    public DateOnly ItemDate { get; set; }
    public DateOnly DueDate { get; set; }
    public long OriginalAmountPaise { get; set; }
    public long SettledAmountPaise { get; set; }
    public long OutstandingAmountPaise { get; set; }
    public string Status { get; set; } = "OPEN";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Persistence entity recording the settlement allocation between a payment voucher and an open AP bill.
/// </summary>
public sealed class ApAllocationRecord
{
    public Guid Id { get; set; }
    public Guid CreditItemId { get; set; }
    public Guid DebitVoucherId { get; set; }
    public DateOnly AllocationDate { get; set; }
    public long AllocatedAmountPaise { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
