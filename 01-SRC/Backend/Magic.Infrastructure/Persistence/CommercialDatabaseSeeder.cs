using Magic.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Magic.Infrastructure.Persistence;

public sealed class CommercialDatabaseSeeder(
    MagicDbContext dbContext,
    ILogger<CommercialDatabaseSeeder> logger)
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SystemUserId = Guid.Parse("6df27c15-6264-45b4-b8b4-e8c7fafb1299"); // admin user

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // 1. Units of Measure
        var existingUom = await dbContext.UnitsOfMeasure
            .Where(u => u.CompanyId == DefaultCompanyId)
            .Select(u => u.Code)
            .ToHashSetAsync(cancellationToken);

        var uomsToSeed = new (string Code, string Name, int Precision)[]
        {
            ("PCS", "Pieces / Units", 0),
            ("KGS", "Kilograms", 3),
            ("GMS", "Grams", 0),
            ("LTR", "Litres", 3),
            ("ML", "Millilitres", 0),
            ("MTR", "Metres", 2),
            ("BOX", "Box / Carton", 0),
            ("SET", "Set / Combo", 0)
        };

        var newUoms = new List<UnitOfMeasureRecord>();
        foreach (var (code, name, precision) in uomsToSeed)
        {
            if (!existingUom.Contains(code))
            {
                newUoms.Add(new UnitOfMeasureRecord
                {
                    Id = Guid.CreateVersion7(),
                    CompanyId = DefaultCompanyId,
                    Code = code,
                    Name = name,
                    Precision = precision,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    CreatedByUserId = SystemUserId
                });
            }
        }

        if (newUoms.Count > 0)
        {
            dbContext.UnitsOfMeasure.AddRange(newUoms);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} units of measure.", newUoms.Count);
        }

        // 2. Tax Codes (Indian GST Slabs)
        var existingTaxes = await dbContext.TaxCodes
            .Where(t => t.CompanyId == DefaultCompanyId)
            .Select(t => t.Code)
            .ToHashSetAsync(cancellationToken);

        var taxesToSeed = new (string Code, string Desc, string Hsn, decimal Rate, decimal Cgst, decimal Sgst, decimal Igst, decimal Cess)[]
        {
            ("GST-0", "Exempt / Nil Rated (0%)", "0000", 0m, 0m, 0m, 0m, 0m),
            ("GST-5", "GST Standard Slab 5%", "1000", 5m, 2.5m, 2.5m, 5m, 0m),
            ("GST-12", "GST Standard Slab 12%", "2000", 12m, 6.0m, 6.0m, 12m, 0m),
            ("GST-18", "GST Standard Slab 18%", "3000", 18m, 9.0m, 9.0m, 18m, 0m),
            ("GST-28", "GST Luxury / De-merit Slab 28%", "4000", 28m, 14.0m, 14.0m, 28m, 0m)
        };

        var newTaxes = new List<TaxCodeRecord>();
        foreach (var (code, desc, hsn, rate, cgst, sgst, igst, cess) in taxesToSeed)
        {
            if (!existingTaxes.Contains(code))
            {
                newTaxes.Add(new TaxCodeRecord
                {
                    Id = Guid.CreateVersion7(),
                    CompanyId = DefaultCompanyId,
                    Code = code,
                    Description = desc,
                    HsnSacCode = hsn,
                    RatePercentage = rate,
                    CgstPercentage = cgst,
                    SgstPercentage = sgst,
                    IgstPercentage = igst,
                    CessPercentage = cess,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        if (newTaxes.Count > 0)
        {
            dbContext.TaxCodes.AddRange(newTaxes);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} statutory tax codes.", newTaxes.Count);
        }

        // 3. Default Department
        var dept = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.CompanyId == DefaultCompanyId && d.Code == "GEN", cancellationToken);

        if (dept == null)
        {
            dept = new DepartmentRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = DefaultCompanyId,
                Code = "GEN",
                Name = "General Merchandise",
                Description = "Default department for retail and commercial items",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = SystemUserId
            };
            dbContext.Departments.Add(dept);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 4. Default Categories
        var existingCatCodes = await dbContext.Categories
            .Where(c => c.CompanyId == DefaultCompanyId)
            .Select(c => c.Code)
            .ToHashSetAsync(cancellationToken);

        var catsToSeed = new (string Code, string Name, string Hsn, decimal GstRate, int Order)[]
        {
            ("GROC", "Groceries & Staples", "1001", 5m, 1),
            ("BEV", "Beverages & Dairy", "2202", 12m, 2),
            ("SNK", "Packaged Snacks & Confectionery", "2106", 18m, 3),
            ("CARE", "Personal Care & Hygiene", "3305", 18m, 4),
            ("STAT", "Office Stationery", "4820", 18m, 5)
        };

        var newCats = new List<CategoryRecord>();
        foreach (var (code, name, hsn, gst, order) in catsToSeed)
        {
            if (!existingCatCodes.Contains(code))
            {
                newCats.Add(new CategoryRecord
                {
                    Id = Guid.CreateVersion7(),
                    CompanyId = DefaultCompanyId,
                    Code = code,
                    Name = name,
                    Description = $"{name} category",
                    DepartmentId = dept.Id,
                    HsnCode = hsn,
                    DefaultGstRate = gst,
                    DisplayOrder = order,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    CreatedByUserId = SystemUserId
                });
            }
        }

        if (newCats.Count > 0)
        {
            dbContext.Categories.AddRange(newCats);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} categories.", newCats.Count);
        }

        // 5. Default Brand
        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(b => b.CompanyId == DefaultCompanyId && b.Code == "GEN", cancellationToken);

        if (brand == null)
        {
            brand = new BrandRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = DefaultCompanyId,
                Code = "GEN",
                Name = "General / Unbranded",
                Description = "Standard brand for generic or non-branded products",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = SystemUserId
            };
            dbContext.Brands.Add(brand);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 6. Default Customer Group
        var group = await dbContext.CustomerGroups
            .FirstOrDefaultAsync(g => g.CompanyId == DefaultCompanyId && g.Code == "RETAIL", cancellationToken);

        if (group == null)
        {
            group = new CustomerGroupRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = DefaultCompanyId,
                Code = "RETAIL",
                Name = "Standard Retail Customers",
                DefaultDiscountPercentage = 0m,
                DefaultCreditLimitPaise = 0,
                DefaultCreditDays = 0,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.CustomerGroups.Add(group);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 7. Seed Initial Demo Products (with exact Paise Money!)
        var pcsUom = await dbContext.UnitsOfMeasure.FirstAsync(u => u.CompanyId == DefaultCompanyId && u.Code == "PCS", cancellationToken);
        var grocCat = await dbContext.Categories.FirstAsync(c => c.CompanyId == DefaultCompanyId && c.Code == "GROC", cancellationToken);
        var bevCat = await dbContext.Categories.FirstAsync(c => c.CompanyId == DefaultCompanyId && c.Code == "BEV", cancellationToken);

        var prdExists = await dbContext.Products.AnyAsync(p => p.CompanyId == DefaultCompanyId && p.ProductCode == "PRD-001", cancellationToken);
        if (!prdExists)
        {
            var p1 = new ProductRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = DefaultCompanyId,
                ProductCode = "PRD-001",
                Name = "Premium Basmati Rice 1kg",
                Description = "Aged long grain aromatic basmati rice",
                ProductType = Contracts.Commercial.ProductType.Goods,
                CategoryId = grocCat.Id,
                BrandId = brand.Id,
                DepartmentId = dept.Id,
                UnitOfMeasureId = pcsUom.Id,
                HsnSacCode = "1006",
                GstRatePercentage = 5m,
                Sku = "GROC-RICE-001",
                PrimaryBarcode = "8901234567890",
                PurchasePricePaise = 12000, // ₹120.00
                CostPricePaise = 12500,     // ₹125.00
                SellingPricePaise = 15500,  // ₹155.00
                MrpPaise = 17500,           // ₹175.00
                MinSellingPricePaise = 14500,
                WholesalePricePaise = 14000,
                ReorderLevel = 25m,
                MinStockLevel = 10m,
                MaxStockLevel = 200m,
                HasVariants = false,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = SystemUserId
            };
            dbContext.Products.Add(p1);

            dbContext.ProductBarcodes.Add(new ProductBarcodeRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = DefaultCompanyId,
                ProductId = p1.Id,
                Barcode = "8901234567890",
                BarcodeType = "EAN13",
                IsPrimary = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            var p2 = new ProductRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = DefaultCompanyId,
                ProductCode = "PRD-002",
                Name = "Roasted Almond Milk 1L",
                Description = "Unsweetened plant-based roasted almond beverage",
                ProductType = Contracts.Commercial.ProductType.Goods,
                CategoryId = bevCat.Id,
                BrandId = brand.Id,
                DepartmentId = dept.Id,
                UnitOfMeasureId = pcsUom.Id,
                HsnSacCode = "2202",
                GstRatePercentage = 12m,
                Sku = "BEV-ALM-002",
                PrimaryBarcode = "8909876543210",
                PurchasePricePaise = 21000, // ₹210.00
                CostPricePaise = 21500,     // ₹215.00
                SellingPricePaise = 28000,  // ₹280.00
                MrpPaise = 31000,           // ₹310.00
                MinSellingPricePaise = 26000,
                WholesalePricePaise = 25000,
                ReorderLevel = 15m,
                MinStockLevel = 5m,
                MaxStockLevel = 100m,
                HasVariants = false,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = SystemUserId
            };
            dbContext.Products.Add(p2);

            dbContext.ProductBarcodes.Add(new ProductBarcodeRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = DefaultCompanyId,
                ProductId = p2.Id,
                Barcode = "8909876543210",
                BarcodeType = "EAN13",
                IsPrimary = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            // Initial customer
            var cust = new CustomerRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = DefaultCompanyId,
                CustomerCode = "CUST-001",
                Name = "Apex Retail Traders",
                CustomerType = Contracts.Commercial.CustomerType.Wholesale,
                GstType = Contracts.Commercial.GstRegistrationType.Registered,
                Gstin = "33AABCA1234A1Z5",
                Pan = "AABCA1234A",
                ContactPerson = "Ramanathan Pillai",
                Phone = "9876543210",
                Email = "ramanathan@apexretail.in",
                CustomerGroupId = group.Id,
                CreditLimitPaise = 5000000, // ₹50,000.00
                CreditPeriodDays = 30,
                IsCreditHold = false,
                IsActive = true,
                Notes = "Primary wholesale partner in Tamil Nadu",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = SystemUserId
            };
            dbContext.Customers.Add(cust);

            dbContext.CustomerAddresses.Add(new CustomerAddressRecord
            {
                Id = Guid.CreateVersion7(),
                CustomerId = cust.Id,
                AddressType = 3,
                AddressLine1 = "42 Anna Salai, Mount Road",
                City = "Chennai",
                District = "Chennai",
                State = "Tamil Nadu",
                StateCode = "33",
                PinCode = "600002",
                Country = "India",
                IsDefaultBilling = true,
                IsDefaultShipping = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            // Initial supplier
            var supp = new SupplierRecord
            {
                Id = Guid.CreateVersion7(),
                CompanyId = DefaultCompanyId,
                SupplierCode = "SUPP-001",
                Name = "South Agro Foods Ltd",
                Category = "Manufacturer",
                GstType = Contracts.Commercial.GstRegistrationType.Registered,
                Gstin = "29AABCS5678B1Z2",
                Pan = "AABCS5678B",
                ContactPerson = "Suresh Venkat",
                Phone = "9845012345",
                Email = "sales@southagro.com",
                PaymentTerms = "Net 30",
                CreditPeriodDays = 30,
                MsmeType = "Small",
                UdyamRegistrationNumber = "UDYAM-KR-03-0012345",
                IsMsmeRegistered = true,
                IsActive = true,
                Notes = "Authorized staple grains supplier",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = SystemUserId
            };
            dbContext.Suppliers.Add(supp);

            dbContext.SupplierAddresses.Add(new SupplierAddressRecord
            {
                Id = Guid.CreateVersion7(),
                SupplierId = supp.Id,
                AddressType = 1,
                AddressLine1 = "Plot 12, Industrial Area, Whitefield",
                City = "Bengaluru",
                State = "Karnataka",
                StateCode = "29",
                PinCode = "560066",
                Country = "India",
                IsPrimary = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded demo products PRD-001, PRD-002, customer CUST-001, supplier SUPP-001.");
        }
    }
}

