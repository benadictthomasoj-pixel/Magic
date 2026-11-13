import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PaginationMeta } from '../contracts/api.models';
import {
  CreateGoodsReceiptRequest,
  CreatePurchaseOrderRequest,
  CreatePurchaseReturnRequest,
  CreateStockAdjustmentRequest,
  CreateStockCountRequest,
  CreateStockTransferRequest,
  CreateStorageLocationRequest,
  CreateWarehouseRequest,
  DispatchStockTransferRequest,
  GoodsReceiptNoteDto,
  PostOpeningStockRequest,
  PurchaseOrderDto,
  PurchaseReturnDto,
  ReceiveStockTransferRequest,
  RecordStockCountLinesRequest,
  ReorderRuleDto,
  ReorderSuggestionDto,
  StockAdjustmentDto,
  StockBalanceDto,
  StockCountDto,
  StockMovementDto,
  StockTransferDto,
  StorageLocationDto,
  UpdateWarehouseRequest,
  UpsertReorderRuleRequest,
  WarehouseDto,
} from '../contracts/inventory.models';

export interface PaginatedResult<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  // ==========================================
  // 1. Warehouses & Storage Locations
  // ==========================================

  getWarehouses(params?: {
    searchTerm?: string;
    branchId?: string;
    isActive?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<WarehouseDto>> {
    let p = new HttpParams();
    if (params?.searchTerm) p = p.set('searchTerm', params.searchTerm);
    if (params?.branchId) p = p.set('branchId', params.branchId);
    if (params?.isActive !== undefined) p = p.set('isActive', String(params.isActive));
    if (params?.page) p = p.set('page', String(params.page));
    if (params?.pageSize) p = p.set('pageSize', String(params.pageSize));

    return this.http
      .get<ApiResponse<readonly WarehouseDto[], PaginationMeta>>(`${this.baseUrl}/api/v1/inventory/warehouses`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data || [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? params?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? params?.pageSize ?? (res.data?.length || 20)),
        }))
      );
  }

  getWarehouse(id: string): Observable<WarehouseDto> {
    return this.http
      .get<ApiResponse<WarehouseDto>>(`${this.baseUrl}/api/v1/inventory/warehouses/${id}`)
      .pipe(map((res) => res.data));
  }

  createWarehouse(req: CreateWarehouseRequest): Observable<WarehouseDto> {
    return this.http
      .post<ApiResponse<WarehouseDto>>(`${this.baseUrl}/api/v1/inventory/warehouses`, req)
      .pipe(map((res) => res.data));
  }

  updateWarehouse(id: string, req: UpdateWarehouseRequest): Observable<WarehouseDto> {
    return this.http
      .put<ApiResponse<WarehouseDto>>(`${this.baseUrl}/api/v1/inventory/warehouses/${id}`, req)
      .pipe(map((res) => res.data));
  }

  archiveWarehouse(id: string): Observable<void> {
    return this.http
      .post<ApiResponse<object>>(`${this.baseUrl}/api/v1/inventory/warehouses/${id}/archive`, {})
      .pipe(map(() => void 0));
  }

  getStorageLocations(warehouseId: string): Observable<readonly StorageLocationDto[]> {
    return this.http
      .get<ApiResponse<readonly StorageLocationDto[]>>(`${this.baseUrl}/api/v1/inventory/warehouses/${warehouseId}/locations`)
      .pipe(map((res) => res.data || []));
  }

  createStorageLocation(warehouseId: string, req: CreateStorageLocationRequest): Observable<StorageLocationDto> {
    return this.http
      .post<ApiResponse<StorageLocationDto>>(`${this.baseUrl}/api/v1/inventory/warehouses/${warehouseId}/locations`, req)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 2. Stock Balances & Ledger Movements
  // ==========================================

  getStockBalances(params?: {
    warehouseId?: string;
    productId?: string;
    locationId?: string;
    batchNumber?: string;
    lowStockOnly?: boolean;
    searchTerm?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<StockBalanceDto>> {
    let p = new HttpParams();
    if (params?.warehouseId) p = p.set('warehouseId', params.warehouseId);
    if (params?.productId) p = p.set('productId', params.productId);
    if (params?.locationId) p = p.set('locationId', params.locationId);
    if (params?.batchNumber) p = p.set('batchNumber', params.batchNumber);
    if (params?.lowStockOnly !== undefined) p = p.set('lowStockOnly', String(params.lowStockOnly));
    if (params?.searchTerm) p = p.set('searchTerm', params.searchTerm);
    if (params?.page) p = p.set('page', String(params.page));
    if (params?.pageSize) p = p.set('pageSize', String(params.pageSize));

    return this.http
      .get<ApiResponse<readonly StockBalanceDto[], PaginationMeta>>(`${this.baseUrl}/api/v1/inventory/stock/balances`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data || [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? params?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? params?.pageSize ?? (res.data?.length || 20)),
        }))
      );
  }

  getStockMovements(params?: {
    warehouseId?: string;
    productId?: string;
    movementType?: string;
    referenceNumber?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<StockMovementDto>> {
    let p = new HttpParams();
    if (params?.warehouseId) p = p.set('warehouseId', params.warehouseId);
    if (params?.productId) p = p.set('productId', params.productId);
    if (params?.movementType) p = p.set('movementType', params.movementType);
    if (params?.referenceNumber) p = p.set('referenceNumber', params.referenceNumber);
    if (params?.fromDate) p = p.set('fromDate', params.fromDate);
    if (params?.toDate) p = p.set('toDate', params.toDate);
    if (params?.page) p = p.set('page', String(params.page));
    if (params?.pageSize) p = p.set('pageSize', String(params.pageSize));

    return this.http
      .get<ApiResponse<readonly StockMovementDto[], PaginationMeta>>(`${this.baseUrl}/api/v1/inventory/stock/movements`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data || [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? params?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? params?.pageSize ?? (res.data?.length || 20)),
        }))
      );
  }

  postOpeningStock(req: PostOpeningStockRequest): Observable<void> {
    return this.http
      .post<ApiResponse<object>>(`${this.baseUrl}/api/v1/inventory/stock/opening-stock`, req)
      .pipe(map(() => void 0));
  }

  // ==========================================
  // 3. Stock Adjustments
  // ==========================================

  getStockAdjustments(params?: {
    warehouseId?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<StockAdjustmentDto>> {
    let p = new HttpParams();
    if (params?.warehouseId) p = p.set('warehouseId', params.warehouseId);
    if (params?.status) p = p.set('status', params.status);
    if (params?.fromDate) p = p.set('fromDate', params.fromDate);
    if (params?.toDate) p = p.set('toDate', params.toDate);
    if (params?.page) p = p.set('page', String(params.page));
    if (params?.pageSize) p = p.set('pageSize', String(params.pageSize));

    return this.http
      .get<ApiResponse<readonly StockAdjustmentDto[], PaginationMeta>>(`${this.baseUrl}/api/v1/inventory/adjustments`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data || [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? params?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? params?.pageSize ?? (res.data?.length || 20)),
        }))
      );
  }

  getStockAdjustment(id: string): Observable<StockAdjustmentDto> {
    return this.http
      .get<ApiResponse<StockAdjustmentDto>>(`${this.baseUrl}/api/v1/inventory/adjustments/${id}`)
      .pipe(map((res) => res.data));
  }

  createStockAdjustment(req: CreateStockAdjustmentRequest): Observable<StockAdjustmentDto> {
    return this.http
      .post<ApiResponse<StockAdjustmentDto>>(`${this.baseUrl}/api/v1/inventory/adjustments`, req)
      .pipe(map((res) => res.data));
  }

  approveStockAdjustment(id: string): Observable<StockAdjustmentDto> {
    return this.http
      .post<ApiResponse<StockAdjustmentDto>>(`${this.baseUrl}/api/v1/inventory/adjustments/${id}/approve`, {})
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 4. Stock Counts
  // ==========================================

  getStockCounts(params?: {
    warehouseId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<StockCountDto>> {
    let p = new HttpParams();
    if (params?.warehouseId) p = p.set('warehouseId', params.warehouseId);
    if (params?.status) p = p.set('status', params.status);
    if (params?.page) p = p.set('page', String(params.page));
    if (params?.pageSize) p = p.set('pageSize', String(params.pageSize));

    return this.http
      .get<ApiResponse<readonly StockCountDto[], PaginationMeta>>(`${this.baseUrl}/api/v1/inventory/counts`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data || [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? params?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? params?.pageSize ?? (res.data?.length || 20)),
        }))
      );
  }

  getStockCount(id: string): Observable<StockCountDto> {
    return this.http
      .get<ApiResponse<StockCountDto>>(`${this.baseUrl}/api/v1/inventory/counts/${id}`)
      .pipe(map((res) => res.data));
  }

  createStockCount(req: CreateStockCountRequest): Observable<StockCountDto> {
    return this.http
      .post<ApiResponse<StockCountDto>>(`${this.baseUrl}/api/v1/inventory/counts`, req)
      .pipe(map((res) => res.data));
  }

  recordStockCountLines(id: string, req: RecordStockCountLinesRequest): Observable<StockCountDto> {
    return this.http
      .post<ApiResponse<StockCountDto>>(`${this.baseUrl}/api/v1/inventory/counts/${id}/record-lines`, req)
      .pipe(map((res) => res.data));
  }

  reconcileStockCount(id: string): Observable<StockCountDto> {
    return this.http
      .post<ApiResponse<StockCountDto>>(`${this.baseUrl}/api/v1/inventory/counts/${id}/reconcile`, {})
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 5. Purchase Orders
  // ==========================================

  getPurchaseOrders(params?: {
    warehouseId?: string;
    supplierId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<PurchaseOrderDto>> {
    let p = new HttpParams();
    if (params?.warehouseId) p = p.set('warehouseId', params.warehouseId);
    if (params?.supplierId) p = p.set('supplierId', params.supplierId);
    if (params?.status) p = p.set('status', params.status);
    if (params?.page) p = p.set('page', String(params.page));
    if (params?.pageSize) p = p.set('pageSize', String(params.pageSize));

    return this.http
      .get<ApiResponse<readonly PurchaseOrderDto[], PaginationMeta>>(`${this.baseUrl}/api/v1/procurement/purchase-orders`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data || [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? params?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? params?.pageSize ?? (res.data?.length || 20)),
        }))
      );
  }

  getPurchaseOrder(id: string): Observable<PurchaseOrderDto> {
    return this.http
      .get<ApiResponse<PurchaseOrderDto>>(`${this.baseUrl}/api/v1/procurement/purchase-orders/${id}`)
      .pipe(map((res) => res.data));
  }

  createPurchaseOrder(req: CreatePurchaseOrderRequest): Observable<PurchaseOrderDto> {
    return this.http
      .post<ApiResponse<PurchaseOrderDto>>(`${this.baseUrl}/api/v1/procurement/purchase-orders`, req)
      .pipe(map((res) => res.data));
  }

  approvePurchaseOrder(id: string): Observable<PurchaseOrderDto> {
    return this.http
      .post<ApiResponse<PurchaseOrderDto>>(`${this.baseUrl}/api/v1/procurement/purchase-orders/${id}/approve`, {})
      .pipe(map((res) => res.data));
  }

  cancelPurchaseOrder(id: string): Observable<PurchaseOrderDto> {
    return this.http
      .post<ApiResponse<PurchaseOrderDto>>(`${this.baseUrl}/api/v1/procurement/purchase-orders/${id}/cancel`, {})
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 6. Goods Receipt Notes (GRN)
  // ==========================================

  getGoodsReceipts(params?: {
    warehouseId?: string;
    supplierId?: string;
    purchaseOrderId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<GoodsReceiptNoteDto>> {
    let p = new HttpParams();
    if (params?.warehouseId) p = p.set('warehouseId', params.warehouseId);
    if (params?.supplierId) p = p.set('supplierId', params.supplierId);
    if (params?.purchaseOrderId) p = p.set('purchaseOrderId', params.purchaseOrderId);
    if (params?.status) p = p.set('status', params.status);
    if (params?.page) p = p.set('page', String(params.page));
    if (params?.pageSize) p = p.set('pageSize', String(params.pageSize));

    return this.http
      .get<ApiResponse<readonly GoodsReceiptNoteDto[], PaginationMeta>>(`${this.baseUrl}/api/v1/procurement/goods-receipts`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data || [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? params?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? params?.pageSize ?? (res.data?.length || 20)),
        }))
      );
  }

  getGoodsReceipt(id: string): Observable<GoodsReceiptNoteDto> {
    return this.http
      .get<ApiResponse<GoodsReceiptNoteDto>>(`${this.baseUrl}/api/v1/procurement/goods-receipts/${id}`)
      .pipe(map((res) => res.data));
  }

  createGoodsReceipt(req: CreateGoodsReceiptRequest): Observable<GoodsReceiptNoteDto> {
    return this.http
      .post<ApiResponse<GoodsReceiptNoteDto>>(`${this.baseUrl}/api/v1/procurement/goods-receipts`, req)
      .pipe(map((res) => res.data));
  }

  confirmGoodsReceipt(id: string): Observable<GoodsReceiptNoteDto> {
    return this.http
      .post<ApiResponse<GoodsReceiptNoteDto>>(`${this.baseUrl}/api/v1/procurement/goods-receipts/${id}/confirm`, {})
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 7. Purchase Returns
  // ==========================================

  getPurchaseReturns(params?: {
    warehouseId?: string;
    supplierId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<PurchaseReturnDto>> {
    let p = new HttpParams();
    if (params?.warehouseId) p = p.set('warehouseId', params.warehouseId);
    if (params?.supplierId) p = p.set('supplierId', params.supplierId);
    if (params?.status) p = p.set('status', params.status);
    if (params?.page) p = p.set('page', String(params.page));
    if (params?.pageSize) p = p.set('pageSize', String(params.pageSize));

    return this.http
      .get<ApiResponse<readonly PurchaseReturnDto[], PaginationMeta>>(`${this.baseUrl}/api/v1/procurement/purchase-returns`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data || [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? params?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? params?.pageSize ?? (res.data?.length || 20)),
        }))
      );
  }

  getPurchaseReturn(id: string): Observable<PurchaseReturnDto> {
    return this.http
      .get<ApiResponse<PurchaseReturnDto>>(`${this.baseUrl}/api/v1/procurement/purchase-returns/${id}`)
      .pipe(map((res) => res.data));
  }

  createPurchaseReturn(req: CreatePurchaseReturnRequest): Observable<PurchaseReturnDto> {
    return this.http
      .post<ApiResponse<PurchaseReturnDto>>(`${this.baseUrl}/api/v1/procurement/purchase-returns`, req)
      .pipe(map((res) => res.data));
  }

  confirmPurchaseReturn(id: string): Observable<PurchaseReturnDto> {
    return this.http
      .post<ApiResponse<PurchaseReturnDto>>(`${this.baseUrl}/api/v1/procurement/purchase-returns/${id}/confirm`, {})
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 8. Stock Transfers
  // ==========================================

  getStockTransfers(params?: {
    sourceWarehouseId?: string;
    destinationWarehouseId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<StockTransferDto>> {
    let p = new HttpParams();
    if (params?.sourceWarehouseId) p = p.set('sourceWarehouseId', params.sourceWarehouseId);
    if (params?.destinationWarehouseId) p = p.set('destinationWarehouseId', params.destinationWarehouseId);
    if (params?.status) p = p.set('status', params.status);
    if (params?.page) p = p.set('page', String(params.page));
    if (params?.pageSize) p = p.set('pageSize', String(params.pageSize));

    return this.http
      .get<ApiResponse<readonly StockTransferDto[], PaginationMeta>>(`${this.baseUrl}/api/v1/inventory/transfers`, { params: p })
      .pipe(
        map((res) => ({
          items: res.data || [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? params?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? params?.pageSize ?? (res.data?.length || 20)),
        }))
      );
  }

  getStockTransfer(id: string): Observable<StockTransferDto> {
    return this.http
      .get<ApiResponse<StockTransferDto>>(`${this.baseUrl}/api/v1/inventory/transfers/${id}`)
      .pipe(map((res) => res.data));
  }

  createStockTransfer(req: CreateStockTransferRequest): Observable<StockTransferDto> {
    return this.http
      .post<ApiResponse<StockTransferDto>>(`${this.baseUrl}/api/v1/inventory/transfers`, req)
      .pipe(map((res) => res.data));
  }

  dispatchStockTransfer(id: string, req: DispatchStockTransferRequest): Observable<StockTransferDto> {
    return this.http
      .post<ApiResponse<StockTransferDto>>(`${this.baseUrl}/api/v1/inventory/transfers/${id}/dispatch`, req)
      .pipe(map((res) => res.data));
  }

  receiveStockTransfer(id: string, req: ReceiveStockTransferRequest): Observable<StockTransferDto> {
    return this.http
      .post<ApiResponse<StockTransferDto>>(`${this.baseUrl}/api/v1/inventory/transfers/${id}/receive`, req)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // 9. Reorder Rules & Replenishment Suggestions
  // ==========================================

  getReorderRules(warehouseId?: string, productId?: string): Observable<readonly ReorderRuleDto[]> {
    let p = new HttpParams();
    if (warehouseId) p = p.set('warehouseId', warehouseId);
    if (productId) p = p.set('productId', productId);

    return this.http
      .get<ApiResponse<readonly ReorderRuleDto[]>>(`${this.baseUrl}/api/v1/inventory/reorder/rules`, { params: p })
      .pipe(map((res) => res.data || []));
  }

  upsertReorderRule(req: UpsertReorderRuleRequest): Observable<ReorderRuleDto> {
    return this.http
      .post<ApiResponse<ReorderRuleDto>>(`${this.baseUrl}/api/v1/inventory/reorder/rules`, req)
      .pipe(map((res) => res.data));
  }

  getReorderSuggestions(warehouseId?: string): Observable<readonly ReorderSuggestionDto[]> {
    let p = new HttpParams();
    if (warehouseId) p = p.set('warehouseId', warehouseId);

    return this.http
      .get<ApiResponse<readonly ReorderSuggestionDto[]>>(`${this.baseUrl}/api/v1/inventory/reorder/suggestions`, { params: p })
      .pipe(map((res) => res.data || []));
  }
}
