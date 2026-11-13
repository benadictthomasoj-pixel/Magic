using Magic.Application.Commercial.Services;
using Magic.Contracts.Commercial;
using Magic.Domain.Commercial;
using Magic.Domain.Common;
using Magic.Infrastructure.Persistence;
using Magic.Infrastructure.Persistence.Entities;
using Magic.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Commercial;

file static class CommercialErrors
{
    public static DomainException Duplicate(string entity, string field, string value) =>
        new($"DUPLICATE_{entity.ToUpperInvariant()}", $"{entity} with {field} '{value}' already exists.");

    public static DomainException NotFound(string entity, object id) =>
        new($"{entity.ToUpperInvariant()}_NOT_FOUND", $"{entity} '{id}' not found.", 404);

    public static DomainException Invalid(string code, string message) =>
        new(code, message, 422);
}

public sealed class ProductCatalogService(
    MagicDbContext dbContext,
    ILogger<ProductCatalogService> logger) : IProductCatalogService
{
    // ==========================================
    // Departments
    // ==========================================

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Departments
            .AsNoTracking()
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                ProductCount = d.Products.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(Guid companyId, Guid userId, CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await dbContext.Departments.AnyAsync(d => d.CompanyId == companyId && d.Code == code, cancellationToken);
        if (exists)
        {
            throw CommercialErrors.Duplicate("Department", "code", code);
        }

        var department = new DepartmentRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            ProductCount = 0
        };
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(Guid companyId, Guid userId, Guid departmentId, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.CompanyId == companyId && d.Id == departmentId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Department", departmentId);

        dept.Name = request.Name.Trim();
        dept.Description = request.Description?.Trim();
        dept.IsActive = request.IsActive;
        dept.UpdatedAtUtc = DateTimeOffset.UtcNow;
        dept.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        var count = await dbContext.Products.CountAsync(p => p.DepartmentId == dept.Id, cancellationToken);
        return new DepartmentDto
        {
            Id = dept.Id,
            Code = dept.Code,
            Name = dept.Name,
            Description = dept.Description,
            IsActive = dept.IsActive,
            ProductCount = count
        };
    }

    // ==========================================
    // Categories
    // ==========================================

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesTreeAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var allCats = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.Description,
                c.ParentCategoryId,
                ParentName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                c.DepartmentId,
                DepartmentName = c.Department != null ? c.Department.Name : null,
                c.HsnCode,
                c.DefaultGstRate,
                c.DisplayOrder,
                c.ImageUrl,
                c.IsActive,
                ProductCount = c.Products.Count
            })
            .ToListAsync(cancellationToken);

        var dtoList = allCats.Select(c => new CategoryDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Description = c.Description,
            ParentCategoryId = c.ParentCategoryId,
            ParentCategoryName = c.ParentName,
            DepartmentId = c.DepartmentId,
            DepartmentName = c.DepartmentName,
            HsnCode = c.HsnCode,
            DefaultGstRate = c.DefaultGstRate,
            DisplayOrder = c.DisplayOrder,
            ImageUrl = c.ImageUrl,
            IsActive = c.IsActive,
            ProductCount = c.ProductCount,
            Children = []
        }).ToList();

        var lookup = dtoList.ToLookup(c => c.ParentCategoryId);
        foreach (var cat in dtoList)
        {
            var children = lookup[cat.Id].ToList();
            if (children.Count > 0)
            {
                var field = typeof(CategoryDto).GetProperty(nameof(CategoryDto.Children));
                field?.SetValue(cat, children);
            }
        }

        return dtoList.Where(c => c.ParentCategoryId == null).ToList();
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(Guid companyId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var c = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.Id == categoryId)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                DepartmentId = c.DepartmentId,
                DepartmentName = c.Department != null ? c.Department.Name : null,
                HsnCode = c.HsnCode,
                DefaultGstRate = c.DefaultGstRate,
                DisplayOrder = c.DisplayOrder,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw CommercialErrors.NotFound("Category", categoryId);

        return c;
    }

    public async Task<CategoryDto> CreateCategoryAsync(Guid companyId, Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await dbContext.Categories.AnyAsync(c => c.CompanyId == companyId && c.Code == code, cancellationToken);
        if (exists)
        {
            throw CommercialErrors.Duplicate("Category", "code", code);
        }

        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await dbContext.Categories.AnyAsync(c => c.CompanyId == companyId && c.Id == request.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
            {
                throw CommercialErrors.NotFound("Parent category", request.ParentCategoryId.Value);
            }
        }

        var cat = new CategoryRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            DepartmentId = request.DepartmentId,
            HsnCode = request.HsnCode?.Trim(),
            DefaultGstRate = request.DefaultGstRate,
            DisplayOrder = request.DisplayOrder,
            ImageUrl = request.ImageUrl?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.Categories.Add(cat);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetCategoryByIdAsync(companyId, cat.Id, cancellationToken);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid companyId, Guid userId, Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var cat = await dbContext.Categories.FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == categoryId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Category", categoryId);

        // Guard against circular parent references
        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == categoryId)
            {
                throw CommercialErrors.Invalid("CIRCULAR_CATEGORY", "A category cannot be its own parent.");
            }

            var current = request.ParentCategoryId.Value;
            while (current != Guid.Empty)
            {
                var next = await dbContext.Categories
                    .Where(c => c.CompanyId == companyId && c.Id == current)
                    .Select(c => c.ParentCategoryId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (next == categoryId)
                {
                    throw CommercialErrors.Invalid("CIRCULAR_CATEGORY", "Circular parent category reference detected.");
                }

                if (!next.HasValue) break;
                current = next.Value;
            }
        }

        cat.Name = request.Name.Trim();
        cat.Description = request.Description?.Trim();
        cat.ParentCategoryId = request.ParentCategoryId;
        cat.DepartmentId = request.DepartmentId;
        cat.HsnCode = request.HsnCode?.Trim();
        cat.DefaultGstRate = request.DefaultGstRate;
        cat.DisplayOrder = request.DisplayOrder;
        cat.ImageUrl = request.ImageUrl?.Trim();
        cat.IsActive = request.IsActive;
        cat.UpdatedAtUtc = DateTimeOffset.UtcNow;
        cat.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCategoryByIdAsync(companyId, categoryId, cancellationToken);
    }

    public async Task DeleteCategoryAsync(Guid companyId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cat = await dbContext.Categories
            .Include(c => c.Children)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == categoryId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Category", categoryId);

        if (cat.Children.Count > 0)
        {
            throw CommercialErrors.Invalid("CATEGORY_HAS_CHILDREN", $"Cannot delete category '{cat.Name}' because it has {cat.Children.Count} child subcategories. Reassign or delete child categories first.");
        }

        if (cat.Products.Count > 0)
        {
            throw CommercialErrors.Invalid("CATEGORY_HAS_PRODUCTS", $"Cannot delete category '{cat.Name}' because {cat.Products.Count} products are linked to it. Reassign products to another category before deleting.");
        }

        dbContext.Categories.Remove(cat);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // Brands
    // ==========================================

    public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Brands
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                Description = b.Description,
                LogoUrl = b.LogoUrl,
                WebsiteUrl = b.WebsiteUrl,
                IsActive = b.IsActive,
                ProductCount = b.Products.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BrandDto> CreateBrandAsync(Guid companyId, Guid userId, CreateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await dbContext.Brands.AnyAsync(b => b.CompanyId == companyId && b.Code == code, cancellationToken);
        if (exists)
        {
            throw CommercialErrors.Duplicate("Brand", "code", code);
        }

        var brand = new BrandRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            LogoUrl = request.LogoUrl?.Trim(),
            WebsiteUrl = request.WebsiteUrl?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BrandDto
        {
            Id = brand.Id,
            Code = brand.Code,
            Name = brand.Name,
            Description = brand.Description,
            LogoUrl = brand.LogoUrl,
            WebsiteUrl = brand.WebsiteUrl,
            IsActive = brand.IsActive,
            ProductCount = 0
        };
    }

    public async Task<BrandDto> UpdateBrandAsync(Guid companyId, Guid userId, Guid brandId, UpdateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var brand = await dbContext.Brands.FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Id == brandId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Brand", brandId);

        brand.Name = request.Name.Trim();
        brand.Description = request.Description?.Trim();
        brand.LogoUrl = request.LogoUrl?.Trim();
        brand.WebsiteUrl = request.WebsiteUrl?.Trim();
        brand.IsActive = request.IsActive;
        brand.UpdatedAtUtc = DateTimeOffset.UtcNow;
        brand.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        var count = await dbContext.Products.CountAsync(p => p.BrandId == brand.Id, cancellationToken);
        return new BrandDto
        {
            Id = brand.Id,
            Code = brand.Code,
            Name = brand.Name,
            Description = brand.Description,
            LogoUrl = brand.LogoUrl,
            WebsiteUrl = brand.WebsiteUrl,
            IsActive = brand.IsActive,
            ProductCount = count
        };
    }

    // ==========================================
    // Units of Measure & Tax Codes
    // ==========================================

    public async Task<IReadOnlyList<UnitOfMeasureDto>> GetUnitsOfMeasureAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UnitsOfMeasure
            .AsNoTracking()
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.Code)
            .Select(u => new UnitOfMeasureDto
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Name,
                Precision = u.Precision,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxCodeDto>> GetTaxCodesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TaxCodes
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .OrderBy(t => t.RatePercentage)
            .Select(t => new TaxCodeDto
            {
                Id = t.Id,
                Code = t.Code,
                Description = t.Description,
                HsnSacCode = t.HsnSacCode,
                RatePercentage = t.RatePercentage,
                CgstPercentage = t.CgstPercentage,
                SgstPercentage = t.SgstPercentage,
                IgstPercentage = t.IgstPercentage,
                CessPercentage = t.CessPercentage,
                IsActive = t.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    // ==========================================
    // Products
    // ==========================================

    public async Task<(IReadOnlyList<ProductSummaryDto> Items, int TotalCount)> GetProductsAsync(
        Guid companyId,
        string? search,
        Guid? categoryId,
        Guid? brandId,
        ProductType? productType,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{term}%") ||
                EF.Functions.ILike(p.ProductCode, $"%{term}%") ||
                EF.Functions.ILike(p.Sku, $"%{term}%") ||
                (p.PrimaryBarcode != null && EF.Functions.ILike(p.PrimaryBarcode, $"%{term}%")));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (productType.HasValue)
        {
            query = query.Where(p => p.ProductType == productType.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                ProductCode = p.ProductCode,
                Name = p.Name,
                ProductType = p.ProductType,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                UnitOfMeasureCode = p.UnitOfMeasure != null ? p.UnitOfMeasure.Code : "PCS",
                HsnSacCode = p.HsnSacCode,
                GstRatePercentage = p.GstRatePercentage,
                Sku = p.Sku,
                PrimaryBarcode = p.PrimaryBarcode,
                SellingPricePaise = p.SellingPricePaise,
                MrpPaise = p.MrpPaise,
                PurchasePricePaise = p.PurchasePricePaise,
                CostPricePaise = p.CostPricePaise,
                HasVariants = p.HasVariants,
                IsActive = p.IsActive,
                ImageUrl = p.ImageUrl
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<ProductDetailDto> GetProductByIdAsync(Guid companyId, Guid productId, CancellationToken cancellationToken = default)
    {
        var p = await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Department)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Variants)
            .Include(p => p.Barcodes)
            .Include(p => p.PriceTiers)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == productId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Product", productId);

        return new ProductDetailDto
        {
            Id = p.Id,
            ProductCode = p.ProductCode,
            Name = p.Name,
            Description = p.Description,
            ProductType = p.ProductType,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            BrandId = p.BrandId,
            BrandName = p.Brand?.Name,
            DepartmentId = p.DepartmentId,
            DepartmentName = p.Department?.Name,
            UnitOfMeasureId = p.UnitOfMeasureId,
            UnitOfMeasureCode = p.UnitOfMeasure?.Code ?? "PCS",
            HsnSacCode = p.HsnSacCode,
            TaxCodeId = p.TaxCodeId,
            GstRatePercentage = p.GstRatePercentage,
            Sku = p.Sku,
            PrimaryBarcode = p.PrimaryBarcode,
            PurchasePricePaise = p.PurchasePricePaise,
            CostPricePaise = p.CostPricePaise,
            SellingPricePaise = p.SellingPricePaise,
            MrpPaise = p.MrpPaise,
            MinSellingPricePaise = p.MinSellingPricePaise,
            WholesalePricePaise = p.WholesalePricePaise,
            ReorderLevel = p.ReorderLevel,
            MinStockLevel = p.MinStockLevel,
            MaxStockLevel = p.MaxStockLevel,
            ImageUrl = p.ImageUrl,
            HasVariants = p.HasVariants,
            IsActive = p.IsActive,
            CreatedAtUtc = p.CreatedAtUtc,
            UpdatedAtUtc = p.UpdatedAtUtc,
            Variants = p.Variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                Sku = v.Sku,
                Barcode = v.Barcode,
                VariantName = v.VariantName,
                AttributesJson = v.AttributesJson,
                SellingPricePaise = v.SellingPricePaise,
                MrpPaise = v.MrpPaise,
                PurchasePricePaise = v.PurchasePricePaise,
                IsActive = v.IsActive
            }).ToList(),
            Barcodes = p.Barcodes.Select(b => new ProductBarcodeDto
            {
                Id = b.Id,
                VariantId = b.VariantId,
                Barcode = b.Barcode,
                BarcodeType = b.BarcodeType,
                IsPrimary = b.IsPrimary
            }).ToList(),
            PriceTiers = p.PriceTiers.Select(t => new ProductPriceTierDto
            {
                Id = t.Id,
                VariantId = t.VariantId,
                BranchId = t.BranchId,
                PriceTier = t.PriceTier,
                MinQuantity = t.MinQuantity,
                UnitPricePaise = t.UnitPricePaise,
                DiscountPercentage = t.DiscountPercentage,
                EffectiveFromUtc = t.EffectiveFromUtc,
                EffectiveToUtc = t.EffectiveToUtc,
                IsActive = t.IsActive
            }).ToList()
        };
    }

    public async Task<BarcodeLookupResultDto?> LookupBarcodeAsync(Guid companyId, string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        var cleanBarcode = barcode.Trim();

        var barcodeRecord = await dbContext.ProductBarcodes
            .AsNoTracking()
            .Include(b => b.Product)
                .ThenInclude(p => p!.UnitOfMeasure)
            .Include(b => b.Variant)
            .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Barcode == cleanBarcode, cancellationToken);

        if (barcodeRecord?.Product != null)
        {
            var p = barcodeRecord.Product;
            var v = barcodeRecord.Variant;
            return new BarcodeLookupResultDto
            {
                ProductId = p.Id,
                VariantId = v?.Id,
                ProductCode = p.ProductCode,
                Name = p.Name,
                VariantName = v?.VariantName,
                Sku = v?.Sku ?? p.Sku,
                Barcode = cleanBarcode,
                UnitOfMeasureCode = p.UnitOfMeasure?.Code ?? "PCS",
                HsnSacCode = p.HsnSacCode,
                GstRatePercentage = p.GstRatePercentage,
                SellingPricePaise = v?.SellingPricePaise ?? p.SellingPricePaise,
                MrpPaise = v?.MrpPaise ?? p.MrpPaise
            };
        }

        var product = await dbContext.Products
            .AsNoTracking()
            .Include(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.PrimaryBarcode == cleanBarcode, cancellationToken);

        if (product != null)
        {
            return new BarcodeLookupResultDto
            {
                ProductId = product.Id,
                VariantId = null,
                ProductCode = product.ProductCode,
                Name = product.Name,
                VariantName = null,
                Sku = product.Sku,
                Barcode = cleanBarcode,
                UnitOfMeasureCode = product.UnitOfMeasure?.Code ?? "PCS",
                HsnSacCode = product.HsnSacCode,
                GstRatePercentage = product.GstRatePercentage,
                SellingPricePaise = product.SellingPricePaise,
                MrpPaise = product.MrpPaise
            };
        }

        return null;
    }

    public async Task<ProductDetailDto> CreateProductAsync(Guid companyId, Guid userId, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var productCode = request.ProductCode.Trim().ToUpperInvariant();
        var exists = await dbContext.Products.AnyAsync(p => p.CompanyId == companyId && p.ProductCode == productCode, cancellationToken);
        if (exists)
        {
            throw CommercialErrors.Duplicate("Product", "code", productCode);
        }

        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == request.CategoryId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Category", request.CategoryId);

        var sku = string.IsNullOrWhiteSpace(request.Sku)
            ? SkuAndBarcodeGenerator.GenerateSku(category.Code, productCode)
            : request.Sku.Trim().ToUpperInvariant();

        var skuExists = await dbContext.Products.AnyAsync(p => p.CompanyId == companyId && p.Sku == sku, cancellationToken);
        if (skuExists)
        {
            throw CommercialErrors.Duplicate("Product", "SKU", sku);
        }

        if (!string.IsNullOrWhiteSpace(request.PrimaryBarcode))
        {
            var bc = request.PrimaryBarcode.Trim();
            var bcExists = await dbContext.Products.AnyAsync(p => p.CompanyId == companyId && p.PrimaryBarcode == bc, cancellationToken)
                || await dbContext.ProductBarcodes.AnyAsync(b => b.CompanyId == companyId && b.Barcode == bc, cancellationToken);
            if (bcExists)
            {
                throw CommercialErrors.Duplicate("Product", "barcode", bc);
            }
        }

        var primaryBarcode = request.PrimaryBarcode?.Trim();
        if (string.IsNullOrWhiteSpace(primaryBarcode))
        {
            var seedNumber = 890000000000L + Math.Abs((long)productCode.GetHashCode()) % 9000000000L;
            primaryBarcode = SkuAndBarcodeGenerator.GenerateEan13(seedNumber);
        }

        var product = new ProductRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            ProductCode = productCode,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ProductType = request.ProductType,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            DepartmentId = request.DepartmentId,
            UnitOfMeasureId = request.UnitOfMeasureId,
            HsnSacCode = request.HsnSacCode.Trim(),
            TaxCodeId = request.TaxCodeId,
            GstRatePercentage = request.GstRatePercentage,
            Sku = sku,
            PrimaryBarcode = primaryBarcode,
            PurchasePricePaise = request.PurchasePricePaise,
            CostPricePaise = request.CostPricePaise,
            SellingPricePaise = request.SellingPricePaise,
            MrpPaise = request.MrpPaise,
            MinSellingPricePaise = request.MinSellingPricePaise,
            WholesalePricePaise = request.WholesalePricePaise,
            ReorderLevel = request.ReorderLevel,
            MinStockLevel = request.MinStockLevel,
            MaxStockLevel = request.MaxStockLevel,
            ImageUrl = request.ImageUrl?.Trim(),
            HasVariants = (request.Variants?.Count ?? 0) > 0,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.Products.Add(product);

        if (!string.IsNullOrWhiteSpace(product.PrimaryBarcode))
        {
            dbContext.ProductBarcodes.Add(new ProductBarcodeRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                ProductId = product.Id,
                VariantId = null,
                Barcode = product.PrimaryBarcode,
                BarcodeType = "EAN13",
                IsPrimary = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        if (request.Barcodes != null)
        {
            foreach (var b in request.Barcodes)
            {
                var bc = b.Barcode.Trim();
                if (bc != product.PrimaryBarcode)
                {
                    dbContext.ProductBarcodes.Add(new ProductBarcodeRecord
                    {
                        Id = Guid.CreateVersion7(),
                        CompanyId = companyId,
                        ProductId = product.Id,
                        VariantId = b.VariantId,
                        Barcode = bc,
                        BarcodeType = b.BarcodeType,
                        IsPrimary = false,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    });
                }
            }
        }

        if (request.Variants != null)
        {
            foreach (var v in request.Variants)
            {
                var vSku = string.IsNullOrWhiteSpace(v.Sku)
                    ? $"{sku}-{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}"
                    : v.Sku.Trim().ToUpperInvariant();

                dbContext.ProductVariants.Add(new ProductVariantRecord
                {
                    Id = Guid.CreateVersion7(),
                    ProductId = product.Id,
                    Sku = vSku,
                    Barcode = v.Barcode?.Trim(),
                    VariantName = v.VariantName.Trim(),
                    AttributesJson = v.AttributesJson.Trim(),
                    SellingPricePaise = v.SellingPricePaise,
                    MrpPaise = v.MrpPaise,
                    PurchasePricePaise = v.PurchasePricePaise,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        dbContext.ProductPriceHistories.Add(new ProductPriceHistoryRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            ProductId = product.Id,
            PriceType = "SELLING",
            OldPricePaise = 0,
            NewPricePaise = product.SellingPricePaise,
            Reason = "Initial product registration",
            ChangedByUserId = userId,
            ChangedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created product {ProductCode} (SKU: {Sku}) for company {CompanyId}", product.ProductCode, product.Sku, companyId);

        return await GetProductByIdAsync(companyId, product.Id, cancellationToken);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(Guid companyId, Guid userId, Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(p => p.PriceHistories)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == productId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Product", productId);

        if (product.SellingPricePaise != request.SellingPricePaise)
        {
            dbContext.ProductPriceHistories.Add(new ProductPriceHistoryRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                ProductId = product.Id,
                PriceType = "SELLING",
                OldPricePaise = product.SellingPricePaise,
                NewPricePaise = request.SellingPricePaise,
                Reason = "Product price update",
                ChangedByUserId = userId,
                ChangedAtUtc = DateTimeOffset.UtcNow
            });
        }

        if (product.MrpPaise != request.MrpPaise)
        {
            dbContext.ProductPriceHistories.Add(new ProductPriceHistoryRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                ProductId = product.Id,
                PriceType = "MRP",
                OldPricePaise = product.MrpPaise,
                NewPricePaise = request.MrpPaise,
                Reason = "Product MRP update",
                ChangedByUserId = userId,
                ChangedAtUtc = DateTimeOffset.UtcNow
            });
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.ProductType = request.ProductType;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.DepartmentId = request.DepartmentId;
        product.UnitOfMeasureId = request.UnitOfMeasureId;
        product.HsnSacCode = request.HsnSacCode.Trim();
        product.TaxCodeId = request.TaxCodeId;
        product.GstRatePercentage = request.GstRatePercentage;
        product.PrimaryBarcode = request.PrimaryBarcode?.Trim();
        product.PurchasePricePaise = request.PurchasePricePaise;
        product.CostPricePaise = request.CostPricePaise;
        product.SellingPricePaise = request.SellingPricePaise;
        product.MrpPaise = request.MrpPaise;
        product.MinSellingPricePaise = request.MinSellingPricePaise;
        product.WholesalePricePaise = request.WholesalePricePaise;
        product.ReorderLevel = request.ReorderLevel;
        product.MinStockLevel = request.MinStockLevel;
        product.MaxStockLevel = request.MaxStockLevel;
        product.ImageUrl = request.ImageUrl?.Trim();
        product.IsActive = request.IsActive;
        product.UpdatedAtUtc = DateTimeOffset.UtcNow;
        product.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetProductByIdAsync(companyId, productId, cancellationToken);
    }

    public async Task DeactivateProductAsync(Guid companyId, Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == productId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Product", productId);

        product.IsActive = false;
        product.UpdatedAtUtc = DateTimeOffset.UtcNow;
        product.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class CustomerPartyService(
    MagicDbContext dbContext,
    ISecurityAuditService auditService,
    ILogger<CustomerPartyService> logger) : ICustomerPartyService
{
    public async Task<IReadOnlyList<CustomerGroupDto>> GetCustomerGroupsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CustomerGroups
            .AsNoTracking()
            .Where(g => g.CompanyId == companyId)
            .OrderBy(g => g.Name)
            .Select(g => new CustomerGroupDto
            {
                Id = g.Id,
                Code = g.Code,
                Name = g.Name,
                DefaultDiscountPercentage = g.DefaultDiscountPercentage,
                DefaultCreditLimitPaise = g.DefaultCreditLimitPaise,
                DefaultCreditDays = g.DefaultCreditDays,
                IsActive = g.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerGroupDto> CreateCustomerGroupAsync(Guid companyId, CreateCustomerGroupRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await dbContext.CustomerGroups.AnyAsync(g => g.CompanyId == companyId && g.Code == code, cancellationToken);
        if (exists)
        {
            throw CommercialErrors.Duplicate("Customer group", "code", code);
        }

        var group = new CustomerGroupRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            Code = code,
            Name = request.Name.Trim(),
            DefaultDiscountPercentage = request.DefaultDiscountPercentage,
            DefaultCreditLimitPaise = request.DefaultCreditLimitPaise,
            DefaultCreditDays = request.DefaultCreditDays,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.CustomerGroups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerGroupDto
        {
            Id = group.Id,
            Code = group.Code,
            Name = group.Name,
            DefaultDiscountPercentage = group.DefaultDiscountPercentage,
            DefaultCreditLimitPaise = group.DefaultCreditLimitPaise,
            DefaultCreditDays = group.DefaultCreditDays,
            IsActive = group.IsActive
        };
    }

    public async Task<(IReadOnlyList<CustomerSummaryDto> Items, int TotalCount)> GetCustomersAsync(
        Guid companyId,
        string? search,
        CustomerType? customerType,
        GstRegistrationType? gstType,
        Guid? customerGroupId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Customers
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{term}%") ||
                EF.Functions.ILike(c.CustomerCode, $"%{term}%") ||
                EF.Functions.ILike(c.Phone, $"%{term}%") ||
                (c.Gstin != null && EF.Functions.ILike(c.Gstin, $"%{term}%")));
        }

        if (customerType.HasValue)
        {
            query = query.Where(c => c.CustomerType == customerType.Value);
        }

        if (gstType.HasValue)
        {
            query = query.Where(c => c.GstType == gstType.Value);
        }

        if (customerGroupId.HasValue)
        {
            query = query.Where(c => c.CustomerGroupId == customerGroupId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerSummaryDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                Name = c.Name,
                CustomerType = c.CustomerType,
                GstType = c.GstType,
                Gstin = c.Gstin,
                ContactPerson = c.ContactPerson,
                Phone = c.Phone,
                Email = c.Email,
                City = c.Addresses.Select(a => a.City).FirstOrDefault(),
                State = c.Addresses.Select(a => a.State).FirstOrDefault(),
                CreditLimitPaise = c.CreditLimitPaise,
                CreditPeriodDays = c.CreditPeriodDays,
                IsCreditHold = c.IsCreditHold,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<CustomerDetailDto> GetCustomerByIdAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var c = await dbContext.Customers
            .AsNoTracking()
            .Include(c => c.CustomerGroup)
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == customerId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Customer", customerId);

        return new CustomerDetailDto
        {
            Id = c.Id,
            CustomerCode = c.CustomerCode,
            Name = c.Name,
            CustomerType = c.CustomerType,
            GstType = c.GstType,
            Gstin = c.Gstin,
            Pan = c.Pan,
            ContactPerson = c.ContactPerson,
            Phone = c.Phone,
            Email = c.Email,
            CustomerGroupId = c.CustomerGroupId,
            CustomerGroupName = c.CustomerGroup?.Name,
            AssignedBranchId = c.AssignedBranchId,
            CreditLimitPaise = c.CreditLimitPaise,
            CreditPeriodDays = c.CreditPeriodDays,
            IsCreditHold = c.IsCreditHold,
            IsActive = c.IsActive,
            Notes = c.Notes,
            GlAccountId = c.GlAccountId,
            CreatedAtUtc = c.CreatedAtUtc,
            UpdatedAtUtc = c.UpdatedAtUtc,
            Addresses = c.Addresses.Select(a => new CustomerAddressDto
            {
                Id = a.Id,
                AddressType = a.AddressType,
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                City = a.City,
                District = a.District,
                State = a.State,
                StateCode = a.StateCode,
                PinCode = a.PinCode,
                Country = a.Country,
                IsDefaultBilling = a.IsDefaultBilling,
                IsDefaultShipping = a.IsDefaultShipping
            }).ToList()
        };
    }

    public async Task<CustomerDetailDto> CreateCustomerAsync(Guid companyId, Guid userId, CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.CustomerCode.Trim().ToUpperInvariant();
        var exists = await dbContext.Customers.AnyAsync(c => c.CompanyId == companyId && c.CustomerCode == code, cancellationToken);
        if (exists)
        {
            throw CommercialErrors.Duplicate("Customer", "code", code);
        }

        if (!string.IsNullOrWhiteSpace(request.Gstin))
        {
            if (!GstinValidator.IsValid(request.Gstin, out var gstinError))
            {
                throw CommercialErrors.Invalid("INVALID_GSTIN", gstinError!);
            }

            var gstinTrim = request.Gstin.Trim().ToUpperInvariant();
            var duplicateGstin = await dbContext.Customers.AnyAsync(c => c.CompanyId == companyId && c.Gstin == gstinTrim, cancellationToken);
            if (duplicateGstin)
            {
                throw CommercialErrors.Duplicate("Customer", "GSTIN", gstinTrim);
            }
        }

        var customer = new CustomerRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            CustomerCode = code,
            Name = request.Name.Trim(),
            CustomerType = request.CustomerType,
            GstType = request.GstType,
            Gstin = request.Gstin?.Trim().ToUpperInvariant(),
            Pan = request.Pan?.Trim().ToUpperInvariant(),
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim(),
            CustomerGroupId = request.CustomerGroupId,
            AssignedBranchId = request.AssignedBranchId,
            CreditLimitPaise = request.CreditLimitPaise,
            CreditPeriodDays = request.CreditPeriodDays,
            IsCreditHold = false,
            IsActive = true,
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.Customers.Add(customer);

        if (request.Addresses != null)
        {
            foreach (var a in request.Addresses)
            {
                dbContext.CustomerAddresses.Add(new CustomerAddressRecord
                {
                    Id = Guid.CreateVersion7(),
                    CustomerId = customer.Id,
                    AddressType = a.AddressType,
                    AddressLine1 = a.AddressLine1.Trim(),
                    AddressLine2 = a.AddressLine2?.Trim(),
                    City = a.City.Trim(),
                    District = a.District?.Trim(),
                    State = a.State.Trim(),
                    StateCode = a.StateCode.Trim(),
                    PinCode = a.PinCode.Trim(),
                    Country = a.Country.Trim(),
                    IsDefaultBilling = a.IsDefaultBilling,
                    IsDefaultShipping = a.IsDefaultShipping,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Registered customer {CustomerCode} for company {CompanyId}", customer.CustomerCode, companyId);

        return await GetCustomerByIdAsync(companyId, customer.Id, cancellationToken);
    }

    public async Task<CustomerDetailDto> UpdateCustomerAsync(Guid companyId, Guid userId, Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == customerId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Customer", customerId);

        if (!string.IsNullOrWhiteSpace(request.Gstin))
        {
            if (!GstinValidator.IsValid(request.Gstin, out var gstinError))
            {
                throw CommercialErrors.Invalid("INVALID_GSTIN", gstinError!);
            }
        }

        customer.Name = request.Name.Trim();
        customer.CustomerType = request.CustomerType;
        customer.GstType = request.GstType;
        customer.Gstin = request.Gstin?.Trim().ToUpperInvariant();
        customer.Pan = request.Pan?.Trim().ToUpperInvariant();
        customer.ContactPerson = request.ContactPerson?.Trim();
        customer.Phone = request.Phone.Trim();
        customer.Email = request.Email?.Trim();
        customer.CustomerGroupId = request.CustomerGroupId;
        customer.AssignedBranchId = request.AssignedBranchId;
        customer.Notes = request.Notes?.Trim();
        customer.IsActive = request.IsActive;
        customer.UpdatedAtUtc = DateTimeOffset.UtcNow;
        customer.UpdatedByUserId = userId;

        if (request.Addresses != null)
        {
            dbContext.CustomerAddresses.RemoveRange(customer.Addresses);
            foreach (var a in request.Addresses)
            {
                dbContext.CustomerAddresses.Add(new CustomerAddressRecord
                {
                    Id = Guid.CreateVersion7(),
                    CustomerId = customer.Id,
                    AddressType = a.AddressType,
                    AddressLine1 = a.AddressLine1.Trim(),
                    AddressLine2 = a.AddressLine2?.Trim(),
                    City = a.City.Trim(),
                    District = a.District?.Trim(),
                    State = a.State.Trim(),
                    StateCode = a.StateCode.Trim(),
                    PinCode = a.PinCode.Trim(),
                    Country = a.Country.Trim(),
                    IsDefaultBilling = a.IsDefaultBilling,
                    IsDefaultShipping = a.IsDefaultShipping,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCustomerByIdAsync(companyId, customerId, cancellationToken);
    }

    public async Task UpdateCreditLimitAsync(Guid companyId, Guid userId, Guid customerId, UpdateCreditLimitRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == customerId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Customer", customerId);

        var oldLimit = customer.CreditLimitPaise;
        customer.CreditLimitPaise = request.NewCreditLimitPaise;
        customer.CreditPeriodDays = request.CreditPeriodDays;
        customer.IsCreditHold = request.IsCreditHold;
        customer.UpdatedAtUtc = DateTimeOffset.UtcNow;
        customer.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogEventAsync(
            companyId,
            "CUSTOMER_CREDIT_LIMIT_MODIFIED",
            userId: userId,
            detailsJson: $"{{\"customerCode\":\"{customer.CustomerCode}\",\"oldLimitPaise\":{oldLimit},\"newLimitPaise\":{request.NewCreditLimitPaise},\"reason\":\"{request.Reason ?? "Administrative adjustment"}\"}}",
            cancellationToken: cancellationToken);
    }
}

public sealed class SupplierPartyService(
    MagicDbContext dbContext,
    IBankDataEncryptionService encryptionService,
    ISecurityAuditService auditService,
    ILogger<SupplierPartyService> logger) : ISupplierPartyService
{
    public async Task<(IReadOnlyList<SupplierSummaryDto> Items, int TotalCount)> GetSuppliersAsync(
        Guid companyId,
        string? search,
        string? category,
        bool? isMsme,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Suppliers
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, $"%{term}%") ||
                EF.Functions.ILike(s.SupplierCode, $"%{term}%") ||
                EF.Functions.ILike(s.Phone, $"%{term}%") ||
                (s.Gstin != null && EF.Functions.ILike(s.Gstin, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Category == category.Trim());
        }

        if (isMsme.HasValue)
        {
            query = query.Where(s => s.IsMsmeRegistered == isMsme.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplierSummaryDto
            {
                Id = s.Id,
                SupplierCode = s.SupplierCode,
                Name = s.Name,
                Category = s.Category,
                GstType = s.GstType,
                Gstin = s.Gstin,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                Email = s.Email,
                PaymentTerms = s.PaymentTerms,
                CreditPeriodDays = s.CreditPeriodDays,
                IsMsmeRegistered = s.IsMsmeRegistered,
                MsmeType = s.MsmeType,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<SupplierDetailDto> GetSupplierByIdAsync(Guid companyId, Guid supplierId, CancellationToken cancellationToken = default)
    {
        var s = await dbContext.Suppliers
            .AsNoTracking()
            .Include(s => s.Addresses)
            .Include(s => s.BankAccounts)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == supplierId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Supplier", supplierId);

        return new SupplierDetailDto
        {
            Id = s.Id,
            SupplierCode = s.SupplierCode,
            Name = s.Name,
            Category = s.Category,
            GstType = s.GstType,
            Gstin = s.Gstin,
            Pan = s.Pan,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            PaymentTerms = s.PaymentTerms,
            CreditPeriodDays = s.CreditPeriodDays,
            MsmeType = s.MsmeType,
            UdyamRegistrationNumber = s.UdyamRegistrationNumber,
            IsMsmeRegistered = s.IsMsmeRegistered,
            IsActive = s.IsActive,
            Notes = s.Notes,
            GlAccountId = s.GlAccountId,
            CreatedAtUtc = s.CreatedAtUtc,
            UpdatedAtUtc = s.UpdatedAtUtc,
            Addresses = s.Addresses.Select(a => new SupplierAddressDto
            {
                Id = a.Id,
                AddressType = a.AddressType,
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                City = a.City,
                State = a.State,
                StateCode = a.StateCode,
                PinCode = a.PinCode,
                Country = a.Country,
                IsPrimary = a.IsPrimary
            }).ToList(),
            BankAccounts = s.BankAccounts.Select(b => new SupplierBankAccountDto
            {
                Id = b.Id,
                BankName = b.BankName,
                BranchName = b.BranchName,
                AccountNumberMasked = b.AccountNumberMasked,
                IfscCode = b.IfscCode,
                AccountType = b.AccountType,
                IsPrimary = b.IsPrimary,
                IsVerified = b.IsVerified
            }).ToList()
        };
    }

    public async Task<SupplierDetailDto> CreateSupplierAsync(Guid companyId, Guid userId, CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.SupplierCode.Trim().ToUpperInvariant();
        var exists = await dbContext.Suppliers.AnyAsync(s => s.CompanyId == companyId && s.SupplierCode == code, cancellationToken);
        if (exists)
        {
            throw CommercialErrors.Duplicate("Supplier", "code", code);
        }

        if (!string.IsNullOrWhiteSpace(request.Gstin))
        {
            if (!GstinValidator.IsValid(request.Gstin, out var gstinError))
            {
                throw CommercialErrors.Invalid("INVALID_GSTIN", gstinError!);
            }

            var gstinTrim = request.Gstin.Trim().ToUpperInvariant();
            var duplicateGstin = await dbContext.Suppliers.AnyAsync(s => s.CompanyId == companyId && s.Gstin == gstinTrim, cancellationToken);
            if (duplicateGstin)
            {
                throw CommercialErrors.Duplicate("Supplier", "GSTIN", gstinTrim);
            }
        }

        var supplier = new SupplierRecord
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            SupplierCode = code,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            GstType = request.GstType,
            Gstin = request.Gstin?.Trim().ToUpperInvariant(),
            Pan = request.Pan?.Trim().ToUpperInvariant(),
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim(),
            PaymentTerms = request.PaymentTerms.Trim(),
            CreditPeriodDays = request.CreditPeriodDays,
            MsmeType = request.MsmeType?.Trim(),
            UdyamRegistrationNumber = request.UdyamRegistrationNumber?.Trim(),
            IsMsmeRegistered = request.IsMsmeRegistered,
            IsActive = true,
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };

        dbContext.Suppliers.Add(supplier);

        if (request.Addresses != null)
        {
            foreach (var a in request.Addresses)
            {
                dbContext.SupplierAddresses.Add(new SupplierAddressRecord
                {
                    Id = Guid.CreateVersion7(),
                    SupplierId = supplier.Id,
                    AddressType = a.AddressType,
                    AddressLine1 = a.AddressLine1.Trim(),
                    AddressLine2 = a.AddressLine2?.Trim(),
                    City = a.City.Trim(),
                    State = a.State.Trim(),
                    StateCode = a.StateCode.Trim(),
                    PinCode = a.PinCode.Trim(),
                    Country = a.Country.Trim(),
                    IsPrimary = a.IsPrimary,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        if (request.BankAccounts != null)
        {
            foreach (var b in request.BankAccounts)
            {
                var cleanAcc = b.AccountNumber.Trim();
                var encrypted = encryptionService.Encrypt(cleanAcc);
                var masked = encryptionService.Mask(cleanAcc);

                dbContext.SupplierBankAccounts.Add(new SupplierBankAccountRecord
                {
                    Id = Guid.CreateVersion7(),
                    SupplierId = supplier.Id,
                    BankName = b.BankName.Trim(),
                    BranchName = b.BranchName.Trim(),
                    AccountNumberEncrypted = encrypted,
                    AccountNumberMasked = masked,
                    IfscCode = b.IfscCode.Trim().ToUpperInvariant(),
                    AccountType = b.AccountType.Trim(),
                    IsPrimary = b.IsPrimary,
                    IsVerified = false,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    CreatedByUserId = userId
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Registered supplier {SupplierCode} for company {CompanyId}", supplier.SupplierCode, companyId);

        return await GetSupplierByIdAsync(companyId, supplier.Id, cancellationToken);
    }

    public async Task<SupplierDetailDto> UpdateSupplierAsync(Guid companyId, Guid userId, Guid supplierId, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await dbContext.Suppliers
            .Include(s => s.Addresses)
            .Include(s => s.BankAccounts)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == supplierId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Supplier", supplierId);

        if (!string.IsNullOrWhiteSpace(request.Gstin))
        {
            if (!GstinValidator.IsValid(request.Gstin, out var gstinError))
            {
                throw CommercialErrors.Invalid("INVALID_GSTIN", gstinError!);
            }
        }

        supplier.Name = request.Name.Trim();
        supplier.Category = request.Category.Trim();
        supplier.GstType = request.GstType;
        supplier.Gstin = request.Gstin?.Trim().ToUpperInvariant();
        supplier.Pan = request.Pan?.Trim().ToUpperInvariant();
        supplier.ContactPerson = request.ContactPerson?.Trim();
        supplier.Phone = request.Phone.Trim();
        supplier.Email = request.Email?.Trim();
        supplier.PaymentTerms = request.PaymentTerms.Trim();
        supplier.CreditPeriodDays = request.CreditPeriodDays;
        supplier.MsmeType = request.MsmeType?.Trim();
        supplier.UdyamRegistrationNumber = request.UdyamRegistrationNumber?.Trim();
        supplier.IsMsmeRegistered = request.IsMsmeRegistered;
        supplier.Notes = request.Notes?.Trim();
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAtUtc = DateTimeOffset.UtcNow;
        supplier.UpdatedByUserId = userId;

        if (request.Addresses != null)
        {
            dbContext.SupplierAddresses.RemoveRange(supplier.Addresses);
            foreach (var a in request.Addresses)
            {
                dbContext.SupplierAddresses.Add(new SupplierAddressRecord
                {
                    Id = Guid.CreateVersion7(),
                    SupplierId = supplier.Id,
                    AddressType = a.AddressType,
                    AddressLine1 = a.AddressLine1.Trim(),
                    AddressLine2 = a.AddressLine2?.Trim(),
                    City = a.City.Trim(),
                    State = a.State.Trim(),
                    StateCode = a.StateCode.Trim(),
                    PinCode = a.PinCode.Trim(),
                    Country = a.Country.Trim(),
                    IsPrimary = a.IsPrimary,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        if (request.BankAccounts != null)
        {
            dbContext.SupplierBankAccounts.RemoveRange(supplier.BankAccounts);
            foreach (var b in request.BankAccounts)
            {
                var cleanAcc = b.AccountNumber.Trim();
                var encrypted = encryptionService.Encrypt(cleanAcc);
                var masked = encryptionService.Mask(cleanAcc);

                dbContext.SupplierBankAccounts.Add(new SupplierBankAccountRecord
                {
                    Id = Guid.CreateVersion7(),
                    SupplierId = supplier.Id,
                    BankName = b.BankName.Trim(),
                    BranchName = b.BranchName.Trim(),
                    AccountNumberEncrypted = encrypted,
                    AccountNumberMasked = masked,
                    IfscCode = b.IfscCode.Trim().ToUpperInvariant(),
                    AccountType = b.AccountType.Trim(),
                    IsPrimary = b.IsPrimary,
                    IsVerified = false,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    CreatedByUserId = userId
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetSupplierByIdAsync(companyId, supplierId, cancellationToken);
    }

    public async Task<UnmaskedBankAccountDto> GetUnmaskedBankAccountAsync(Guid companyId, Guid userId, Guid supplierId, Guid bankAccountId, CancellationToken cancellationToken = default)
    {
        var acc = await dbContext.SupplierBankAccounts
            .Include(b => b.Supplier)
            .FirstOrDefaultAsync(b => b.Supplier != null && b.Supplier.CompanyId == companyId && b.SupplierId == supplierId && b.Id == bankAccountId, cancellationToken)
            ?? throw CommercialErrors.NotFound("Bank account", bankAccountId);

        var plaintext = encryptionService.Decrypt(acc.AccountNumberEncrypted);

        await auditService.LogEventAsync(
            companyId,
            "SUPPLIER_BANK_UNMASKED",
            userId: userId,
            detailsJson: $"{{\"supplierCode\":\"{acc.Supplier!.SupplierCode}\",\"bankName\":\"{acc.BankName}\",\"masked\":\"{acc.AccountNumberMasked}\"}}",
            cancellationToken: cancellationToken);

        return new UnmaskedBankAccountDto
        {
            BankAccountId = acc.Id,
            BankName = acc.BankName,
            BranchName = acc.BranchName,
            PlaintextAccountNumber = plaintext,
            IfscCode = acc.IfscCode,
            AccountType = acc.AccountType
        };
    }
}
