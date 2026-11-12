import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AccountDto,
  AccountGroupDto,
  AccountingDashboardDto,
  CreateAccountRequest,
  CreateJournalDraftRequest,
  FiscalYearDto,
  GeneralLedgerReportDto,
  JournalValidationResultDto,
  PostVoucherRequest,
  ReverseVoucherRequest,
  SubledgerReportDto,
  TrialBalanceReportDto,
  UpdateAccountRequest,
  VoucherDetailDto,
  VoucherSummaryDto,
} from '../contracts/accounting.models';
import { ApiResponse } from '../contracts/api.models';

@Injectable({ providedIn: 'root' })
export class AccountingApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getDashboard(): Observable<AccountingDashboardDto> {
    return this.http
      .get<ApiResponse<AccountingDashboardDto>>(`${this.baseUrl}/v1/accounting/dashboard`)
      .pipe(map((res) => res.data));
  }

  getAccountGroups(): Observable<readonly AccountGroupDto[]> {
    return this.http
      .get<ApiResponse<readonly AccountGroupDto[]>>(`${this.baseUrl}/v1/accounting/account-groups`)
      .pipe(map((res) => res.data));
  }

  getAccounts(
    rootClass?: string,
    activeOnly?: boolean,
    search?: string
  ): Observable<readonly AccountDto[]> {
    let params = new HttpParams();
    if (rootClass) params = params.set('rootClass', rootClass);
    if (activeOnly !== undefined) params = params.set('activeOnly', activeOnly.toString());
    if (search) params = params.set('search', search);

    return this.http
      .get<ApiResponse<readonly AccountDto[]>>(`${this.baseUrl}/v1/accounting/accounts`, { params })
      .pipe(map((res) => res.data));
  }

  getAccountById(id: string): Observable<AccountDto> {
    return this.http
      .get<ApiResponse<AccountDto>>(`${this.baseUrl}/v1/accounting/accounts/${id}`)
      .pipe(map((res) => res.data));
  }

  createAccount(request: CreateAccountRequest): Observable<AccountDto> {
    return this.http
      .post<ApiResponse<AccountDto>>(`${this.baseUrl}/v1/accounting/accounts`, request)
      .pipe(map((res) => res.data));
  }

  updateAccount(id: string, request: UpdateAccountRequest): Observable<AccountDto> {
    return this.http
      .put<ApiResponse<AccountDto>>(`${this.baseUrl}/v1/accounting/accounts/${id}`, request)
      .pipe(map((res) => res.data));
  }

  getVouchers(filters?: {
    dateFrom?: string;
    dateTo?: string;
    voucherType?: string;
    status?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }): Observable<{ items: readonly VoucherSummaryDto[]; total: number }> {
    let params = new HttpParams();
    if (filters?.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters?.dateTo) params = params.set('dateTo', filters.dateTo);
    if (filters?.voucherType) params = params.set('voucherType', filters.voucherType);
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.page) params = params.set('page', filters.page.toString());
    if (filters?.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http
      .get<ApiResponse<readonly VoucherSummaryDto[]>>(`${this.baseUrl}/v1/accounting/vouchers`, { params })
      .pipe(
        map((res) => {
          const meta = res.meta as { totalCount?: number } | undefined;
          return {
            items: res.data,
            total: meta?.totalCount ?? res.data.length,
          };
        })
      );
  }

  getVoucherById(id: string): Observable<VoucherDetailDto> {
    return this.http
      .get<ApiResponse<VoucherDetailDto>>(`${this.baseUrl}/v1/accounting/vouchers/${id}`)
      .pipe(map((res) => res.data));
  }

  createJournalDraft(request: CreateJournalDraftRequest): Observable<VoucherDetailDto> {
    return this.http
      .post<ApiResponse<VoucherDetailDto>>(`${this.baseUrl}/v1/accounting/vouchers/draft`, request)
      .pipe(map((res) => res.data));
  }

  validateJournal(request: CreateJournalDraftRequest): Observable<JournalValidationResultDto> {
    return this.http
      .post<ApiResponse<JournalValidationResultDto>>(`${this.baseUrl}/v1/accounting/vouchers/validate`, request)
      .pipe(map((res) => res.data));
  }

  postVoucher(id: string, request: PostVoucherRequest): Observable<VoucherDetailDto> {
    return this.http
      .post<ApiResponse<VoucherDetailDto>>(`${this.baseUrl}/v1/accounting/vouchers/${id}/post`, request)
      .pipe(map((res) => res.data));
  }

  reverseVoucher(id: string, request: ReverseVoucherRequest): Observable<VoucherDetailDto> {
    return this.http
      .post<ApiResponse<VoucherDetailDto>>(`${this.baseUrl}/v1/accounting/vouchers/${id}/reverse`, request)
      .pipe(map((res) => res.data));
  }

  getGeneralLedger(accountId: string, dateFrom?: string, dateTo?: string): Observable<GeneralLedgerReportDto> {
    let params = new HttpParams().set('accountId', accountId);
    if (dateFrom) params = params.set('dateFrom', dateFrom);
    if (dateTo) params = params.set('dateTo', dateTo);

    return this.http
      .get<ApiResponse<GeneralLedgerReportDto>>(`${this.baseUrl}/v1/accounting/general-ledger`, { params })
      .pipe(map((res) => res.data));
  }

  getTrialBalance(asOfDate?: string): Observable<TrialBalanceReportDto> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);

    return this.http
      .get<ApiResponse<TrialBalanceReportDto>>(`${this.baseUrl}/v1/accounting/trial-balance`, { params })
      .pipe(map((res) => res.data));
  }

  getFiscalYears(): Observable<readonly FiscalYearDto[]> {
    return this.http
      .get<ApiResponse<readonly FiscalYearDto[]>>(`${this.baseUrl}/v1/accounting/fiscal-years`)
      .pipe(map((res) => res.data));
  }

  lockFiscalPeriod(id: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.baseUrl}/v1/accounting/fiscal-periods/${id}/lock`, {})
      .pipe(map(() => void 0));
  }

  closeFiscalPeriod(id: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.baseUrl}/v1/accounting/fiscal-periods/${id}/close`, {})
      .pipe(map(() => void 0));
  }

  reopenFiscalPeriod(id: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.baseUrl}/v1/accounting/fiscal-periods/${id}/reopen`, {})
      .pipe(map(() => void 0));
  }

  getSubledgerReport(type: 'ar' | 'ap'): Observable<SubledgerReportDto> {
    return this.http
      .get<ApiResponse<SubledgerReportDto>>(`${this.baseUrl}/v1/accounting/subledgers/${type}`)
      .pipe(map((res) => res.data));
  }
}
