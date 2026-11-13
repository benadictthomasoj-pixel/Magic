namespace Magic.Infrastructure.Persistence.Entities;

// ==========================================
// 1. Sales Invoices
// ==========================================

public sealed class SalesInvoiceRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required string InvoiceNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public CustomerRecord? Customer { get; set; }
    public Guid? CashierSessionId { get; set; }
    public CashierSessionRecord? CashierSession { get; set; }
    public required Guid CashierUserId { get; set; }
    public required DateTimeOffset InvoiceDateUtc { get; set; }
    public required string Status { get; set; } // Draft, Paid, PartiallyPaid, Cancelled, PartiallyRefunded, Refunded
    public required long SubtotalPaise { get; set; }
    public required long DiscountPaise { get; set; }
    public required long CgstPaise { get; set; }
    public required long SgstPaise { get; set; }
    public required long IgstPaise { get; set; }
    public required long TotalTaxPaise { get; set; }
    public required long GrandTotalPaise { get; set; }
    public required long AmountPaidPaise { get; set; }
    public required long OutstandingBalancePaise { get; set; }
    public required string PlaceOfSupplyStateCode { get; set; }
    public string? CustomerGstin { get; set; }
    public required string SellerGstin { get; set; }
    public string? Notes { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? AccountingVoucherId { get; set; }
    public int ConcurrencyVersion { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public ICollection<SalesInvoiceLineRecord> Lines { get; set; } = [];
    public ICollection<SalesPaymentRecord> Payments { get; set; } = [];
    public ICollection<SalesReturnRecord> Returns { get; set; } = [];
}

public sealed class SalesInvoiceLineRecord
{
    public required Guid Id { get; set; }
    public required Guid InvoiceId { get; set; }
    public SalesInvoiceRecord? Invoice { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public required string ProductCode { get; set; }
    public required string ProductName { get; set; }
    public string? HsnCode { get; set; }
    public required decimal Quantity { get; set; }
    public required long UnitPricePaise { get; set; }
    public required decimal DiscountPercentage { get; set; }
    public required long DiscountPaise { get; set; }
    public required decimal GstRatePercentage { get; set; }
    public required decimal CgstPercentage { get; set; }
    public required decimal SgstPercentage { get; set; }
    public required decimal IgstPercentage { get; set; }
    public required long TaxAmountPaise { get; set; }
    public required long LineTotalPaise { get; set; }
    public Guid? StorageLocationId { get; set; }
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
}

public sealed class SalesPaymentRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required Guid InvoiceId { get; set; }
    public SalesInvoiceRecord? Invoice { get; set; }
    public Guid? CashierSessionId { get; set; }
    public CashierSessionRecord? CashierSession { get; set; }
    public required string PaymentMethod { get; set; } // Cash, Upi, Card, BankTransfer, CustomerCredit
    public required long AmountPaise { get; set; }
    public string? ReferenceNumber { get; set; }
    public required DateTimeOffset PaymentDateUtc { get; set; }
    public required Guid CashierUserId { get; set; }
    public required string Status { get; set; } // Completed, Failed, Voided
    public DateTimeOffset CreatedAtUtc { get; set; }
}

// ==========================================
// 2. Sales Returns & Credit Notes
// ==========================================

public sealed class SalesReturnRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required string ReturnNumber { get; set; }
    public required Guid InvoiceId { get; set; }
    public SalesInvoiceRecord? Invoice { get; set; }
    public Guid? CustomerId { get; set; }
    public CustomerRecord? Customer { get; set; }
    public Guid? CashierSessionId { get; set; }
    public CashierSessionRecord? CashierSession { get; set; }
    public required Guid CashierUserId { get; set; }
    public required DateTimeOffset ReturnDateUtc { get; set; }
    public required string Status { get; set; } // Draft, Confirmed, Cancelled
    public required string ReasonCode { get; set; }
    public required long TotalRefundPaise { get; set; }
    public string? CreditNoteNumber { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<SalesReturnLineRecord> Lines { get; set; } = [];
}

public sealed class SalesReturnLineRecord
{
    public required Guid Id { get; set; }
    public required Guid ReturnId { get; set; }
    public SalesReturnRecord? Return { get; set; }
    public required Guid InvoiceLineId { get; set; }
    public SalesInvoiceLineRecord? InvoiceLine { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public required string ProductCode { get; set; }
    public required string ProductName { get; set; }
    public required decimal ReturnQuantity { get; set; }
    public required long UnitPricePaise { get; set; }
    public required decimal GstRatePercentage { get; set; }
    public required long TaxAmountPaise { get; set; }
    public required long TotalAmountPaise { get; set; }
    public required bool IsResalable { get; set; }
    public required string Reason { get; set; }
}

public sealed class CreditNoteRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required string CreditNoteNumber { get; set; }
    public required Guid SalesReturnId { get; set; }
    public SalesReturnRecord? SalesReturn { get; set; }
    public Guid? CustomerId { get; set; }
    public CustomerRecord? Customer { get; set; }
    public required DateTimeOffset IssueDateUtc { get; set; }
    public required long AmountPaise { get; set; }
    public required string Status { get; set; } // Issued, Utilized, Cancelled
    public DateTimeOffset CreatedAtUtc { get; set; }
}

// ==========================================
// 3. Cashier Sessions & Cash Management
// ==========================================

public sealed class CashierSessionRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public Guid? TerminalId { get; set; }
    public required Guid CashierUserId { get; set; }
    public required string CashierUsername { get; set; }
    public required DateTimeOffset OpenedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public required string Status { get; set; } // Open, Closed, Reconciled
    public required long OpeningCashPaise { get; set; }
    public required long CashSalesPaise { get; set; }
    public required long CashRefundsPaise { get; set; }
    public required long CashDepositsPaise { get; set; }
    public required long CashWithdrawalsPaise { get; set; }
    public required long ExpectedCashPaise { get; set; }
    public long? ActualClosingCashPaise { get; set; }
    public long? VariancePaise { get; set; }
    public string? ClosingNotes { get; set; }
    public Guid? SupervisorUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<CashMovementRecord> Movements { get; set; } = [];
    public ICollection<SalesInvoiceRecord> Invoices { get; set; } = [];
    public ICollection<SalesPaymentRecord> Payments { get; set; } = [];
}

public sealed class CashMovementRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid BranchId { get; set; }
    public required Guid CashierSessionId { get; set; }
    public CashierSessionRecord? CashierSession { get; set; }
    public required string MovementType { get; set; } // Deposit, Withdrawal, FloatAdjustment
    public required long AmountPaise { get; set; }
    public required string Reason { get; set; }
    public required DateTimeOffset MovementDateUtc { get; set; }
    public required Guid CashierUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
