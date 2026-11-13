// ==========================================
// 1. Receivables & Aging Models
// ==========================================

export interface CustomerReceivableDto {
  id: string;
  customerId: string;
  customerName: string;
  documentNumber: string;
  invoiceId?: string | null;
  voucherId: string;
  itemDate: string;
  dueDate: string;
  originalAmountPaise: number;
  settledAmountPaise: number;
  outstandingAmountPaise: number;
  status: string;
  overdueDays: number;
}

export interface ReceivablesSummaryDto {
  totalReceivablePaise: number;
  totalOverduePaise: number;
  customerCount: number;
  openInvoiceCount: number;
}

export interface CustomerAgingRowDto {
  customerId: string;
  customerName: string;
  totalOutstandingPaise: number;
  currentPaise: number;
  overdue1To30Paise: number;
  overdue31To60Paise: number;
  overdue61To90Paise: number;
  overdueAbove90Paise: number;
}

export interface CustomerAgingReportDto {
  reportDate: string;
  totalOutstandingPaise: number;
  totalCurrentPaise: number;
  total1To30Paise: number;
  total31To60Paise: number;
  total61To90Paise: number;
  totalAbove90Paise: number;
  customers: CustomerAgingRowDto[];
}

export interface CustomerStatementLineDto {
  date: string;
  documentType: string;
  documentNumber: string;
  referenceNumber?: string | null;
  debitPaise: number;
  creditPaise: number;
  runningBalancePaise: number;
  narration?: string | null;
}

export interface CustomerStatementDto {
  customerId: string;
  customerName: string;
  fromDate: string;
  toDate: string;
  openingBalancePaise: number;
  totalDebitPaise: number;
  totalCreditPaise: number;
  closingBalancePaise: number;
  lines: CustomerStatementLineDto[];
}

// ==========================================
// 2. Customer Receipts Models
// ==========================================

export interface CustomerReceiptAllocationRequest {
  arItemId: string;
  allocatedAmountPaise: number;
}

export interface CreateCustomerReceiptRequest {
  customerId: string;
  receiptDate: string;
  paymentMethod: string;
  referenceNumber?: string | null;
  bankCashAccountId: string;
  totalAmountPaise: number;
  narration?: string | null;
  allocations?: CustomerReceiptAllocationRequest[];
}

export interface CustomerReceiptAllocationDto {
  id: string;
  arItemId: string;
  documentNumber: string;
  allocatedAmountPaise: number;
  allocationDate: string;
}

export interface CustomerReceiptDto {
  id: string;
  receiptNumber: string;
  customerId: string;
  customerName: string;
  receiptDate: string;
  paymentMethod: string;
  referenceNumber?: string | null;
  bankCashAccountId: string;
  bankCashAccountName: string;
  totalAmountPaise: number;
  allocatedAmountPaise: number;
  unallocatedAmountPaise: number;
  status: string;
  voucherId: string;
  voucherNumber: string;
  narration: string;
  createdAtUtc: string;
  allocations: CustomerReceiptAllocationDto[];
}

export interface CancelReceiptRequest {
  reason: string;
}

// ==========================================
// 3. Payables & Supplier Bills Models
// ==========================================

export interface SupplierPayableDto {
  id: string;
  supplierId: string;
  supplierName: string;
  billNumber: string;
  systemInvoiceNumber: string;
  billId?: string | null;
  voucherId: string;
  itemDate: string;
  dueDate: string;
  originalAmountPaise: number;
  settledAmountPaise: number;
  outstandingAmountPaise: number;
  status: string;
  overdueDays: number;
}

export interface PayablesSummaryDto {
  totalPayablePaise: number;
  totalOverduePaise: number;
  supplierCount: number;
  openBillCount: number;
}

export interface SupplierAgingRowDto {
  supplierId: string;
  supplierName: string;
  totalOutstandingPaise: number;
  currentPaise: number;
  overdue1To30Paise: number;
  overdue31To60Paise: number;
  overdue61To90Paise: number;
  overdueAbove90Paise: number;
}

export interface SupplierAgingReportDto {
  reportDate: string;
  totalOutstandingPaise: number;
  totalCurrentPaise: number;
  total1To30Paise: number;
  total31To60Paise: number;
  total61To90Paise: number;
  totalAbove90Paise: number;
  suppliers: SupplierAgingRowDto[];
}

export interface SupplierStatementLineDto {
  date: string;
  documentType: string;
  documentNumber: string;
  referenceNumber?: string | null;
  debitPaise: number;
  creditPaise: number;
  runningBalancePaise: number;
  narration?: string | null;
}

export interface SupplierStatementDto {
  supplierId: string;
  supplierName: string;
  fromDate: string;
  toDate: string;
  openingBalancePaise: number;
  totalDebitPaise: number;
  totalCreditPaise: number;
  closingBalancePaise: number;
  lines: SupplierStatementLineDto[];
}

export interface CreateSupplierBillLineRequest {
  description: string;
  quantity: number;
  unitPricePaise: number;
  taxRatePercentage: number;
}

export interface CreateSupplierBillRequest {
  supplierId: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  poId?: string | null;
  grnId?: string | null;
  notes?: string | null;
  lines: CreateSupplierBillLineRequest[];
}

export interface SupplierBillLineDto {
  id: string;
  description: string;
  quantity: number;
  unitPricePaise: number;
  taxRatePercentage: number;
  taxAmountPaise: number;
  totalAmountPaise: number;
}

export interface SupplierBillDto {
  id: string;
  systemInvoiceNumber: string;
  invoiceNumber: string;
  supplierId: string;
  supplierName: string;
  invoiceDate: string;
  dueDate: string;
  poId?: string | null;
  grnId?: string | null;
  subTotalPaise: number;
  taxAmountPaise: number;
  totalAmountPaise: number;
  settledAmountPaise: number;
  outstandingAmountPaise: number;
  status: string;
  voucherId: string;
  voucherNumber: string;
  notes?: string | null;
  createdAtUtc: string;
  lines: SupplierBillLineDto[];
}

// ==========================================
// 4. Supplier Payments Models
// ==========================================

export interface SupplierPaymentAllocationRequest {
  apItemId: string;
  allocatedAmountPaise: number;
}

export interface CreateSupplierPaymentRequest {
  supplierId: string;
  paymentDate: string;
  paymentMethod: string;
  referenceNumber?: string | null;
  bankCashAccountId: string;
  totalAmountPaise: number;
  narration?: string | null;
  allocations?: SupplierPaymentAllocationRequest[];
}

export interface SupplierPaymentAllocationDto {
  id: string;
  apItemId: string;
  documentNumber: string;
  allocatedAmountPaise: number;
  allocationDate: string;
}

export interface SupplierPaymentDto {
  id: string;
  paymentNumber: string;
  supplierId: string;
  supplierName: string;
  paymentDate: string;
  paymentMethod: string;
  referenceNumber?: string | null;
  bankCashAccountId: string;
  bankCashAccountName: string;
  totalAmountPaise: number;
  allocatedAmountPaise: number;
  unallocatedAmountPaise: number;
  status: string;
  voucherId: string;
  voucherNumber: string;
  narration: string;
  createdAtUtc: string;
  allocations: SupplierPaymentAllocationDto[];
}

export interface CancelPaymentRequest {
  reason: string;
}

// ==========================================
// 5. Bank & Cash Account Models
// ==========================================

export interface BankCashAccountDto {
  id: string;
  accountName: string;
  accountType: string;
  bankName?: string | null;
  branchName?: string | null;
  maskedAccountNumber: string;
  ifscCode?: string | null;
  glAccountId: string;
  glAccountCode: string;
  glAccountName: string;
  openingBalancePaise: number;
  currentBalancePaise: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateBankCashAccountRequest {
  accountName: string;
  accountType: string;
  bankName?: string | null;
  branchName?: string | null;
  accountNumber: string;
  ifscCode?: string | null;
  glAccountId: string;
  openingBalancePaise: number;
}

export interface UpdateBankCashAccountRequest {
  accountName: string;
  bankName?: string | null;
  branchName?: string | null;
  ifscCode?: string | null;
  isActive: boolean;
}

export interface RevealAccountNumberResponse {
  id: string;
  accountNumber: string;
  revealedAtUtc: string;
}

// ==========================================
// 6. Treasury Models
// ==========================================

export interface TreasuryTransactionDto {
  id: string;
  transactionNumber: string;
  transactionType: string;
  transactionDate: string;
  sourceAccountId: string;
  sourceAccountName: string;
  destinationAccountId?: string | null;
  destinationAccountName?: string | null;
  amountPaise: number;
  referenceNumber?: string | null;
  narration: string;
  status: string;
  voucherId: string;
  voucherNumber: string;
  createdAtUtc: string;
}

export interface CreateContraTransactionRequest {
  transactionType: string;
  transactionDate: string;
  sourceAccountId: string;
  destinationAccountId: string;
  amountPaise: number;
  referenceNumber?: string | null;
  narration?: string | null;
}

export interface CreateTreasuryAdjustmentRequest {
  transactionType: string;
  transactionDate: string;
  bankAccountId: string;
  amountPaise: number;
  nominalAccountId: string;
  referenceNumber?: string | null;
  narration?: string | null;
}

// ==========================================
// 7. Bank Reconciliation Models
// ==========================================

export interface BankStatementSummaryDto {
  id: string;
  bankAccountId: string;
  bankAccountName: string;
  statementStartDate: string;
  statementEndDate: string;
  openingBalancePaise: number;
  closingBalancePaise: number;
  filename: string;
  status: string;
  totalLines: number;
  reconciledLines: number;
  importedAtUtc: string;
}

export interface BankStatementLineDto {
  id: string;
  transactionDate: string;
  valueDate?: string | null;
  description: string;
  referenceNumber?: string | null;
  withdrawalDebitPaise: number;
  depositCreditPaise: number;
  balancePaise: number;
  isReconciled: boolean;
  matchedVoucherLineId?: string | null;
  matchedVoucherNumber?: string | null;
  matchType?: string | null;
}

export interface BankStatementDetailDto {
  id: string;
  bankAccountId: string;
  bankAccountName: string;
  statementStartDate: string;
  statementEndDate: string;
  openingBalancePaise: number;
  closingBalancePaise: number;
  filename: string;
  status: string;
  lines: BankStatementLineDto[];
}

export interface ImportBankStatementRequest {
  bankAccountId: string;
  statementStartDate: string;
  statementEndDate: string;
  openingBalancePaise: number;
  closingBalancePaise: number;
  filename: string;
  csvContent: string;
}

export interface UnmatchedLedgerTransactionDto {
  journalLineId: string;
  voucherId: string;
  voucherNumber: string;
  voucherDate: string;
  debitPaise: number;
  creditPaise: number;
  referenceNumber?: string | null;
  narration: string;
}

export interface ReconciliationWorkspaceDto {
  statementId: string;
  bankAccountId: string;
  bankAccountName: string;
  bookBalancePaise: number;
  statementBalancePaise: number;
  differencePaise: number;
  statementLines: BankStatementLineDto[];
  unmatchedLedgerLines: UnmatchedLedgerTransactionDto[];
}

export interface MatchStatementLineRequest {
  statementLineId: string;
  journalLineId: string;
  matchType: string;
}

export interface UnmatchStatementLineRequest {
  statementLineId: string;
}

// ==========================================
// 8. Financial Reports Models
// ==========================================

export interface CashBookEntryDto {
  date: string;
  voucherNumber: string;
  voucherType: string;
  oppositeAccountName: string;
  narration: string;
  receiptPaise: number;
  paymentPaise: number;
  runningBalancePaise: number;
}

export interface CashBookReportDto {
  accountName: string;
  fromDate: string;
  toDate: string;
  openingBalancePaise: number;
  totalReceiptsPaise: number;
  totalPaymentsPaise: number;
  closingBalancePaise: number;
  entries: CashBookEntryDto[];
}

export interface BankBookEntryDto {
  date: string;
  voucherNumber: string;
  voucherType: string;
  referenceNumber?: string | null;
  oppositeAccountName: string;
  narration: string;
  depositPaise: number;
  withdrawalPaise: number;
  runningBalancePaise: number;
}

export interface BankBookReportDto {
  accountName: string;
  bankName?: string | null;
  fromDate: string;
  toDate: string;
  openingBalancePaise: number;
  totalDepositsPaise: number;
  totalWithdrawalsPaise: number;
  closingBalancePaise: number;
  entries: BankBookEntryDto[];
}

export interface FinanceSummaryKpiDto {
  totalReceivablePaise: number;
  totalOverdueArPaise: number;
  totalPayablePaise: number;
  totalOverdueApPaise: number;
  totalCashBalancePaise: number;
  totalBankBalancePaise: number;
}
