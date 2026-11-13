import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PaginationMeta } from '../contracts/api.models';
import {
  AttendanceDto,
  BackorderDto,
  CampaignDto,
  CorrectAttendanceRequest,
  CreateCampaignRequest,
  CreateCustomerSegmentRequest,
  CreateDesignationRequest,
  CreateEmployeeRequest,
  CreateExpenseCategoryRequest,
  CreateExpenseRequest,
  CreateLeaveTypeRequest,
  CreateLoyaltyProgramRequest,
  CreatePettyCashAccountRequest,
  CreatePromotionCouponRequest,
  CreateSalaryStructureRequest,
  CreateSalesOrderRequest,
  CreateServiceCatalogRequest,
  CreateServiceQuotationRequest,
  CreateServiceRequest,
  CustomerSegmentDto,
  DesignationDto,
  EmployeeDto,
  EmployeeSalaryAssignmentDto,
  ExpenseCategoryDto,
  ExpenseDto,
  ExpenseSummaryDto,
  FulfillOrderRequest,
  HrSummaryDto,
  InvoiceServiceRequest,
  LeaveApplicationDto,
  LeaveBalanceDto,
  LeaveTypeDto,
  LoyaltyBalanceDto,
  LoyaltyProgramDto,
  LoyaltyTransactionDto,
  MarketingSummaryDto,
  OrderFulfillmentDto,
  OrderSummaryDto,
  PayExpenseRequest,
  PayrollItemDto,
  PayrollPeriodDto,
  PayrollSummaryDto,
  PettyCashAccountDto,
  PettyCashTransactionDto,
  PromotionCouponDto,
  PunchAttendanceRequest,
  ReconcilePettyCashRequest,
  RedeemLoyaltyPointsRequest,
  ReplenishPettyCashRequest,
  SalesOrderDto,
  SalaryStructureDto,
  ServiceCatalogDto,
  ServicePartConsumptionDto,
  ServiceQuotationDto,
  ServiceRequestDto,
  ServiceSummaryDto,
  SubmitLeaveApplicationRequest,
  UpdateEmployeeRequest,
  UpdateServiceRequestStatusRequest,
  ValidateCouponRequest,
  ValidateCouponResponse,
} from '../contracts/operations.models';

export interface PaginatedList<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class OperationsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  // ==========================================
  // Module 11: Expense Management
  // ==========================================

  getExpenseSummary(branchId?: string): Observable<ExpenseSummaryDto> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http
      .get<ApiResponse<ExpenseSummaryDto>>(`${this.baseUrl}/v1/expenses/summary`, { params })
      .pipe(map((res) => res.data));
  }

  getExpenseCategories(): Observable<readonly ExpenseCategoryDto[]> {
    return this.http
      .get<ApiResponse<readonly ExpenseCategoryDto[]>>(`${this.baseUrl}/v1/expenses/categories`)
      .pipe(map((res) => res.data));
  }

  createExpenseCategory(request: CreateExpenseCategoryRequest): Observable<ExpenseCategoryDto> {
    return this.http
      .post<ApiResponse<ExpenseCategoryDto>>(`${this.baseUrl}/v1/expenses/categories`, request)
      .pipe(map((res) => res.data));
  }

  getExpenses(paramsObj?: {
    branchId?: string;
    status?: string;
    categoryId?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedList<ExpenseDto>> {
    let params = new HttpParams();
    if (paramsObj?.branchId) params = params.set('branchId', paramsObj.branchId);
    if (paramsObj?.status) params = params.set('status', paramsObj.status);
    if (paramsObj?.categoryId) params = params.set('categoryId', paramsObj.categoryId);
    if (paramsObj?.fromDate) params = params.set('fromDate', paramsObj.fromDate);
    if (paramsObj?.toDate) params = params.set('toDate', paramsObj.toDate);
    if (paramsObj?.page) params = params.set('page', paramsObj.page);
    if (paramsObj?.pageSize) params = params.set('pageSize', paramsObj.pageSize);

    return this.http
      .get<ApiResponse<readonly ExpenseDto[], PaginationMeta>>(`${this.baseUrl}/v1/expenses`, { params })
      .pipe(map((res) => ({ items: res.data, totalCount: res.meta?.totalCount ?? res.data.length })));
  }

  getExpenseById(id: string): Observable<ExpenseDto> {
    return this.http
      .get<ApiResponse<ExpenseDto>>(`${this.baseUrl}/v1/expenses/${id}`)
      .pipe(map((res) => res.data));
  }

  createExpense(request: CreateExpenseRequest): Observable<ExpenseDto> {
    return this.http
      .post<ApiResponse<ExpenseDto>>(`${this.baseUrl}/v1/expenses`, request)
      .pipe(map((res) => res.data));
  }

  approveExpense(id: string): Observable<ExpenseDto> {
    return this.http
      .post<ApiResponse<ExpenseDto>>(`${this.baseUrl}/v1/expenses/${id}/approve`, {})
      .pipe(map((res) => res.data));
  }

  rejectExpense(id: string, reason: string): Observable<ExpenseDto> {
    return this.http
      .post<ApiResponse<ExpenseDto>>(`${this.baseUrl}/v1/expenses/${id}/reject`, { reason })
      .pipe(map((res) => res.data));
  }

  payExpense(id: string, request: PayExpenseRequest): Observable<ExpenseDto> {
    return this.http
      .post<ApiResponse<ExpenseDto>>(`${this.baseUrl}/v1/expenses/${id}/pay`, request)
      .pipe(map((res) => res.data));
  }

  getPettyCashAccounts(branchId?: string): Observable<readonly PettyCashAccountDto[]> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http
      .get<ApiResponse<readonly PettyCashAccountDto[]>>(`${this.baseUrl}/v1/expenses/petty-cash`, { params })
      .pipe(map((res) => res.data));
  }

  createPettyCashAccount(request: CreatePettyCashAccountRequest): Observable<PettyCashAccountDto> {
    return this.http
      .post<ApiResponse<PettyCashAccountDto>>(`${this.baseUrl}/v1/expenses/petty-cash`, request)
      .pipe(map((res) => res.data));
  }

  getPettyCashTransactions(accountId: string): Observable<readonly PettyCashTransactionDto[]> {
    return this.http
      .get<ApiResponse<readonly PettyCashTransactionDto[]>>(`${this.baseUrl}/v1/expenses/petty-cash/${accountId}/transactions`)
      .pipe(map((res) => res.data));
  }

  replenishPettyCash(accountId: string, request: ReplenishPettyCashRequest): Observable<PettyCashTransactionDto> {
    return this.http
      .post<ApiResponse<PettyCashTransactionDto>>(`${this.baseUrl}/v1/expenses/petty-cash/${accountId}/replenish`, request)
      .pipe(map((res) => res.data));
  }

  reconcilePettyCash(accountId: string, request: ReconcilePettyCashRequest): Observable<PettyCashTransactionDto> {
    return this.http
      .post<ApiResponse<PettyCashTransactionDto>>(`${this.baseUrl}/v1/expenses/petty-cash/${accountId}/reconcile`, request)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // Module 12: Human Resource Management
  // ==========================================

  getHrSummary(branchId?: string): Observable<HrSummaryDto> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http
      .get<ApiResponse<HrSummaryDto>>(`${this.baseUrl}/v1/hr/summary`, { params })
      .pipe(map((res) => res.data));
  }

  getDesignations(): Observable<readonly DesignationDto[]> {
    return this.http
      .get<ApiResponse<readonly DesignationDto[]>>(`${this.baseUrl}/v1/hr/designations`)
      .pipe(map((res) => res.data));
  }

  createDesignation(request: CreateDesignationRequest): Observable<DesignationDto> {
    return this.http
      .post<ApiResponse<DesignationDto>>(`${this.baseUrl}/v1/hr/designations`, request)
      .pipe(map((res) => res.data));
  }

  getEmployees(paramsObj?: {
    branchId?: string;
    departmentId?: string;
    designationId?: string;
    status?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedList<EmployeeDto>> {
    let params = new HttpParams();
    if (paramsObj?.branchId) params = params.set('branchId', paramsObj.branchId);
    if (paramsObj?.departmentId) params = params.set('departmentId', paramsObj.departmentId);
    if (paramsObj?.designationId) params = params.set('designationId', paramsObj.designationId);
    if (paramsObj?.status) params = params.set('status', paramsObj.status);
    if (paramsObj?.search) params = params.set('search', paramsObj.search);
    if (paramsObj?.page) params = params.set('page', paramsObj.page);
    if (paramsObj?.pageSize) params = params.set('pageSize', paramsObj.pageSize);

    return this.http
      .get<ApiResponse<readonly EmployeeDto[], PaginationMeta>>(`${this.baseUrl}/v1/hr/employees`, { params })
      .pipe(map((res) => ({ items: res.data, totalCount: res.meta?.totalCount ?? res.data.length })));
  }

  getEmployeeById(id: string): Observable<EmployeeDto> {
    return this.http
      .get<ApiResponse<EmployeeDto>>(`${this.baseUrl}/v1/hr/employees/${id}`)
      .pipe(map((res) => res.data));
  }

  createEmployee(request: CreateEmployeeRequest): Observable<EmployeeDto> {
    return this.http
      .post<ApiResponse<EmployeeDto>>(`${this.baseUrl}/v1/hr/employees`, request)
      .pipe(map((res) => res.data));
  }

  updateEmployee(id: string, request: UpdateEmployeeRequest): Observable<EmployeeDto> {
    return this.http
      .put<ApiResponse<EmployeeDto>>(`${this.baseUrl}/v1/hr/employees/${id}`, request)
      .pipe(map((res) => res.data));
  }

  getAttendance(attendanceDate: string, branchId?: string, employeeId?: string): Observable<PaginatedList<AttendanceDto>> {
    let params = new HttpParams().set('attendanceDate', attendanceDate);
    if (branchId) params = params.set('branchId', branchId);
    if (employeeId) params = params.set('employeeId', employeeId);
    return this.http
      .get<ApiResponse<readonly AttendanceDto[], PaginationMeta>>(`${this.baseUrl}/v1/hr/attendance`, { params })
      .pipe(map((res) => ({ items: res.data, totalCount: res.meta?.totalCount ?? res.data.length })));
  }

  punchAttendance(request: PunchAttendanceRequest): Observable<AttendanceDto> {
    return this.http
      .post<ApiResponse<AttendanceDto>>(`${this.baseUrl}/v1/hr/attendance/punch`, request)
      .pipe(map((res) => res.data));
  }

  correctAttendance(request: CorrectAttendanceRequest): Observable<AttendanceDto> {
    return this.http
      .post<ApiResponse<AttendanceDto>>(`${this.baseUrl}/v1/hr/attendance/correct`, request)
      .pipe(map((res) => res.data));
  }

  getLeaveTypes(): Observable<readonly LeaveTypeDto[]> {
    return this.http
      .get<ApiResponse<readonly LeaveTypeDto[]>>(`${this.baseUrl}/v1/hr/leaves/types`)
      .pipe(map((res) => res.data));
  }

  createLeaveType(request: CreateLeaveTypeRequest): Observable<LeaveTypeDto> {
    return this.http
      .post<ApiResponse<LeaveTypeDto>>(`${this.baseUrl}/v1/hr/leaves/types`, request)
      .pipe(map((res) => res.data));
  }

  getLeaveBalances(employeeId: string): Observable<readonly LeaveBalanceDto[]> {
    return this.http
      .get<ApiResponse<readonly LeaveBalanceDto[]>>(`${this.baseUrl}/v1/hr/leaves/balances/${employeeId}`)
      .pipe(map((res) => res.data));
  }

  getLeaveApplications(employeeId?: string, status?: string): Observable<PaginatedList<LeaveApplicationDto>> {
    let params = new HttpParams();
    if (employeeId) params = params.set('employeeId', employeeId);
    if (status) params = params.set('status', status);
    return this.http
      .get<ApiResponse<readonly LeaveApplicationDto[], PaginationMeta>>(`${this.baseUrl}/v1/hr/leaves/applications`, { params })
      .pipe(map((res) => ({ items: res.data, totalCount: res.meta?.totalCount ?? res.data.length })));
  }

  submitLeaveApplication(request: SubmitLeaveApplicationRequest): Observable<LeaveApplicationDto> {
    return this.http
      .post<ApiResponse<LeaveApplicationDto>>(`${this.baseUrl}/v1/hr/leaves/apply`, request)
      .pipe(map((res) => res.data));
  }

  approveLeaveApplication(id: string): Observable<LeaveApplicationDto> {
    return this.http
      .post<ApiResponse<LeaveApplicationDto>>(`${this.baseUrl}/v1/hr/leaves/applications/${id}/approve`, {})
      .pipe(map((res) => res.data));
  }

  rejectLeaveApplication(id: string, reason: string): Observable<LeaveApplicationDto> {
    return this.http
      .post<ApiResponse<LeaveApplicationDto>>(`${this.baseUrl}/v1/hr/leaves/applications/${id}/reject`, { reason })
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // Module 13: Payroll Management
  // ==========================================

  getPayrollSummary(branchId?: string): Observable<PayrollSummaryDto> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http
      .get<ApiResponse<PayrollSummaryDto>>(`${this.baseUrl}/v1/payroll/summary`, { params })
      .pipe(map((res) => res.data));
  }

  getSalaryStructures(): Observable<readonly SalaryStructureDto[]> {
    return this.http
      .get<ApiResponse<readonly SalaryStructureDto[]>>(`${this.baseUrl}/v1/payroll/structures`)
      .pipe(map((res) => res.data));
  }

  createSalaryStructure(request: CreateSalaryStructureRequest): Observable<SalaryStructureDto> {
    return this.http
      .post<ApiResponse<SalaryStructureDto>>(`${this.baseUrl}/v1/payroll/structures`, request)
      .pipe(map((res) => res.data));
  }

  assignSalaryStructure(request: { employeeId: string; salaryStructureId: string; baseMonthlyCtcPaise: number; effectiveFromDate: string }): Observable<EmployeeSalaryAssignmentDto> {
    return this.http
      .post<ApiResponse<EmployeeSalaryAssignmentDto>>(`${this.baseUrl}/v1/payroll/assign`, request)
      .pipe(map((res) => res.data));
  }

  getEmployeeSalaryAssignment(employeeId: string): Observable<EmployeeSalaryAssignmentDto | null> {
    return this.http
      .get<ApiResponse<EmployeeSalaryAssignmentDto | null>>(`${this.baseUrl}/v1/payroll/assignments/${employeeId}`)
      .pipe(map((res) => res.data));
  }

  getPayrollPeriods(branchId?: string): Observable<PaginatedList<PayrollPeriodDto>> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http
      .get<ApiResponse<readonly PayrollPeriodDto[], PaginationMeta>>(`${this.baseUrl}/v1/payroll/periods`, { params })
      .pipe(map((res) => ({ items: res.data, totalCount: res.meta?.totalCount ?? res.data.length })));
  }

  getPayrollPeriodById(id: string): Observable<PayrollPeriodDto> {
    return this.http
      .get<ApiResponse<PayrollPeriodDto>>(`${this.baseUrl}/v1/payroll/periods/${id}`)
      .pipe(map((res) => res.data));
  }

  calculatePayroll(periodMonth: number, periodYear: number): Observable<PayrollPeriodDto> {
    return this.http
      .post<ApiResponse<PayrollPeriodDto>>(`${this.baseUrl}/v1/payroll/calculate`, { periodMonth, periodYear })
      .pipe(map((res) => res.data));
  }

  approvePayroll(id: string): Observable<PayrollPeriodDto> {
    return this.http
      .post<ApiResponse<PayrollPeriodDto>>(`${this.baseUrl}/v1/payroll/periods/${id}/approve`, {})
      .pipe(map((res) => res.data));
  }

  finalizePayroll(id: string): Observable<PayrollPeriodDto> {
    return this.http
      .post<ApiResponse<PayrollPeriodDto>>(`${this.baseUrl}/v1/payroll/periods/${id}/finalize`, {})
      .pipe(map((res) => res.data));
  }

  disbursePayroll(id: string, request: { sourceBankAccountId: string; paymentReference: string; disbursementDate: string }): Observable<PayrollPeriodDto> {
    return this.http
      .post<ApiResponse<PayrollPeriodDto>>(`${this.baseUrl}/v1/payroll/periods/${id}/disburse`, request)
      .pipe(map((res) => res.data));
  }

  getPayslip(payrollItemId: string): Observable<PayrollItemDto> {
    return this.http
      .get<ApiResponse<PayrollItemDto>>(`${this.baseUrl}/v1/payroll/payslip/${payrollItemId}`)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // Module 14: Marketing & Growth
  // ==========================================

  getMarketingSummary(): Observable<MarketingSummaryDto> {
    return this.http
      .get<ApiResponse<MarketingSummaryDto>>(`${this.baseUrl}/v1/marketing/summary`)
      .pipe(map((res) => res.data));
  }

  getSegments(): Observable<readonly CustomerSegmentDto[]> {
    return this.http
      .get<ApiResponse<readonly CustomerSegmentDto[]>>(`${this.baseUrl}/v1/marketing/segments`)
      .pipe(map((res) => res.data));
  }

  createSegment(request: CreateCustomerSegmentRequest): Observable<CustomerSegmentDto> {
    return this.http
      .post<ApiResponse<CustomerSegmentDto>>(`${this.baseUrl}/v1/marketing/segments`, request)
      .pipe(map((res) => res.data));
  }

  getCampaigns(): Observable<readonly CampaignDto[]> {
    return this.http
      .get<ApiResponse<readonly CampaignDto[]>>(`${this.baseUrl}/v1/marketing/campaigns`)
      .pipe(map((res) => res.data));
  }

  createCampaign(request: CreateCampaignRequest): Observable<CampaignDto> {
    return this.http
      .post<ApiResponse<CampaignDto>>(`${this.baseUrl}/v1/marketing/campaigns`, request)
      .pipe(map((res) => res.data));
  }

  getCoupons(): Observable<readonly PromotionCouponDto[]> {
    return this.http
      .get<ApiResponse<readonly PromotionCouponDto[]>>(`${this.baseUrl}/v1/marketing/coupons`)
      .pipe(map((res) => res.data));
  }

  createCoupon(request: CreatePromotionCouponRequest): Observable<PromotionCouponDto> {
    return this.http
      .post<ApiResponse<PromotionCouponDto>>(`${this.baseUrl}/v1/marketing/coupons`, request)
      .pipe(map((res) => res.data));
  }

  validateCoupon(request: ValidateCouponRequest): Observable<ValidateCouponResponse> {
    return this.http
      .post<ApiResponse<ValidateCouponResponse>>(`${this.baseUrl}/v1/marketing/coupons/validate`, request)
      .pipe(map((res) => res.data));
  }

  getLoyaltyPrograms(): Observable<readonly LoyaltyProgramDto[]> {
    return this.http
      .get<ApiResponse<readonly LoyaltyProgramDto[]>>(`${this.baseUrl}/v1/marketing/loyalty/programs`)
      .pipe(map((res) => res.data));
  }

  createLoyaltyProgram(request: CreateLoyaltyProgramRequest): Observable<LoyaltyProgramDto> {
    return this.http
      .post<ApiResponse<LoyaltyProgramDto>>(`${this.baseUrl}/v1/marketing/loyalty/programs`, request)
      .pipe(map((res) => res.data));
  }

  getCustomerLoyalty(customerId: string): Observable<LoyaltyBalanceDto> {
    return this.http
      .get<ApiResponse<LoyaltyBalanceDto>>(`${this.baseUrl}/v1/marketing/loyalty/${customerId}`)
      .pipe(map((res) => res.data));
  }

  getCustomerLoyaltyHistory(customerId: string): Observable<readonly LoyaltyTransactionDto[]> {
    return this.http
      .get<ApiResponse<readonly LoyaltyTransactionDto[]>>(`${this.baseUrl}/v1/marketing/loyalty/${customerId}/history`)
      .pipe(map((res) => res.data));
  }

  redeemLoyaltyPoints(request: RedeemLoyaltyPointsRequest): Observable<LoyaltyBalanceDto> {
    return this.http
      .post<ApiResponse<LoyaltyBalanceDto>>(`${this.baseUrl}/v1/marketing/loyalty/redeem`, request)
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // Module 15: Advanced Order Management
  // ==========================================

  getOrderSummary(branchId?: string): Observable<OrderSummaryDto> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http
      .get<ApiResponse<OrderSummaryDto>>(`${this.baseUrl}/v1/orders/summary`, { params })
      .pipe(map((res) => res.data));
  }

  getOrders(paramsObj?: {
    branchId?: string;
    customerId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedList<SalesOrderDto>> {
    let params = new HttpParams();
    if (paramsObj?.branchId) params = params.set('branchId', paramsObj.branchId);
    if (paramsObj?.customerId) params = params.set('customerId', paramsObj.customerId);
    if (paramsObj?.status) params = params.set('status', paramsObj.status);
    if (paramsObj?.page) params = params.set('page', paramsObj.page);
    if (paramsObj?.pageSize) params = params.set('pageSize', paramsObj.pageSize);

    return this.http
      .get<ApiResponse<readonly SalesOrderDto[], PaginationMeta>>(`${this.baseUrl}/v1/orders`, { params })
      .pipe(map((res) => ({ items: res.data, totalCount: res.meta?.totalCount ?? res.data.length })));
  }

  getOrderById(id: string): Observable<SalesOrderDto> {
    return this.http
      .get<ApiResponse<SalesOrderDto>>(`${this.baseUrl}/v1/orders/${id}`)
      .pipe(map((res) => res.data));
  }

  createOrder(request: CreateSalesOrderRequest): Observable<SalesOrderDto> {
    return this.http
      .post<ApiResponse<SalesOrderDto>>(`${this.baseUrl}/v1/orders`, request)
      .pipe(map((res) => res.data));
  }

  confirmOrder(id: string): Observable<SalesOrderDto> {
    return this.http
      .post<ApiResponse<SalesOrderDto>>(`${this.baseUrl}/v1/orders/${id}/confirm`, {})
      .pipe(map((res) => res.data));
  }

  fulfillOrder(id: string, request: FulfillOrderRequest): Observable<OrderFulfillmentDto> {
    return this.http
      .post<ApiResponse<OrderFulfillmentDto>>(`${this.baseUrl}/v1/orders/${id}/fulfill`, request)
      .pipe(map((res) => res.data));
  }

  cancelOrder(id: string, reason: string): Observable<SalesOrderDto> {
    return this.http
      .post<ApiResponse<SalesOrderDto>>(`${this.baseUrl}/v1/orders/${id}/cancel`, { reason })
      .pipe(map((res) => res.data));
  }

  getBackorders(branchId?: string): Observable<readonly BackorderDto[]> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http
      .get<ApiResponse<readonly BackorderDto[]>>(`${this.baseUrl}/v1/orders/backorders`, { params })
      .pipe(map((res) => res.data));
  }

  convertOrderToInvoice(id: string, paymentMethod: string): Observable<string> {
    return this.http
      .post<ApiResponse<string>>(`${this.baseUrl}/v1/orders/${id}/invoice`, { paymentMethod })
      .pipe(map((res) => res.data));
  }

  // ==========================================
  // Module 16: Service Management
  // ==========================================

  getServiceSummary(branchId?: string): Observable<ServiceSummaryDto> {
    let params = new HttpParams();
    if (branchId) params = params.set('branchId', branchId);
    return this.http
      .get<ApiResponse<ServiceSummaryDto>>(`${this.baseUrl}/v1/services/summary`, { params })
      .pipe(map((res) => res.data));
  }

  getServiceCatalog(): Observable<readonly ServiceCatalogDto[]> {
    return this.http
      .get<ApiResponse<readonly ServiceCatalogDto[]>>(`${this.baseUrl}/v1/services/catalog`)
      .pipe(map((res) => res.data));
  }

  createServiceCatalogEntry(request: CreateServiceCatalogRequest): Observable<ServiceCatalogDto> {
    return this.http
      .post<ApiResponse<ServiceCatalogDto>>(`${this.baseUrl}/v1/services/catalog`, request)
      .pipe(map((res) => res.data));
  }

  getServiceRequests(paramsObj?: {
    branchId?: string;
    customerId?: string;
    status?: string;
    priority?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PaginatedList<ServiceRequestDto>> {
    let params = new HttpParams();
    if (paramsObj?.branchId) params = params.set('branchId', paramsObj.branchId);
    if (paramsObj?.customerId) params = params.set('customerId', paramsObj.customerId);
    if (paramsObj?.status) params = params.set('status', paramsObj.status);
    if (paramsObj?.priority) params = params.set('priority', paramsObj.priority);
    if (paramsObj?.page) params = params.set('page', paramsObj.page);
    if (paramsObj?.pageSize) params = params.set('pageSize', paramsObj.pageSize);

    return this.http
      .get<ApiResponse<readonly ServiceRequestDto[], PaginationMeta>>(`${this.baseUrl}/v1/services/requests`, { params })
      .pipe(map((res) => ({ items: res.data, totalCount: res.meta?.totalCount ?? res.data.length })));
  }

  getServiceRequestById(id: string): Observable<ServiceRequestDto> {
    return this.http
      .get<ApiResponse<ServiceRequestDto>>(`${this.baseUrl}/v1/services/requests/${id}`)
      .pipe(map((res) => res.data));
  }

  createServiceRequest(request: CreateServiceRequest): Observable<ServiceRequestDto> {
    return this.http
      .post<ApiResponse<ServiceRequestDto>>(`${this.baseUrl}/v1/services/requests`, request)
      .pipe(map((res) => res.data));
  }

  updateServiceRequestStatus(id: string, request: UpdateServiceRequestStatusRequest): Observable<ServiceRequestDto> {
    return this.http
      .put<ApiResponse<ServiceRequestDto>>(`${this.baseUrl}/v1/services/requests/${id}/status`, request)
      .pipe(map((res) => res.data));
  }

  createQuotation(requestId: string, request: CreateServiceQuotationRequest): Observable<ServiceQuotationDto> {
    return this.http
      .post<ApiResponse<ServiceQuotationDto>>(`${this.baseUrl}/v1/services/requests/${requestId}/quotations`, request)
      .pipe(map((res) => res.data));
  }

  approveQuotation(quotationId: string): Observable<ServiceQuotationDto> {
    return this.http
      .post<ApiResponse<ServiceQuotationDto>>(`${this.baseUrl}/v1/services/quotations/${quotationId}/approve`, {})
      .pipe(map((res) => res.data));
  }

  consumePart(requestId: string, request: { productId: string; warehouseId: string; quantity: number }): Observable<ServicePartConsumptionDto> {
    return this.http
      .post<ApiResponse<ServicePartConsumptionDto>>(`${this.baseUrl}/v1/services/requests/${requestId}/parts`, request)
      .pipe(map((res) => res.data));
  }

  invoiceServiceRequest(requestId: string, request: InvoiceServiceRequest): Observable<string> {
    return this.http
      .post<ApiResponse<string>>(`${this.baseUrl}/v1/services/requests/${requestId}/invoice`, request)
      .pipe(map((res) => res.data));
  }
}
