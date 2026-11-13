using Magic.Contracts.Commercial;

namespace Magic.Infrastructure.Persistence.Entities;

public sealed class DepartmentRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<CategoryRecord> Categories { get; set; } = [];
    public ICollection<ProductRecord> Products { get; set; } = [];
}

public sealed class CategoryRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public CategoryRecord? ParentCategory { get; set; }
    public ICollection<CategoryRecord> Children { get; set; } = [];
    public Guid? DepartmentId { get; set; }
    public DepartmentRecord? Department { get; set; }
    public string? HsnCode { get; set; }
    public decimal? DefaultGstRate { get; set; }
    public int DisplayOrder { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<ProductRecord> Products { get; set; } = [];
}

public sealed class BrandRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<ProductRecord> Products { get; set; } = [];
}

public sealed class UnitOfMeasureRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int Precision { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }

    public ICollection<ProductRecord> Products { get; set; } = [];
}

public sealed class TaxCodeRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Description { get; set; }
    public string? HsnSacCode { get; set; }
    public decimal RatePercentage { get; set; }
    public decimal CgstPercentage { get; set; }
    public decimal SgstPercentage { get; set; }
    public decimal IgstPercentage { get; set; }
    public decimal CessPercentage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<ProductRecord> Products { get; set; } = [];
}

public sealed class ProductRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string ProductCode { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ProductType ProductType { get; set; }
    public required Guid CategoryId { get; set; }
    public CategoryRecord? Category { get; set; }
    public Guid? BrandId { get; set; }
    public BrandRecord? Brand { get; set; }
    public Guid? DepartmentId { get; set; }
    public DepartmentRecord? Department { get; set; }
    public required Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasureRecord? UnitOfMeasure { get; set; }
    public required string HsnSacCode { get; set; }
    public Guid? TaxCodeId { get; set; }
    public TaxCodeRecord? TaxCode { get; set; }
    public decimal GstRatePercentage { get; set; }
    public required string Sku { get; set; }
    public string? PrimaryBarcode { get; set; }
    public long PurchasePricePaise { get; set; }
    public long CostPricePaise { get; set; }
    public long SellingPricePaise { get; set; }
    public long MrpPaise { get; set; }
    public long MinSellingPricePaise { get; set; }
    public long WholesalePricePaise { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal MaxStockLevel { get; set; }
    public string? ImageUrl { get; set; }
    public bool HasVariants { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<ProductVariantRecord> Variants { get; set; } = [];
    public ICollection<ProductBarcodeRecord> Barcodes { get; set; } = [];
    public ICollection<ProductPriceRecord> PriceTiers { get; set; } = [];
    public ICollection<ProductPriceHistoryRecord> PriceHistories { get; set; } = [];
}

public sealed class ProductVariantRecord
{
    public required Guid Id { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public required string Sku { get; set; }
    public string? Barcode { get; set; }
    public required string VariantName { get; set; }
    public required string AttributesJson { get; set; }
    public long SellingPricePaise { get; set; }
    public long MrpPaise { get; set; }
    public long PurchasePricePaise { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

public sealed class ProductBarcodeRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public required string Barcode { get; set; }
    public string BarcodeType { get; set; } = "EAN13";
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ProductPriceRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public Guid? VariantId { get; set; }
    public ProductVariantRecord? Variant { get; set; }
    public Guid? BranchId { get; set; }
    public required string PriceTier { get; set; }
    public decimal MinQuantity { get; set; } = 1m;
    public long UnitPricePaise { get; set; }
    public decimal DiscountPercentage { get; set; }
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public sealed class ProductPriceHistoryRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid ProductId { get; set; }
    public ProductRecord? Product { get; set; }
    public required string PriceType { get; set; }
    public long OldPricePaise { get; set; }
    public long NewPricePaise { get; set; }
    public string? Reason { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
}

public sealed class CustomerGroupRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal DefaultDiscountPercentage { get; set; }
    public long DefaultCreditLimitPaise { get; set; }
    public int DefaultCreditDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<CustomerRecord> Customers { get; set; } = [];
}

public sealed class CustomerRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string CustomerCode { get; set; }
    public required string Name { get; set; }
    public CustomerType CustomerType { get; set; }
    public GstRegistrationType GstType { get; set; }
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public string? ContactPerson { get; set; }
    public required string Phone { get; set; }
    public string? Email { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public CustomerGroupRecord? CustomerGroup { get; set; }
    public Guid? AssignedBranchId { get; set; }
    public long CreditLimitPaise { get; set; }
    public int CreditPeriodDays { get; set; }
    public bool IsCreditHold { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public Guid? GlAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<CustomerAddressRecord> Addresses { get; set; } = [];
}

public sealed class CustomerAddressRecord
{
    public required Guid Id { get; set; }
    public required Guid CustomerId { get; set; }
    public CustomerRecord? Customer { get; set; }
    public int AddressType { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public string? District { get; set; }
    public required string State { get; set; }
    public required string StateCode { get; set; }
    public required string PinCode { get; set; }
    public string Country { get; set; } = "India";
    public bool IsDefaultBilling { get; set; }
    public bool IsDefaultShipping { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SupplierRecord
{
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required string SupplierCode { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public GstRegistrationType GstType { get; set; }
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public string? ContactPerson { get; set; }
    public required string Phone { get; set; }
    public string? Email { get; set; }
    public string PaymentTerms { get; set; } = "Net 30";
    public int CreditPeriodDays { get; set; } = 30;
    public string? MsmeType { get; set; }
    public string? UdyamRegistrationNumber { get; set; }
    public bool IsMsmeRegistered { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public Guid? GlAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<SupplierAddressRecord> Addresses { get; set; } = [];
    public ICollection<SupplierBankAccountRecord> BankAccounts { get; set; } = [];
}

public sealed class SupplierAddressRecord
{
    public required Guid Id { get; set; }
    public required Guid SupplierId { get; set; }
    public SupplierRecord? Supplier { get; set; }
    public int AddressType { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string StateCode { get; set; }
    public required string PinCode { get; set; }
    public string Country { get; set; } = "India";
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SupplierBankAccountRecord
{
    public required Guid Id { get; set; }
    public required Guid SupplierId { get; set; }
    public SupplierRecord? Supplier { get; set; }
    public required string BankName { get; set; }
    public required string BranchName { get; set; }
    public required string AccountNumberEncrypted { get; set; }
    public required string AccountNumberMasked { get; set; }
    public required string IfscCode { get; set; }
    public string AccountType { get; set; } = "Current";
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
}

