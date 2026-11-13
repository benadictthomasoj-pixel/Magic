import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PaginationMeta } from '../contracts/api.models';
import {
  BarcodeLookupResultDto,
  BrandDto,
  CategoryDto,
  CreateBrandRequest,
  CreateCategoryRequest,
  CreateCustomerGroupRequest,
  CreateCustomerRequest,
  CreateDepartmentRequest,
  CreateProductRequest,
  CreateSupplierRequest,
  CustomerDetailDto,
  CustomerGroupDto,
  CustomerSummaryDto,
  CustomerType,
  DepartmentDto,
  GstRegistrationType,
  ProductDetailDto,
  ProductSummaryDto,
  ProductType,
  SupplierDetailDto,
  SupplierSummaryDto,
  TaxCodeDto,
  UnitOfMeasureDto,
  UnmaskedBankAccountDto,
  UpdateBrandRequest,
  UpdateCategoryRequest,
  UpdateCreditLimitRequest,
  UpdateCustomerRequest,
  UpdateDepartmentRequest,
  UpdateProductRequest,
  UpdateSupplierRequest,
} from '../contracts/commercial.models';

export interface PaginatedResult<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class CommercialApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  // -------------------------------------------------------------
  // Products
  // -------------------------------------------------------------
  getProducts(options?: {
    search?: string;
    categoryId?: string;
    brandId?: string;
    productType?: ProductType;
    isActive?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<ProductSummaryDto>> {
    let params = new HttpParams();
    if (options?.search) params = params.set('search', options.search);
    if (options?.categoryId) params = params.set('categoryId', options.categoryId);
    if (options?.brandId) params = params.set('brandId', options.brandId);
    if (options?.productType !== undefined) params = params.set('productType', options.productType.toString());
    if (options?.isActive !== undefined) params = params.set('isActive', options.isActive.toString());
    if (options?.page) params = params.set('page', options.page.toString());
    if (options?.pageSize) params = params.set('pageSize', options.pageSize.toString());

    return this.http
      .get<ApiResponse<readonly ProductSummaryDto[], PaginationMeta>>(`${this.baseUrl}/v1/commercial/products`, { params })
      .pipe(
        map((res) => ({
          items: res.data ?? [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? options?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? options?.pageSize ?? 25),
        }))
      );
  }

  getProductById(id: string): Observable<ProductDetailDto> {
    return this.http
      .get<ApiResponse<ProductDetailDto>>(`${this.baseUrl}/v1/commercial/products/${id}`)
      .pipe(map((res) => res.data));
  }

  lookupBarcode(barcode: string): Observable<BarcodeLookupResultDto> {
    return this.http
      .get<ApiResponse<BarcodeLookupResultDto>>(`${this.baseUrl}/v1/commercial/products/lookup/barcode/${encodeURIComponent(barcode)}`)
      .pipe(map((res) => res.data));
  }

  createProduct(request: CreateProductRequest): Observable<ProductDetailDto> {
    return this.http
      .post<ApiResponse<ProductDetailDto>>(`${this.baseUrl}/v1/commercial/products`, request)
      .pipe(map((res) => res.data));
  }

  updateProduct(id: string, request: UpdateProductRequest): Observable<ProductDetailDto> {
    return this.http
      .put<ApiResponse<ProductDetailDto>>(`${this.baseUrl}/v1/commercial/products/${id}`, request)
      .pipe(map((res) => res.data));
  }

  deactivateProduct(id: string): Observable<string> {
    return this.http
      .delete<ApiResponse<string>>(`${this.baseUrl}/v1/commercial/products/${id}`)
      .pipe(map((res) => res.data));
  }

  // -------------------------------------------------------------
  // Categories, Brands, Departments, Units & Tax Codes
  // -------------------------------------------------------------
  getCategoriesTree(): Observable<readonly CategoryDto[]> {
    return this.http
      .get<ApiResponse<readonly CategoryDto[]>>(`${this.baseUrl}/v1/commercial/categories`)
      .pipe(map((res) => res.data));
  }

  getCategoryById(id: string): Observable<CategoryDto> {
    return this.http
      .get<ApiResponse<CategoryDto>>(`${this.baseUrl}/v1/commercial/categories/${id}`)
      .pipe(map((res) => res.data));
  }

  createCategory(request: CreateCategoryRequest): Observable<CategoryDto> {
    return this.http
      .post<ApiResponse<CategoryDto>>(`${this.baseUrl}/v1/commercial/categories`, request)
      .pipe(map((res) => res.data));
  }

  updateCategory(id: string, request: UpdateCategoryRequest): Observable<CategoryDto> {
    return this.http
      .put<ApiResponse<CategoryDto>>(`${this.baseUrl}/v1/commercial/categories/${id}`, request)
      .pipe(map((res) => res.data));
  }

  deleteCategory(id: string): Observable<string> {
    return this.http
      .delete<ApiResponse<string>>(`${this.baseUrl}/v1/commercial/categories/${id}`)
      .pipe(map((res) => res.data));
  }

  getBrands(): Observable<readonly BrandDto[]> {
    return this.http
      .get<ApiResponse<readonly BrandDto[]>>(`${this.baseUrl}/v1/commercial/brands`)
      .pipe(map((res) => res.data));
  }

  createBrand(request: CreateBrandRequest): Observable<BrandDto> {
    return this.http
      .post<ApiResponse<BrandDto>>(`${this.baseUrl}/v1/commercial/brands`, request)
      .pipe(map((res) => res.data));
  }

  updateBrand(id: string, request: UpdateBrandRequest): Observable<BrandDto> {
    return this.http
      .put<ApiResponse<BrandDto>>(`${this.baseUrl}/v1/commercial/brands/${id}`, request)
      .pipe(map((res) => res.data));
  }

  getDepartments(): Observable<readonly DepartmentDto[]> {
    return this.http
      .get<ApiResponse<readonly DepartmentDto[]>>(`${this.baseUrl}/v1/commercial/departments`)
      .pipe(map((res) => res.data));
  }

  createDepartment(request: CreateDepartmentRequest): Observable<DepartmentDto> {
    return this.http
      .post<ApiResponse<DepartmentDto>>(`${this.baseUrl}/v1/commercial/departments`, request)
      .pipe(map((res) => res.data));
  }

  updateDepartment(id: string, request: UpdateDepartmentRequest): Observable<DepartmentDto> {
    return this.http
      .put<ApiResponse<DepartmentDto>>(`${this.baseUrl}/v1/commercial/departments/${id}`, request)
      .pipe(map((res) => res.data));
  }

  getUnitsOfMeasure(): Observable<readonly UnitOfMeasureDto[]> {
    return this.http
      .get<ApiResponse<readonly UnitOfMeasureDto[]>>(`${this.baseUrl}/v1/commercial/units`)
      .pipe(map((res) => res.data));
  }

  getTaxCodes(): Observable<readonly TaxCodeDto[]> {
    return this.http
      .get<ApiResponse<readonly TaxCodeDto[]>>(`${this.baseUrl}/v1/commercial/tax-codes`)
      .pipe(map((res) => res.data));
  }

  // -------------------------------------------------------------
  // Customers & Customer Groups
  // -------------------------------------------------------------
  getCustomers(options?: {
    search?: string;
    customerType?: CustomerType;
    gstType?: GstRegistrationType;
    customerGroupId?: string;
    isActive?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<CustomerSummaryDto>> {
    let params = new HttpParams();
    if (options?.search) params = params.set('search', options.search);
    if (options?.customerType !== undefined) params = params.set('customerType', options.customerType.toString());
    if (options?.gstType !== undefined) params = params.set('gstType', options.gstType.toString());
    if (options?.customerGroupId) params = params.set('customerGroupId', options.customerGroupId);
    if (options?.isActive !== undefined) params = params.set('isActive', options.isActive.toString());
    if (options?.page) params = params.set('page', options.page.toString());
    if (options?.pageSize) params = params.set('pageSize', options.pageSize.toString());

    return this.http
      .get<ApiResponse<readonly CustomerSummaryDto[], PaginationMeta>>(`${this.baseUrl}/v1/commercial/customers`, { params })
      .pipe(
        map((res) => ({
          items: res.data ?? [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? options?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? options?.pageSize ?? 25),
        }))
      );
  }

  getCustomerById(id: string): Observable<CustomerDetailDto> {
    return this.http
      .get<ApiResponse<CustomerDetailDto>>(`${this.baseUrl}/v1/commercial/customers/${id}`)
      .pipe(map((res) => res.data));
  }

  createCustomer(request: CreateCustomerRequest): Observable<CustomerDetailDto> {
    return this.http
      .post<ApiResponse<CustomerDetailDto>>(`${this.baseUrl}/v1/commercial/customers`, request)
      .pipe(map((res) => res.data));
  }

  updateCustomer(id: string, request: UpdateCustomerRequest): Observable<CustomerDetailDto> {
    return this.http
      .put<ApiResponse<CustomerDetailDto>>(`${this.baseUrl}/v1/commercial/customers/${id}`, request)
      .pipe(map((res) => res.data));
  }

  updateCreditLimit(id: string, request: UpdateCreditLimitRequest): Observable<string> {
    return this.http
      .put<ApiResponse<string>>(`${this.baseUrl}/v1/commercial/customers/${id}/credit-limit`, request)
      .pipe(map((res) => res.data));
  }

  getCustomerGroups(): Observable<readonly CustomerGroupDto[]> {
    return this.http
      .get<ApiResponse<readonly CustomerGroupDto[]>>(`${this.baseUrl}/v1/commercial/customers/groups`)
      .pipe(map((res) => res.data));
  }

  createCustomerGroup(request: CreateCustomerGroupRequest): Observable<CustomerGroupDto> {
    return this.http
      .post<ApiResponse<CustomerGroupDto>>(`${this.baseUrl}/v1/commercial/customers/groups`, request)
      .pipe(map((res) => res.data));
  }

  // -------------------------------------------------------------
  // Suppliers & Vendors
  // -------------------------------------------------------------
  getSuppliers(options?: {
    search?: string;
    category?: string;
    isMsme?: boolean;
    isActive?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedResult<SupplierSummaryDto>> {
    let params = new HttpParams();
    if (options?.search) params = params.set('search', options.search);
    if (options?.category) params = params.set('category', options.category);
    if (options?.isMsme !== undefined) params = params.set('isMsme', options.isMsme.toString());
    if (options?.isActive !== undefined) params = params.set('isActive', options.isActive.toString());
    if (options?.page) params = params.set('page', options.page.toString());
    if (options?.pageSize) params = params.set('pageSize', options.pageSize.toString());

    return this.http
      .get<ApiResponse<readonly SupplierSummaryDto[], PaginationMeta>>(`${this.baseUrl}/v1/commercial/suppliers`, { params })
      .pipe(
        map((res) => ({
          items: res.data ?? [],
          totalCount: Number(res.meta?.totalCount ?? res.data?.length ?? 0),
          page: Number(res.meta?.page ?? options?.page ?? 1),
          pageSize: Number(res.meta?.pageSize ?? options?.pageSize ?? 25),
        }))
      );
  }

  getSupplierById(id: string): Observable<SupplierDetailDto> {
    return this.http
      .get<ApiResponse<SupplierDetailDto>>(`${this.baseUrl}/v1/commercial/suppliers/${id}`)
      .pipe(map((res) => res.data));
  }

  createSupplier(request: CreateSupplierRequest): Observable<SupplierDetailDto> {
    return this.http
      .post<ApiResponse<SupplierDetailDto>>(`${this.baseUrl}/v1/commercial/suppliers`, request)
      .pipe(map((res) => res.data));
  }

  updateSupplier(id: string, request: UpdateSupplierRequest): Observable<SupplierDetailDto> {
    return this.http
      .put<ApiResponse<SupplierDetailDto>>(`${this.baseUrl}/v1/commercial/suppliers/${id}`, request)
      .pipe(map((res) => res.data));
  }

  getUnmaskedBankAccount(supplierId: string, bankAccountId: string): Observable<UnmaskedBankAccountDto> {
    return this.http
      .get<ApiResponse<UnmaskedBankAccountDto>>(
        `${this.baseUrl}/v1/commercial/suppliers/${supplierId}/bank-accounts/${bankAccountId}/unmasked`
      )
      .pipe(map((res) => res.data));
  }
}
