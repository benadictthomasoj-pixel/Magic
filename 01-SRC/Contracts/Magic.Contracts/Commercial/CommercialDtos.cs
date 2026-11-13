namespace Magic.Contracts.Commercial;

// ==========================================
// 1. Units of Measure & Tax Codes
// ==========================================

public sealed record UnitOfMeasureDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int Precision { get; init; }
    public required bool IsActive { get; init; }
}

public sealed record TaxCodeDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public string? HsnSacCode { get; init; }
    public required decimal RatePercentage { get; init; }
    public required decimal CgstPercentage { get; init; }
    public required decimal SgstPercentage { get; init; }
    public required decimal IgstPercentage { get; init; }
    public required decimal CessPercentage { get; init; }
    public required bool IsActive { get; init; }
}

// ==========================================
// 2. Departments, Categories & Brands
// ==========================================

public sealed record DepartmentDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public int ProductCount { get; init; }
}

public sealed record CreateDepartmentRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public sealed record UpdateDepartmentRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record CategoryDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public string? ParentCategoryName { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string? HsnCode { get; init; }
    public decimal? DefaultGstRate { get; init; }
    public int DisplayOrder { get; init; }
    public string? ImageUrl { get; init; }
    public required bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public IReadOnlyList<CategoryDto>? Children { get; init; }
}

public sealed record CreateCategoryRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? HsnCode { get; init; }
    public decimal? DefaultGstRate { get; init; }
    public int DisplayOrder { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed record UpdateCategoryRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? HsnCode { get; init; }
    public decimal? DefaultGstRate { get; init; }
    public int DisplayOrder { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record BrandDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public required bool IsActive { get; init; }
    public int ProductCount { get; init; }
}

public sealed record CreateBrandRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? WebsiteUrl { get; init; }
}

public sealed record UpdateBrandRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public bool IsActive { get; init; } = true;
}

// ==========================================
// 3. Products, Variants, Barcodes & Pricing
// ==========================================

public enum ProductType
{
    Goods = 1,
    Service = 2,
    NonStock = 3,
    Composite = 4
}

public sealed record ProductSummaryDto
{
    public required Guid Id { get; init; }
    public required string ProductCode { get; init; }
    public required string Name { get; init; }
    public required ProductType ProductType { get; init; }
    public required string CategoryName { get; init; }
    public string? BrandName { get; init; }
    public required string UnitOfMeasureCode { get; init; }
    public required string HsnSacCode { get; init; }
    public required decimal GstRatePercentage { get; init; }
    public required string Sku { get; init; }
    public string? PrimaryBarcode { get; init; }
    public required long SellingPricePaise { get; init; }
    public required long MrpPaise { get; init; }
    public required long PurchasePricePaise { get; init; }
    public required long CostPricePaise { get; init; }
    public required bool HasVariants { get; init; }
    public required bool IsActive { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed record ProductVariantDto
{
    public required Guid Id { get; init; }
    public required string Sku { get; init; }
    public string? Barcode { get; init; }
    public required string VariantName { get; init; }
    public required string AttributesJson { get; init; }
    public required long SellingPricePaise { get; init; }
    public required long MrpPaise { get; init; }
    public required long PurchasePricePaise { get; init; }
    public required bool IsActive { get; init; }
}

public sealed record ProductBarcodeDto
{
    public required Guid Id { get; init; }
    public Guid? VariantId { get; init; }
    public required string Barcode { get; init; }
    public required string BarcodeType { get; init; }
    public required bool IsPrimary { get; init; }
}

public sealed record ProductPriceTierDto
{
    public required Guid Id { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? BranchId { get; init; }
    public required string PriceTier { get; init; }
    public required decimal MinQuantity { get; init; }
    public required long UnitPricePaise { get; init; }
    public required decimal DiscountPercentage { get; init; }
    public required DateTimeOffset EffectiveFromUtc { get; init; }
    public DateTimeOffset? EffectiveToUtc { get; init; }
    public required bool IsActive { get; init; }
}

public sealed record ProductDetailDto
{
    public required Guid Id { get; init; }
    public required string ProductCode { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required ProductType ProductType { get; init; }
    public required Guid CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public Guid? BrandId { get; init; }
    public string? BrandName { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public required Guid UnitOfMeasureId { get; init; }
    public required string UnitOfMeasureCode { get; init; }
    public required string HsnSacCode { get; init; }
    public Guid? TaxCodeId { get; init; }
    public required decimal GstRatePercentage { get; init; }
    public required string Sku { get; init; }
    public string? PrimaryBarcode { get; init; }
    public required long PurchasePricePaise { get; init; }
    public required long CostPricePaise { get; init; }
    public required long SellingPricePaise { get; init; }
    public required long MrpPaise { get; init; }
    public required long MinSellingPricePaise { get; init; }
    public required long WholesalePricePaise { get; init; }
    public required decimal ReorderLevel { get; init; }
    public required decimal MinStockLevel { get; init; }
    public required decimal MaxStockLevel { get; init; }
    public string? ImageUrl { get; init; }
    public required bool HasVariants { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public IReadOnlyList<ProductVariantDto> Variants { get; init; } = [];
    public IReadOnlyList<ProductBarcodeDto> Barcodes { get; init; } = [];
    public IReadOnlyList<ProductPriceTierDto> PriceTiers { get; init; } = [];
}

public sealed record CreateProductRequest
{
    public required string ProductCode { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public ProductType ProductType { get; init; } = ProductType.Goods;
    public required Guid CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public Guid? DepartmentId { get; init; }
    public required Guid UnitOfMeasureId { get; init; }
    public required string HsnSacCode { get; init; }
    public Guid? TaxCodeId { get; init; }
    public decimal GstRatePercentage { get; init; } = 18m;
    public string? Sku { get; init; }
    public string? PrimaryBarcode { get; init; }
    public long PurchasePricePaise { get; init; }
    public long CostPricePaise { get; init; }
    public required long SellingPricePaise { get; init; }
    public required long MrpPaise { get; init; }
    public long MinSellingPricePaise { get; init; }
    public long WholesalePricePaise { get; init; }
    public decimal ReorderLevel { get; init; }
    public decimal MinStockLevel { get; init; }
    public decimal MaxStockLevel { get; init; }
    public string? ImageUrl { get; init; }
    public List<CreateVariantDto>? Variants { get; init; }
    public List<CreateBarcodeDto>? Barcodes { get; init; }
    public List<CreatePriceTierDto>? PriceTiers { get; init; }
}

public sealed record CreateVariantDto
{
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public required string VariantName { get; init; }
    public required string AttributesJson { get; init; }
    public required long SellingPricePaise { get; init; }
    public required long MrpPaise { get; init; }
    public long PurchasePricePaise { get; init; }
}

public sealed record CreateBarcodeDto
{
    public Guid? VariantId { get; init; }
    public required string Barcode { get; init; }
    public string BarcodeType { get; init; } = "EAN13";
    public bool IsPrimary { get; init; }
}

public sealed record CreatePriceTierDto
{
    public Guid? VariantId { get; init; }
    public Guid? BranchId { get; init; }
    public required string PriceTier { get; init; }
    public decimal MinQuantity { get; init; } = 1m;
    public required long UnitPricePaise { get; init; }
    public decimal DiscountPercentage { get; init; }
    public DateTimeOffset? EffectiveFromUtc { get; init; }
    public DateTimeOffset? EffectiveToUtc { get; init; }
}

public sealed record UpdateProductRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public ProductType ProductType { get; init; }
    public required Guid CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public Guid? DepartmentId { get; init; }
    public required Guid UnitOfMeasureId { get; init; }
    public required string HsnSacCode { get; init; }
    public Guid? TaxCodeId { get; init; }
    public decimal GstRatePercentage { get; init; }
    public string? PrimaryBarcode { get; init; }
    public long PurchasePricePaise { get; init; }
    public long CostPricePaise { get; init; }
    public required long SellingPricePaise { get; init; }
    public required long MrpPaise { get; init; }
    public long MinSellingPricePaise { get; init; }
    public long WholesalePricePaise { get; init; }
    public decimal ReorderLevel { get; init; }
    public decimal MinStockLevel { get; init; }
    public decimal MaxStockLevel { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record BarcodeLookupResultDto
{
    public required Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public required string ProductCode { get; init; }
    public required string Name { get; init; }
    public string? VariantName { get; init; }
    public required string Sku { get; init; }
    public required string Barcode { get; init; }
    public required string UnitOfMeasureCode { get; init; }
    public required string HsnSacCode { get; init; }
    public required decimal GstRatePercentage { get; init; }
    public required long SellingPricePaise { get; init; }
    public required long MrpPaise { get; init; }
}

// ==========================================
// 4. Customers & Customer Groups
// ==========================================

public enum CustomerType
{
    Individual = 1,
    Retail = 2,
    Wholesale = 3,
    Business = 4
}

public enum GstRegistrationType
{
    Registered = 1,
    Unregistered = 2,
    Composition = 3,
    Overseas = 4,
    SEZ = 5
}

public sealed record CustomerGroupDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public decimal DefaultDiscountPercentage { get; init; }
    public long DefaultCreditLimitPaise { get; init; }
    public int DefaultCreditDays { get; init; }
    public bool IsActive { get; init; }
}

public sealed record CreateCustomerGroupRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public decimal DefaultDiscountPercentage { get; init; }
    public long DefaultCreditLimitPaise { get; init; }
    public int DefaultCreditDays { get; init; }
}

public sealed record CustomerAddressDto
{
    public required Guid Id { get; init; }
    public required int AddressType { get; init; } // 1=Billing, 2=Shipping, 3=Both
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public string? District { get; init; }
    public required string State { get; init; }
    public required string StateCode { get; init; }
    public required string PinCode { get; init; }
    public required string Country { get; init; }
    public required bool IsDefaultBilling { get; init; }
    public required bool IsDefaultShipping { get; init; }
}

public sealed record CreateCustomerAddressDto
{
    public int AddressType { get; init; } = 3;
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public string? District { get; init; }
    public required string State { get; init; }
    public required string StateCode { get; init; }
    public required string PinCode { get; init; }
    public string Country { get; init; } = "India";
    public bool IsDefaultBilling { get; init; } = true;
    public bool IsDefaultShipping { get; init; } = true;
}

public sealed record CustomerSummaryDto
{
    public required Guid Id { get; init; }
    public required string CustomerCode { get; init; }
    public required string Name { get; init; }
    public required CustomerType CustomerType { get; init; }
    public required GstRegistrationType GstType { get; init; }
    public string? Gstin { get; init; }
    public string? ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public required long CreditLimitPaise { get; init; }
    public required int CreditPeriodDays { get; init; }
    public required bool IsCreditHold { get; init; }
    public required bool IsActive { get; init; }
}

public sealed record CustomerDetailDto
{
    public required Guid Id { get; init; }
    public required string CustomerCode { get; init; }
    public required string Name { get; init; }
    public required CustomerType CustomerType { get; init; }
    public required GstRegistrationType GstType { get; init; }
    public string? Gstin { get; init; }
    public string? Pan { get; init; }
    public string? ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public Guid? CustomerGroupId { get; init; }
    public string? CustomerGroupName { get; init; }
    public Guid? AssignedBranchId { get; init; }
    public required long CreditLimitPaise { get; init; }
    public required int CreditPeriodDays { get; init; }
    public required bool IsCreditHold { get; init; }
    public required bool IsActive { get; init; }
    public string? Notes { get; init; }
    public Guid? GlAccountId { get; init; }
    public string? GlAccountCode { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public IReadOnlyList<CustomerAddressDto> Addresses { get; init; } = [];
}

public sealed record CreateCustomerRequest
{
    public required string CustomerCode { get; init; }
    public required string Name { get; init; }
    public CustomerType CustomerType { get; init; } = CustomerType.Retail;
    public GstRegistrationType GstType { get; init; } = GstRegistrationType.Unregistered;
    public string? Gstin { get; init; }
    public string? Pan { get; init; }
    public string? ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public Guid? CustomerGroupId { get; init; }
    public Guid? AssignedBranchId { get; init; }
    public long CreditLimitPaise { get; init; }
    public int CreditPeriodDays { get; init; }
    public string? Notes { get; init; }
    public List<CreateCustomerAddressDto>? Addresses { get; init; }
}

public sealed record UpdateCustomerRequest
{
    public required string Name { get; init; }
    public CustomerType CustomerType { get; init; }
    public GstRegistrationType GstType { get; init; }
    public string? Gstin { get; init; }
    public string? Pan { get; init; }
    public string? ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public Guid? CustomerGroupId { get; init; }
    public Guid? AssignedBranchId { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
    public List<CreateCustomerAddressDto>? Addresses { get; init; }
}

public sealed record UpdateCreditLimitRequest
{
    public required long NewCreditLimitPaise { get; init; }
    public required int CreditPeriodDays { get; init; }
    public bool IsCreditHold { get; init; }
    public string? Reason { get; init; }
}

// ==========================================
// 5. Suppliers & Vendor Management
// ==========================================

public sealed record SupplierAddressDto
{
    public required Guid Id { get; init; }
    public required int AddressType { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string StateCode { get; init; }
    public required string PinCode { get; init; }
    public required string Country { get; init; }
    public required bool IsPrimary { get; init; }
}

public sealed record CreateSupplierAddressDto
{
    public int AddressType { get; init; } = 1;
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string StateCode { get; init; }
    public required string PinCode { get; init; }
    public string Country { get; init; } = "India";
    public bool IsPrimary { get; init; } = true;
}

public sealed record SupplierBankAccountDto
{
    public required Guid Id { get; init; }
    public required string BankName { get; init; }
    public required string BranchName { get; init; }
    public required string AccountNumberMasked { get; init; }
    public required string IfscCode { get; init; }
    public required string AccountType { get; init; }
    public required bool IsPrimary { get; init; }
    public required bool IsVerified { get; init; }
}

public sealed record CreateSupplierBankAccountDto
{
    public required string BankName { get; init; }
    public required string BranchName { get; init; }
    public required string AccountNumber { get; init; }
    public required string IfscCode { get; init; }
    public string AccountType { get; init; } = "Current";
    public bool IsPrimary { get; init; } = true;
}

public sealed record UnmaskedBankAccountDto
{
    public required Guid BankAccountId { get; init; }
    public required string BankName { get; init; }
    public required string BranchName { get; init; }
    public required string PlaintextAccountNumber { get; init; }
    public required string IfscCode { get; init; }
    public required string AccountType { get; init; }
}

public sealed record SupplierSummaryDto
{
    public required Guid Id { get; init; }
    public required string SupplierCode { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required GstRegistrationType GstType { get; init; }
    public string? Gstin { get; init; }
    public string? ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public required string PaymentTerms { get; init; }
    public required int CreditPeriodDays { get; init; }
    public required bool IsMsmeRegistered { get; init; }
    public string? MsmeType { get; init; }
    public required bool IsActive { get; init; }
}

public sealed record SupplierDetailDto
{
    public required Guid Id { get; init; }
    public required string SupplierCode { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required GstRegistrationType GstType { get; init; }
    public string? Gstin { get; init; }
    public string? Pan { get; init; }
    public string? ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public required string PaymentTerms { get; init; }
    public required int CreditPeriodDays { get; init; }
    public string? MsmeType { get; init; }
    public string? UdyamRegistrationNumber { get; init; }
    public required bool IsMsmeRegistered { get; init; }
    public required bool IsActive { get; init; }
    public string? Notes { get; init; }
    public Guid? GlAccountId { get; init; }
    public string? GlAccountCode { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
    public IReadOnlyList<SupplierAddressDto> Addresses { get; init; } = [];
    public IReadOnlyList<SupplierBankAccountDto> BankAccounts { get; init; } = [];
}

public sealed record CreateSupplierRequest
{
    public required string SupplierCode { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public GstRegistrationType GstType { get; init; } = GstRegistrationType.Registered;
    public string? Gstin { get; init; }
    public string? Pan { get; init; }
    public string? ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public string PaymentTerms { get; init; } = "Net 30";
    public int CreditPeriodDays { get; init; } = 30;
    public string? MsmeType { get; init; }
    public string? UdyamRegistrationNumber { get; init; }
    public bool IsMsmeRegistered { get; init; }
    public string? Notes { get; init; }
    public List<CreateSupplierAddressDto>? Addresses { get; init; }
    public List<CreateSupplierBankAccountDto>? BankAccounts { get; init; }
}

public sealed record UpdateSupplierRequest
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public GstRegistrationType GstType { get; init; }
    public string? Gstin { get; init; }
    public string? Pan { get; init; }
    public string? ContactPerson { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public string PaymentTerms { get; init; } = "Net 30";
    public int CreditPeriodDays { get; init; } = 30;
    public string? MsmeType { get; init; }
    public string? UdyamRegistrationNumber { get; init; }
    public bool IsMsmeRegistered { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
    public List<CreateSupplierAddressDto>? Addresses { get; init; }
    public List<CreateSupplierBankAccountDto>? BankAccounts { get; init; }
}

