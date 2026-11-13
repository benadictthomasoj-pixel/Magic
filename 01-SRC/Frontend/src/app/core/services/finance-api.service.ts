import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PaginationMeta } from '../contracts/api.models';
import {
  BankBookReportDto,
  BankCashAccountDto,
  BankStatementDetailDto,
  BankStatementSummaryDto,
  CancelPaymentRequest,
  CancelReceiptRequest,
  CashBookReportDto,
  CreateBankCashAccountRequest,
  CreateContraTransactionRequest,
  CreateCustomerReceiptRequest,
  CreateSupplierBillRequest,
  CreateSupplierPaymentRequest,
  CreateTreasuryAdjustmentRequest,
  CustomerAgingReportDto,
  CustomerReceiptDto,
  CustomerReceivableDto,
  CustomerStatementDto,
  FinanceSummaryKpiDto,
  ImportBankStatementRequest,
  MatchStatementLineRequest,
  PayablesSummaryDto,
  ReceivablesSummaryDto,
  ReconciliationWorkspaceDto,
  RevealAccountNumberResponse,
  SupplierAgingReportDto,
  SupplierBillDto,
  SupplierPayableDto,
  SupplierPaymentDto,
  SupplierStatementDto,
  TreasuryTransactionDto,
  UnmatchStatementLineRequest,
  UpdateBankCashAccountRequest,
} from '../contracts/finance.models';

export interface PaginatedResult<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class FinanceApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  // ==========================================
  // 1. Receivables & Aging
  // ==========================================

  getReceivablesSummary(): Observable<ReceivablesSummaryDto> {
    return this.http
      .get<ApiResponse<ReceivablesSummaryDto>>(`${this.baseUrl}/v1/accounting/receivables/summary`)
      .pipe(map((res) => res.data));
  }

  getReceivables(params?: {
    customerId?: string;
    status?: string;
    overdueOnly?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<CustomerReceivableDto>> {
    let httpParams = new HttpParams();
    if (params?.customerId) httpParams = httpParams.set('customerId', params.customerId);
    if (params?.status) httpParams = httpParams.set('status', params.status);
    if (params?.overdueOnly) httpParams = httpParams.set('overdueOnly', 'true');
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<CustomerReceivableDto[], PaginationMeta>>(`${this.baseUrl}/v1/accounting/receivables`, { params: httpParams })
      .pipe(
        map((res) => ({
          items: res.data,
          totalCount: res.meta?.totalCount ?? res.data.length,
          page: res.meta?.page ?? 1,
          pageSize: res.meta?.pageSize ?? 50,
        }))
      );
  }

  getCustomerAging(reportDate?: string): Observable<CustomerAgingReportDto> {
    let httpParams = new HttpParams();
    if (reportDate) httpParams = httpParams.set('reportDate', reportDate);
    return this.http
      .get<ApiResponse<CustomerAgingReportDto>>(`${this.baseUrl}/v1/accounting/receivables/aging`, { params: httpParams })
      .pipe(map((res) => res.data));
  }

  getCustomerStatement(customerId: string, fromDate?: string, toDate?: string): Observable<CustomerStatementDto> {
    let httpParams = new HttpParams();
    if (fromDate) httpParams = httpParams.set('fromDate', fromDate);
    if (toDate) httpParams = httpParams.set('toDate', toDate);
    return this.http
      .get<ApiResponse<CustomerStatementDto>>(`${this.baseUrl}/v1/accounting/receivables/statement/${customerId}`, { params: httpParams })
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 2. Customer Receipts
  // ==========================================

  getReceipts(params?: {
    customerId?: string;
    fromDate?: string;
    toDate?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<CustomerReceiptDto>> {
    let httpParams = new HttpParams();
    if (params?.customerId) httpParams = httpParams.set('customerId', params.customerId);
    if (params?.fromDate) httpParams = httpParams.set('fromDate', params.fromDate);
    if (params?.toDate) httpParams = httpParams.set('toDate', params.toDate);
    if (params?.status) httpParams = httpParams.set('status', params.status);
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<CustomerReceiptDto[], PaginationMeta>>(`${this.baseUrl}/v1/accounting/customer-receipts`, { params: httpParams })
      .pipe(
        map((res) => ({
          items: res.data,
          totalCount: res.meta?.totalCount ?? res.data.length,
          page: res.meta?.page ?? 1,
          pageSize: res.meta?.pageSize ?? 50,
        }))
      );
  }

  getReceiptById(id: string): Observable<CustomerReceiptDto> {
    return this.http
      .get<ApiResponse<CustomerReceiptDto>>(`${this.baseUrl}/v1/accounting/customer-receipts/${id}`)
      .pipe(map((res) => res.data));
  }

  createReceipt(request: CreateCustomerReceiptRequest): Observable<CustomerReceiptDto> {
    return this.http
      .post<ApiResponse<CustomerReceiptDto>>(`${this.baseUrl}/v1/accounting/customer-receipts`, request)
      .pipe(map((res) => res.data));
  }

  cancelReceipt(id: string, request: CancelReceiptRequest): Observable<CustomerReceiptDto> {
    return this.http
      .post<ApiResponse<CustomerReceiptDto>>(`${this.baseUrl}/v1/accounting/customer-receipts/${id}/cancel`, request)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 3. Payables & Supplier Bills
  // ==========================================

  getPayablesSummary(): Observable<PayablesSummaryDto> {
    return this.http
      .get<ApiResponse<PayablesSummaryDto>>(`${this.baseUrl}/v1/accounting/payables/summary`)
      .pipe(map((res) => res.data));
  }

  getPayables(params?: {
    supplierId?: string;
    status?: string;
    overdueOnly?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<SupplierPayableDto>> {
    let httpParams = new HttpParams();
    if (params?.supplierId) httpParams = httpParams.set('supplierId', params.supplierId);
    if (params?.status) httpParams = httpParams.set('status', params.status);
    if (params?.overdueOnly) httpParams = httpParams.set('overdueOnly', 'true');
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<SupplierPayableDto[], PaginationMeta>>(`${this.baseUrl}/v1/accounting/payables`, { params: httpParams })
      .pipe(
        map((res) => ({
          items: res.data,
          totalCount: res.meta?.totalCount ?? res.data.length,
          page: res.meta?.page ?? 1,
          pageSize: res.meta?.pageSize ?? 50,
        }))
      );
  }

  getSupplierAging(reportDate?: string): Observable<SupplierAgingReportDto> {
    let httpParams = new HttpParams();
    if (reportDate) httpParams = httpParams.set('reportDate', reportDate);
    return this.http
      .get<ApiResponse<SupplierAgingReportDto>>(`${this.baseUrl}/v1/accounting/payables/aging`, { params: httpParams })
      .pipe(map((res) => res.data));
  }

  getSupplierStatement(supplierId: string, fromDate?: string, toDate?: string): Observable<SupplierStatementDto> {
    let httpParams = new HttpParams();
    if (fromDate) httpParams = httpParams.set('fromDate', fromDate);
    if (toDate) httpParams = httpParams.set('toDate', toDate);
    return this.http
      .get<ApiResponse<SupplierStatementDto>>(`${this.baseUrl}/v1/accounting/payables/statement/${supplierId}`, { params: httpParams })
      .pipe(map((res) => res.data));
  }

  createSupplierBill(request: CreateSupplierBillRequest): Observable<SupplierBillDto> {
    return this.http
      .post<ApiResponse<SupplierBillDto>>(`${this.baseUrl}/v1/accounting/payables/bills`, request)
      .pipe(map((res) => res.data));
  }

  getSupplierBillById(id: string): Observable<SupplierBillDto> {
    return this.http
      .get<ApiResponse<SupplierBillDto>>(`${this.baseUrl}/v1/accounting/payables/bills/${id}`)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 4. Supplier Payments
  // ==========================================

  getPayments(params?: {
    supplierId?: string;
    fromDate?: string;
    toDate?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<SupplierPaymentDto>> {
    let httpParams = new HttpParams();
    if (params?.supplierId) httpParams = httpParams.set('supplierId', params.supplierId);
    if (params?.fromDate) httpParams = httpParams.set('fromDate', params.fromDate);
    if (params?.toDate) httpParams = httpParams.set('toDate', params.toDate);
    if (params?.status) httpParams = httpParams.set('status', params.status);
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<SupplierPaymentDto[], PaginationMeta>>(`${this.baseUrl}/v1/accounting/supplier-payments`, { params: httpParams })
      .pipe(
        map((res) => ({
          items: res.data,
          totalCount: res.meta?.totalCount ?? res.data.length,
          page: res.meta?.page ?? 1,
          pageSize: res.meta?.pageSize ?? 50,
        }))
      );
  }

  getPaymentById(id: string): Observable<SupplierPaymentDto> {
    return this.http
      .get<ApiResponse<SupplierPaymentDto>>(`${this.baseUrl}/v1/accounting/supplier-payments/${id}`)
      .pipe(map((res) => res.data));
  }

  createPayment(request: CreateSupplierPaymentRequest): Observable<SupplierPaymentDto> {
    return this.http
      .post<ApiResponse<SupplierPaymentDto>>(`${this.baseUrl}/v1/accounting/supplier-payments`, request)
      .pipe(map((res) => res.data));
  }

  cancelPayment(id: string, request: CancelPaymentRequest): Observable<SupplierPaymentDto> {
    return this.http
      .post<ApiResponse<SupplierPaymentDto>>(`${this.baseUrl}/v1/accounting/supplier-payments/${id}/cancel`, request)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 5. Bank & Cash Accounts
  // ==========================================

  getBankAccounts(accountType?: string, activeOnly: boolean = false): Observable<readonly BankCashAccountDto[]> {
    let params = new HttpParams();
    if (accountType) params = params.set('accountType', accountType);
    if (activeOnly) params = params.set('activeOnly', 'true');

    return this.http
      .get<ApiResponse<BankCashAccountDto[]>>(`${this.baseUrl}/v1/accounting/bank-accounts`, { params })
      .pipe(map((res) => res.data));
  }

  getBankAccountById(id: string): Observable<BankCashAccountDto> {
    return this.http
      .get<ApiResponse<BankCashAccountDto>>(`${this.baseUrl}/v1/accounting/bank-accounts/${id}`)
      .pipe(map((res) => res.data));
  }

  createBankAccount(request: CreateBankCashAccountRequest): Observable<BankCashAccountDto> {
    return this.http
      .post<ApiResponse<BankCashAccountDto>>(`${this.baseUrl}/v1/accounting/bank-accounts`, request)
      .pipe(map((res) => res.data));
  }

  updateBankAccount(id: string, request: UpdateBankCashAccountRequest): Observable<BankCashAccountDto> {
    return this.http
      .put<ApiResponse<BankCashAccountDto>>(`${this.baseUrl}/v1/accounting/bank-accounts/${id}`, request)
      .pipe(map((res) => res.data));
  }

  revealAccountNumber(id: string): Observable<RevealAccountNumberResponse> {
    return this.http
      .post<ApiResponse<RevealAccountNumberResponse>>(`${this.baseUrl}/v1/accounting/bank-accounts/${id}/reveal`, {})
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 6. Treasury Operations
  // ==========================================

  getTreasuryTransactions(params?: {
    fromDate?: string;
    toDate?: string;
    transactionType?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<TreasuryTransactionDto>> {
    let httpParams = new HttpParams();
    if (params?.fromDate) httpParams = httpParams.set('fromDate', params.fromDate);
    if (params?.toDate) httpParams = httpParams.set('toDate', params.toDate);
    if (params?.transactionType) httpParams = httpParams.set('transactionType', params.transactionType);
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<TreasuryTransactionDto[], PaginationMeta>>(`${this.baseUrl}/v1/accounting/treasury`, { params: httpParams })
      .pipe(
        map((res) => ({
          items: res.data,
          totalCount: res.meta?.totalCount ?? res.data.length,
          page: res.meta?.page ?? 1,
          pageSize: res.meta?.pageSize ?? 50,
        }))
      );
  }

  createContra(request: CreateContraTransactionRequest): Observable<TreasuryTransactionDto> {
    return this.http
      .post<ApiResponse<TreasuryTransactionDto>>(`${this.baseUrl}/v1/accounting/treasury/contra`, request)
      .pipe(map((res) => res.data));
  }

  createAdjustment(request: CreateTreasuryAdjustmentRequest): Observable<TreasuryTransactionDto> {
    return this.http
      .post<ApiResponse<TreasuryTransactionDto>>(`${this.baseUrl}/v1/accounting/treasury/adjustment`, request)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 7. Bank Reconciliation
  // ==========================================

  getReconciliationStatements(bankAccountId?: string): Observable<readonly BankStatementSummaryDto[]> {
    let params = new HttpParams();
    if (bankAccountId) params = params.set('bankAccountId', bankAccountId);
    return this.http
      .get<ApiResponse<BankStatementSummaryDto[]>>(`${this.baseUrl}/v1/accounting/bank-reconciliation/statements`, { params })
      .pipe(map((res) => res.data));
  }

  importStatementCsv(request: ImportBankStatementRequest): Observable<BankStatementDetailDto> {
    return this.http
      .post<ApiResponse<BankStatementDetailDto>>(`${this.baseUrl}/v1/accounting/bank-reconciliation/import-csv`, request)
      .pipe(map((res) => res.data));
  }

  getReconciliationWorkspace(statementId: string): Observable<ReconciliationWorkspaceDto> {
    return this.http
      .get<ApiResponse<ReconciliationWorkspaceDto>>(`${this.baseUrl}/v1/accounting/bank-reconciliation/workspace/${statementId}`)
      .pipe(map((res) => res.data));
  }

  matchStatementLine(statementId: string, request: MatchStatementLineRequest): Observable<ReconciliationWorkspaceDto> {
    const params = new HttpParams().set('statementId', statementId);
    return this.http
      .post<ApiResponse<ReconciliationWorkspaceDto>>(`${this.baseUrl}/v1/accounting/bank-reconciliation/match`, request, { params })
      .pipe(map((res) => res.data));
  }

  unmatchStatementLine(statementId: string, request: UnmatchStatementLineRequest): Observable<ReconciliationWorkspaceDto> {
    const params = new HttpParams().set('statementId', statementId);
    return this.http
      .post<ApiResponse<ReconciliationWorkspaceDto>>(`${this.baseUrl}/v1/accounting/bank-reconciliation/unmatch`, request, { params })
      .pipe(map((res) => res.data));
  }

  completeReconciliation(statementId: string): Observable<BankStatementSummaryDto> {
    return this.http
      .post<ApiResponse<BankStatementSummaryDto>>(`${this.baseUrl}/v1/accounting/bank-reconciliation/complete/${statementId}`, {})
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 8. Financial Reports
  // ==========================================

  getFinancialSummary(): Observable<FinanceSummaryKpiDto> {
    return this.http
      .get<ApiResponse<FinanceSummaryKpiDto>>(`${this.baseUrl}/v1/accounting/financial-reports/summary`)
      .pipe(map((res) => res.data));
  }

  getCashBook(params?: { accountId?: string; fromDate?: string; toDate?: string }): Observable<CashBookReportDto> {
    let httpParams = new HttpParams();
    if (params?.accountId) httpParams = httpParams.set('accountId', params.accountId);
    if (params?.fromDate) httpParams = httpParams.set('fromDate', params.fromDate);
    if (params?.toDate) httpParams = httpParams.set('toDate', params.toDate);

    return this.http
      .get<ApiResponse<CashBookReportDto>>(`${this.baseUrl}/v1/accounting/financial-reports/cash-book`, { params: httpParams })
      .pipe(map((res) => res.data));
  }

  getBankBook(params?: { bankAccountId?: string; fromDate?: string; toDate?: string }): Observable<BankBookReportDto> {
    let httpParams = new HttpParams();
    if (params?.bankAccountId) httpParams = httpParams.set('bankAccountId', params.bankAccountId);
    if (params?.fromDate) httpParams = httpParams.set('fromDate', params.fromDate);
    if (params?.toDate) httpParams = httpParams.set('toDate', params.toDate);

    return this.http
      .get<ApiResponse<BankBookReportDto>>(`${this.baseUrl}/v1/accounting/financial-reports/bank-book`, { params: httpParams })
      .pipe(map((res) => res.data));
  }
}
