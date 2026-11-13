namespace Magic.Contracts.Sales;

// ==========================================
// 1. POS Operations
// ==========================================

public sealed record PosProductLookupDto
{
    public required Guid Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public string? Barcode { get; init; }
    public required string CategoryName { get; init; }
    public required string UnitOfMeasure { get; init; }
    public required long SellingPricePaise { get; init; }
    public required decimal GstRatePercentage { get; init; }
    public string? HsnCode { get; init; }
    public required decimal QuantityOnHand { get; init; }
    public required decimal QuantityAvailable { get; init; }
    public Guid? DefaultStorageLocationId { get; init; }
}

public sealed record PosSaleLineRequest
{
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public long? UnitPriceOverridePaise { get; init; }
    public decimal DiscountPercentage { get; init; }
    public long DiscountPaise { get; init; }
    public Guid? StorageLocationId { get; init; }
    public string? BatchNumber { get; init; }
    public string? SerialNumber { get; init; }
}

public sealed record PosPaymentRequest
{
    public required string PaymentMethod { get; init; } // Cash, Upi, Card, BankTransfer, CustomerCredit
    public required long AmountPaise { get; init; }
    public string? ReferenceNumber { get; init; }
}

public sealed record CreatePosSaleRequest
{
    public required Guid WarehouseId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? CashierSessionId { get; init; }
    public string? PlaceOfSupplyStateCode { get; init; }
    public string? CustomerGstin { get; init; }
    public string? Notes { get; init; }
    public required IReadOnlyList<PosSaleLineRequest> Lines { get; init; }
    public required IReadOnlyList<PosPaymentRequest> Payments { get; init; }
}

// ==========================================
// 2. Sales Invoices
// ==========================================

public sealed record SalesInvoiceLineDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductCode { get; init; }
    public required string ProductName { get; init; }
    public string? HsnCode { get; init; }
    public required decimal Quantity { get; init; }
    public required long UnitPricePaise { get; init; }
    public required decimal DiscountPercentage { get; init; }
    public required long DiscountPaise { get; init; }
    public required decimal GstRatePercentage { get; init; }
    public required decimal CgstPercentage { get; init; }
    public required decimal SgstPercentage { get; init; }
    public required decimal IgstPercentage { get; init; }
    public required long TaxAmountPaise { get; init; }
    public required long LineTotalPaise { get; init; }
    public string? BatchNumber { get; init; }
    public string? SerialNumber { get; init; }
}

public sealed record SalesPaymentDto
{
    public required Guid Id { get; init; }
    public required string PaymentMethod { get; init; }
    public required long AmountPaise { get; init; }
    public string? ReferenceNumber { get; init; }
    public required DateTime PaymentDateUtc { get; init; }
    public required Guid CashierUserId { get; init; }
    public required string Status { get; init; }
}

public sealed record SalesInvoiceDto
{
    public required Guid Id { get; init; }
    public required Guid BranchId { get; init; }
    public required string InvoiceNumber { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerGstin { get; init; }
    public Guid? CashierSessionId { get; init; }
    public required Guid CashierUserId { get; init; }
    public required string CashierName { get; init; }
    public required DateTime InvoiceDateUtc { get; init; }
    public required string Status { get; init; } // Draft, Paid, PartiallyPaid, Cancelled, PartiallyRefunded, Refunded
    public required long SubtotalPaise { get; init; }
    public required long DiscountPaise { get; init; }
    public required long CgstPaise { get; init; }
    public required long SgstPaise { get; init; }
    public required long IgstPaise { get; init; }
    public required long TotalTaxPaise { get; init; }
    public required long GrandTotalPaise { get; init; }
    public required long AmountPaidPaise { get; init; }
    public required long OutstandingBalancePaise { get; init; }
    public required string PlaceOfSupplyStateCode { get; init; }
    public required string SellerGstin { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? AccountingVoucherId { get; init; }
    public string? Notes { get; init; }
    public required IReadOnlyList<SalesInvoiceLineDto> Lines { get; init; }
    public required IReadOnlyList<SalesPaymentDto> Payments { get; init; }
}

public sealed record CancelInvoiceRequest
{
    public required string Reason { get; init; }
}

public sealed record RecordSalesPaymentRequest
{
    public required string PaymentMethod { get; init; }
    public required long AmountPaise { get; init; }
    public string? ReferenceNumber { get; init; }
    public Guid? CashierSessionId { get; init; }
}

// ==========================================
// 3. Sales Returns & Refunds
// ==========================================

public sealed record SalesReturnLineDto
{
    public required Guid Id { get; init; }
    public required Guid InvoiceLineId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductCode { get; init; }
    public required string ProductName { get; init; }
    public required decimal ReturnQuantity { get; init; }
    public required long UnitPricePaise { get; init; }
    public required decimal GstRatePercentage { get; init; }
    public required long TaxAmountPaise { get; init; }
    public required long TotalAmountPaise { get; init; }
    public required bool IsResalable { get; init; }
    public required string Reason { get; init; }
}

public sealed record SalesReturnDto
{
    public required Guid Id { get; init; }
    public required string ReturnNumber { get; init; }
    public required Guid InvoiceId { get; init; }
    public required string InvoiceNumber { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public required DateTime ReturnDateUtc { get; init; }
    public required string Status { get; init; } // Draft, Confirmed, Cancelled
    public required string ReasonCode { get; init; }
    public required long TotalRefundPaise { get; init; }
    public string? CreditNoteNumber { get; init; }
    public string? Notes { get; init; }
    public required IReadOnlyList<SalesReturnLineDto> Lines { get; init; }
}

public sealed record CreateSalesReturnLineRequest
{
    public required Guid InvoiceLineId { get; init; }
    public required decimal ReturnQuantity { get; init; }
    public required bool IsResalable { get; init; }
    public required string Reason { get; init; }
}

public sealed record CreateSalesReturnRequest
{
    public required Guid InvoiceId { get; init; }
    public required string ReasonCode { get; init; } // Defective, WrongItem, CustomerCancelled, Other
    public string? RefundMethod { get; init; } // Cash, Card, UPI, CreditNote
    public string? Notes { get; init; }
    public required IReadOnlyList<CreateSalesReturnLineRequest> Lines { get; init; }
}

public sealed record CreditNoteDto
{
    public required Guid Id { get; init; }
    public required string CreditNoteNumber { get; init; }
    public required Guid SalesReturnId { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public required DateTime IssueDateUtc { get; init; }
    public required long AmountPaise { get; init; }
    public required string Status { get; init; } // Issued, Utilized, Cancelled
}

// ==========================================
// 4. Cashier Sessions & Cash Management
// ==========================================

public sealed record CashMovementDto
{
    public required Guid Id { get; init; }
    public required Guid CashierSessionId { get; init; }
    public required string MovementType { get; init; } // Deposit, Withdrawal, FloatAdjustment
    public required long AmountPaise { get; init; }
    public required string Reason { get; init; }
    public required DateTime MovementDateUtc { get; init; }
    public required Guid CashierUserId { get; init; }
}

public sealed record CashierSessionDto
{
    public required Guid Id { get; init; }
    public required Guid BranchId { get; init; }
    public Guid? TerminalId { get; init; }
    public required Guid CashierUserId { get; init; }
    public required string CashierUsername { get; init; }
    public required DateTime OpenedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }
    public required string Status { get; init; } // Open, Closed, Reconciled
    public required long OpeningCashPaise { get; init; }
    public required long CashSalesPaise { get; init; }
    public required long CashRefundsPaise { get; init; }
    public required long CashDepositsPaise { get; init; }
    public required long CashWithdrawalsPaise { get; init; }
    public required long ExpectedCashPaise { get; init; }
    public long? ActualClosingCashPaise { get; init; }
    public long? VariancePaise { get; init; }
    public string? ClosingNotes { get; init; }
    public Guid? SupervisorUserId { get; init; }
    public DateTime? ApprovedAtUtc { get; init; }
    public required IReadOnlyList<CashMovementDto> Movements { get; init; }
}

public sealed record OpenCashierSessionRequest
{
    public Guid? TerminalId { get; init; }
    public required long OpeningCashPaise { get; init; }
}

public sealed record CloseCashierSessionRequest
{
    public required long ActualClosingCashPaise { get; init; }
    public string? Notes { get; init; }
}

public sealed record ReconcileCashierSessionRequest
{
    public string? SupervisorNotes { get; init; }
}

public sealed record RecordCashMovementRequest
{
    public required string MovementType { get; init; } // Deposit, Withdrawal
    public required long AmountPaise { get; init; }
    public required string Reason { get; init; }
}

// ==========================================
// 5. Sales Reporting & Analytics
// ==========================================

public sealed record SalesReportSummaryDto
{
    public required long GrossSalesPaise { get; init; }
    public required long NetSalesPaise { get; init; }
    public required long TotalTaxPaise { get; init; }
    public required long TotalDiscountPaise { get; init; }
    public required long TotalRefundsPaise { get; init; }
    public required int TotalInvoicesCount { get; init; }
    public required long AverageBillValuePaise { get; init; }
}

public sealed record SalesByPaymentMethodDto
{
    public required string PaymentMethod { get; init; }
    public required long TotalAmountPaise { get; init; }
    public required int TransactionCount { get; init; }
    public required decimal PercentageOfTotal { get; init; }
}

public sealed record SalesByProductDto
{
    public required Guid ProductId { get; init; }
    public required string ProductCode { get; init; }
    public required string ProductName { get; init; }
    public required decimal TotalQuantitySold { get; init; }
    public required long TotalRevenuePaise { get; init; }
}

public sealed record HourlySalesDto
{
    public required int HourOfDay { get; init; } // 0 - 23
    public required int InvoiceCount { get; init; }
    public required long TotalSalesPaise { get; init; }
}
