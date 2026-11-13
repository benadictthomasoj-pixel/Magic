import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BrandDto,
  CategoryDto,
  CreateProductRequest,
  ProductDetailDto,
  ProductSummaryDto,
  ProductType,
  TaxCodeDto,
  UnitOfMeasureDto,
  UpdateProductRequest,
} from '../../../core/contracts/commercial.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';

@Component({
  selector: 'magic-products-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './products-list.component.html',
  styleUrl: './products-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductsListComponent implements OnInit {
  private readonly api = inject(CommercialApiService);

  protected readonly products = signal<readonly ProductSummaryDto[]>([]);
  protected readonly categories = signal<readonly CategoryDto[]>([]);
  protected readonly brands = signal<readonly BrandDto[]>([]);
  protected readonly units = signal<readonly UnitOfMeasureDto[]>([]);
  protected readonly taxCodes = signal<readonly TaxCodeDto[]>([]);

  protected readonly selectedProduct = signal<ProductDetailDto | null>(null);
  protected readonly isCreateOpen = signal<boolean>(false);
  protected readonly isEditOpen = signal<boolean>(false);
  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  // Pagination & Filtering
  protected filterSearch = '';
  protected filterCategoryId = '';
  protected filterBrandId = '';
  protected filterActive: boolean | undefined = undefined;
  protected currentPage = 1;
  protected pageSize = 20;
  protected totalProducts = 0;

  // Barcode quick lookup widget
  protected quickBarcode = '';
  protected barcodeLookupResult = signal<string | null>(null);

  // Product Form Model
  protected formModel: {
    id?: string;
    productCode: string;
    name: string;
    description: string;
    productType: ProductType;
    categoryId: string;
    brandId: string;
    departmentId: string;
    unitOfMeasureId: string;
    hsnSacCode: string;
    taxCodeId: string;
    gstRatePercentage: number;
    sku: string;
    primaryBarcode: string;
    sellingPriceRupees: number;
    mrpRupees: number;
    purchasePriceRupees: number;
    costPriceRupees: number;
    minSellingPriceRupees: number;
    wholesalePriceRupees: number;
    reorderLevel: number;
    minStockLevel: number;
    maxStockLevel: number;
    isActive: boolean;
  } = this.getEmptyForm();

  ngOnInit(): void {
    this.loadMetadata();
    this.loadProducts();
  }

  protected loadMetadata(): void {
    this.api.getCategoriesTree().subscribe({
      next: (cats) => this.categories.set(cats),
      error: () => {},
    });
    this.api.getBrands().subscribe({
      next: (b) => this.brands.set(b),
      error: () => {},
    });
    this.api.getUnitsOfMeasure().subscribe({
      next: (u) => this.units.set(u),
      error: () => {},
    });
    this.api.getTaxCodes().subscribe({
      next: (t) => this.taxCodes.set(t),
      error: () => {},
    });
  }

  protected loadProducts(): void {
    this.loading.set(true);
    this.api
      .getProducts({
        search: this.filterSearch.trim() || undefined,
        categoryId: this.filterCategoryId || undefined,
        brandId: this.filterBrandId || undefined,
        isActive: this.filterActive,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.products.set(res.items);
          this.totalProducts = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to load products from database.',
          });
        },
      });
  }

  protected onSearchChange(): void {
    this.currentPage = 1;
    this.loadProducts();
  }

  protected viewProduct(p: ProductSummaryDto): void {
    this.api.getProductById(p.id).subscribe({
      next: (detail) => this.selectedProduct.set(detail),
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err.error?.detail || 'Failed to load product details.',
        });
      },
    });
  }

  protected openCreate(): void {
    this.formModel = this.getEmptyForm();
    if (this.categories().length > 0) {
      this.formModel.categoryId = this.categories()[0].id;
    }
    if (this.units().length > 0) {
      this.formModel.unitOfMeasureId = this.units()[0].id;
    }
    this.isCreateOpen.set(true);
    this.isEditOpen.set(false);
  }

  protected openEdit(detail: ProductDetailDto): void {
    this.formModel = {
      id: detail.id,
      productCode: detail.productCode,
      name: detail.name,
      description: detail.description || '',
      productType: detail.productType,
      categoryId: detail.categoryId,
      brandId: detail.brandId || '',
      departmentId: detail.departmentId || '',
      unitOfMeasureId: detail.unitOfMeasureId,
      hsnSacCode: detail.hsnSacCode,
      taxCodeId: detail.taxCodeId || '',
      gstRatePercentage: detail.gstRatePercentage,
      sku: detail.sku,
      primaryBarcode: detail.primaryBarcode || '',
      sellingPriceRupees: detail.sellingPricePaise / 100,
      mrpRupees: detail.mrpPaise / 100,
      purchasePriceRupees: detail.purchasePricePaise / 100,
      costPriceRupees: detail.costPricePaise / 100,
      minSellingPriceRupees: detail.minSellingPricePaise / 100,
      wholesalePriceRupees: detail.wholesalePricePaise / 100,
      reorderLevel: detail.reorderLevel,
      minStockLevel: detail.minStockLevel,
      maxStockLevel: detail.maxStockLevel,
      isActive: detail.isActive,
    };
    this.isEditOpen.set(true);
    this.isCreateOpen.set(false);
  }

  protected closeDrawers(): void {
    this.selectedProduct.set(null);
    this.isCreateOpen.set(false);
    this.isEditOpen.set(false);
  }

  protected saveProduct(): void {
    if (!this.formModel.name || !this.formModel.categoryId || !this.formModel.unitOfMeasureId) {
      this.actionMessage.set({
        type: 'error',
        text: 'Product name, category, and unit of measure are mandatory.',
      });
      return;
    }

    this.saving.set(true);
    this.actionMessage.set(null);

    const sellingPaise = Math.round(this.formModel.sellingPriceRupees * 100);
    const mrpPaise = Math.round(this.formModel.mrpRupees * 100);
    const purchasePaise = Math.round(this.formModel.purchasePriceRupees * 100);
    const costPaise = Math.round(this.formModel.costPriceRupees * 100);

    if (this.isEditOpen() && this.formModel.id) {
      const updateReq: UpdateProductRequest = {
        name: this.formModel.name.trim(),
        description: this.formModel.description.trim() || null,
        productType: this.formModel.productType,
        categoryId: this.formModel.categoryId,
        brandId: this.formModel.brandId || null,
        departmentId: this.formModel.departmentId || null,
        unitOfMeasureId: this.formModel.unitOfMeasureId,
        hsnSacCode: this.formModel.hsnSacCode.trim() || '9999',
        taxCodeId: this.formModel.taxCodeId || null,
        gstRatePercentage: this.formModel.gstRatePercentage,
        primaryBarcode: this.formModel.primaryBarcode.trim() || null,
        purchasePricePaise: purchasePaise,
        costPricePaise: costPaise,
        sellingPricePaise: sellingPaise,
        mrpPaise: mrpPaise,
        minSellingPricePaise: Math.round(this.formModel.minSellingPriceRupees * 100),
        wholesalePricePaise: Math.round(this.formModel.wholesalePriceRupees * 100),
        reorderLevel: this.formModel.reorderLevel,
        minStockLevel: this.formModel.minStockLevel,
        maxStockLevel: this.formModel.maxStockLevel,
        isActive: this.formModel.isActive,
      };

      this.api.updateProduct(this.formModel.id, updateReq).subscribe({
        next: (saved) => {
          this.saving.set(false);
          this.closeDrawers();
          this.actionMessage.set({
            type: 'ok',
            text: `Product "${saved.name}" (${saved.productCode}) updated successfully.`,
          });
          this.loadProducts();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to update product.',
          });
        },
      });
    } else {
      const createReq: CreateProductRequest = {
        productCode: this.formModel.productCode.trim() || `PRD-${Date.now().toString().slice(-4)}`,
        name: this.formModel.name.trim(),
        description: this.formModel.description.trim() || null,
        productType: this.formModel.productType,
        categoryId: this.formModel.categoryId,
        brandId: this.formModel.brandId || null,
        departmentId: this.formModel.departmentId || null,
        unitOfMeasureId: this.formModel.unitOfMeasureId,
        hsnSacCode: this.formModel.hsnSacCode.trim() || '9999',
        taxCodeId: this.formModel.taxCodeId || null,
        gstRatePercentage: this.formModel.gstRatePercentage,
        sku: this.formModel.sku.trim() || null,
        primaryBarcode: this.formModel.primaryBarcode.trim() || null,
        purchasePricePaise: purchasePaise,
        costPricePaise: costPaise,
        sellingPricePaise: sellingPaise,
        mrpPaise: mrpPaise,
        minSellingPricePaise: Math.round(this.formModel.minSellingPriceRupees * 100),
        wholesalePricePaise: Math.round(this.formModel.wholesalePriceRupees * 100),
        reorderLevel: this.formModel.reorderLevel,
        minStockLevel: this.formModel.minStockLevel,
        maxStockLevel: this.formModel.maxStockLevel,
      };

      this.api.createProduct(createReq).subscribe({
        next: (created) => {
          this.saving.set(false);
          this.closeDrawers();
          this.actionMessage.set({
            type: 'ok',
            text: `Product "${created.name}" (${created.productCode}) created successfully.`,
          });
          this.loadProducts();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to create product.',
          });
        },
      });
    }
  }

  protected deactivate(product: ProductSummaryDto): void {
    if (!confirm(`Are you sure you want to deactivate product "${product.name}" (${product.productCode})?`)) {
      return;
    }

    this.api.deactivateProduct(product.id).subscribe({
      next: () => {
        this.actionMessage.set({
          type: 'ok',
          text: `Product "${product.name}" has been deactivated.`,
        });
        this.loadProducts();
        if (this.selectedProduct()?.id === product.id) {
          this.selectedProduct.set(null);
        }
      },
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err.error?.detail || 'Failed to deactivate product.',
        });
      },
    });
  }

  protected testBarcodeLookup(): void {
    const code = this.quickBarcode.trim();
    if (!code) return;

    this.api.lookupBarcode(code).subscribe({
      next: (item) => {
        this.barcodeLookupResult.set(
          `Found: ${item.name} (${item.productCode}) · SKU: ${item.sku} · ₹${(item.sellingPricePaise / 100).toFixed(2)}`
        );
      },
      error: () => {
        this.barcodeLookupResult.set(`No product matched barcode "${code}".`);
      },
    });
  }

  private getEmptyForm() {
    return {
      productCode: '',
      name: '',
      description: '',
      productType: ProductType.Goods,
      categoryId: '',
      brandId: '',
      departmentId: '',
      unitOfMeasureId: '',
      hsnSacCode: '',
      taxCodeId: '',
      gstRatePercentage: 18,
      sku: '',
      primaryBarcode: '',
      sellingPriceRupees: 0,
      mrpRupees: 0,
      purchasePriceRupees: 0,
      costPriceRupees: 0,
      minSellingPriceRupees: 0,
      wholesalePriceRupees: 0,
      reorderLevel: 10,
      minStockLevel: 5,
      maxStockLevel: 100,
      isActive: true,
    };
  }
}
