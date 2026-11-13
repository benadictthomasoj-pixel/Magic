using Magic.Contracts.Commercial;

namespace Magic.Application.Commercial.Services;

public interface IProductCatalogService
{
    // Departments
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateDepartmentAsync(Guid companyId, Guid userId, CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateDepartmentAsync(Guid companyId, Guid userId, Guid departmentId, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);

    // Categories
    Task<IReadOnlyList<CategoryDto>> GetCategoriesTreeAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CategoryDto> GetCategoryByIdAsync(Guid companyId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(Guid companyId, Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateCategoryAsync(Guid companyId, Guid userId, Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid companyId, Guid categoryId, CancellationToken cancellationToken = default);

    // Brands
    Task<IReadOnlyList<BrandDto>> GetBrandsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<BrandDto> CreateBrandAsync(Guid companyId, Guid userId, CreateBrandRequest request, CancellationToken cancellationToken = default);
    Task<BrandDto> UpdateBrandAsync(Guid companyId, Guid userId, Guid brandId, UpdateBrandRequest request, CancellationToken cancellationToken = default);

    // Units & Tax Codes
    Task<IReadOnlyList<UnitOfMeasureDto>> GetUnitsOfMeasureAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxCodeDto>> GetTaxCodesAsync(Guid companyId, CancellationToken cancellationToken = default);

    // Products
    Task<(IReadOnlyList<ProductSummaryDto> Items, int TotalCount)> GetProductsAsync(
        Guid companyId,
        string? search,
        Guid? categoryId,
        Guid? brandId,
        ProductType? productType,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto> GetProductByIdAsync(Guid companyId, Guid productId, CancellationToken cancellationToken = default);
    Task<BarcodeLookupResultDto?> LookupBarcodeAsync(Guid companyId, string barcode, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateProductAsync(Guid companyId, Guid userId, CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateProductAsync(Guid companyId, Guid userId, Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeactivateProductAsync(Guid companyId, Guid userId, Guid productId, CancellationToken cancellationToken = default);
}

public interface ICustomerPartyService
{
    Task<IReadOnlyList<CustomerGroupDto>> GetCustomerGroupsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CustomerGroupDto> CreateCustomerGroupAsync(Guid companyId, CreateCustomerGroupRequest request, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerSummaryDto> Items, int TotalCount)> GetCustomersAsync(
        Guid companyId,
        string? search,
        CustomerType? customerType,
        GstRegistrationType? gstType,
        Guid? customerGroupId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CustomerDetailDto> GetCustomerByIdAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default);
    Task<CustomerDetailDto> CreateCustomerAsync(Guid companyId, Guid userId, CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDetailDto> UpdateCustomerAsync(Guid companyId, Guid userId, Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task UpdateCreditLimitAsync(Guid companyId, Guid userId, Guid customerId, UpdateCreditLimitRequest request, CancellationToken cancellationToken = default);
}

public interface ISupplierPartyService
{
    Task<(IReadOnlyList<SupplierSummaryDto> Items, int TotalCount)> GetSuppliersAsync(
        Guid companyId,
        string? search,
        string? category,
        bool? isMsme,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SupplierDetailDto> GetSupplierByIdAsync(Guid companyId, Guid supplierId, CancellationToken cancellationToken = default);
    Task<SupplierDetailDto> CreateSupplierAsync(Guid companyId, Guid userId, CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDetailDto> UpdateSupplierAsync(Guid companyId, Guid userId, Guid supplierId, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<UnmaskedBankAccountDto> GetUnmaskedBankAccountAsync(Guid companyId, Guid userId, Guid supplierId, Guid bankAccountId, CancellationToken cancellationToken = default);
}

