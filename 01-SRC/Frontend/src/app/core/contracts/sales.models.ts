export interface PosProductLookupDto {
  id: string;
  sku: string;
  name: string;
  barcode?: string | null;
  categoryName: string;
  unitOfMeasure: string;
  sellingPricePaise: number;
  gstRatePercentage: number;
  hsnCode?: string | null;
  quantityOnHand: number;
  quantityAvailable: number;
  defaultStorageLocationId?: string | null;
}

export interface PosSaleLineRequest {
  productId: string;
  quantity: number;
  unitPriceOverridePaise?: number | null;
  discountPercentage: number;
  discountPaise: number;
  storageLocationId?: string | null;
  batchNumber?: string | null;
  serialNumber?: string | null;
}

export interface PosPaymentRequest {
  paymentMethod: string; // 'Cash' | 'Upi' | 'Card' | 'BankTransfer' | 'CustomerCredit'
  amountPaise: number;
  referenceNumber?: string | null;
}

export interface CreatePosSaleRequest {
  warehouseId: string;
  customerId?: string | null;
  cashierSessionId?: string | null;
  placeOfSupplyStateCode?: string | null;
  customerGstin?: string | null;
  notes?: string | null;
  lines: PosSaleLineRequest[];
  payments: PosPaymentRequest[];
}

export interface SalesInvoiceLineDto {
  id: string;
  productId: string;
  productCode: string;
  productName: string;
  hsnCode?: string | null;
  quantity: number;
  unitPricePaise: number;
  discountPercentage: number;
  discountPaise: number;
  gstRatePercentage: number;
  cgstPercentage: number;
  sgstPercentage: number;
  igstPercentage: number;
  taxAmountPaise: number;
  lineTotalPaise: number;
  batchNumber?: string | null;
  serialNumber?: string | null;
}

export interface SalesPaymentDto {
  id: string;
  paymentMethod: string;
  amountPaise: number;
  referenceNumber?: string | null;
  paymentDateUtc: string;
  cashierUserId: string;
  status: string;
}

export interface SalesInvoiceDto {
  id: string;
  branchId: string;
  invoiceNumber: string;
  customerId?: string | null;
  customerName?: string | null;
  customerGstin?: string | null;
  cashierSessionId?: string | null;
  cashierUserId: string;
  cashierName: string;
  invoiceDateUtc: string;
  status: string;
  subtotalPaise: number;
  discountPaise: number;
  cgstPaise: number;
  sgstPaise: number;
  igstPaise: number;
  totalTaxPaise: number;
  grandTotalPaise: number;
  amountPaidPaise: number;
  outstandingBalancePaise: number;
  placeOfSupplyStateCode: string;
  sellerGstin: string;
  notes?: string | null;
  lines: SalesInvoiceLineDto[];
  payments: SalesPaymentDto[];
}

export interface CancelInvoiceRequest {
  reason: string;
}

export interface SalesReturnLineDto {
  id: string;
  invoiceLineId: string;
  productId: string;
  productCode: string;
  productName: string;
  returnQuantity: number;
  unitPricePaise: number;
  gstRatePercentage: number;
  taxAmountPaise: number;
  totalAmountPaise: number;
  isResalable: boolean;
  reason: string;
}

export interface SalesReturnDto {
  id: string;
  returnNumber: string;
  invoiceId: string;
  invoiceNumber: string;
  customerId?: string | null;
  customerName?: string | null;
  returnDateUtc: string;
  status: string;
  reasonCode: string;
  totalRefundPaise: number;
  creditNoteNumber?: string | null;
  notes?: string | null;
  lines: SalesReturnLineDto[];
}

export interface CreateSalesReturnLineRequest {
  invoiceLineId: string;
  returnQuantity: number;
  isResalable: boolean;
  reason: string;
}

export interface CreateSalesReturnRequest {
  invoiceId: string;
  reasonCode: string;
  notes?: string | null;
  lines: CreateSalesReturnLineRequest[];
}

export interface CreditNoteDto {
  id: string;
  creditNoteNumber: string;
  salesReturnId: string;
  customerId?: string | null;
  customerName?: string | null;
  issueDateUtc: string;
  amountPaise: number;
  status: string;
}

export interface CashMovementDto {
  id: string;
  cashierSessionId: string;
  movementType: string;
  amountPaise: number;
  reason: string;
  movementDateUtc: string;
  cashierUserId: string;
}

export interface CashierSessionDto {
  id: string;
  branchId: string;
  terminalId?: string | null;
  cashierUserId: string;
  cashierUsername: string;
  openedAtUtc: string;
  closedAtUtc?: string | null;
  status: string;
  openingCashPaise: number;
  cashSalesPaise: number;
  cashRefundsPaise: number;
  cashDepositsPaise: number;
  cashWithdrawalsPaise: number;
  expectedCashPaise: number;
  actualClosingCashPaise?: number | null;
  variancePaise?: number | null;
  closingNotes?: string | null;
  supervisorUserId?: string | null;
  approvedAtUtc?: string | null;
  movements: CashMovementDto[];
}

export interface OpenCashierSessionRequest {
  terminalId?: string | null;
  openingCashPaise: number;
}

export interface CloseCashierSessionRequest {
  actualClosingCashPaise: number;
  notes?: string | null;
}

export interface RecordCashMovementRequest {
  movementType: string;
  amountPaise: number;
  reason: string;
}

export interface SalesReportSummaryDto {
  grossSalesPaise: number;
  netSalesPaise: number;
  totalTaxPaise: number;
  totalDiscountPaise: number;
  totalRefundsPaise: number;
  totalInvoicesCount: number;
  averageBillValuePaise: number;
}

export interface SalesByPaymentMethodDto {
  paymentMethod: string;
  totalAmountPaise: number;
  transactionCount: number;
  percentageOfTotal: number;
}

export interface SalesByProductDto {
  productId: string;
  productCode: string;
  productName: string;
  totalQuantitySold: number;
  totalRevenuePaise: number;
}

export interface HourlySalesDto {
  hourOfDay: number;
  invoiceCount: number;
  totalSalesPaise: number;
}

export interface PosCartItem {
  product: PosProductLookupDto;
  quantity: number;
  unitPriceOverridePaise?: number;
  discountPercentage: number;
  discountPaise: number;
}
