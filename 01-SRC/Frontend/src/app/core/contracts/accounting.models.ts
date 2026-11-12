export interface AccountingDashboardDto {
  readonly totalDebitPaise: number;
  readonly totalCreditPaise: number;
  readonly postedVouchersCount: number;
  readonly draftVouchersCount: number;
  readonly activeFiscalYearName: string | null;
  readonly activeFiscalPeriodCode: string | null;
  readonly isActivePeriodLocked: boolean;
  readonly recentVouchers: readonly VoucherSummaryDto[];
  readonly systemValidationStatus: 'BALANCED' | 'EMPTY' | 'UNBALANCED';
}

export interface AccountGroupDto {
  readonly id: string;
  readonly parentGroupId: string | null;
  readonly code: string;
  readonly name: string;
  readonly rootClass: 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense';
  readonly hierarchyDepth: number;
  readonly isSystem: boolean;
  readonly isActive: boolean;
}

export interface AccountDto {
  readonly id: string;
  readonly groupId: string;
  readonly groupName: string;
  readonly code: string;
  readonly name: string;
  readonly rootClass: 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense';
  readonly currencyCode: string;
  readonly isActive: boolean;
  readonly controlAccountType?: string | null;
  readonly currentBalancePaise: number;
}

export interface CreateAccountRequest {
  readonly groupId: string;
  readonly code: string;
  readonly name: string;
  readonly rootClass: string;
  readonly controlAccountType?: string | null;
}

export interface UpdateAccountRequest {
  readonly name: string;
  readonly isActive: boolean;
}

export interface VoucherSummaryDto {
  readonly id: string;
  readonly voucherNumber: string;
  readonly voucherType: string;
  readonly voucherDate: string;
  readonly status: string;
  readonly narration: string;
  readonly referenceNumber?: string | null;
  readonly totalDebitPaise: number;
  readonly totalCreditPaise: number;
  readonly isReversed: boolean;
  readonly reversalVoucherNumber?: string | null;
  readonly linesCount: number;
  readonly postedAtUtc?: string | null;
}

export interface VoucherDetailDto {
  readonly id: string;
  readonly branchId: string;
  readonly fiscalPeriodId: string;
  readonly periodCode: string;
  readonly voucherNumber: string;
  readonly voucherType: string;
  readonly voucherDate: string;
  readonly status: string;
  readonly narration: string;
  readonly referenceNumber?: string | null;
  readonly totalDebitPaise: number;
  readonly totalCreditPaise: number;
  readonly isReversed: boolean;
  readonly reversalVoucherId?: string | null;
  readonly reversalVoucherNumber?: string | null;
  readonly traceCorrelationId: string;
  readonly postedAtUtc?: string | null;
  readonly postedByUserId?: string | null;
  readonly createdAtUtc: string;
  readonly lines: readonly JournalLineDto[];
}

export interface JournalLineDto {
  readonly id: string;
  readonly lineNumber: number;
  readonly accountId: string;
  readonly accountCode: string;
  readonly accountName: string;
  readonly debitPaise: number;
  readonly creditPaise: number;
  readonly narration: string;
  readonly partyId?: string | null;
  readonly partyType?: string | null;
}

export interface CreateJournalDraftRequest {
  readonly fiscalPeriodId: string;
  readonly voucherType: string;
  readonly voucherDate: string;
  readonly narration: string;
  readonly referenceNumber?: string | null;
  readonly lines: readonly JournalLineInputDto[];
}

export interface JournalLineInputDto {
  accountId: string;
  debitPaise: number;
  creditPaise: number;
  narration: string;
  partyId?: string | null;
  partyType?: string | null;
}

export interface JournalValidationResultDto {
  readonly isValid: boolean;
  readonly totalDebitPaise: number;
  readonly totalCreditPaise: number;
  readonly differencePaise: number;
  readonly isBalanced: boolean;
  readonly errors: readonly string[];
  readonly warnings: readonly string[];
}

export interface PostVoucherRequest {
  readonly branchCode: string;
  readonly fyCode: string;
  readonly supervisorAuthToken?: string | null;
}

export interface ReverseVoucherRequest {
  readonly branchCode: string;
  readonly fyCode: string;
  readonly reason: string;
}

export interface GeneralLedgerReportDto {
  readonly accountId: string;
  readonly accountCode: string;
  readonly accountName: string;
  readonly rootClass: string;
  readonly dateFrom: string;
  readonly dateTo: string;
  readonly openingBalancePaise: number;
  readonly totalDebitPaise: number;
  readonly totalCreditPaise: number;
  readonly closingBalancePaise: number;
  readonly entries: readonly GeneralLedgerEntryDto[];
}

export interface GeneralLedgerEntryDto {
  readonly voucherId: string;
  readonly voucherNumber: string;
  readonly voucherType: string;
  readonly voucherDate: string;
  readonly narration: string;
  readonly debitPaise: number;
  readonly creditPaise: number;
  readonly runningBalancePaise: number;
}

export interface TrialBalanceReportDto {
  readonly fiscalYearCode: string;
  readonly fiscalPeriodCode?: string | null;
  readonly asOfDate: string;
  readonly totalOpeningDebitPaise: number;
  readonly totalOpeningCreditPaise: number;
  readonly totalPeriodDebitPaise: number;
  readonly totalPeriodCreditPaise: number;
  readonly totalClosingDebitPaise: number;
  readonly totalClosingCreditPaise: number;
  readonly differencePaise: number;
  readonly isBalanced: boolean;
  readonly accounts: readonly TrialBalanceAccountRowDto[];
}

export interface TrialBalanceAccountRowDto {
  readonly accountId: string;
  readonly accountCode: string;
  readonly accountName: string;
  readonly groupCode: string;
  readonly groupName: string;
  readonly rootClass: string;
  readonly openingDebitPaise: number;
  readonly openingCreditPaise: number;
  readonly periodDebitPaise: number;
  readonly periodCreditPaise: number;
  readonly closingDebitPaise: number;
  readonly closingCreditPaise: number;
}

export interface FiscalYearDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly startDate: string;
  readonly endDate: string;
  readonly isClosed: boolean;
  readonly periods: readonly FiscalPeriodDto[];
}

export interface FiscalPeriodDto {
  readonly id: string;
  readonly fiscalYearId: string;
  readonly periodNumber: number;
  readonly periodCode: string;
  readonly startDate: string;
  readonly endDate: string;
  readonly isLocked: boolean;
  readonly isClosed: boolean;
}

export interface SubledgerReportDto {
  readonly subledgerType: 'AR' | 'AP';
  readonly totalOutstandingPaise: number;
  readonly itemsCount: number;
  readonly items: readonly SubledgerItemDto[];
  readonly message: string;
}

export interface SubledgerItemDto {
  readonly id: string;
  readonly partyId: string;
  readonly partyName: string;
  readonly documentNumber: string;
  readonly documentDate: string;
  readonly dueDate: string;
  readonly totalAmountPaise: number;
  readonly outstandingBalancePaise: number;
  readonly status: string;
}
