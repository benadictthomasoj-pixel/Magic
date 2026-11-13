export enum ProductType {
  Goods = 1,
  Service = 2,
  NonStock = 3,
  Composite = 4,
}

export enum CustomerType {
  Individual = 1,
  Retail = 2,
  Wholesale = 3,
  Business = 4,
}

export enum GstRegistrationType {
  Registered = 1,
  Unregistered = 2,
  Composition = 3,
  Overseas = 4,
  SEZ = 5,
}

export interface UnitOfMeasureDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly precision: number;
  readonly isActive: boolean;
}

export interface TaxCodeDto {
  readonly id: string;
  readonly code: string;
  readonly description: string;
  readonly hsnSacCode?: string | null;
  readonly ratePercentage: number;
  readonly cgstPercentage: number;
  readonly sgstPercentage: number;
  readonly igstPercentage: number;
  readonly cessPercentage: number;
  readonly isActive: boolean;
}

export interface DepartmentDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly isActive: boolean;
  readonly productCount: number;
}

export interface CreateDepartmentRequest {
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
}

export interface UpdateDepartmentRequest {
  readonly name: string;
  readonly description?: string | null;
  readonly isActive: boolean;
}

export interface CategoryDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly parentCategoryId?: string | null;
  readonly parentCategoryName?: string | null;
  readonly departmentId?: string | null;
  readonly departmentName?: string | null;
  readonly hsnCode?: string | null;
  readonly defaultGstRate?: number | null;
  readonly displayOrder: number;
  readonly imageUrl?: string | null;
  readonly isActive: boolean;
  readonly productCount: number;
  readonly children?: readonly CategoryDto[] | null;
}

export interface CreateCategoryRequest {
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly parentCategoryId?: string | null;
  readonly departmentId?: string | null;
  readonly hsnCode?: string | null;
  readonly defaultGstRate?: number | null;
  readonly displayOrder: number;
  readonly imageUrl?: string | null;
}

export interface UpdateCategoryRequest {
  readonly name: string;
  readonly description?: string | null;
  readonly parentCategoryId?: string | null;
  readonly departmentId?: string | null;
  readonly hsnCode?: string | null;
  readonly defaultGstRate?: number | null;
  readonly displayOrder: number;
  readonly imageUrl?: string | null;
  readonly isActive: boolean;
}

export interface BrandDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly logoUrl?: string | null;
  readonly websiteUrl?: string | null;
  readonly isActive: boolean;
  readonly productCount: number;
}

export interface CreateBrandRequest {
  readonly code: string;
  readonly name: string;
  readonly description?: string | null;
  readonly logoUrl?: string | null;
  readonly websiteUrl?: string | null;
}

export interface UpdateBrandRequest {
  readonly name: string;
  readonly description?: string | null;
  readonly logoUrl?: string | null;
  readonly websiteUrl?: string | null;
  readonly isActive: boolean;
}

export interface ProductSummaryDto {
  readonly id: string;
  readonly productCode: string;
  readonly name: string;
  readonly productType: ProductType;
  readonly categoryName: string;
  readonly brandName?: string | null;
  readonly unitOfMeasureCode: string;
  readonly hsnSacCode: string;
  readonly gstRatePercentage: number;
  readonly sku: string;
  readonly primaryBarcode?: string | null;
  readonly sellingPricePaise: number;
  readonly mrpPaise: number;
  readonly purchasePricePaise: number;
  readonly costPricePaise: number;
  readonly hasVariants: boolean;
  readonly isActive: boolean;
  readonly imageUrl?: string | null;
}

export interface ProductVariantDto {
  readonly id: string;
  readonly sku: string;
  readonly barcode?: string | null;
  readonly variantName: string;
  readonly attributesJson: string;
  readonly sellingPricePaise: number;
  readonly mrpPaise: number;
  readonly purchasePricePaise: number;
  readonly isActive: boolean;
}

export interface ProductBarcodeDto {
  readonly id: string;
  readonly variantId?: string | null;
  readonly barcode: string;
  readonly barcodeType: string;
  readonly isPrimary: boolean;
}

export interface ProductPriceTierDto {
  readonly id: string;
  readonly variantId?: string | null;
  readonly branchId?: string | null;
  readonly priceTier: string;
  readonly minQuantity: number;
  readonly unitPricePaise: number;
  readonly discountPercentage: number;
  readonly effectiveFromUtc: string;
  readonly effectiveToUtc?: string | null;
  readonly isActive: boolean;
}

export interface ProductDetailDto {
  readonly id: string;
  readonly productCode: string;
  readonly name: string;
  readonly description?: string | null;
  readonly productType: ProductType;
  readonly categoryId: string;
  readonly categoryName: string;
  readonly brandId?: string | null;
  readonly brandName?: string | null;
  readonly departmentId?: string | null;
  readonly departmentName?: string | null;
  readonly unitOfMeasureId: string;
  readonly unitOfMeasureCode: string;
  readonly hsnSacCode: string;
  readonly taxCodeId?: string | null;
  readonly gstRatePercentage: number;
  readonly sku: string;
  readonly primaryBarcode?: string | null;
  readonly purchasePricePaise: number;
  readonly costPricePaise: number;
  readonly sellingPricePaise: number;
  readonly mrpPaise: number;
  readonly minSellingPricePaise: number;
  readonly wholesalePricePaise: number;
  readonly reorderLevel: number;
  readonly minStockLevel: number;
  readonly maxStockLevel: number;
  readonly imageUrl?: string | null;
  readonly hasVariants: boolean;
  readonly isActive: boolean;
  readonly createdAtUtc: string;
  readonly updatedAtUtc?: string | null;
  readonly variants: readonly ProductVariantDto[];
  readonly barcodes: readonly ProductBarcodeDto[];
  readonly priceTiers: readonly ProductPriceTierDto[];
}

export interface CreateProductRequest {
  readonly productCode: string;
  readonly name: string;
  readonly description?: string | null;
  readonly productType: ProductType;
  readonly categoryId: string;
  readonly brandId?: string | null;
  readonly departmentId?: string | null;
  readonly unitOfMeasureId: string;
  readonly hsnSacCode: string;
  readonly taxCodeId?: string | null;
  readonly gstRatePercentage: number;
  readonly sku?: string | null;
  readonly primaryBarcode?: string | null;
  readonly purchasePricePaise: number;
  readonly costPricePaise: number;
  readonly sellingPricePaise: number;
  readonly mrpPaise: number;
  readonly minSellingPricePaise: number;
  readonly wholesalePricePaise: number;
  readonly reorderLevel: number;
  readonly minStockLevel: number;
  readonly maxStockLevel: number;
  readonly imageUrl?: string | null;
}

export interface UpdateProductRequest {
  readonly name: string;
  readonly description?: string | null;
  readonly productType: ProductType;
  readonly categoryId: string;
  readonly brandId?: string | null;
  readonly departmentId?: string | null;
  readonly unitOfMeasureId: string;
  readonly hsnSacCode: string;
  readonly taxCodeId?: string | null;
  readonly gstRatePercentage: number;
  readonly primaryBarcode?: string | null;
  readonly purchasePricePaise: number;
  readonly costPricePaise: number;
  readonly sellingPricePaise: number;
  readonly mrpPaise: number;
  readonly minSellingPricePaise: number;
  readonly wholesalePricePaise: number;
  readonly reorderLevel: number;
  readonly minStockLevel: number;
  readonly maxStockLevel: number;
  readonly imageUrl?: string | null;
  readonly isActive: boolean;
}

export interface BarcodeLookupResultDto {
  readonly productId: string;
  readonly variantId?: string | null;
  readonly productCode: string;
  readonly name: string;
  readonly variantName?: string | null;
  readonly sku: string;
  readonly barcode: string;
  readonly unitOfMeasureCode: string;
  readonly hsnSacCode: string;
  readonly gstRatePercentage: number;
  readonly sellingPricePaise: number;
  readonly mrpPaise: number;
}

export interface CustomerGroupDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly defaultDiscountPercentage: number;
  readonly defaultCreditLimitPaise: number;
  readonly defaultCreditDays: number;
  readonly isActive: boolean;
}

export interface CreateCustomerGroupRequest {
  readonly code: string;
  readonly name: string;
  readonly defaultDiscountPercentage: number;
  readonly defaultCreditLimitPaise: number;
  readonly defaultCreditDays: number;
}

export interface CustomerAddressDto {
  readonly id: string;
  readonly addressType: number;
  readonly addressLine1: string;
  readonly addressLine2?: string | null;
  readonly city: string;
  readonly district?: string | null;
  readonly state: string;
  readonly stateCode: string;
  readonly pinCode: string;
  readonly country: string;
  readonly isDefaultBilling: boolean;
  readonly isDefaultShipping: boolean;
}

export interface CustomerSummaryDto {
  readonly id: string;
  readonly customerCode: string;
  readonly name: string;
  readonly customerType: CustomerType;
  readonly gstType: GstRegistrationType;
  readonly gstin?: string | null;
  readonly contactPerson?: string | null;
  readonly phone: string;
  readonly email?: string | null;
  readonly city?: string | null;
  readonly state?: string | null;
  readonly creditLimitPaise: number;
  readonly creditPeriodDays: number;
  readonly isCreditHold: boolean;
  readonly isActive: boolean;
}

export interface CustomerDetailDto {
  readonly id: string;
  readonly customerCode: string;
  readonly name: string;
  readonly customerType: CustomerType;
  readonly gstType: GstRegistrationType;
  readonly gstin?: string | null;
  readonly pan?: string | null;
  readonly contactPerson?: string | null;
  readonly phone: string;
  readonly email?: string | null;
  readonly customerGroupId?: string | null;
  readonly customerGroupName?: string | null;
  readonly assignedBranchId?: string | null;
  readonly creditLimitPaise: number;
  readonly creditPeriodDays: number;
  readonly isCreditHold: boolean;
  readonly isActive: boolean;
  readonly notes?: string | null;
  readonly glAccountId?: string | null;
  readonly glAccountCode?: string | null;
  readonly createdAtUtc: string;
  readonly updatedAtUtc?: string | null;
  readonly addresses: readonly CustomerAddressDto[];
}

export interface CreateCustomerRequest {
  readonly customerCode: string;
  readonly name: string;
  readonly customerType: CustomerType;
  readonly gstType: GstRegistrationType;
  readonly gstin?: string | null;
  readonly pan?: string | null;
  readonly contactPerson?: string | null;
  readonly phone: string;
  readonly email?: string | null;
  readonly customerGroupId?: string | null;
  readonly assignedBranchId?: string | null;
  readonly creditLimitPaise: number;
  readonly creditPeriodDays: number;
  readonly notes?: string | null;
  readonly addresses?: readonly {
    addressType: number;
    addressLine1: string;
    addressLine2?: string | null;
    city: string;
    district?: string | null;
    state: string;
    stateCode: string;
    pinCode: string;
    country: string;
    isDefaultBilling: boolean;
    isDefaultShipping: boolean;
  }[];
}

export interface UpdateCustomerRequest {
  readonly name: string;
  readonly customerType: CustomerType;
  readonly gstType: GstRegistrationType;
  readonly gstin?: string | null;
  readonly pan?: string | null;
  readonly contactPerson?: string | null;
  readonly phone: string;
  readonly email?: string | null;
  readonly customerGroupId?: string | null;
  readonly assignedBranchId?: string | null;
  readonly notes?: string | null;
  readonly isActive: boolean;
  readonly addresses?: readonly {
    addressType: number;
    addressLine1: string;
    addressLine2?: string | null;
    city: string;
    district?: string | null;
    state: string;
    stateCode: string;
    pinCode: string;
    country: string;
    isDefaultBilling: boolean;
    isDefaultShipping: boolean;
  }[];
}

export interface UpdateCreditLimitRequest {
  readonly newCreditLimitPaise: number;
  readonly creditPeriodDays: number;
  readonly isCreditHold: boolean;
  readonly reason?: string | null;
}

export interface SupplierAddressDto {
  readonly id: string;
  readonly addressType: number;
  readonly addressLine1: string;
  readonly addressLine2?: string | null;
  readonly city: string;
  readonly state: string;
  readonly stateCode: string;
  readonly pinCode: string;
  readonly country: string;
  readonly isPrimary: boolean;
}

export interface SupplierBankAccountDto {
  readonly id: string;
  readonly bankName: string;
  readonly branchName: string;
  readonly accountNumberMasked: string;
  readonly ifscCode: string;
  readonly accountType: string;
  readonly isPrimary: boolean;
  readonly isVerified: boolean;
}

export interface UnmaskedBankAccountDto {
  readonly bankAccountId: string;
  readonly bankName: string;
  readonly branchName: string;
  readonly plaintextAccountNumber: string;
  readonly ifscCode: string;
  readonly accountType: string;
}

export interface SupplierSummaryDto {
  readonly id: string;
  readonly supplierCode: string;
  readonly name: string;
  readonly category: string;
  readonly gstType: GstRegistrationType;
  readonly gstin?: string | null;
  readonly contactPerson?: string | null;
  readonly phone: string;
  readonly email?: string | null;
  readonly paymentTerms: string;
  readonly creditPeriodDays: number;
  readonly isMsmeRegistered: boolean;
  readonly msmeType?: string | null;
  readonly isActive: boolean;
}

export interface SupplierDetailDto {
  readonly id: string;
  readonly supplierCode: string;
  readonly name: string;
  readonly category: string;
  readonly gstType: GstRegistrationType;
  readonly gstin?: string | null;
  readonly pan?: string | null;
  readonly contactPerson?: string | null;
  readonly phone: string;
  readonly email?: string | null;
  readonly paymentTerms: string;
  readonly creditPeriodDays: number;
  readonly msmeType?: string | null;
  readonly udyamRegistrationNumber?: string | null;
  readonly isMsmeRegistered: boolean;
  readonly isActive: boolean;
  readonly notes?: string | null;
  readonly glAccountId?: string | null;
  readonly glAccountCode?: string | null;
  readonly createdAtUtc: string;
  readonly updatedAtUtc?: string | null;
  readonly addresses: readonly SupplierAddressDto[];
  readonly bankAccounts: readonly SupplierBankAccountDto[];
}

export interface CreateSupplierRequest {
  readonly supplierCode: string;
  readonly name: string;
  readonly category: string;
  readonly gstType: GstRegistrationType;
  readonly gstin?: string | null;
  readonly pan?: string | null;
  readonly contactPerson?: string | null;
  readonly phone: string;
  readonly email?: string | null;
  readonly paymentTerms: string;
  readonly creditPeriodDays: number;
  readonly msmeType?: string | null;
  readonly udyamRegistrationNumber?: string | null;
  readonly isMsmeRegistered: boolean;
  readonly notes?: string | null;
  readonly addresses?: readonly {
    addressType: number;
    addressLine1: string;
    addressLine2?: string | null;
    city: string;
    state: string;
    stateCode: string;
    pinCode: string;
    country: string;
    isPrimary: boolean;
  }[];
  readonly bankAccounts?: readonly {
    bankName: string;
    branchName: string;
    accountNumber: string;
    ifscCode: string;
    accountType: string;
    isPrimary: boolean;
  }[];
}

export interface UpdateSupplierRequest {
  readonly name: string;
  readonly category: string;
  readonly gstType: GstRegistrationType;
  readonly gstin?: string | null;
  readonly pan?: string | null;
  readonly contactPerson?: string | null;
  readonly phone: string;
  readonly email?: string | null;
  readonly paymentTerms: string;
  readonly creditPeriodDays: number;
  readonly msmeType?: string | null;
  readonly udyamRegistrationNumber?: string | null;
  readonly isMsmeRegistered: boolean;
  readonly notes?: string | null;
  readonly isActive: boolean;
  readonly addresses?: readonly {
    addressType: number;
    addressLine1: string;
    addressLine2?: string | null;
    city: string;
    state: string;
    stateCode: string;
    pinCode: string;
    country: string;
    isPrimary: boolean;
  }[];
  readonly bankAccounts?: readonly {
    bankName: string;
    branchName: string;
    accountNumber: string;
    ifscCode: string;
    accountType: string;
    isPrimary: boolean;
  }[];
}
