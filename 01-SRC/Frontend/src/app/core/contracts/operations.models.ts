// ==========================================
// Phase F: Operations Models (Modules 11 - 16)
// ==========================================

// Module 11: Expense Management
export interface ExpenseCategoryDto {
  readonly id: string;
  readonly companyId: string;
  readonly code: string;
  readonly name: string;
  readonly defaultGlAccountId: string;
  readonly defaultGlAccountName?: string;
  readonly description?: string;
  readonly isActive: boolean;
}

export interface CreateExpenseCategoryRequest {
  readonly code: string;
  readonly name: string;
  readonly defaultGlAccountId: string;
  readonly description?: string;
}

export interface ExpenseDto {
  readonly id: string;
  readonly companyId: string;
  readonly branchId: string;
  readonly expenseNumber: string;
  readonly expenseCategoryId: string;
  readonly expenseCategoryName: string;
  readonly glAccountId: string;
  readonly glAccountName: string;
  readonly vendorId?: string;
  readonly vendorName?: string;
  readonly paymentMethod: string;
  readonly bankCashAccountId?: string;
  readonly bankCashAccountName?: string;
  readonly expenseDate: string;
  readonly amountPaise: number;
  readonly taxAmountPaise: number;
  readonly totalAmountPaise: number;
  readonly costCenter?: string;
  readonly referenceNumber?: string;
  readonly description: string;
  readonly status: string;
  readonly approvedByUserId?: string;
  readonly approvedAtUtc?: string;
  readonly rejectionReason?: string;
  readonly journalVoucherId?: string;
  readonly reversalVoucherId?: string;
  readonly attachmentPath?: string;
  readonly createdAtUtc: string;
}

export interface CreateExpenseRequest {
  readonly expenseCategoryId: string;
  readonly glAccountId?: string;
  readonly vendorId?: string;
  readonly paymentMethod: string;
  readonly bankCashAccountId?: string;
  readonly expenseDate: string;
  readonly amountPaise: number;
  readonly taxAmountPaise: number;
  readonly costCenter?: string;
  readonly referenceNumber?: string;
  readonly description: string;
  readonly attachmentPath?: string;
}

export interface PayExpenseRequest {
  readonly paymentMethod: string;
  readonly bankCashAccountId?: string;
  readonly paymentDate: string;
  readonly referenceNumber?: string;
}

export interface ExpenseSummaryDto {
  readonly totalExpensesPaise: number;
  readonly pendingApprovalPaise: number;
  readonly paidExpensesPaise: number;
  readonly pendingApprovalCount: number;
  readonly totalExpensesCount: number;
}

export interface PettyCashAccountDto {
  readonly id: string;
  readonly companyId: string;
  readonly branchId: string;
  readonly accountName: string;
  readonly glAccountId: string;
  readonly glAccountName: string;
  readonly custodianUserId: string;
  readonly custodianUserName: string;
  readonly floatLimitPaise: number;
  readonly currentBalancePaise: number;
  readonly isActive: boolean;
}

export interface CreatePettyCashAccountRequest {
  readonly accountName: string;
  readonly glAccountId: string;
  readonly custodianUserId: string;
  readonly floatLimitPaise: number;
}

export interface PettyCashTransactionDto {
  readonly id: string;
  readonly pettyCashAccountId: string;
  readonly transactionType: string;
  readonly amountPaise: number;
  readonly balanceAfterPaise: number;
  readonly expenseId?: string;
  readonly journalVoucherId?: string;
  readonly performedByUserId: string;
  readonly notes?: string;
  readonly transactionDateUtc: string;
}

export interface ReplenishPettyCashRequest {
  readonly sourceBankAccountId: string;
  readonly amountPaise: number;
  readonly notes?: string;
}

export interface ReconcilePettyCashRequest {
  readonly actualCountPaise: number;
  readonly notes?: string;
}

// Module 12: Human Resource Management
export interface HrSummaryDto {
  readonly totalEmployees: number;
  readonly activeEmployees: number;
  readonly presentToday: number;
  readonly onLeaveToday: number;
  readonly pendingLeaves: number;
}

export interface DesignationDto {
  readonly id: string;
  readonly companyId: string;
  readonly code: string;
  readonly title: string;
  readonly description?: string;
  readonly isActive: boolean;
}

export interface CreateDesignationRequest {
  readonly code: string;
  readonly title: string;
  readonly description?: string;
}

export interface EmployeeDto {
  readonly id: string;
  readonly companyId: string;
  readonly branchId: string;
  readonly employeeCode: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phone?: string;
  readonly departmentId?: string;
  readonly departmentName?: string;
  readonly designationId?: string;
  readonly designationTitle?: string;
  readonly joiningDate: string;
  readonly status: string;
  readonly bankAccountNumber?: string;
  readonly bankIfsc?: string;
  readonly bankName?: string;
  readonly panNumber?: string;
  readonly linkedUserId?: string;
  readonly createdAtUtc: string;
}

export interface CreateEmployeeRequest {
  readonly employeeCode: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phone?: string;
  readonly departmentId?: string;
  readonly designationId?: string;
  readonly joiningDate: string;
  readonly bankAccountNumber?: string;
  readonly bankIfsc?: string;
  readonly bankName?: string;
  readonly panNumber?: string;
  readonly linkedUserId?: string;
}

export interface UpdateEmployeeRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phone?: string;
  readonly departmentId?: string;
  readonly designationId?: string;
  readonly status: string;
  readonly bankAccountNumber?: string;
  readonly bankIfsc?: string;
  readonly bankName?: string;
  readonly panNumber?: string;
  readonly linkedUserId?: string;
}

export interface AttendanceDto {
  readonly id: string;
  readonly employeeId: string;
  readonly employeeName: string;
  readonly employeeCode: string;
  readonly attendanceDate: string;
  readonly punchInTimeUtc?: string;
  readonly punchOutTimeUtc?: string;
  readonly totalWorkMinutes: number;
  readonly status: string;
  readonly isManualCorrection: boolean;
  readonly notes?: string;
}

export interface PunchAttendanceRequest {
  readonly employeeId: string;
  readonly attendanceDate: string;
  readonly punchType: string; // "In" or "Out"
  readonly timestampUtc: string;
}

export interface CorrectAttendanceRequest {
  readonly employeeId: string;
  readonly attendanceDate: string;
  readonly punchInTimeUtc?: string;
  readonly punchOutTimeUtc?: string;
  readonly status: string;
  readonly reason: string;
}

export interface LeaveTypeDto {
  readonly id: string;
  readonly companyId: string;
  readonly code: string;
  readonly name: string;
  readonly daysAllowedPerYear: number;
  readonly isPaid: boolean;
  readonly isActive: boolean;
}

export interface CreateLeaveTypeRequest {
  readonly code: string;
  readonly name: string;
  readonly daysAllowedPerYear: number;
  readonly isPaid: boolean;
}

export interface LeaveBalanceDto {
  readonly id: string;
  readonly employeeId: string;
  readonly leaveTypeId: string;
  readonly leaveTypeName: string;
  readonly fiscalYearId: string;
  readonly totalAllocatedDays: number;
  readonly usedDays: number;
  readonly pendingDays: number;
  readonly availableDays: number;
}

export interface LeaveApplicationDto {
  readonly id: string;
  readonly employeeId: string;
  readonly employeeName: string;
  readonly employeeCode: string;
  readonly leaveTypeId: string;
  readonly leaveTypeName: string;
  readonly startDate: string;
  readonly endDate: string;
  readonly numberOfDays: number;
  readonly reason: string;
  readonly status: string;
  readonly reviewedByUserId?: string;
  readonly reviewRemarks?: string;
  readonly reviewedAtUtc?: string;
  readonly createdAtUtc: string;
}

export interface SubmitLeaveApplicationRequest {
  readonly employeeId: string;
  readonly leaveTypeId: string;
  readonly startDate: string;
  readonly endDate: string;
  readonly reason: string;
}

// Module 13: Payroll Management
export interface PayrollSummaryDto {
  readonly lastPayrollMonth: number;
  readonly lastPayrollYear: number;
  readonly lastPayrollStatus: string;
  readonly totalGrossSalariesPaise: number;
  readonly totalNetSalariesPaise: number;
  readonly pendingDisbursementsCount: number;
}

export interface SalaryStructureComponentDto {
  readonly id: string;
  readonly salaryStructureId: string;
  readonly componentName: string;
  readonly componentType: string;
  readonly calculationType: string;
  readonly percentageOrValue: number;
  readonly glAccountId?: string;
}

export interface SalaryStructureDto {
  readonly id: string;
  readonly companyId: string;
  readonly name: string;
  readonly description?: string;
  readonly isActive: boolean;
  readonly components: readonly SalaryStructureComponentDto[];
}

export interface CreateSalaryStructureRequest {
  readonly name: string;
  readonly description?: string;
  readonly components: readonly {
    readonly componentName: string;
    readonly componentType: string;
    readonly calculationType: string;
    readonly percentageOrValue: number;
    readonly glAccountId?: string;
  }[];
}

export interface EmployeeSalaryAssignmentDto {
  readonly id: string;
  readonly employeeId: string;
  readonly employeeName: string;
  readonly salaryStructureId: string;
  readonly salaryStructureName: string;
  readonly baseMonthlyCtcPaise: number;
  readonly effectiveFromDate: string;
  readonly isActive: boolean;
}

export interface AssignEmployeeSalaryRequest {
  readonly employeeId: string;
  readonly salaryStructureId: string;
  readonly baseMonthlyCtcPaise: number;
  readonly effectiveFromDate: string;
}

export interface PayrollPeriodDto {
  readonly id: string;
  readonly companyId: string;
  readonly branchId: string;
  readonly periodMonth: number;
  readonly periodYear: number;
  readonly startDate: string;
  readonly endDate: string;
  readonly status: string;
  readonly totalGrossPaise: number;
  readonly totalDeductionsPaise: number;
  readonly totalNetPaise: number;
  readonly accrualVoucherId?: string;
  readonly disbursementVoucherId?: string;
  readonly createdAtUtc: string;
  readonly items: readonly PayrollItemDto[];
}

export interface PayrollItemDto {
  readonly id: string;
  readonly payrollPeriodId: string;
  readonly employeeId: string;
  readonly employeeName: string;
  readonly employeeCode: string;
  readonly totalWorkDays: number;
  readonly presentDays: number;
  readonly paidLeaveDays: number;
  readonly lopDays: number;
  readonly baseSalaryPaise: number;
  readonly grossEarningsPaise: number;
  readonly totalDeductionsPaise: number;
  readonly netPayablePaise: number;
  readonly status: string;
}

export interface CalculatePayrollRequest {
  readonly periodMonth: number;
  readonly periodYear: number;
}

export interface DisbursePayrollRequest {
  readonly sourceBankAccountId: string;
  readonly paymentReference: string;
  readonly disbursementDate: string;
}

// Module 14: Marketing & Growth
export interface MarketingSummaryDto {
  readonly totalActiveCampaigns: number;
  readonly totalActiveCoupons: number;
  readonly totalLoyaltyMembers: number;
  readonly totalRedemptionsPaise: number;
}

export interface CustomerSegmentDto {
  readonly id: string;
  readonly companyId: string;
  readonly name: string;
  readonly criteriaType: string;
  readonly minOrderValuePaise: number;
  readonly minOrderCount: number;
  readonly description?: string;
  readonly isActive: boolean;
}

export interface CreateCustomerSegmentRequest {
  readonly name: string;
  readonly criteriaType: string;
  readonly minOrderValuePaise: number;
  readonly minOrderCount: number;
  readonly description?: string;
}

export interface CampaignDto {
  readonly id: string;
  readonly companyId: string;
  readonly name: string;
  readonly channel: string;
  readonly startDate: string;
  readonly endDate: string;
  readonly budgetPaise: number;
  readonly targetAudienceSegmentId?: string;
  readonly targetAudienceSegmentName?: string;
  readonly status: string;
  readonly notes?: string;
}

export interface CreateCampaignRequest {
  readonly name: string;
  readonly channel: string;
  readonly startDate: string;
  readonly endDate: string;
  readonly budgetPaise: number;
  readonly targetAudienceSegmentId?: string;
  readonly notes?: string;
}

export interface PromotionCouponDto {
  readonly id: string;
  readonly companyId: string;
  readonly campaignId?: string;
  readonly couponCode: string;
  readonly discountType: string;
  readonly discountValue: number;
  readonly minOrderAmountPaise: number;
  readonly maxDiscountAmountPaise?: number;
  readonly validFromUtc: string;
  readonly validToUtc: string;
  readonly maxUsageCount: number;
  readonly currentUsageCount: number;
  readonly isActive: boolean;
}

export interface CreatePromotionCouponRequest {
  readonly campaignId?: string;
  readonly couponCode: string;
  readonly discountType: string;
  readonly discountValue: number;
  readonly minOrderAmountPaise: number;
  readonly maxDiscountAmountPaise?: number;
  readonly validFromUtc: string;
  readonly validToUtc: string;
  readonly maxUsageCount: number;
}

export interface ValidateCouponRequest {
  readonly couponCode: string;
  readonly customerId?: string;
  readonly orderAmountPaise: number;
}

export interface ValidateCouponResponse {
  readonly isValid: boolean;
  readonly couponId?: string;
  readonly discountType?: string;
  readonly discountAmountPaise: number;
  readonly message: string;
}

export interface LoyaltyProgramDto {
  readonly id: string;
  readonly companyId: string;
  readonly name: string;
  readonly spendToPointsRatio: number;
  readonly pointToRupeeRatio: number;
  readonly minPointsToRedeem: number;
  readonly isActive: boolean;
}

export interface CreateLoyaltyProgramRequest {
  readonly name: string;
  readonly spendToPointsRatio: number;
  readonly pointToRupeeRatio: number;
  readonly minPointsToRedeem: number;
}

export interface LoyaltyBalanceDto {
  readonly customerId: string;
  readonly currentPoints: number;
  readonly lifetimePointsEarned: number;
  readonly lifetimePointsRedeemed: number;
  readonly monetaryValuePaise: number;
}

export interface LoyaltyTransactionDto {
  readonly id: string;
  readonly customerId: string;
  readonly transactionType: string;
  readonly points: number;
  readonly balanceAfter: number;
  readonly referenceType?: string;
  readonly referenceId?: string;
  readonly notes?: string;
  readonly createdAtUtc: string;
}

export interface RedeemLoyaltyPointsRequest {
  readonly customerId: string;
  readonly pointsToRedeem: number;
  readonly salesInvoiceId: string;
}

// Module 15: Advanced Order Management
export interface OrderSummaryDto {
  readonly totalOrders: number;
  readonly pendingOrders: number;
  readonly confirmedOrders: number;
  readonly fulfilledOrders: number;
  readonly totalBackorders: number;
}

export interface SalesOrderLineDto {
  readonly id: string;
  readonly salesOrderId: string;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly quantity: number;
  readonly unitPricePaise: number;
  readonly discountAmountPaise: number;
  readonly taxRatePercent: number;
  readonly taxAmountPaise: number;
  readonly lineTotalPaise: number;
  readonly fulfilledQuantity: number;
  readonly backorderQuantity: number;
  readonly invoicedQuantity: number;
}

export interface SalesOrderDto {
  readonly id: string;
  readonly companyId: string;
  readonly branchId: string;
  readonly orderNumber: string;
  readonly customerId: string;
  readonly customerName: string;
  readonly customerPhone?: string;
  readonly orderDate: string;
  readonly status: string;
  readonly subtotalPaise: number;
  readonly discountPaise: number;
  readonly taxAmountPaise: number;
  readonly totalAmountPaise: number;
  readonly shippingAddress?: string;
  readonly warehouseId?: string;
  readonly notes?: string;
  readonly createdAtUtc: string;
  readonly lines: readonly SalesOrderLineDto[];
}

export interface CreateSalesOrderRequest {
  readonly customerId: string;
  readonly orderDate: string;
  readonly shippingAddress?: string;
  readonly warehouseId?: string;
  readonly notes?: string;
  readonly lines: readonly {
    readonly productId: string;
    readonly quantity: number;
    readonly unitPricePaise: number;
    readonly discountAmountPaise?: number;
    readonly taxRatePercent: number;
  }[];
}

export interface FulfillOrderRequest {
  readonly warehouseId: string;
  readonly carrierName?: string;
  readonly trackingNumber?: string;
  readonly lines: readonly {
    readonly salesOrderLineId: string;
    readonly quantityDispatched: number;
  }[];
}

export interface OrderFulfillmentLineDto {
  readonly id: string;
  readonly salesOrderLineId: string;
  readonly productName: string;
  readonly quantityDispatched: number;
}

export interface OrderFulfillmentDto {
  readonly id: string;
  readonly salesOrderId: string;
  readonly fulfillmentNumber: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly carrierName?: string;
  readonly trackingNumber?: string;
  readonly dispatchedAtUtc: string;
  readonly deliveredAtUtc?: string;
  readonly status: string;
  readonly lines: readonly OrderFulfillmentLineDto[];
}

export interface BackorderDto {
  readonly id: string;
  readonly salesOrderId: string;
  readonly orderNumber: string;
  readonly salesOrderLineId: string;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly pendingQuantity: number;
  readonly status: string;
  readonly expectedRestockDate?: string;
  readonly createdAtUtc: string;
}

// Module 16: Service Management
export interface ServiceSummaryDto {
  readonly openRequests: number;
  readonly inProgressRequests: number;
  readonly waitingForPartsRequests: number;
  readonly completedRequests: number;
}

export interface ServiceCatalogDto {
  readonly id: string;
  readonly companyId: string;
  readonly code: string;
  readonly name: string;
  readonly description?: string;
  readonly standardLaborRatePaise: number;
  readonly estimatedHours: number;
  readonly isActive: boolean;
}

export interface CreateServiceCatalogRequest {
  readonly code: string;
  readonly name: string;
  readonly description?: string;
  readonly standardLaborRatePaise: number;
  readonly estimatedHours: number;
}

export interface ServiceQuotationLineDto {
  readonly id: string;
  readonly serviceQuotationId: string;
  readonly lineType: string; // "Labor" or "Part"
  readonly productId?: string;
  readonly productCode?: string;
  readonly productName?: string;
  readonly description: string;
  readonly quantity: number;
  readonly unitRatePaise: number;
  readonly lineTotalPaise: number;
}

export interface ServiceQuotationDto {
  readonly id: string;
  readonly serviceRequestId: string;
  readonly quotationNumber: string;
  readonly laborTotalPaise: number;
  readonly partsTotalPaise: number;
  readonly discountPaise: number;
  readonly taxAmountPaise: number;
  readonly totalAmountPaise: number;
  readonly status: string;
  readonly approvedAtUtc?: string;
  readonly notes?: string;
  readonly createdAtUtc: string;
  readonly lines: readonly ServiceQuotationLineDto[];
}

export interface CreateServiceQuotationRequest {
  readonly discountPaise?: number;
  readonly notes?: string;
  readonly lines: readonly {
    readonly lineType: string;
    readonly productId?: string;
    readonly description: string;
    readonly quantity: number;
    readonly unitRatePaise: number;
  }[];
}

export interface ServicePartConsumptionDto {
  readonly id: string;
  readonly serviceRequestId: string;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly quantity: number;
  readonly unitCostPaise: number;
  readonly stockMovementId: string;
  readonly consumedAtUtc: string;
}

export interface ConsumeServicePartRequest {
  readonly productId: string;
  readonly warehouseId: string;
  readonly quantity: number;
}

export interface ServiceRequestDto {
  readonly id: string;
  readonly companyId: string;
  readonly branchId: string;
  readonly requestNumber: string;
  readonly customerId: string;
  readonly customerName: string;
  readonly serviceCatalogId?: string;
  readonly serviceCatalogName?: string;
  readonly itemDescription: string;
  readonly serialNumber?: string;
  readonly reportedIssue: string;
  readonly priority: string;
  readonly status: string;
  readonly assignedEmployeeId?: string;
  readonly assignedEmployeeName?: string;
  readonly receivedDate: string;
  readonly targetCompletionDate?: string;
  readonly completedAtUtc?: string;
  readonly deliveredAtUtc?: string;
  readonly technicianNotes?: string;
  readonly salesInvoiceId?: string;
  readonly createdAtUtc: string;
  readonly quotations?: readonly ServiceQuotationDto[];
  readonly partsConsumed?: readonly ServicePartConsumptionDto[];
}

export interface CreateServiceRequest {
  readonly customerId: string;
  readonly serviceCatalogId?: string;
  readonly itemDescription: string;
  readonly serialNumber?: string;
  readonly reportedIssue: string;
  readonly priority: string;
  readonly assignedEmployeeId?: string;
  readonly targetCompletionDate?: string;
}

export interface UpdateServiceRequestStatusRequest {
  readonly status: string;
  readonly technicianNotes?: string;
  readonly assignedEmployeeId?: string;
}

export interface InvoiceServiceRequest {
  readonly paymentMethod: string;
  readonly notes?: string;
}

