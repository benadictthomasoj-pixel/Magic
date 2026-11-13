import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PaginationMeta } from '../contracts/api.models';
import {
  CashMovementDto,
  CashierSessionDto,
  CloseCashierSessionRequest,
  CreatePosSaleRequest,
  CreateSalesReturnRequest,
  CreditNoteDto,
  HourlySalesDto,
  OpenCashierSessionRequest,
  PosProductLookupDto,
  RecordCashMovementRequest,
  SalesByPaymentMethodDto,
  SalesByProductDto,
  SalesInvoiceDto,
  SalesReportSummaryDto,
  SalesReturnDto,
} from '../contracts/sales.models';

export interface PaginatedResult<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class SalesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  // ==========================================
  // 1. POS Operations
  // ==========================================

  lookupProducts(warehouseId: string, query?: string): Observable<readonly PosProductLookupDto[]> {
    let params = new HttpParams().set('warehouseId', warehouseId);
    if (query) {
      params = params.set('query', query.trim());
    }
    return this.http
      .get<ApiResponse<PosProductLookupDto[]>>(`${this.baseUrl}/v1/pos/lookup`, { params })
      .pipe(map((res) => res.data));
  }

  checkout(request: CreatePosSaleRequest): Observable<SalesInvoiceDto> {
    return this.http
      .post<ApiResponse<SalesInvoiceDto>>(`${this.baseUrl}/v1/pos/checkout`, request)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 2. Sales Invoices
  // ==========================================

  getInvoices(params?: {
    branchId?: string;
    search?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<SalesInvoiceDto>> {
    let p = new HttpParams();
    if (params?.branchId) p = p.set('branchId', params.branchId);
    if (params?.search) p = p.set('search', params.search.trim());
    if (params?.status) p = p.set('status', params.status);
    if (params?.fromDate) p = p.set('fromDate', params.fromDate);
    if (params?.toDate) p = p.set('toDate', params.toDate);
    if (params?.page) p = p.set('page', params.page.toString());
    if (params?.pageSize) p = p.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<SalesInvoiceDto[], PaginationMeta>>(`${this.baseUrl}/v1/sales/invoices`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data ?? [],
          totalCount: res.meta?.totalCount ?? 0,
          page: res.meta?.page ?? 1,
          pageSize: res.meta?.pageSize ?? 20,
        }))
      );
  }

  getInvoiceById(id: string): Observable<SalesInvoiceDto> {
    return this.http
      .get<ApiResponse<SalesInvoiceDto>>(`${this.baseUrl}/v1/sales/invoices/${id}`)
      .pipe(map((res) => res.data));
  }

  cancelInvoice(id: string, reason: string): Observable<SalesInvoiceDto> {
    return this.http
      .post<ApiResponse<SalesInvoiceDto>>(`${this.baseUrl}/v1/sales/invoices/${id}/cancel`, { reason })
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 3. Sales Returns & Credit Notes
  // ==========================================

  getReturns(params?: {
    branchId?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<SalesReturnDto>> {
    let p = new HttpParams();
    if (params?.branchId) p = p.set('branchId', params.branchId);
    if (params?.search) p = p.set('search', params.search.trim());
    if (params?.page) p = p.set('page', params.page.toString());
    if (params?.pageSize) p = p.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<SalesReturnDto[], PaginationMeta>>(`${this.baseUrl}/v1/sales/returns`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data ?? [],
          totalCount: res.meta?.totalCount ?? 0,
          page: res.meta?.page ?? 1,
          pageSize: res.meta?.pageSize ?? 20,
        }))
      );
  }

  getReturnById(id: string): Observable<SalesReturnDto> {
    return this.http
      .get<ApiResponse<SalesReturnDto>>(`${this.baseUrl}/v1/sales/returns/${id}`)
      .pipe(map((res) => res.data));
  }

  createReturn(request: CreateSalesReturnRequest): Observable<SalesReturnDto> {
    return this.http
      .post<ApiResponse<SalesReturnDto>>(`${this.baseUrl}/v1/sales/returns`, request)
      .pipe(map((res) => res.data));
  }

  getCreditNotes(params?: {
    customerId?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<CreditNoteDto>> {
    let p = new HttpParams();
    if (params?.customerId) p = p.set('customerId', params.customerId);
    if (params?.page) p = p.set('page', params.page.toString());
    if (params?.pageSize) p = p.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<CreditNoteDto[], PaginationMeta>>(`${this.baseUrl}/v1/sales/credit-notes`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data ?? [],
          totalCount: res.meta?.totalCount ?? 0,
          page: res.meta?.page ?? 1,
          pageSize: res.meta?.pageSize ?? 20,
        }))
      );
  }

  getCreditNoteById(id: string): Observable<CreditNoteDto> {
    return this.http
      .get<ApiResponse<CreditNoteDto>>(`${this.baseUrl}/v1/sales/credit-notes/${id}`)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 4. Cashier Sessions & Cash Management
  // ==========================================

  getActiveSession(): Observable<CashierSessionDto | null> {
    return this.http
      .get<ApiResponse<CashierSessionDto | null>>(`${this.baseUrl}/v1/cashier/sessions/active`)
      .pipe(map((res) => res.data));
  }

  getSessions(params?: {
    branchId?: string;
    userId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<CashierSessionDto>> {
    let p = new HttpParams();
    if (params?.branchId) p = p.set('branchId', params.branchId);
    if (params?.userId) p = p.set('userId', params.userId);
    if (params?.status) p = p.set('status', params.status);
    if (params?.page) p = p.set('page', params.page.toString());
    if (params?.pageSize) p = p.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<CashierSessionDto[], PaginationMeta>>(`${this.baseUrl}/v1/cashier/sessions`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data ?? [],
          totalCount: res.meta?.totalCount ?? 0,
          page: res.meta?.page ?? 1,
          pageSize: res.meta?.pageSize ?? 20,
        }))
      );
  }

  getSessionById(id: string): Observable<CashierSessionDto> {
    return this.http
      .get<ApiResponse<CashierSessionDto>>(`${this.baseUrl}/v1/cashier/sessions/${id}`)
      .pipe(map((res) => res.data));
  }

  openSession(request: OpenCashierSessionRequest): Observable<CashierSessionDto> {
    return this.http
      .post<ApiResponse<CashierSessionDto>>(`${this.baseUrl}/v1/cashier/sessions/open`, request)
      .pipe(map((res) => res.data));
  }

  recordMovement(sessionId: string, request: RecordCashMovementRequest): Observable<CashMovementDto> {
    return this.http
      .post<ApiResponse<CashMovementDto>>(`${this.baseUrl}/v1/cashier/sessions/${sessionId}/movements`, request)
      .pipe(map((res) => res.data));
  }

  closeSession(sessionId: string, request: CloseCashierSessionRequest): Observable<CashierSessionDto> {
    return this.http
      .post<ApiResponse<CashierSessionDto>>(`${this.baseUrl}/v1/cashier/sessions/${sessionId}/close`, request)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 5. Sales Reports & Analytics
  // ==========================================

  getSummary(params?: {
    branchId?: string;
    fromDate?: string;
    toDate?: string;
  }): Observable<SalesReportSummaryDto> {
    let p = new HttpParams();
    if (params?.branchId) p = p.set('branchId', params.branchId);
    if (params?.fromDate) p = p.set('fromDate', params.fromDate);
    if (params?.toDate) p = p.set('toDate', params.toDate);

    return this.http
      .get<ApiResponse<SalesReportSummaryDto>>(`${this.baseUrl}/v1/sales/reports/summary`, { params: p })
      .pipe(map((res) => res.data));
  }

  getPaymentBreakdown(params?: {
    branchId?: string;
    fromDate?: string;
    toDate?: string;
  }): Observable<readonly SalesByPaymentMethodDto[]> {
    let p = new HttpParams();
    if (params?.branchId) p = p.set('branchId', params.branchId);
    if (params?.fromDate) p = p.set('fromDate', params.fromDate);
    if (params?.toDate) p = p.set('toDate', params.toDate);

    return this.http
      .get<ApiResponse<SalesByPaymentMethodDto[]>>(`${this.baseUrl}/v1/sales/reports/payment-methods`, { params: p })
      .pipe(map((res) => res.data));
  }

  getTopProducts(params?: {
    branchId?: string;
    fromDate?: string;
    toDate?: string;
    limit?: number;
  }): Observable<readonly SalesByProductDto[]> {
    let p = new HttpParams();
    if (params?.branchId) p = p.set('branchId', params.branchId);
    if (params?.fromDate) p = p.set('fromDate', params.fromDate);
    if (params?.toDate) p = p.set('toDate', params.toDate);
    if (params?.limit) p = p.set('limit', params.limit.toString());

    return this.http
      .get<ApiResponse<SalesByProductDto[]>>(`${this.baseUrl}/v1/sales/reports/top-products`, { params: p })
      .pipe(map((res) => res.data));
  }

  getHourlySales(params?: {
    branchId?: string;
    date?: string;
  }): Observable<readonly HourlySalesDto[]> {
    let p = new HttpParams();
    if (params?.branchId) p = p.set('branchId', params.branchId);
    if (params?.date) p = p.set('date', params.date);

    return this.http
      .get<ApiResponse<HourlySalesDto[]>>(`${this.baseUrl}/v1/sales/reports/hourly`, { params: p })
      .pipe(map((res) => res.data));
  }
}
