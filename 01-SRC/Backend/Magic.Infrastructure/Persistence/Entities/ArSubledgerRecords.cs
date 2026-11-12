namespace Magic.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity representing an open accounts receivable subledger item (invoice or credit note).
/// </summary>
public sealed class ArSubledgerItemRecord
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid VoucherId { get; set; }
    public Guid? InvoiceId { get; set; }
    public DateOnly ItemDate { get; set; }
    public DateOnly DueDate { get; set; }
    public long OriginalAmountPaise { get; set; }
    public long SettledAmountPaise { get; set; }
    public long OutstandingAmountPaise { get; set; }
    public string Status { get; set; } = "OPEN";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// Persistence entity recording the settlement allocation between a receipt voucher and an open AR item.
/// </summary>
public sealed class ArAllocationRecord
{
    public Guid Id { get; set; }
    public Guid DebitItemId { get; set; }
    public Guid CreditVoucherId { get; set; }
    public DateOnly AllocationDate { get; set; }
    public long AllocatedAmountPaise { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
