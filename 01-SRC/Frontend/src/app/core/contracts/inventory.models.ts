export interface StorageLocationDto {
  readonly id: string;
  readonly warehouseId: string;
  readonly code: string;
  readonly name: string;
  readonly locationType: string;
  readonly aisle?: string | null;
  readonly rack?: string | null;
  readonly shelf?: string | null;
  readonly bin?: string | null;
  readonly capacityWeightKg?: number | null;
  readonly capacityVolumeCbm?: number | null;
  readonly isPickLocation: boolean;
  readonly isReceivingLocation: boolean;
  readonly isReturnLocation: boolean;
  readonly isActive: boolean;
  readonly notes?: string | null;
}

export interface WarehouseDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly addressLine1: string;
  readonly addressLine2?: string | null;
  readonly city: string;
  readonly state: string;
  readonly stateCode: string;
  readonly pinCode: string;
  readonly country: string;
  readonly phone?: string | null;
  readonly email?: string | null;
  readonly managerName?: string | null;
  readonly capacitySqFt?: number | null;
  readonly isActive: boolean;
  readonly assignedBranchIds: readonly string[];
  readonly locations: readonly StorageLocationDto[];
  readonly createdAtUtc: string;
}

export interface CreateWarehouseRequest {
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly addressLine1: string;
  readonly addressLine2?: string | null;
  readonly city: string;
  readonly state: string;
  readonly stateCode: string;
  readonly pinCode: string;
  readonly country?: string;
  readonly phone?: string | null;
  readonly email?: string | null;
  readonly managerName?: string | null;
  readonly capacitySqFt?: number | null;
  readonly assignedBranchIds?: readonly string[];
}

export interface UpdateWarehouseRequest {
  readonly name: string;
  readonly description?: string | null;
  readonly addressLine1: string;
  readonly addressLine2?: string | null;
  readonly city: string;
  readonly state: string;
  readonly stateCode: string;
  readonly pinCode: string;
  readonly country?: string;
  readonly phone?: string | null;
  readonly email?: string | null;
  readonly managerName?: string | null;
  readonly capacitySqFt?: number | null;
  readonly isActive: boolean;
  readonly assignedBranchIds?: readonly string[];
}

export interface CreateStorageLocationRequest {
  readonly code: string;
  readonly name: string;
  readonly locationType: string;
  readonly aisle?: string | null;
  readonly rack?: string | null;
  readonly shelf?: string | null;
  readonly bin?: string | null;
  readonly capacityWeightKg?: number | null;
  readonly capacityVolumeCbm?: number | null;
  readonly isPickLocation?: boolean;
  readonly isReceivingLocation?: boolean;
  readonly isReturnLocation?: boolean;
  readonly notes?: string | null;
}

export interface StockBalanceDto {
  readonly id: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly locationId?: string | null;
  readonly locationCode?: string | null;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly batchNumber?: string | null;
  readonly expiryDate?: string | null;
  readonly serialNumber?: string | null;
  readonly quantityOnHand: number;
  readonly quantityReserved: number;
  readonly quantityAvailable: number;
  readonly quantityInTransit: number;
  readonly averageCostPaise?: number | null;
  readonly lastInwardDateUtc?: string | null;
  readonly lastOutwardDateUtc?: string | null;
}

export interface StockMovementDto {
  readonly id: string;
  readonly movementNumber: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly locationId?: string | null;
  readonly locationCode?: string | null;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly movementType: string;
  readonly direction: string;
  readonly quantity: number;
  readonly unitCostPaise?: number | null;
  readonly totalValuePaise?: number | null;
  readonly referenceType: string;
  readonly referenceNumber: string;
  readonly batchNumber?: string | null;
  readonly expiryDate?: string | null;
  readonly movementDateUtc: string;
  readonly balanceAfterMovement: number;
}

export interface OpeningStockItemRequest {
  readonly productId: string;
  readonly storageLocationId?: string | null;
  readonly batchNumber?: string | null;
  readonly expiryDate?: string | null;
  readonly quantity: number;
  readonly unitCostPaise: number;
}

export interface PostOpeningStockRequest {
  readonly warehouseId: string;
  readonly branchId: string;
  readonly notes?: string | null;
  readonly items: readonly OpeningStockItemRequest[];
}

export interface StockAdjustmentLineDto {
  readonly id: string;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly locationId?: string | null;
  readonly locationCode?: string | null;
  readonly batchNumber?: string | null;
  readonly expiryDate?: string | null;
  readonly adjustmentType: string;
  readonly quantity: number;
  readonly unitCostPaise?: number | null;
  readonly reasonCode: string;
  readonly notes?: string | null;
}

export interface StockAdjustmentDto {
  readonly id: string;
  readonly adjustmentNumber: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly branchId: string;
  readonly status: string;
  readonly reasonCode: string;
  readonly description?: string | null;
  readonly approvedByUserId?: string | null;
  readonly approvedAtUtc?: string | null;
  readonly createdAtUtc: string;
  readonly lines: readonly StockAdjustmentLineDto[];
}

export interface CreateStockAdjustmentLineRequest {
  readonly productId: string;
  readonly locationId?: string | null;
  readonly batchNumber?: string | null;
  readonly expiryDate?: string | null;
  readonly adjustmentType: string;
  readonly quantity: number;
  readonly unitCostPaise?: number | null;
  readonly reasonCode: string;
  readonly notes?: string | null;
}

export interface CreateStockAdjustmentRequest {
  readonly warehouseId: string;
  readonly branchId: string;
  readonly reasonCode: string;
  readonly description?: string | null;
  readonly lines: readonly CreateStockAdjustmentLineRequest[];
}

export interface StockCountLineDto {
  readonly id: string;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly locationId?: string | null;
  readonly locationCode?: string | null;
  readonly batchNumber?: string | null;
  readonly systemQuantity: number;
  readonly countedQuantity?: number | null;
  readonly discrepancyQuantity?: number | null;
  readonly unitCostPaise?: number | null;
  readonly notes?: string | null;
}

export interface StockCountDto {
  readonly id: string;
  readonly countNumber: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly branchId: string;
  readonly countType: string;
  readonly status: string;
  readonly scheduledDate: string;
  readonly countedAtUtc?: string | null;
  readonly reconciledAtUtc?: string | null;
  readonly notes?: string | null;
  readonly lines: readonly StockCountLineDto[];
}

export interface CreateStockCountRequest {
  readonly warehouseId: string;
  readonly branchId: string;
  readonly countType: string;
  readonly scheduledDate: string;
  readonly notes?: string | null;
  readonly productIds?: readonly string[];
}

export interface RecordStockCountLineRequest {
  readonly lineId: string;
  readonly countedQuantity: number;
  readonly notes?: string | null;
}

export interface RecordStockCountLinesRequest {
  readonly lines: readonly RecordStockCountLineRequest[];
}

export interface PurchaseOrderLineDto {
  readonly id: string;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly quantity: number;
  readonly receivedQuantity: number;
  readonly returnedQuantity: number;
  readonly unitPricePaise: number;
  readonly gstRatePercentage: number;
  readonly taxAmountPaise: number;
  readonly totalAmountPaise: number;
  readonly expectedDeliveryDate?: string | null;
  readonly notes?: string | null;
}

export interface PurchaseOrderDto {
  readonly id: string;
  readonly orderNumber: string;
  readonly branchId: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly orderDate: string;
  readonly expectedDeliveryDate?: string | null;
  readonly paymentTerms: string;
  readonly status: string;
  readonly subtotalPaise: number;
  readonly totalTaxPaise: number;
  readonly grandTotalPaise: number;
  readonly notes?: string | null;
  readonly lines: readonly PurchaseOrderLineDto[];
}

export interface CreatePurchaseOrderLineRequest {
  readonly productId: string;
  readonly quantity: number;
  readonly unitPricePaise: number;
  readonly gstRatePercentage: number;
  readonly expectedDeliveryDate?: string | null;
  readonly notes?: string | null;
}

export interface CreatePurchaseOrderRequest {
  readonly branchId: string;
  readonly warehouseId: string;
  readonly supplierId: string;
  readonly orderDate?: string;
  readonly expectedDeliveryDate?: string | null;
  readonly paymentTerms: string;
  readonly notes?: string | null;
  readonly lines: readonly CreatePurchaseOrderLineRequest[];
}

export interface GoodsReceiptLineDto {
  readonly id: string;
  readonly purchaseOrderLineId?: string | null;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly storageLocationId?: string | null;
  readonly storageLocationCode?: string | null;
  readonly batchNumber?: string | null;
  readonly expiryDate?: string | null;
  readonly receivedQuantity: number;
  readonly acceptedQuantity: number;
  readonly rejectedQuantity: number;
  readonly rejectionReason?: string | null;
  readonly unitPricePaise: number;
  readonly gstRatePercentage: number;
  readonly taxAmountPaise: number;
  readonly totalAmountPaise: number;
}

export interface GoodsReceiptNoteDto {
  readonly id: string;
  readonly grnNumber: string;
  readonly branchId: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly purchaseOrderId?: string | null;
  readonly purchaseOrderNumber?: string | null;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly receiptDate: string;
  readonly supplierInvoiceNumber?: string | null;
  readonly supplierInvoiceDate?: string | null;
  readonly status: string;
  readonly subtotalPaise: number;
  readonly totalTaxPaise: number;
  readonly grandTotalPaise: number;
  readonly notes?: string | null;
  readonly lines: readonly GoodsReceiptLineDto[];
}

export interface CreateGoodsReceiptLineRequest {
  readonly purchaseOrderLineId?: string | null;
  readonly productId: string;
  readonly storageLocationId?: string | null;
  readonly batchNumber?: string | null;
  readonly expiryDate?: string | null;
  readonly receivedQuantity: number;
  readonly acceptedQuantity: number;
  readonly rejectedQuantity?: number;
  readonly rejectionReason?: string | null;
  readonly unitPricePaise: number;
  readonly gstRatePercentage: number;
}

export interface CreateGoodsReceiptRequest {
  readonly branchId: string;
  readonly warehouseId: string;
  readonly purchaseOrderId?: string | null;
  readonly supplierId: string;
  readonly receiptDate?: string;
  readonly supplierInvoiceNumber?: string | null;
  readonly supplierInvoiceDate?: string | null;
  readonly notes?: string | null;
  readonly lines: readonly CreateGoodsReceiptLineRequest[];
}

export interface PurchaseReturnLineDto {
  readonly id: string;
  readonly goodsReceiptLineId?: string | null;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly quantity: number;
  readonly unitPricePaise: number;
  readonly gstRatePercentage: number;
  readonly taxAmountPaise: number;
  readonly totalAmountPaise: number;
  readonly returnReason: string;
}

export interface PurchaseReturnDto {
  readonly id: string;
  readonly returnNumber: string;
  readonly branchId: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly goodsReceiptId?: string | null;
  readonly returnDate: string;
  readonly status: string;
  readonly reasonCode: string;
  readonly subtotalPaise: number;
  readonly totalTaxPaise: number;
  readonly grandTotalPaise: number;
  readonly notes?: string | null;
  readonly lines: readonly PurchaseReturnLineDto[];
}

export interface CreatePurchaseReturnLineRequest {
  readonly goodsReceiptLineId?: string | null;
  readonly productId: string;
  readonly quantity: number;
  readonly unitPricePaise: number;
  readonly gstRatePercentage: number;
  readonly returnReason: string;
}

export interface CreatePurchaseReturnRequest {
  readonly branchId: string;
  readonly warehouseId: string;
  readonly supplierId: string;
  readonly goodsReceiptId?: string | null;
  readonly returnDate?: string;
  readonly reasonCode: string;
  readonly notes?: string | null;
  readonly lines: readonly CreatePurchaseReturnLineRequest[];
}

export interface StockTransferLineDto {
  readonly id: string;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly sourceLocationId?: string | null;
  readonly destinationLocationId?: string | null;
  readonly batchNumber?: string | null;
  readonly requestedQuantity: number;
  readonly dispatchedQuantity: number;
  readonly receivedQuantity: number;
  readonly damagedQuantity: number;
  readonly unitCostPaise?: number | null;
}

export interface StockTransferDto {
  readonly id: string;
  readonly transferNumber: string;
  readonly sourceBranchId: string;
  readonly destinationBranchId: string;
  readonly sourceWarehouseId: string;
  readonly sourceWarehouseName: string;
  readonly destinationWarehouseId: string;
  readonly destinationWarehouseName: string;
  readonly status: string;
  readonly transferType: string;
  readonly transferDate: string;
  readonly dispatchDateUtc?: string | null;
  readonly receivedDateUtc?: string | null;
  readonly carrierName?: string | null;
  readonly trackingNumber?: string | null;
  readonly notes?: string | null;
  readonly lines: readonly StockTransferLineDto[];
}

export interface CreateStockTransferLineRequest {
  readonly productId: string;
  readonly sourceLocationId?: string | null;
  readonly destinationLocationId?: string | null;
  readonly batchNumber?: string | null;
  readonly requestedQuantity: number;
  readonly unitCostPaise?: number | null;
}

export interface CreateStockTransferRequest {
  readonly sourceBranchId: string;
  readonly destinationBranchId: string;
  readonly sourceWarehouseId: string;
  readonly destinationWarehouseId: string;
  readonly transferType?: string;
  readonly notes?: string | null;
  readonly lines: readonly CreateStockTransferLineRequest[];
}

export interface DispatchStockTransferRequest {
  readonly carrierName?: string | null;
  readonly trackingNumber?: string | null;
}

export interface ReceiveStockTransferLineRequest {
  readonly lineId: string;
  readonly receivedQuantity: number;
  readonly damagedQuantity?: number;
  readonly destinationLocationId?: string | null;
}

export interface ReceiveStockTransferRequest {
  readonly lines: readonly ReceiveStockTransferLineRequest[];
}

export interface ReorderRuleDto {
  readonly id: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly minStockLevel: number;
  readonly maxStockLevel: number;
  readonly reorderPoint: number;
  readonly safetyStock: number;
  readonly leadTimeDays: number;
  readonly reorderQuantity: number;
  readonly isActive: boolean;
}

export interface UpsertReorderRuleRequest {
  readonly warehouseId: string;
  readonly productId: string;
  readonly minStockLevel: number;
  readonly maxStockLevel: number;
  readonly reorderPoint: number;
  readonly safetyStock: number;
  readonly leadTimeDays: number;
  readonly reorderQuantity: number;
  readonly isActive: boolean;
}

export interface ReorderSuggestionDto {
  readonly productId: string;
  readonly productCode: string;
  readonly productName: string;
  readonly warehouseId: string;
  readonly warehouseName: string;
  readonly currentStock: number;
  readonly reorderPoint: number;
  readonly safetyStock: number;
  readonly recommendedOrderQuantity: number;
  readonly leadTimeDays: number;
  readonly urgency: 'Critical' | 'Warning' | 'Normal';
}

